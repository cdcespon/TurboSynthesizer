using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Filters;

public class StateVariableFilter : IFilter
{
    public float Cutoff { get; set; } = 20000.0f;
    public float Resonance { get; set; } = 0.0f; // 0.0 to 1.0 (approaching self-oscillation)
    public FilterType Type { get; set; } = FilterType.LowPass;

    private float _low;
    private float _high;
    private float _band;
    private float _notch;
    private int _sampleRate = 48000;

    public void SetSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
    }

    public float Process(float input)
    {
        // Simple Chamberlin SVF
        // F = 2 * sin(pi * cutoff / samplerate)
        // accuracy good for low freqs, stability issues at high freqs without oversampling.
        // Let's use the stable discretization if possible, or just clamp F.
        
        float cutoffFreq = Math.Clamp(Cutoff, 20.0f, _sampleRate / 2.0f - 100.0f);
        float f = 2.0f * MathF.Sin(MathF.PI * cutoffFreq / _sampleRate);
        
        // Q = 1 / (2 * damping). Res maps to damping.
        // Res 0 -> Damping max (no peak). Res 1 -> Damping min (high peak).
        // Let's map Res 0..1 to q 0.5 .. infinity?
        // q = 1 / damping. damping varies usually 2 to 0.
        float q = 1.0f - Resonance;
        // avoid explosion
        if (q < 0.01f) q = 0.01f; 

        // Run filter
        // low = low + f * band
        // high = input - low - q*band
        // band = band + f * high
        
        // Note: Tuning of Q/Damping factor varies by implementation.
        // Standard Chamberlin:
        // low += f * band;
        // high = input - low - q * band;
        // band += f * high;
        // notch = high + low;
        
        _low += f * _band;
        _high = input - _low - (q * _band); // q acts as damping here.
        _band += f * _high;
        _notch = _high + _low;

        // Clip states to prevent explosion if unstable
        if (_low > 10.0f) _low = 10.0f; else if (_low < -10.0f) _low = -10.0f;
        if (_band > 10.0f) _band = 10.0f; else if (_band < -10.0f) _band = -10.0f;

        switch (Type)
        {
            case FilterType.LowPass: return _low;
            case FilterType.HighPass: return _high;
            case FilterType.BandPass: return _band;
            case FilterType.Notch: return _notch;
            default: return _low;
        }
    }

    public void Reset()
    {
        _low = 0.0f;
        _high = 0.0f;
        _band = 0.0f;
        _notch = 0.0f;
    }
}
