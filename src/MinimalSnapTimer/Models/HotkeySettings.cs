namespace MinimalSnapTimer.Models;

public sealed class HotkeySettings
{
    public bool EnableWindowHotkeys { get; set; } = true;

    public bool EnableGlobalHotkeys { get; set; }

    public List<HotkeyBinding> Bindings { get; set; } = new();
}
