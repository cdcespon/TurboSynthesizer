using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TurboSynthesizer.Core.Models;
using TurboSynthesizer.Data.Context;
using TurboSynthesizer.Data.Entities;

namespace TurboSynthesizer.Data.Services;

public class PresetService
{
    private readonly SynthDbContext _context;

    public PresetService()
    {
        _context = new SynthDbContext();
    }

    public PresetService(DbContextOptions<SynthDbContext> options)
    {
        _context = new SynthDbContext(options);
    }

    public async Task InitializeAsync()
    {
        try
        {
            await _context.Database.EnsureCreatedAsync();
            await SeedFactoryPresetsAsync();
        }
        catch (Exception)
        {
            // If creation or seeding fails (likely schema mismatch), recreation is needed for dev.
            await RecreateDatabaseAsync();
        }
    }

    private async Task RecreateDatabaseAsync()
    {
        try
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbPath = Path.Join(path, "turbosynth.db");

            if (File.Exists(dbPath))
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // Try to delete file-based database for a clean slate
                await _context.Database.EnsureDeletedAsync();
            }
        }
        catch (Exception)
        {
            // Fallback: If file is locked, at least try to clear the tables
            _context.Presets.RemoveRange(_context.Presets);
            await _context.SaveChangesAsync();
        }

        await _context.Database.EnsureCreatedAsync();
        await SeedFactoryPresetsAsync();
    }

    private async Task SeedFactoryPresetsAsync()
    {
        // Automatically clear out existing factory presets to ensure no duplicates.
        // We do this transparently for the user.
        var factoryPresetsCount = await _context.Presets.CountAsync(p => p.Category == "Factory");
        if (factoryPresetsCount > 0)
        {
            await _context.Presets.Where(p => p.Category == "Factory").ExecuteDeleteAsync();
        }

        var factoryPresets = new List<Preset>
        {
            CreateFactoryPreset("Violin Solo", "Strings", new SynthParameters 
            { 
                Osc1Waveform = "Saw", MixOsc1 = 0.8f, MixOsc2 = 0f,
                FilterCutoff = 3000f, FilterResonance = 0.2f,
                AmpAttack = 0.3f, AmpDecay = 0.2f, AmpSustain = 0.8f, AmpRelease = 0.4f,
                FilterAttack = 0.5f, FilterEnvAmount = 0.3f
            }),
            CreateFactoryPreset("Soft Cello", "Strings", new SynthParameters 
            { 
                Osc1Waveform = "Saw", MixOsc1 = 0.6f, Osc2Waveform = "Saw", MixOsc2 = 0.3f,
                FilterCutoff = 800f, FilterResonance = 0.5f,
                AmpAttack = 0.6f, AmpSustain = 0.9f, AmpRelease = 0.8f,
                ReverbMix = 0.3f
            }),
            CreateFactoryPreset("Etheral Pad", "Pads", new SynthParameters 
            { 
                Osc1Waveform = "Triangle", MixOsc1 = 0.5f, Osc2Waveform = "Square", MixOsc2 = 0.4f,
                FilterCutoff = 1200f, FilterResonance = 0.1f,
                AmpAttack = 1.2f, AmpSustain = 1.0f, AmpRelease = 2.0f,
                DelayMix = 0.3f, ReverbMix = 0.5f
            }),
            CreateFactoryPreset("Deep Pulse Bass", "Bass", new SynthParameters 
            { 
                Osc1Waveform = "Square", MixOsc1 = 0.9f, MixOsc2 = 0f,
                FilterCutoff = 400f, FilterResonance = 0.4f,
                FilterEnvAmount = 0.6f, FilterDecay = 0.15f,
                AmpAttack = 0.01f, AmpSustain = 0.4f, AmpRelease = 0.1f
            }),
            CreateFactoryPreset("Classic Lead", "Leads", new SynthParameters 
            { 
                Osc1Waveform = "Saw", MixOsc1 = 0.5f, Osc2Waveform = "Saw", MixOsc2 = 0.5f,
                FilterCutoff = 5000f, FilterResonance = 0.3f,
                FilterLfoAmount = 0.2f, LfoFrequency = 5.0f,
                AmpAttack = 0.05f, DelayMix = 0.2f
            }),
            CreateFactoryPreset("Windy Noise", "FX", new SynthParameters 
            { 
                Osc1Waveform = "Noise", MixOsc1 = 1.0f, MixOsc2 = 0f,
                FilterCutoff = 2000f, FilterResonance = 0.7f,
                FilterAttack = 2.0f, FilterRelease = 2.0f, FilterEnvAmount = 0.8f,
                AmpAttack = 1.5f, AmpRelease = 2.0f,
                ReverbMix = 0.6f
            })
        };

        _context.Presets.AddRange(factoryPresets);
        await _context.SaveChangesAsync();
    }

    private Preset CreateFactoryPreset(string name, string category, SynthParameters parameters)
    {
        return new Preset
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = "Factory", // Force category for factory presets
            SubCategory = category,
            ParametersJson = JsonSerializer.Serialize(parameters),
            CreatedAt = DateTime.UtcNow
        };
    }

    public async Task<List<Preset>> GetAllPresetsAsync()
    {
        return await _context.Presets.AsNoTracking().ToListAsync();
    }

    public async Task SavePresetAsync(string name, string category, SynthParameters parameters)
    {
        var json = JsonSerializer.Serialize(parameters);
        
        var preset = new Preset
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            ParametersJson = json,
            CreatedAt = DateTime.UtcNow
        };

        _context.Presets.Add(preset);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePresetAsync(Guid id)
    {
        var preset = await _context.Presets.FindAsync(id);
        if (preset != null)
        {
            if (preset.Category == "Factory")
            {
                throw new InvalidOperationException("Cannot delete Factory presets.");
            }

            _context.Presets.Remove(preset);
            await _context.SaveChangesAsync();
        }
    }
    
    public SynthParameters DeserializeParameters(string json)
    {
        return JsonSerializer.Deserialize<SynthParameters>(json) ?? new SynthParameters();
    }
}
