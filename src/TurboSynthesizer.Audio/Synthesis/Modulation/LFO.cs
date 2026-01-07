using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Modulation;

public class LFO : IModulator
{
    public float Frequency { get; set; } = 1.0f; // Hz
    public float Amplitude { get; set; } = 1.0f;
    // We could reuse IOscillator, but LFOs usually simpler and don't need anti-aliasing.
    // Also "Amount" is often handled at destination or matrix.
    
    private float _phase;
    private int _sampleRate = 48000;

    public void SetSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public float Process()
    {
        // Simple Sine LFO
        float output = Amplitude * MathF.Sin(2 * MathF.PI * _phase);
        
        _phase += Frequency / _sampleRate;
        if (_phase >= 1.0f) _phase -= 1.0f;
        
        return output;
    }

    public void Reset()
    {
        _phase = 0.0f;
    }
}
