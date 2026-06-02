using System.ComponentModel;

namespace MinimalSnapTimer.Services;

public interface ILocalizationService : INotifyPropertyChanged
{
    string CurrentLanguage { get; }

    string this[string key] { get; }

    void ApplyLanguage(string languageCode);
}
