using FluentAssertions;
using Xunit;
using TurboSynthesizer.Audio.Synthesis.Filters;
using TurboSynthesizer.Audio.Synthesis.Modulation;
using TurboSynthesizer.Core.Interfaces;

namespace TurboSynthesizer.Audio.Tests;

public class FilterTests
{
    [Fact]
    public void SVF_LowPass_ShouldPassLowFreq()
    {
        var filter = new StateVariableFilter();
        filter.Cutoff = 1000.0f;
        filter.Type = FilterType.LowPass;
        
        // Feed DC
        float output = filter.Process(1.0f);
        // It takes time to settle, but initially it should start reacting.
        // For DC input 1.0, Lowpass should eventually reach 1.0.
        
        // Run loop
        for(int i=0; i<100; i++) output = filter.Process(1.0f);
        
        output.Should().BeGreaterThan(0.5f);
    }
}

public class LfoTests
{
    [Fact]
    public void LFO_ShouldOscillate()
    {
        var lfo = new LFO();
        lfo.Frequency = 100.0f; // Fast enough to see change in few samples
        lfo.SetSampleRate(1000); // Low SR
        
        float val1 = lfo.Process();
        float val2 = lfo.Process();
        float val3 = lfo.Process();
        
        // Should vary
        val1.Should().NotBe(val2);
    }
}
