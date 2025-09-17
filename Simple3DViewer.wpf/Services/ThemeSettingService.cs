using MaterialDesignThemes.Wpf;

namespace Simple3DViewer.wpf.Services;

public interface IThemeSettingService
{
    bool IsDarkTheme { get; }

    void ToggleThemeLightDark();

    void ApplyThemeLightDark(bool isDarkTheme);
}

internal class ThemeSettingService : IThemeSettingService
{
    private readonly PaletteHelper paletteHelper;
    private readonly Theme theme;

    public ThemeSettingService()
    {
        paletteHelper = new PaletteHelper();
        theme = paletteHelper.GetTheme();
    }

    public bool IsDarkTheme
    {
        get
        {
            BaseTheme currentBaseTheme = theme.GetBaseTheme();
            return currentBaseTheme == BaseTheme.Dark;
        }
    }

    public void ApplyThemeLightDark(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            theme.SetDarkTheme();
        }
        else
        {
            theme.SetLightTheme();
        }
        paletteHelper.SetTheme(theme);
    }

    public void ToggleThemeLightDark()
    {
        BaseTheme currentBaseTheme = theme.GetBaseTheme();
        if (currentBaseTheme != BaseTheme.Dark)
        {
            theme.SetDarkTheme();
        }
        else
        {
            theme.SetLightTheme();
        }
        paletteHelper.SetTheme(theme);
    }
}
