using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Effects;

public class Reverb : IEffect
{
    public bool IsEnabled { get; set; } = false;
    public float Mix { get; set; } = 0.0f; // 0..1
    public float RoomSize { get; set; } = 0.8f; // Feedback amount basically
    public float Damping { get; set; } = 0.5f;

    // Schroeder Reverb: 4 Comb Filters in parallel -> 2 Allpass Filters in series
    // Simplification for this Phase: 2 Comb, 1 Allpass or just a simple feedback loop if CPU constrained.
    // Let's implemented 4 parallel combs + 2 series allpass.
    
    // Helper Class for Comb
    class Comb
    {
        private float[] _buffer;
        private int _index;
        public float Feedback;
        public float Damp;
        private float _filterState;

        public Comb(int size)
        {
            _buffer = new float[size];
        }

        public float Process(float input)
        {
            float output = _buffer[_index];
            
            _filterState = (output * (1 - Damp)) + (_filterState * Damp);
            
            float toBuf = input + (_filterState * Feedback);
            _buffer[_index] = toBuf;

            _index++;
            if (_index >= _buffer.Length) _index = 0;

            return output;
        }
    }
    
    // Helper for Allpass
    class Allpass
    {
        private float[] _buffer;
        private int _index;
        public float Feedback = 0.5f;

        public Allpass(int size)
        {
            _buffer = new float[size];
        }

        public float Process(float input)
        {
            float bufOut = _buffer[_index];
            
            float toBuf = input + (bufOut * Feedback);
            
            float output = -input + (toBuf * (1+Feedback)); // Standard? No, Allpass: y[n] = -g*x[n] + x[n-D] + g*y[n-D]
            // Simpler: out = bufOut - input; newBuf = input + bufOut * 0.5
            // Standard Schroeder: output = -input + bufOut; buffer = input + (bufOut * g) ?? 
            // Using: output = buffer - g * input; buffer = input + g * output -- Incorrect?
            
            // Canonical:
            // delayed = buffer[index]
            // output = -input + delayed
            // buffer[index] = input + (delayed * 0.5)
            
            output = -input + bufOut;
            _buffer[_index] = input + (bufOut * Feedback);

            _index++;
            if (_index >= _buffer.Length) _index = 0;
            return output;
        }
    }

    private Comb[] _combs;
    private Allpass[] _allpasses;
    private int _sampleRate = 48000;

    public Reverb()
    {
        SetSampleRate(48000);
    }
    
    public void SetSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
        // Tuning values for 44.1kHz usually, scaled for 48k
        float scale = sampleRate / 44100.0f;
        
        _combs = new Comb[4];
        _combs[0] = new Comb((int)(1116 * scale));
        _combs[1] = new Comb((int)(1188 * scale));
        _combs[2] = new Comb((int)(1277 * scale));
        _combs[3] = new Comb((int)(1356 * scale));
        
        _allpasses = new Allpass[2];
        _allpasses[0] = new Allpass((int)(225 * scale));
        _allpasses[1] = new Allpass((int)(556 * scale));
    }

    public void Reset()
    {
        // Re-init buffers implicitly clears them or add loop
    }

    public void Process(float[] buffer, int offset, int count, int channels)
    {
        // Mono-in, Mono-out processing mixed to stereo buffer?
        // Basic algo is mono. We'll run it on sum of L+R (or just L if lazy) and add to output.
        // Better: Process each channel? Reverb usually merges.
        // Let's do: Input = (L+R)*0.5. Reverb -> Wet. L = L + Wet*Mix. R = R + Wet*Mix.
        
        for (int i = 0; i < count; i += channels)
        {
            // Sync parameters
            foreach(var c in _combs) 
            {
                c.Feedback = RoomSize;
                c.Damp = Damping;
            }

            // Input: Mix down to mono
            float input = buffer[offset + i];
            if (channels > 1) input = (input + buffer[offset + i + 1]) * 0.5f;

            // Parallel Combs
            float combsOut = 0.0f;
            foreach(var c in _combs) combsOut += c.Process(input);

            // Series Allpasses
            float output = _allpasses[0].Process(combsOut);
            output = _allpasses[1].Process(output);
            
            // Mix Wet/Dry
            // buffer contains Dry.
            // We want Out = Dry*(1-Mix) + Wet*Mix ??
            // Usually Reverb is an ADD effect on mixer sends, but here insert.
            
            float wet = output * 0.2f; // Scale down a bit, Schroeder can be loud

            for (int c = 0; c < channels; c++)
            {
                buffer[offset + i + c] = (buffer[offset + i + c] * (1.0f - Mix)) + (wet * Mix);
            }
        }
    }
}
