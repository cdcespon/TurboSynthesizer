using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Oscillators;

public class TriangleOscillator : IOscillator
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
        float output = 0.0f;
        float halfPhase = Phase * 2.0f;
        
        if (halfPhase < 1.0f)
        {
            output = -1.0f + (2.0f * halfPhase);
        }
        else
        {
            output = 3.0f - (2.0f * halfPhase);
        }
        
        Phase += Frequency / _sampleRate;
        if (Phase >= 1.0f) Phase -= 1.0f;
        
        return output * Amplitude;
    }

    public void Reset()
    {
        Phase = 0.0f;
    }
}
