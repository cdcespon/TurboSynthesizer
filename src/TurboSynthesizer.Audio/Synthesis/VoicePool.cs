using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using TurboSynthesizer.Audio.Engine;
using TurboSynthesizer.Core.Modulation;

namespace TurboSynthesizer.Audio.Synthesis;

public class VoicePool
{
    private const int MaxVoices = 16;
    private readonly List<SynthVoice> _voices;
    private readonly AudioContext _audioContext;

    public VoicePool(AudioContext audioContext)
    {
        _audioContext = audioContext;
        _voices = new List<SynthVoice>(MaxVoices);
        
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(audioContext.SampleRate, audioContext.Channels);

        for (int i = 0; i < MaxVoices; i++)
        {
            var voice = new SynthVoice(waveFormat);
            _voices.Add(voice);
            _audioContext.AddInput(voice);
        }

        // Register visual sources that tap into the latest active voice
        _audioContext.RegisterInternalSource("Osc1", (buf, offset, count) => {
            var v = _voices.FirstOrDefault(x => x.IsActive) ?? _voices[0];
            v.GetTappedSamples(1, buf, offset, count);
        });

        _audioContext.RegisterInternalSource("Osc2", (buf, offset, count) => {
            var v = _voices.FirstOrDefault(x => x.IsActive) ?? _voices[0];
            v.GetTappedSamples(2, buf, offset, count);
        });
    }

    public SynthVoice GetVoice(int note)
    {
        // 1. Check for retrigger of same note
        // If a voice is already playing this note, re-use it (monophonic behavior per note key)
        var existing = _voices.FirstOrDefault(v => v.Note == note && v.IsActive);
        if (existing != null) return existing;

        // 2. Find free voice
        var voice = _voices.FirstOrDefault(v => !v.IsActive);
        if (voice != null) return voice;

        // 3. Steal voice (oldest/first)
        return _voices.First();
    }
    
    public void NoteOn(int note, int velocity)
    {
        var voice = GetVoice(note);
        voice.NoteOn(note, velocity);
    }
    
    public void NoteOff(int note)
    {
        foreach (var voice in _voices.Where(v => v.Note == note && v.IsActive))
        {
            voice.NoteOff();
        }
    }

    public void ForEachVoice(Action<SynthVoice> action)
    {
        foreach (var voice in _voices)
        {
            action(voice);
        }
    }

    public void SetModulationRoutes(IList<ModulationRoute> routes)
    {
        foreach (var voice in _voices)
        {
            voice.ModulationRoutes = routes;
        }
    }

    public SynthVoice GetActiveVoice()
    {
        return _voices.FirstOrDefault(x => x.IsActive) ?? _voices[0];
    }
}
