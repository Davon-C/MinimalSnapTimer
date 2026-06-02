using System.Windows;
using MinimalSnapTimer.Models;
using MinimalSnapTimer.Services;
using MinimalSnapTimer.ViewModels;

namespace MinimalSnapTimer.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    private void AddPreset_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.Settings.Presets.Add(new TimerPreset
        {
            Name = LocalizationService.Instance["settings.preset.new"],
            DurationMinutes = 10,
            Type = PresetType.Normal,
            AutoStart = true
        });
        PresetGrid.Items.Refresh();
    }

    private void DuplicatePreset_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || PresetGrid.SelectedItem is not TimerPreset preset)
        {
            return;
        }

        ViewModel.Settings.Presets.Add(new TimerPreset
        {
            Name = $"{preset.Name} {LocalizationService.Instance["settings.preset.copySuffix"]}",
            DurationMinutes = preset.DurationMinutes,
            Type = preset.Type,
            AutoStart = preset.AutoStart,
            CompletionAction = preset.CompletionAction,
            AutoSwitchToNext = preset.AutoSwitchToNext,
            NextPresetId = preset.NextPresetId
        });
        PresetGrid.Items.Refresh();
    }

    private void DeletePreset_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null || PresetGrid.SelectedItem is not TimerPreset preset)
        {
            return;
        }

        ViewModel.Settings.Presets.Remove(preset);
        PresetGrid.Items.Refresh();
    }

    private void MoveUpPreset_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedPreset(-1);
    }

    private void MoveDownPreset_Click(object sender, RoutedEventArgs e)
    {
        MoveSelectedPreset(1);
    }

    private void MoveSelectedPreset(int offset)
    {
        if (ViewModel is null || PresetGrid.SelectedItem is not TimerPreset preset)
        {
            return;
        }

        var list = ViewModel.Settings.Presets;
        var index = list.IndexOf(preset);
        if (index < 0)
        {
            return;
        }

        var newIndex = Math.Clamp(index + offset, 0, list.Count - 1);
        if (newIndex == index)
        {
            return;
        }

        list.RemoveAt(index);
        list.Insert(newIndex, preset);
        PresetGrid.SelectedItem = preset;
        PresetGrid.Items.Refresh();
    }
}
