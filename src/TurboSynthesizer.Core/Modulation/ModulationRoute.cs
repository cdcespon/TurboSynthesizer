namespace TurboSynthesizer.Core.Modulation;

public class ModulationRoute
{
    public ModulationSource Source { get; set; }
    public ModulationDestination Destination { get; set; }
    
    // Amount range: -1.0 to 1.0
    public double Amount { get; set; }
    
    public bool IsActive => Source != ModulationSource.None && Destination != ModulationDestination.None && Amount != 0;
}
