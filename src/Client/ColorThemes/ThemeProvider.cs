using MudBlazor;
using System;

namespace RestIdentity.Client.ColorThemes
{
    public class ThemeProvider
    {
        public MudTheme Theme 
        {
            get => _theme; 
            set
            {
                if (value is null || value == _theme)
                    return;

                OnThemeChanged?.Invoke(value);
                _theme = value;
            }
        }
        private MudTheme _theme;

        public event Action<MudTheme> OnThemeChanged;

        public ThemeProvider()
        {
            Theme = Themes.DarkTheme;
        }

        public void InvertTheme()
        {
            Theme = Theme == Themes.DarkTheme ? Themes.LightTheme : Themes.DarkTheme;
        }

        public void DarkTheme()
        {
            Theme = Themes.DarkTheme;
        }

        public void LightTheme()
        {
            Theme = Themes.LightTheme;
        }
    }
}
