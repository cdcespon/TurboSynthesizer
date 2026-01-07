using TurboSynthesizer.UI.Views;

namespace TurboSynthesizer.UI;

public partial class App : Application
{
	public App()
	{
        try 
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
             var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
             var logPath = System.IO.Path.Combine(desktop, "turbosynth_app_crash.txt");
             System.IO.File.WriteAllText(logPath, $"Crash in App Ctor: {ex}");
             throw; // Re-throw to fail fast
        }
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
        try 
        {
		    return new Window(new MainSynthView());
        }
        catch (Exception ex)
        {
             var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
             var logPath = System.IO.Path.Combine(desktop, "turbosynth_window_crash.txt");
             System.IO.File.WriteAllText(logPath, $"Crash in CreateWindow: {ex}");
             throw;
        }
	}
}