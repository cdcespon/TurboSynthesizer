using System;
using System.Collections.Generic;
using NAudio.Midi;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Midi;

public class MidiService : IMidiService, IDisposable
{
    private MidiIn? _midiIn;
    
    public event Action<int, int>? NoteOn;
    public event Action<int>? NoteOff;
    public event Action<int, int>? ControlChange;

    public IList<string> GetInputDevices()
    {
        var devices = new List<string>();
        try 
        {
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                devices.Add(MidiIn.DeviceInfo(i).ProductName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enumerating MIDI devices: {ex.Message}");
        }
        return devices;
    }

    public void Start(int deviceIndex)
    {
        Stop(); // Stop existing if any

        try
        {
            if (deviceIndex >= 0 && deviceIndex < MidiIn.NumberOfDevices)
            {
                _midiIn = new MidiIn(deviceIndex);
                _midiIn.MessageReceived += OnMessageReceived;
                _midiIn.Start();
                Console.WriteLine($"MIDI Device {deviceIndex} started.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting MIDI device {deviceIndex}: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_midiIn != null)
        {
            try
            {
                _midiIn.Stop();
                _midiIn.MessageReceived -= OnMessageReceived;
                _midiIn.Dispose();
            }
            catch(Exception ex)
            {
                 Console.WriteLine($"Error stopping MIDI device: {ex.Message}");
            }
            _midiIn = null;
        }
    }

    private void OnMessageReceived(object? sender, MidiInMessageEventArgs e)
    {
        // Handle events
        if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn)
        {
            var noteOn = (NoteOnEvent)e.MidiEvent;
            if (noteOn.Velocity > 0)
                NoteOn?.Invoke(noteOn.NoteNumber, noteOn.Velocity);
            else
                NoteOff?.Invoke(noteOn.NoteNumber); // Some devices send NoteOn Vel 0 as NoteOff
        }
        else if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOff)
        {
             var noteOff = (NoteEvent)e.MidiEvent;
             NoteOff?.Invoke(noteOff.NoteNumber);
        }
        else if (e.MidiEvent.CommandCode == MidiCommandCode.ControlChange)
        {
             var cc = (ControlChangeEvent)e.MidiEvent;
             ControlChange?.Invoke((int)cc.Controller, cc.ControllerValue);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
