namespace SyncNetWasm.Services
{
    /// <summary>Dark-mode toggle lives in MainLayout's app bar, but MudThemeProvider itself
    /// is declared in App.razor (so AuthLayout pages get theming too) — this is the shared
    /// state between the two.</summary>
    public class ThemeState
    {
        public bool IsDarkMode { get; private set; }

        public event Action? Changed;

        public void Toggle()
        {
            IsDarkMode = !IsDarkMode;
            Changed?.Invoke();
        }
    }
}
