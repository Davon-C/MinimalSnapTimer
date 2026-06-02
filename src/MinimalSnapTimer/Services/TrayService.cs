using System.Drawing;
using System.Windows.Forms;
using MinimalSnapTimer.Models;

namespace MinimalSnapTimer.Services;

public sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon = new()
    {
        Visible = false,
        Text = "MinimalSnapTimer"
    };

    private readonly ILocalizationService _localizer;
    private readonly IIconService _iconService;
    private readonly Action<string, Exception?>? _logger;
    private Icon? _trayIcon;
    private TrayContext? _context;

    public TrayService(
        ILocalizationService? localizer = null,
        IIconService? iconService = null,
        Action<string, Exception?>? logger = null)
    {
        _localizer = localizer ?? LocalizationService.Instance;
        _iconService = iconService ?? new IconService();
        _logger = logger;
        _notifyIcon.DoubleClick += (_, _) => _context?.ToggleMainWindowVisibility();
    }

    public bool IsAvailable => _notifyIcon.Visible && _notifyIcon.ContextMenuStrip is not null && _notifyIcon.Icon is not null;

    public void Initialize(TrayContext context)
    {
        _context = context;
        EnsureTrayVisible();
        RebuildMenu();
        SetStateIcon(TimerState.Idle);
    }

    public void RebuildMenu()
    {
        if (_context is null)
        {
            return;
        }

        EnsureTrayIcon();
        var menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem(_localizer["tray.header"]) { Enabled = false });
        menu.Items.Add(new ToolStripMenuItem($"{_localizer["tray.currentStatus"]}: {_context.GetCurrentStatus()}") { Enabled = false });
        menu.Items.Add(new ToolStripMenuItem($"{_localizer["tray.currentTime"]}: {_context.GetCurrentTimeText()}") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_localizer["tray.toggleWindow"], null, (_, _) => _context.ToggleMainWindowVisibility());
        menu.Items.Add(_localizer["tray.showPureWindow"], null, (_, _) => _context.ShowPureWindow());
        menu.Items.Add(_localizer["tray.returnStandard"], null, (_, _) => _context.ReturnToStandardMode());
        menu.Items.Add(_localizer["tray.togglePureMode"], null, (_, _) => _context.TogglePureMode());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_localizer["tray.startSit"], null, (_, _) => _context.StartSit());
        menu.Items.Add(_localizer["tray.startStand"], null, (_, _) => _context.StartStand());

        var presetsMenu = new ToolStripMenuItem(_localizer["tray.presets"]);
        foreach (var preset in _context.GetPresets())
        {
            presetsMenu.DropDownItems.Add(preset.Name, null, (_, _) => _context.StartPresetById(preset.Id));
        }

        menu.Items.Add(presetsMenu);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_localizer["tray.pauseResume"], null, (_, _) => _context.PauseOrResume());
        menu.Items.Add(_localizer["main.stop"], null, (_, _) => _context.Stop());
        menu.Items.Add(_localizer["main.reset"], null, (_, _) => _context.Reset());
        menu.Items.Add(new ToolStripSeparator());

        if (_context.IsPureClickThrough())
        {
            menu.Items.Add(_localizer["pure.clickThroughEnabled"], null, (_, _) => { }).Enabled = false;
        }

        menu.Items.Add(GetPureClickThroughMenuText(_context.IsPureClickThrough(), _localizer), null, (_, _) => _context.SetPureClickThrough(!_context.IsPureClickThrough()));
        menu.Items.Add(_localizer["main.topmost"], null, (_, _) => _context.ToggleAlwaysOnTop());
        menu.Items.Add(_localizer["main.settings"], null, (_, _) => _context.OpenSettings());
        menu.Items.Add(_localizer["tray.exit"], null, (_, _) => _context.Exit());

        _notifyIcon.ContextMenuStrip = menu;
        EnsureTrayIcon();
    }

    public static string GetPureClickThroughMenuText(bool enabled, ILocalizationService? localizer = null)
    {
        var strings = localizer ?? LocalizationService.Instance;
        return enabled ? strings["pure.disableClickThrough"] : strings["pure.enableClickThrough"];
    }

    public static IReadOnlyList<string> GetTopLevelMenuTexts(TrayContext context, ILocalizationService? localizer = null)
    {
        var strings = localizer ?? LocalizationService.Instance;
        var labels = new List<string>
        {
            strings["tray.header"],
            strings["tray.currentStatus"],
            strings["tray.currentTime"],
            strings["tray.toggleWindow"],
            strings["tray.showPureWindow"],
            strings["tray.returnStandard"],
            strings["tray.togglePureMode"],
            strings["tray.startSit"],
            strings["tray.startStand"],
            strings["tray.presets"],
            strings["tray.pauseResume"],
            strings["main.stop"],
            strings["main.reset"]
        };

        if (context.IsPureClickThrough())
        {
            labels.Add(strings["pure.clickThroughEnabled"]);
        }

        labels.Add(GetPureClickThroughMenuText(context.IsPureClickThrough(), strings));
        labels.Add(strings["main.topmost"]);
        labels.Add(strings["main.settings"]);
        labels.Add(strings["tray.exit"]);
        return labels;
    }

    public void UpdateTooltip(string status, string timeText)
    {
        EnsureTrayIcon();
        var text = $"{status} {timeText}".Trim();
        if (text.Length > 63)
        {
            text = text[..63];
        }

        _notifyIcon.Text = string.IsNullOrWhiteSpace(text) ? "MinimalSnapTimer" : text;
    }

    public void ShowBalloon(string title, string message)
    {
        EnsureTrayVisible();
        _notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
    }

    public void SetStateIcon(TimerState state)
    {
        _ = state;
        EnsureTrayIcon();
    }

    public void EnsureTrayVisible()
    {
        EnsureTrayIcon();
        _notifyIcon.Visible = true;
    }

    public void ReloadTrayIconIfMissing()
    {
        if (_notifyIcon.Icon is not null)
        {
            return;
        }

        EnsureTrayIcon(forceReload: true);
    }

    private void EnsureTrayIcon(bool forceReload = false)
    {
        if (!forceReload && _notifyIcon.Icon is not null)
        {
            return;
        }

        try
        {
            var icon = _iconService.LoadTrayIcon(_logger);
            ReplaceTrayIcon(icon);
        }
        catch (Exception ex)
        {
            _logger?.Invoke("加载托盘图标时发生未预期异常，已回退到默认图标。", ex);
            ReplaceTrayIcon((Icon)SystemIcons.Application.Clone());
        }
    }

    private void ReplaceTrayIcon(Icon newIcon)
    {
        var previousIcon = _trayIcon;
        _trayIcon = newIcon;
        _notifyIcon.Icon = _trayIcon;
        previousIcon?.Dispose();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _trayIcon?.Dispose();
    }
}
