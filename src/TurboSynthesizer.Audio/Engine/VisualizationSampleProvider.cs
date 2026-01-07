using NAudio.Wave;
using System;

namespace TurboSynthesizer.Audio.Engine;

public class VisualizationSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _buffer;
    private readonly object _lock = new();

    public WaveFormat WaveFormat => _source.WaveFormat;
    public int BufferLength => _buffer.Length;

    public VisualizationSampleProvider(ISampleProvider source, int bufferSize = 2048)
    {
        _source = source;
        _buffer = new float[bufferSize];
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);
        int channels = _source.WaveFormat.Channels;

        lock (_lock)
        {
            if (channels == 1)
            {
                if (samplesRead >= _buffer.Length)
                {
                    Array.Copy(buffer, offset + samplesRead - _buffer.Length, _buffer, 0, _buffer.Length);
                }
                else
                {
                    Array.Copy(_buffer, samplesRead, _buffer, 0, _buffer.Length - samplesRead);
                    Array.Copy(buffer, offset, _buffer, _buffer.Length - samplesRead, samplesRead);
                }
            }
            else if (channels > 1)
            {
                // Extract only the first channel (Left) for visualization
                int framesRead = samplesRead / channels;
                int framesToCopy = Math.Min(framesRead, _buffer.Length);

                // Shift existing
                if (framesToCopy < _buffer.Length)
                {
                    Array.Copy(_buffer, framesToCopy, _buffer, 0, _buffer.Length - framesToCopy);
                }

                // Copy new frames (one channel only)
                int startFrame = framesRead - framesToCopy;
                int writeOffset = _buffer.Length - framesToCopy;
                
                for (int i = 0; i < framesToCopy; i++)
                {
                    _buffer[writeOffset + i] = buffer[offset + (startFrame + i) * channels];
                }
            }
        }

        return samplesRead;
    }

    public void GetSamples(float[] output)
    {
        lock (_lock)
        {
            int len = Math.Min(_buffer.Length, output.Length);
            Array.Copy(_buffer, 0, output, 0, len);
        }
    }
}
