using FluentAssertions;
using Xunit;
using TurboSynthesizer.Audio.Synthesis.Effects;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Tests;

public class EffectTests
{
    [Fact]
    public void Delay_Should_DelaySignal()
    {
        // Arrange
        var delay = new Delay();
        delay.SetSampleRate(100); // Low SR for easy counting
        delay.Time = 0.05f; // 5 samples
        delay.Mix = 0.5f;
        delay.Feedback = 0.0f;
        delay.IsEnabled = true;
        delay.Reset();

        var buffer = new float[10]; 
        // 1 sample impulse
        buffer[0] = 1.0f; 
        
        // Act
        delay.Process(buffer, 0, 10, 1);

        // Assert
        // Output[0] = In[0]*(1-Mix) + Delayed[0]*Mix = 1*0.5 + 0 = 0.5
        buffer[0].Should().Be(0.5f);
        
        // Delay is 5 samples. So Input at 0 should appear at 5.
        // Output[5] = In[5]*(0.5) + Delayed[5]*0.5
        // Delayed[5] comes from buffer[0].
        // In[5] is 0.
        // So Output[5] = 0.5 * 1.0 = 0.5.
        
        buffer[5].Should().Be(0.5f);
    }
    
    [Fact]
    public void Reverb_Should_ProcessWithoutCrashing()
    {
        var reverb = new Reverb();
        reverb.SetSampleRate(48000);
        reverb.IsEnabled = true;
        reverb.Mix = 0.5f;
        
        
        float[] buffer = new float[2500];
        buffer[0] = 1.0f; // Impulse
        
        // Just verify it runs and changes something
        reverb.Process(buffer, 0, 2500, 1);
        
        // Input 1.0. Wet 0.0 (initial). Mix 0.5. Result should be 0.5.
        // If it returns 1.0, Mix is ignored.
        buffer[0].Should().Be(0.5f, "Mix is 0.5 and Wet is 0 (initial delay)");
        
        // Check for any signal in the tail (sparse reflections)
        // Skip initial dry/early part
        bool hasReflections = false;
        for(int i = 1000; i < buffer.Length; i++)
        {
            if (Math.Abs(buffer[i]) > 0.0001f)
            {
                hasReflections = true;
                break;
            }
        }
        
        hasReflections.Should().BeTrue("Reverb tail should contain reflections");
    }
}
