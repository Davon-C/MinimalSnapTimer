using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using MinimalSnapTimer.ViewModels;

namespace MinimalSnapTimer.Tests;

public sealed class PresetManagerTests
{
    [Fact]
    public void Presets_CanBeReadAndLaunched()
    {
        var settings = SettingsService.CreateDefaultSettings();
        var manager = new PresetManager(settings);
        var timer = new TimerEngine(new ManualTickSource());
        var vm = new MainViewModel(timer, manager, new NotificationService(), new TaskbarService(), new TrayService(), settings);
        var preset = manager.GetByName("\u5750\u7740\u5de5\u4f5c");

        Assert.NotNull(preset);

        vm.StartPresetById(preset!.Id);

        Assert.Equal(WorkflowStage.Sit, vm.Snapshot.Stage);
        Assert.Equal(TimeSpan.FromMinutes(40), vm.Snapshot.CurrentTime);
    }

    [Fact]
    public void Duplicate_CreatesCopy()
    {
        var settings = SettingsService.CreateDefaultSettings();
        var manager = new PresetManager(settings);
        var source = manager.Presets[0];

        var copy = manager.Duplicate(source.Id);

        Assert.NotEqual(source.Id, copy.Id);
        Assert.Equal($"{source.Name} Copy", copy.Name);
    }
}
