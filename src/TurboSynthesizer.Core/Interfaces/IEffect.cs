namespace TurboSynthesizer.Core.Interfaces;

public interface IEffect
{
    bool IsEnabled { get; set; }
    void Process(float[] buffer, int offset, int count, int channels);
    void SetSampleRate(int sampleRate);
    void Reset();
}
