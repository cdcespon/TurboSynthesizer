using Microsoft.Maui.Controls;

namespace TurboSynthesizer.UI.Views;

public partial class MainSynthView : ContentPage
{
    public MainSynthView()
    {
        try {
            InitializeComponent();
#if WINDOWS
            this.HandlerChanged += OnHandlerChanged;
#endif
        } catch(Exception ex) {
             var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
             var logPath = System.IO.Path.Combine(desktop, "turbosynth_view_crash.txt");
             System.IO.File.WriteAllText(logPath, $"Crash in MainSynthView: {ex}");
             throw;
        }
    }

#if WINDOWS
    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement element)
        {
            // Use AddHandler with handledEventsToo = true to ensure we catch keys 
            // even if a sub-control (like a slider) has focus and "handles" the event.
            element.AddHandler(Microsoft.UI.Xaml.UIElement.KeyDownEvent, new Microsoft.UI.Xaml.Input.KeyEventHandler(HandleNativeKeyDown), true);
            element.AddHandler(Microsoft.UI.Xaml.UIElement.KeyUpEvent, new Microsoft.UI.Xaml.Input.KeyEventHandler(HandleNativeKeyUp), true);
            
            // Try to ensure the view can receive focus for keyboard events
            element.IsTabStop = true;
            element.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }
    }

    private void HandleNativeKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (BindingContext is ViewModels.MainViewModel vm)
        {
            vm.HandleKeyPress(e.Key.ToString());
        }
    }

    private void HandleNativeKeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (BindingContext is ViewModels.MainViewModel vm)
        {
            vm.HandleKeyRelease(e.Key.ToString());
        }
    }

    private void OnBackgroundTapped(object? sender, EventArgs e)
    {
        if (Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement element)
        {
            element.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
        }
    }
#endif
}
