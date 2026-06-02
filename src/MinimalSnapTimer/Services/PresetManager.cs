using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class PresetManager
{
    private AppSettings _settings;

    public PresetManager(AppSettings settings)
    {
        _settings = settings;
        SettingsService.EnsureDefaults(_settings);
    }

    public IReadOnlyList<TimerPreset> Presets => _settings.Presets;

    public void ReplaceSettings(AppSettings settings)
    {
        _settings = settings;
        SettingsService.EnsureDefaults(_settings);
    }

    public TimerPreset? GetById(string id) => _settings.Presets.FirstOrDefault(p => p.Id == id);

    public TimerPreset? GetByName(string name) =>
        _settings.Presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    public TimerPreset? GetWorkflowPreset(WorkflowStage stage)
    {
        var (name, type) = stage switch
        {
            WorkflowStage.Sit => ("\u5750\u7740\u5de5\u4f5c", PresetType.Sit),
            WorkflowStage.Stand => ("\u7ad9\u7acb\u6d3b\u52a8", PresetType.Stand),
            _ => (string.Empty, PresetType.Normal)
        };

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return _settings.Presets.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? _settings.Presets.FirstOrDefault(p => p.Type == type);
    }

    public TimerPreset Add(TimerPreset preset)
    {
        preset.Id = string.IsNullOrWhiteSpace(preset.Id) ? Guid.NewGuid().ToString("N") : preset.Id;
        _settings.Presets.Add(preset);
        return preset;
    }

    public void Update(TimerPreset preset)
    {
        var index = _settings.Presets.FindIndex(p => p.Id == preset.Id);
        if (index >= 0)
        {
            _settings.Presets[index] = preset;
        }
    }

    public void Remove(string id)
    {
        _settings.Presets.RemoveAll(p => p.Id == id);
    }

    public TimerPreset Duplicate(string id)
    {
        var source = GetById(id) ?? throw new InvalidOperationException("Preset not found.");
        var copy = new TimerPreset
        {
            Name = $"{source.Name} Copy",
            DurationMinutes = source.DurationMinutes,
            Type = source.Type,
            AutoStart = source.AutoStart,
            CompletionAction = source.CompletionAction,
            AutoSwitchToNext = source.AutoSwitchToNext,
            NextPresetId = source.NextPresetId
        };

        _settings.Presets.Add(copy);
        return copy;
    }

    public void Move(string id, int newIndex)
    {
        var preset = GetById(id);
        if (preset is null)
        {
            return;
        }

        _settings.Presets.Remove(preset);
        newIndex = Math.Clamp(newIndex, 0, _settings.Presets.Count);
        _settings.Presets.Insert(newIndex, preset);
    }
}
