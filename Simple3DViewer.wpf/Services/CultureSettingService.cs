using System.Globalization;
using System.Windows;

namespace Simple3DViewer.wpf.Services;

public interface ICultureSettingService
{
    void ChangeCulture(string language);

    void ChangeCulture(int lcid);
}

internal class CultureSettingService : ICultureSettingService
{
    public void ChangeCulture(string language)
    {
        CultureInfo culture = new(language);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        I18NExtension.Culture = culture;
    }

    public void ChangeCulture(int lcid)
    {
        CultureInfo culture = new(lcid);
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
        I18NExtension.Culture = culture;
    }
}