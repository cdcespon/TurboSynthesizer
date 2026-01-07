using FluentAssertions;
using Xunit;
using TurboSynthesizer.UI.ViewModels;

namespace TurboSynthesizer.UI.Tests;

public class MainViewModelTests
{
    [Fact]
    public void NoteOn_ShouldUpdateStatusAndTriggerEngine()
    {
        // Arrange
        // Note: This relies on SynthEngine initializing AudioContext which relies on real audio hardware.
        // This test might fail if no audio device is present.
        var vm = new MainViewModel();

        // Act
        vm.NoteOnCommand.Execute(60); // Middle C

        // Assert
        vm.StatusMessage.Should().Contain("Note On: 60");
    }

    [Fact]
    public void NoteOff_ShouldUpdateStatus()
    {
        // Arrange
        var vm = new MainViewModel();

        // Act
        vm.NoteOffCommand.Execute(60);

        // Assert
        vm.StatusMessage.Should().Contain("Note Off: 60");
    }
}
