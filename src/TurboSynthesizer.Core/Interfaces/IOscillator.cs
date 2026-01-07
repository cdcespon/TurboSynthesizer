namespace TurboSynthesizer.Core.Interfaces;

public interface IOscillator
{
    float Frequency { get; set; }
    float Phase { get; set; }
    float Amplitude { get; set; }
    float Process();
    void Reset();
    void SetSampleRate(int sampleRate);
}
