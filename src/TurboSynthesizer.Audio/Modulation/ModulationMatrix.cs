using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TurboSynthesizer.Core.Modulation;

namespace TurboSynthesizer.Audio.Modulation;

public class ModulationMatrix : IModulationMatrix
{
    private readonly List<ModulationRoute> _routes = new();
    private readonly double[] _sourceValues;
    private readonly object _lock = new();

    public IList<ModulationRoute> Routes => _routes;

    public ModulationMatrix()
    {
        // Initialize source values array based on Enum count
        int maxSource = Enum.GetValues(typeof(ModulationSource)).Cast<int>().Max();
        _sourceValues = new double[maxSource + 1];
    }

    public void AddRoute(ModulationSource source, ModulationDestination destination, double amount)
    {
        lock (_lock)
        {
            _routes.Add(new ModulationRoute
            {
                Source = source,
                Destination = destination,
                Amount = amount
            });
        }
    }

    public void RemoveRoute(ModulationRoute route)
    {
        lock (_lock)
        {
            _routes.Remove(route);
        }
    }

    public void UpdateSourceValue(ModulationSource source, double value)
    {
        int index = (int)source;
        if (index >= 0 && index < _sourceValues.Length)
        {
            _sourceValues[index] = value;
        }
    }

    public double GetModulationValue(ModulationDestination destination)
    {
        double totalModulation = 0;

        // Iterate through routes (thread-safe copy or lock if needed, but for audio thread performance prefer lock-free or minimal lock)
        // For simplicity and safety, we'll lock for now. In high-perf C++, we'd verify lock-freedom. 
        // In C# lock is fast enough for < 100 routes usually.
        lock (_lock)
        {
            foreach (var route in _routes)
            {
                if (route.Destination == destination && route.IsActive)
                {
                    int sourceIndex = (int)route.Source;
                    if (sourceIndex >= 0 && sourceIndex < _sourceValues.Length)
                    {
                        totalModulation += _sourceValues[sourceIndex] * route.Amount;
                    }
                }
            }
        }

        return totalModulation;
    }

    public void Clear()
    {
        lock (_lock)
        {
            _routes.Clear();
            Array.Clear(_sourceValues, 0, _sourceValues.Length);
        }
    }
}
