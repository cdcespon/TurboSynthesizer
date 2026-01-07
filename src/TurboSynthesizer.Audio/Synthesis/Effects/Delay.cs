using System;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Synthesis.Effects;

public class Delay : IEffect
{
    public bool IsEnabled { get; set; } = false;
    
    public float Time { get; set; } = 0.4f; // Seconds
    public float Feedback { get; set; } = 0.3f; // 0..1
    public float Mix { get; set; } = 0.3f; // 0..1

    private float[] _delayBuffer;
    private int _bufferIndex;
    private int _bufferLength;
    private int _sampleRate = 48000;
    private int _maxDelaySamples;

    public Delay()
    {
        SetSampleRate(48000);
    }

    public void SetSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
        // 2 seconds max delay
        _maxDelaySamples = sampleRate * 2; 
        _delayBuffer = new float[_maxDelaySamples * 2]; // *2 for safety/stereo interleaved if needed? 
        // Actually, for stereo interleaved handling properly, we need separate buffers or carefully managed interleaved buffer.
        // Let's implement Stereo Delay (interleaved in single buffer requires 2x jump).
        // For simplicity, let's treat buffer as Mono-summed or per-channel?
        // Let's do 2 separate buffers for L and R if simplest. 
        // Or single buffer size [MaxSamples * Channels].
        
        // Simple approach: One buffer, interleaved.
        // Index jumps by channels.
        _delayBuffer = new float[_maxDelaySamples * 2]; // Supports stereo up to 2 seconds
        _bufferLength = _delayBuffer.Length;
        _bufferIndex = 0;
    }

    public void Reset()
    {
        Array.Clear(_delayBuffer, 0, _delayBuffer.Length);
        _bufferIndex = 0;
    }

    public void Process(float[] buffer, int offset, int count, int channels)
    {
        // Simple Check: if mono buffer come in but we expected stereo or vice versa?
        // Assuming fixed channel count from SetSampleRate context usually, but Interface passes channels.
        
        int delaySamples = (int)(Time * _sampleRate);
        if (delaySamples > _maxDelaySamples) delaySamples = _maxDelaySamples;
        if (delaySamples < 1) delaySamples = 1;

        int delayOffset = delaySamples * channels; // interleaved offset

        for (int i = 0; i < count; i++)
        {
            // Read from circular buffer
            // Playback pos is current index - delayOffset
            int readIndex = _bufferIndex - delayOffset;
            if (readIndex < 0) readIndex += _bufferLength;
            
            float inputSample = buffer[offset + i];
            float delayedSample = _delayBuffer[readIndex];
            
            // Mix
            float outputSample = (inputSample * (1.0f - Mix)) + (delayedSample * Mix);
            
            // Write to circular buffer (Input + Feedback)
            float writeSample = inputSample + (delayedSample * Feedback);
            // Soft clip in feedback loop
            if (writeSample > 1.0f) writeSample = 1.0f; else if (writeSample < -1.0f) writeSample = -1.0f;
            
            _delayBuffer[_bufferIndex] = writeSample;
            
            // Output
            buffer[offset + i] = outputSample;

            // Advance
            _bufferIndex++;
            if (_bufferIndex >= _bufferLength) _bufferIndex = 0;
            
            // Note: This logic works for interleaved IF delaySamples is identical for all channels 
            // AND i iterates sequentially (L, R, L, R).
            // L writes to L slot in delay buffer. R writes to R slot.
            // Because bufferIndex++ happens every sample (i.e. every channel sample), 
            // and readIndex is - (delay*channels), we stay in phase.
        }
    }
}
