namespace TurboSynthesizer.Core.Interfaces;

public interface IModulator
{
    float Process();
    void Reset();
    void SetSampleRate(int sampleRate);
}
