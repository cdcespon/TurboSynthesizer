using NAudio.Wave;
using System.Collections.Generic;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Effects;

public class EffectsChain : NAudio.Wave.ISampleProvider
{
    private readonly NAudio.Wave.ISampleProvider _source;
    private readonly List<IEffect> _effects = new();

    public WaveFormat WaveFormat => _source.WaveFormat;

    public EffectsChain(NAudio.Wave.ISampleProvider source)
    {
        _source = source;
    }

    public void AddEffect(IEffect effect)
    {
        effect.SetSampleRate(WaveFormat.SampleRate);
        _effects.Add(effect);
    }
    
    public IEnumerable<IEffect> GetEffects() => _effects;

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        if (samplesRead > 0)
        {
            foreach (var effect in _effects)
            {
                if (effect.IsEnabled)
                {
                    effect.Process(buffer, offset, samplesRead, WaveFormat.Channels);
                }
            }
        }

        return samplesRead;
    }
}
