using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Oscillators;

public class SquareOscillator : IOscillator
{
    public float Frequency { get; set; } = 440.0f;
    public float Phase { get; set; } = 0.0f;
    public float Amplitude { get; set; } = 1.0f;
    public float PulseWidth { get; set; } = 0.5f;
    
    private int _sampleRate = 48000;

    public void SetSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public float Process()
    {
        float t = Phase;
        float dt = Frequency / _sampleRate;
        
        // Square wave = Saw(t) - Saw(t + PulseWidth)
        float saw1 = (2.0f * t) - 1.0f;
        saw1 -= PolyBLEP(t, dt);
        
        float t2 = t + PulseWidth;
        if (t2 >= 1.0f) t2 -= 1.0f;
        
        float saw2 = (2.0f * t2) - 1.0f;
        saw2 -= PolyBLEP(t2, dt);
        
        float output = 0.5f * (saw1 - saw2); // Scale to -1..1
        
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
