using System;
using System.Collections.Generic;

namespace TurboSynthesizer.Core.Interfaces;

public interface IMidiService
{
    event Action<int, int> NoteOn;
    event Action<int> NoteOff;
    event Action<int, int> ControlChange;
    
    IList<string> GetInputDevices();
    void Start(int deviceIndex);
    void Stop();
}
