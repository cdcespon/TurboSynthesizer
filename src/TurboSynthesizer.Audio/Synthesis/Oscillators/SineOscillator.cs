using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Oscillators;

public class SineOscillator : IOscillator
{
    public float Frequency { get; set; } = 440.0f;
    public float Phase { get; set; } = 0.0f;
    public float Amplitude { get; set; } = 1.0f;
    
    private int _sampleRate = 48000;

    public void SetSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public float Process()
    {
        float output = Amplitude * MathF.Sin(2 * MathF.PI * Phase);
        
        Phase += Frequency / _sampleRate;
        if (Phase >= 1.0f) Phase -= 1.0f;
        
        return output;
    }

    public void Reset()
    {
        Phase = 0.0f;
    }
}
