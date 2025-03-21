using SerialSync.Misc;

namespace SerialSync
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }
        protected override void OnSleep()
        {
            base.OnSleep();
        }
        public override void CloseWindow(Window window)
        {
            base.CloseWindow(window);
        }
        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new MainPage()) { Title = "SerialSync" };
        }
    }
}
