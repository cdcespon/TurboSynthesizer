using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace TurboSynthesizer.UI.Controls;

public partial class WaveformSelector : ContentView
{
    public static readonly BindableProperty SelectedWaveformProperty =
        BindableProperty.Create(nameof(SelectedWaveform), typeof(string), typeof(WaveformSelector), "Sine", BindingMode.TwoWay, propertyChanged: OnSelectedWaveformChanged);

    public string SelectedWaveform
    {
        get => (string)GetValue(SelectedWaveformProperty);
        set => SetValue(SelectedWaveformProperty, value);
    }
    
    public static readonly BindableProperty SelectCommandProperty =
        BindableProperty.Create(nameof(SelectCommand), typeof(ICommand), typeof(WaveformSelector));

    public ICommand SelectCommand
    {
        get => (ICommand)GetValue(SelectCommandProperty);
        set => SetValue(SelectCommandProperty, value);
    }

    public WaveformSelector()
    {
        InitializeComponent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        UpdateVisuals();
    }
    
    // Bindable property changed
    private static void OnSelectedWaveformChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is WaveformSelector ws)
        {
             ws.UpdateVisuals();
        }
    }

    private void OnWaveformClicked(object sender, System.EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string waveform)
        {
            SelectedWaveform = waveform;
            SelectCommand?.Execute(waveform);
        }
    }

    private void UpdateVisuals()
    {
        // Traverse visual tree or just IDs? IDs are hard in ContentViews without x:Name
        // Simplest: Find children
        if (this.Content is Border b && b.Content is HorizontalStackLayout sl)
        {
            foreach(var child in sl.Children)
            {
                if (child is Button btn)
                {
                    bool isSelected = btn.CommandParameter?.ToString() == SelectedWaveform;
                    if (isSelected) 
                    {
                        btn.BackgroundColor = Microsoft.Maui.Graphics.Colors.BlueViolet; // Using a distinct color
                        btn.TextColor = Microsoft.Maui.Graphics.Colors.White;
                    }
                    else
                    {
                         btn.BackgroundColor = Microsoft.Maui.Graphics.Colors.Transparent;
                         btn.TextColor = Microsoft.Maui.Graphics.Colors.LightGray;
                    }
                }
            }
        }
    }
}
