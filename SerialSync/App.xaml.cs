using SerialSync.Misc;
using SerialSync.Services;

namespace SerialSync
{
    public partial class App : Application
    {
        private readonly GlobalState _globalState;
        public App(GlobalState globalState)
        {
            InitializeComponent();
            _globalState = globalState;
            _globalState.LayoutInfo = Unilities.LoadLayoutInfo();
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
