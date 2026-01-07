using System;

namespace TurboSynthesizer.Core.Models;

public class SynthParameters
{
    // Oscillators
    public string Osc1Waveform { get; set; } = "Sine";
    public string Osc2Waveform { get; set; } = "Saw";
    public float MixOsc1 { get; set; } = 0.5f;
    public float MixOsc2 { get; set; } = 0.5f;

    // Filter
    public float FilterCutoff { get; set; } = 5000.0f;
    public float FilterResonance { get; set; } = 0.0f;
    public float FilterEnvAmount { get; set; } = 0.0f;
    public float FilterLfoAmount { get; set; } = 0.0f;
    
    // Envelopes
    public float AmpAttack { get; set; } = 0.01f;
    public float AmpDecay { get; set; } = 0.1f;
    public float AmpSustain { get; set; } = 0.7f;
    public float AmpRelease { get; set; } = 0.2f;

    public float FilterAttack { get; set; } = 0.01f;
    public float FilterDecay { get; set; } = 0.2f;
    public float FilterSustain { get; set; } = 0.5f;
    public float FilterRelease { get; set; } = 0.2f;

    // LFO
    public float LfoFrequency { get; set; } = 1.0f;
    
    // Effects
    public float DelayTime { get; set; } = 0.4f;
    public float DelayFeedback { get; set; } = 0.3f;
    public float DelayMix { get; set; } = 0.0f;
    
    public float ReverbRoomSize { get; set; } = 0.8f;
    public float ReverbDamping { get; set; } = 0.5f;
    public float ReverbMix { get; set; } = 0.0f;
}
