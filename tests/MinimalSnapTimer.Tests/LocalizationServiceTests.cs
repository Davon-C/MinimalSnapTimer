using MinimalSnapTimer.Services;

namespace MinimalSnapTimer.Tests;

public sealed class LocalizationServiceTests
{
    [Fact]
    public void DefaultsToSimplifiedChinese()
    {
        var service = new LocalizationService();

        Assert.Equal("zh-CN", service.CurrentLanguage);
        Assert.Equal("开启点击穿透", service["pure.enableClickThrough"]);
    }

    [Fact]
    public void ApplyLanguage_SwitchesToEnglish()
    {
        var service = new LocalizationService();

        service.ApplyLanguage("en-US");

        Assert.Equal("en-US", service.CurrentLanguage);
        Assert.Equal("Disable Click-Through", service["pure.disableClickThrough"]);
        Assert.Equal("Timer finished", service["notification.timerTitle"]);
    }

    [Fact]
    public void ApplyLanguage_FallsBackToChineseForUnknownLanguage()
    {
        var service = new LocalizationService();
        service.ApplyLanguage("fr-FR");

        Assert.Equal("zh-CN", service.CurrentLanguage);
        Assert.Equal("系统通知发送失败，已自动降级为托盘提醒。", service["error.notificationFailed"]);
    }

    [Fact]
    public void MissingKey_ReturnsKeyName()
    {
        var service = new LocalizationService();

        Assert.Equal("missing.key", service["missing.key"]);
    }
}
