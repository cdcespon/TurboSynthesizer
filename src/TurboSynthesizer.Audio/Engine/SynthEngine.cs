using System;
using System.Linq;
using TurboSynthesizer.Audio.Modulation;
using TurboSynthesizer.Audio.Synthesis;
using TurboSynthesizer.Core.Interfaces;
using TurboSynthesizer.Core.Modulation;

namespace TurboSynthesizer.Audio.Engine;

public class SynthEngine
{
    private readonly AudioContext _audioContext;
    private readonly VoicePool _voicePool;

    public SynthEngine()
    {
        _audioContext = new AudioContext();
        _voicePool = new VoicePool(_audioContext);
        
        ModulationMatrix = new ModulationMatrix();
        _voicePool.SetModulationRoutes(ModulationMatrix.Routes);
        
        MidiService = new Midi.MidiService();
        MidiService.NoteOn += NoteOn;
        MidiService.NoteOff += NoteOff;
        MidiService.ControlChange += OnMidiControlChange;
    }

    public ModulationMatrix ModulationMatrix { get; } = null!;
    public IMidiService MidiService { get; } = null!;

    private void OnMidiControlChange(int controller, int value)
    {
        // Map common CCs
        // CC 1 = ModWheel
        if(controller == 1)
        {
             ModulationMatrix.UpdateSourceValue(Core.Modulation.ModulationSource.ModWheel, value / 127.0f);
        }
        else 
        {
             // Maybe generic CC handling?
             // ModulationMatrix.UpdateSourceValue(ModulationSource.MidiCC, ... ); 
             // We need a way to map specific CCs? Phase 6 plan says "MidiCC" as generic? 
             // Let's assume generic CC mapping is future work or needs detailed implementation.
             // For now, only ModWheel.
        }
    }

    public void Start() => _audioContext.Start();
    public void Stop() => _audioContext.Stop();

    public void NoteOn(int note, int velocity) => _voicePool.NoteOn(note, velocity);
    public void NoteOff(int note) => _voicePool.NoteOff(note);

    // --- Parameter Controls ---
    // In a real synth, these would target specific voices or a global "Patch" object that voices reference.
    // Here we will iterate all voices in the pool (active or inactive) to update their settings.
    // Ideally VoicePool would expose an UpdateAll method.
    // Since VoicePool._voices is private, we depend on VoicePool to expose something or we make it internal.
    // For this phase, let's assume we can't access list directly easily without changing VoicePool.
    // I will add methods to VoicePool to set parameters, then call them here.
    // BUT I am editing SynthEngine now.
    
    // Let's modify VoicePool first? Or make SynthEngine access VoicePool better.
    // I'll add the logic to SynthEngine assuming VoicePool exposes 'Voices' or similar, 
    // OR likely I should add the iteration logic to VoicePool and call it from here.
    // That's cleaner.
    
    // But I will overwrite SynthEngine now. I'll add the methods and call `_voicePool.SetParam...` 
    // expecting to update VoicePool next.
    
    public void SetFilterCutoff(float cutoff)
    {
        _voicePool.ForEachVoice(v => v.BaseCutoff = cutoff);
    }
    
    public void SetFilterResonance(float resonance)
    {
        _voicePool.ForEachVoice(v => v.BaseResonance = resonance);
    }

    public void SetFilterEnvelopeAmount(float amount)
    {
        _voicePool.ForEachVoice(v => v.FilterEnvAmount = amount);
    }
    
    public void SetAmpEnvelope(float a, float d, float s, float r)
    {
        _voicePool.ForEachVoice(v => {
            v.AmpEnvelope.Attack = a;
            v.AmpEnvelope.Decay = d;
            v.AmpEnvelope.Sustain = s;
            v.AmpEnvelope.Release = r;
            v.AmpEnvelope.UpdateCoefficients();
        });
    }

    public void SetOscillatorWaveform(int oscIndex, string waveform)
    {
        // Requires factory or switching logic in SynthVoice.
        // For now not implementing dynamic waveform switching extensively
        // but ready for Phase 3 requirements.
    }

    public Core.Models.SynthParameters GetParameters()
    {
        // Capture current state from the first voice (assuming unison patch)
        var voice = _voicePool.GetVoice(0); // Hacky, ideally stored in a Patch object
        // Since GetVoice(note) expects a note, we need a way to just peek a voice.
        // Let's assume default values or expose a "TemplateVoice" in VoicePool?
        // Or just return default for now since we don't have 2-way binding from Voice to UI yet.
        
        return new Core.Models.SynthParameters
        {
            // In a real app, we'd read back from the engine.
            // For this phase, we rely on the ViewModel to maintain state 
            // and push it TO the engine, so maybe GetParameters comes from VM?
            // Actually, for saving "what you hear", reading from engine is best.
            // But engine state is distributed across voices.
            // Let's assume MainViewModel holds the "Source of Truth" for the Patch.
        };
    }

    public void SetParameters(Core.Models.SynthParameters p)
    {
        SetFilterCutoff(p.FilterCutoff);
        SetFilterResonance(p.FilterResonance);
        SetFilterEnvelopeAmount(p.FilterEnvAmount);
        
        // Add LFO/Env settings here
        _voicePool.ForEachVoice(v => {
            v.BaseCutoff = p.FilterCutoff;
            v.BaseResonance = p.FilterResonance;
            v.FilterEnvAmount = p.FilterEnvAmount;
            v.LfoFilterAmount = p.FilterLfoAmount;
            
            v.BaseLfo1Rate = p.LfoFrequency;
            
            v.AmpEnvelope.Attack = p.AmpAttack;
            v.AmpEnvelope.Decay = p.AmpDecay;
            v.AmpEnvelope.Sustain = p.AmpSustain;
            v.AmpEnvelope.Release = p.AmpRelease;
            
            v.FilterEnvelope.Attack = p.FilterAttack;
            v.FilterEnvelope.Decay = p.FilterDecay;
            v.FilterEnvelope.Sustain = p.FilterSustain;
            v.FilterEnvelope.Release = p.FilterRelease;
            
            v.SetOsc1Waveform(p.Osc1Waveform);
            v.SetOsc2Waveform(p.Osc2Waveform);

            v.MixOsc1 = p.MixOsc1;
            v.MixOsc2 = p.MixOsc2;
        });

        // Update Effects
        var chain = _audioContext.Effects;
        foreach(var effect in chain.GetEffects())
        {
            if (effect is Synthesis.Effects.Delay delay)
            {
                delay.IsEnabled = p.DelayMix > 0;
                delay.Time = p.DelayTime;
                delay.Feedback = p.DelayFeedback;
                delay.Mix = p.DelayMix;
            }
            else if (effect is Synthesis.Effects.Reverb reverb)
            {
                reverb.IsEnabled = p.ReverbMix > 0;
                reverb.RoomSize = p.ReverbRoomSize;
                reverb.Damping = p.ReverbDamping;
                reverb.Mix = p.ReverbMix;
            }
        }
    }

    public void GetScopeData(float[] buffer, string source = "Master")
    {
        _audioContext.GetVisualizationData(buffer, source);
    }

    public (string ampStage, float ampProgress, string filtStage, float filtProgress) GetEnvelopeStatus()
    {
        var v = _voicePool.GetActiveVoice();
        return (
            v.AmpEnvelope.Stage.ToString(), v.AmpEnvelope.StageProgress,
            v.FilterEnvelope.Stage.ToString(), v.FilterEnvelope.StageProgress
        );
    }
}
