using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Oscillators;

public class SawOscillator : IOscillator
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
        float t = Phase;
        float output = (2.0f * t) - 1.0f;
        
        // PolyBLEP anti-aliasing
        float dt = Frequency / _sampleRate;
        output -= PolyBLEP(t, dt);
        
        Phase += dt;
        if (Phase >= 1.0f) Phase -= 1.0f;
        
        return output * Amplitude;
    }

    private float PolyBLEP(float t, float dt)
    {
        if (t < dt)
        {
            t /= dt;
            return t + t - t * t - 1.0f;
        }
        else if (t > 1.0f - dt)
        {
            t = (t - 1.0f) / dt;
            return t * t + t + t + 1.0f;
        }
        else
        {
            return 0.0f;
        }
    }

    public void Reset()
    {
        Phase = 0.0f;
    }
}
