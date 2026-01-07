using Microsoft.EntityFrameworkCore;
using System.IO;
using System;
using TurboSynthesizer.Data.Entities;

namespace TurboSynthesizer.Data.Context;

public class SynthDbContext : DbContext
{
    public DbSet<Preset> Presets { get; set; }

    public SynthDbContext()
    {
    }

    public SynthDbContext(DbContextOptions<SynthDbContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            var dbPath = Path.Join(path, "turbosynth.db");
            
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }
}
