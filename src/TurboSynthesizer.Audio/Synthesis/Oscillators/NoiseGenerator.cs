using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Oscillators;

public class NoiseGenerator : IOscillator
{
    private Random _random = new Random();
    
    public float Frequency { get; set; } // Not used for white noise
    public float Phase { get; set; }     // Not used for white noise
    public float Amplitude { get; set; } = 1.0f;
    
    private int _sampleRate = 48000;

    public void SetSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public float Process()
    {
        // White noise: random value between -1.0 to 1.0
        return ((float)_random.NextDouble() * 2.0f - 1.0f) * Amplitude;
    }

    public void Reset()
    {
        // No phase to reset
    }
}
