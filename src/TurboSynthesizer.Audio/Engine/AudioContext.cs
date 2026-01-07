using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Engine;

public class AudioContext : IAudioDevice
{
    private readonly WasapiOut _wasapiOut;
    private readonly MixingSampleProvider _mixer;
    private readonly Synthesis.Effects.EffectsChain _effectsChain; // Wrapper
    private bool _isInitialized;

    public int SampleRate => 48000;
    public int Channels => 2;
    public bool IsRunning => _wasapiOut?.PlaybackState == PlaybackState.Playing;
    
    // Expose EffectsChain to Engine
    public Synthesis.Effects.EffectsChain Effects => _effectsChain;

    public AudioContext()
    {
        _wasapiOut = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 10); // 10ms latency
        
        // Define the mixer format: 48kHz, Stereo, IEEE Float
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);
        _mixer = new MixingSampleProvider(waveFormat);
        _mixer.ReadFully = true; // Keep mixer alive even when no inputs
        
        // Create Chain
        _effectsChain = new Synthesis.Effects.EffectsChain(_mixer);
        // Add Effects
        _effectsChain.AddEffect(new Synthesis.Effects.Delay());
        _effectsChain.AddEffect(new Synthesis.Effects.Reverb());
    }

    private readonly Dictionary<string, VisualizationSampleProvider> _visProviders = new();

    public void Initialize()
    {
        if (_isInitialized) return;

        // Default Master provider
        var masterVis = new VisualizationSampleProvider(_effectsChain);
        _visProviders["Master"] = masterVis;

        _wasapiOut.Init(masterVis); // Output now comes from VisProvider
        _isInitialized = true;
    }

    public void RegisterVisualizationSource(string name, NAudio.Wave.ISampleProvider provider)
    {
        _visProviders[name] = new VisualizationSampleProvider(provider);
    }

    public void GetVisualizationData(float[] buffer, string source = "Master")
    {
        if (_visProviders.TryGetValue(source, out var provider))
        {
            // If it's an internal source (not Master), it's not "pushed" by the audio device
            // so we must "pull" data manually here to update its internal buffer.
            if (source != "Master")
            {
                // We read a batch of samples to update the visualization buffer
                // 512 samples is usually enough for a smooth animation at UI frame rates
                float[] temp = new float[512];
                provider.Read(temp, 0, temp.Length);
            }
            
            provider.GetSamples(buffer);
        }
    }

    public void RegisterInternalSource(string name, Action<float[], int, int> tapAction)
    {
         _visProviders[name] = new VisualizationSampleProvider(new TapSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1), tapAction));
    }

    private class TapSampleProvider : NAudio.Wave.ISampleProvider
    {
        private readonly Action<float[], int, int> _tapAction;
        public NAudio.Wave.WaveFormat WaveFormat { get; }

        public TapSampleProvider(NAudio.Wave.WaveFormat format, Action<float[], int, int> tapAction)
        {
            WaveFormat = format;
            _tapAction = tapAction;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            _tapAction(buffer, offset, count);
            return count;
        }
    }

    public void Start()
    {
        if (!_isInitialized) Initialize();
        _wasapiOut.Play();
    }

    public void Stop()
    {
        _wasapiOut.Stop();
    }

    public void AddInput(NAudio.Wave.ISampleProvider input)
    {
        _mixer.AddMixerInput(input);
    }

    public void RemoveInput(NAudio.Wave.ISampleProvider input)
    {
        _mixer.RemoveMixerInput(input);
    }

    public void Dispose()
    {
        Stop();
        _wasapiOut?.Dispose();
    }
}
