using System;

namespace TurboSynthesizer.Core.Interfaces;

public interface IAudioDevice : IDisposable
{
    int SampleRate { get; }
    int Channels { get; }
    bool IsRunning { get; }
    void Initialize();
    void Start();
    void Stop();
}
