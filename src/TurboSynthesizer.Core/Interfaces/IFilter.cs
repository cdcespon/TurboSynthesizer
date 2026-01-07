namespace TurboSynthesizer.Core.Interfaces;

public enum FilterType
{
    LowPass,
    HighPass,
    BandPass,
    Notch
}

public interface IFilter
{
    float Cutoff { get; set; }
    float Resonance { get; set; }
    FilterType Type { get; set; }
    
    float Process(float input);
    void Reset();
    void SetSampleRate(int sampleRate);
}
