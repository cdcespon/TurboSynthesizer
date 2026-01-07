using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Xunit;
using TurboSynthesizer.Core.Models;
using TurboSynthesizer.Data.Context;
using TurboSynthesizer.Data.Services;

namespace TurboSynthesizer.Data.Tests;

public class PresetServiceTests
{
    private DbContextOptions<SynthDbContext> GetOptions(string dbName)
    {
        return new DbContextOptionsBuilder<SynthDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
    }

    [Fact]
    public async Task SaveAndLoad_ShouldPersistData()
    {
        // Arrange
        var options = GetOptions("SaveAndLoad");
        var service = new PresetService(options);
        
        var paramsDto = new SynthParameters { FilterCutoff = 1234.5f };

        // Act
        await service.SavePresetAsync("Test Preset", "Bass", paramsDto);
        var presets = await service.GetAllPresetsAsync();

        // Assert
        presets.Should().HaveCount(1);
        presets[0].Name.Should().Be("Test Preset");
        
        var loadedParams = service.DeserializeParameters(presets[0].ParametersJson);
        loadedParams.FilterCutoff.Should().Be(1234.5f);
    }
    
    [Fact]
    public async Task Delete_ShouldRemoveData()
    {
        // Arrange
        var options = GetOptions("Delete");
        var service = new PresetService(options);
        var paramsDto = new SynthParameters();
        await service.SavePresetAsync("To Delete", "Pad", paramsDto);
        var stored = await service.GetAllPresetsAsync();
        var idToDelete = stored[0].Id;

        // Act
        await service.DeletePresetAsync(idToDelete);
        var final = await service.GetAllPresetsAsync();

        // Assert
        final.Should().BeEmpty();
    }
}
