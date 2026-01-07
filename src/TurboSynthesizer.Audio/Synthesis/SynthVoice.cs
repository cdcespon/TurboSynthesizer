using NAudioWave = NAudio.Wave;
using System;
using System.Collections.Generic;
using TurboSynthesizer.Audio.Synthesis.Envelopes;
using TurboSynthesizer.Audio.Synthesis.Filters;
using TurboSynthesizer.Audio.Synthesis.Modulation;
using TurboSynthesizer.Audio.Synthesis.Oscillators;
using TurboSynthesizer.Core.Interfaces;
using TurboSynthesizer.Core.Modulation;

namespace TurboSynthesizer.Audio.Synthesis;

public class SynthVoice : NAudioWave.ISampleProvider
{
    // Properties required by NAudio ISampleProvider
    public NAudioWave.WaveFormat WaveFormat { get; }

    // Oscillator 1
    public IOscillator Oscillator1 { get; set; }
    // Oscillator 2
    public IOscillator Oscillator2 { get; set; }
    public float MixOsc1 { get; set; } = 0.5f;
    public float MixOsc2 { get; set; } = 0.5f;

    // Filter
    public IFilter Filter { get; set; }
    public float FilterEnvAmount { get; set; } = 0.0f; // Range centered at 0 or unidirectional? usually +/-
    public float LfoFilterAmount { get; set; } = 0.0f;

    // Modulation
    public ADSREnvelope AmpEnvelope { get; private set; }
    public ADSREnvelope FilterEnvelope { get; private set; }
    public LFO Lfo1 { get; private set; }

    // State
    public int Note { get => _note; private set => _note = value; }
    public int Velocity { get => _velocity; private set => _velocity = value; }
    public bool IsActive => AmpEnvelope.Stage != ADSREnvelope.EnvelopeStage.Idle;

    private int _note;
    private int _velocity;

    // Tapping support
    private readonly float[] _tapBuffer1 = new float[1024];
    private readonly float[] _tapBuffer2 = new float[1024];
    private int _tapIdx;
    private readonly object _tapLock = new();

    public SynthVoice(NAudioWave.WaveFormat waveFormat)
    {
        WaveFormat = waveFormat;
        int sampleRate = waveFormat.SampleRate;

        // Initialize Components
        Oscillator1 = new SineOscillator();
        Oscillator1.SetSampleRate(sampleRate);

        Oscillator2 = new SawOscillator();
        Oscillator2.SetSampleRate(sampleRate);
        Oscillator2.Amplitude = 0.5f; 

        Filter = new StateVariableFilter();
        Filter.SetSampleRate(sampleRate);
        Filter.Cutoff = 5000.0f;

        AmpEnvelope = new ADSREnvelope();
        AmpEnvelope.SetSampleRate(sampleRate);
        
        FilterEnvelope = new ADSREnvelope();
        FilterEnvelope.SetSampleRate(sampleRate);
        FilterEnvelope.Sustain = 0.0f; 
        FilterEnvelope.Decay = 0.2f;

        Lfo1 = new LFO();
        Lfo1.SetSampleRate(sampleRate);
        Lfo1.Frequency = 3.0f;
    }

    public IList<ModulationRoute> ModulationRoutes { get; set; }
    
    // User Settings (Base values before modulation)
    public float BaseCutoff { get; set; } = 5000.0f;
    public float BaseResonance { get; set; } = 0.0f;
    public float BasePulseWidth { get; set; } = 0.5f;
    public float BaseLfo1Rate { get; set; } = 3.0f;

    public void NoteOn(int note, int velocity)
    {
        Note = note;
        Velocity = velocity;
        
        float frequency = 440.0f * MathF.Pow(2.0f, (note - 69.0f) / 12.0f);
        Oscillator1.Frequency = frequency;
        
        // Osc 2 Detune slightly? Or octave down?
        // Let's keep unison for now.
        Oscillator2.Frequency = frequency; 
        
        // Velocity processing
        float velAmplitude = velocity / 127.0f;
        // Apply velocity to Amp Env level? Or just a scalar.
        // For ADSR class, it outputs 0-1. We multiply result by this scalar.
        
        Oscillator1.Reset();
        Oscillator2.Reset();
        Filter.Reset();
        Lfo1.Reset();
        
        AmpEnvelope.NoteOn();
        FilterEnvelope.NoteOn();
        
        // Set initial values
        Filter.Cutoff = BaseCutoff;
        Filter.Resonance = BaseResonance;
        Lfo1.Frequency = BaseLfo1Rate;
    }

    public void NoteOff()
    {
        AmpEnvelope.NoteOff();
        FilterEnvelope.NoteOff();
    }

    public void SetOsc1Waveform(string waveform) => Oscillator1 = UpdateOscillator(Oscillator1, waveform);
    public void SetOsc2Waveform(string waveform) => Oscillator2 = UpdateOscillator(Oscillator2, waveform);

