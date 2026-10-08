namespace gangiChi
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

#if ANDROID
            // keep the message box above the soft keyboard
            Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.Application.SetWindowSoftInputModeAdjust(
                this, Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.WindowSoftInputModeAdjust.Resize);
#endif
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            window.Title = "gangiChi";
#if WINDOWS
            window.Width = 1000;
            window.Height = 720;
            window.MinimumWidth = 420;
            window.MinimumHeight = 560;
#endif
            return window;
        }
    }
}
