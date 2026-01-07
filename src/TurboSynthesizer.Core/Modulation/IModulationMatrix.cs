using System.Collections.Generic;

namespace TurboSynthesizer.Core.Modulation;

public interface IModulationMatrix
{
    IList<ModulationRoute> Routes { get; }
    void AddRoute(ModulationSource source, ModulationDestination destination, double amount);
    void RemoveRoute(ModulationRoute route);
    double GetModulationValue(ModulationDestination destination);
    void UpdateSourceValue(ModulationSource source, double value);
    void Clear();
}