    private IOscillator UpdateOscillator(IOscillator current, string waveform)
    {
        string currentType = current.GetType().Name.Replace("Oscillator", "");
        if (currentType == "NoiseGenerator") currentType = "Noise";
        // Handle Saw/Sawtooth naming
        if (waveform == "Saw") waveform = "Sawtooth";
        if (currentType == "Sawtooth") currentType = "Saw";

        if (currentType.Equals(waveform, StringComparison.OrdinalIgnoreCase)) return current;

        IOscillator next = waveform.ToLower() switch
        {
            "sine" => new SineOscillator(),
            "saw" or "sawtooth" => new SawOscillator(),
            "square" => new SquareOscillator(),
            "triangle" => new TriangleOscillator(),
            "noise" => new NoiseGenerator(),
            _ => new SineOscillator()
        };
        
        next.SetSampleRate(WaveFormat.SampleRate);
        next.Frequency = current.Frequency;
        next.Amplitude = current.Amplitude;
        return next;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (!IsActive)
        {
             Array.Clear(buffer, offset, count);
             lock(_tapLock)
             {
                 Array.Clear(_tapBuffer1, 0, _tapBuffer1.Length);
                 Array.Clear(_tapBuffer2, 0, _tapBuffer2.Length);
             }
             return count;
        }

        int samplesRead = 0;
        bool isStereo = WaveFormat.Channels == 2;
        int increment = isStereo ? 2 : 1;

        for (int i = 0; i < count; i += increment)
        {
            if (!IsActive) 
            {
               buffer[offset + i] = 0;
               if (isStereo) buffer[offset + i + 1] = 0;
               samplesRead += increment;
               continue;
            }

            // 1. Internal Modulators (Per Sample)
            float ampEnv = AmpEnvelope.Process();
            float filtEnv = FilterEnvelope.Process();
            float lfoVal = Lfo1.Process();
            
            // 2. Resolve Modulation Matrix (Accumulate Offsets)
            // Initial Targets
            float targetCutoff = BaseCutoff;
            float targetPulseWidth = BasePulseWidth;
            float targetLfoRate = BaseLfo1Rate;
            float targetAmp = ampEnv; 

            // Hardcoded "Pre-wired" Modulations (Legacy/Fixed)
            // Filter Env -> Cutoff
            targetCutoff += (filtEnv * FilterEnvAmount * 5000.0f);
            // LFO1 -> Cutoff
            targetCutoff += (lfoVal * LfoFilterAmount * 1000.0f);

            // Matrix Modulations
            if (ModulationRoutes != null)
            {
                foreach(var route in ModulationRoutes)
                {
                    if(!route.IsActive) continue;
                    
                    float srcVal = 0f;
                    switch(route.Source)
                    {
                        case Core.Modulation.ModulationSource.LFO1: srcVal = lfoVal; break;
                        case Core.Modulation.ModulationSource.Envelope1: srcVal = ampEnv; break; // Assuming Env1 = Amp
                        case Core.Modulation.ModulationSource.Envelope2: srcVal = filtEnv; break; // Assuming Env2 = Filter
                        case Core.Modulation.ModulationSource.Velocity: srcVal = Velocity / 127.0f; break;
                        // For Global Mod Sources (ModWheel, CC), we need access to global state or they are pushed to voice?
                        // For now, skip global sources in local processing, OR assume they are passed/injected.
                        // We'll focus on LFO/Env internal routing first.
                    }

                    float amount = (float)route.Amount;
                    
                    switch(route.Destination)
                    {
                        case Core.Modulation.ModulationDestination.FilterCutoff:
                            targetCutoff += srcVal * amount * 5000.0f; // Scale appropriately
                            break;
                         case Core.Modulation.ModulationDestination.LFO1Rate:
                            targetLfoRate += srcVal * amount * 10.0f; 
                            break;
                         case Core.Modulation.ModulationDestination.PulseWidth:
                            targetPulseWidth += srcVal * amount;
                            break;
                         // Add more destinations
                    }
                }
            }

            // Apply Modulations
            Filter.Cutoff = Math.Clamp(targetCutoff, 20f, 20000f);
            Lfo1.Frequency = Math.Clamp(targetLfoRate, 0.1f, 50f);
            
            if (Oscillator1 is SquareOscillator sq1) sq1.PulseWidth = Math.Clamp(targetPulseWidth, 0.1f, 0.9f);
            // if (Oscillator2 is SquareOscillator sq2) sq2.PulseWidth = Math.Clamp(targetPulseWidth, 0.1f, 0.9f);

            // 3. Oscillators
            float osc1 = Oscillator1.Process();
            float osc2 = Oscillator2.Process();
            
            // Tap
            lock(_tapLock)
            {
                _tapBuffer1[_tapIdx] = osc1;
                _tapBuffer2[_tapIdx] = osc2;
                _tapIdx = (_tapIdx + 1) % _tapBuffer1.Length;
            }

            float rawMix = (osc1 * MixOsc1) + (osc2 * MixOsc2);

            // 4. Filter
            float filtered = Filter.Process(rawMix);

            // 5. Amp
            float output = filtered * targetAmp;

            // Clip
            if (output > 1.0f) output = 1.0f;
            else if (output < -1.0f) output = -1.0f;

            buffer[offset + i] = output;
            if (isStereo)
            {
                buffer[offset + i + 1] = output;
            }
            
            samplesRead += increment;
        }

        return samplesRead;
    }

    public void GetTappedSamples(int oscIndex, float[] buffer, int offset, int count)
    {
        lock(_tapLock)
        {
            float[] src = oscIndex == 1 ? _tapBuffer1 : _tapBuffer2;
            // Read backwards from current index to get latest samples
            for (int i = 0; i < count; i++)
            {
                int idx = (_tapIdx - 1 - i + src.Length) % src.Length;
                buffer[offset + count - 1 - i] = src[idx];
            }
        }
    }
}
