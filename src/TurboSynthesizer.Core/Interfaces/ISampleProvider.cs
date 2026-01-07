namespace TurboSynthesizer.Core.Interfaces;

public interface ISampleProvider
{
    /// <summary>
    /// Gets the WaveFormat of this Sample Provider.
    /// </summary>
    // Note: We might need a custom WaveFormat object if we want to stay decoupled from NAudio in Core.
    // For now, let's keep it simple or assume standard float format.
    
    int Read(float[] buffer, int offset, int count);
}
