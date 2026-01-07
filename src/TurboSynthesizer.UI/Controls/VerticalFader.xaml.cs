using Microsoft.Maui.Controls;
using System;
using System.Runtime.CompilerServices;

namespace TurboSynthesizer.UI.Controls;

public partial class VerticalFader : ContentView
{
    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(VerticalFader), "VAR");

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(double), typeof(VerticalFader), 0.0, BindingMode.TwoWay, propertyChanged: OnValueChanged);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly BindableProperty MinimumProperty =
        BindableProperty.Create(nameof(Minimum), typeof(double), typeof(VerticalFader), 0.0);

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(nameof(Maximum), typeof(double), typeof(VerticalFader), 1.0);

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public static readonly BindableProperty IsLogarithmicProperty =
        BindableProperty.Create(nameof(IsLogarithmic), typeof(bool), typeof(VerticalFader), false, propertyChanged: (b, o, n) => ((VerticalFader)b).UpdateThumbPosition());

    public bool IsLogarithmic
    {
        get => (bool)GetValue(IsLogarithmicProperty);
        set => SetValue(IsLogarithmicProperty, value);
    }

    private double _totalHeight = 100;

    public VerticalFader()
    {
        InitializeComponent();
        NameLabelHost.SetBinding(Microsoft.Maui.Controls.Label.TextProperty, new Binding(nameof(Label), source: this));
        
        // Listen for layout changes to know height
        this.SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, EventArgs e)
    {
        // Calculate track height
        if (TrackContainer != null)
        {
             _totalHeight = TrackContainer.Height;
             UpdateThumbPosition();
        }
    }

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is VerticalFader fader)
        {
            fader.UpdateThumbPosition();
            fader.UpdateValueLabel();
        }
    }

    private void UpdateThumbPosition()
    {
        if (_totalHeight <= 0) return;

        double normalized = ValueToPosition(Value);
        
        double availableTravel = _totalHeight - FaderThumb.HeightRequest;
        if(availableTravel < 0) availableTravel = 0;

        // Move Thumb (Negative Up)
        double currentBottomOffset = normalized * availableTravel;
        FaderThumb.TranslationY = -currentBottomOffset;
        
        // Update Active Track Height
        // Should go from bottom to center of thumb
        if (ActiveTrack != null)
        {
            double halfThumb = FaderThumb.HeightRequest / 2;
            ActiveTrack.HeightRequest = currentBottomOffset + halfThumb;
        }

        if (Application.Current.Resources.TryGetValue("Primary", out var primaryColor) && primaryColor is Color pColor)
        {
             if(normalized > 0)
             {
                 FaderThumb.Stroke = pColor;
                 ActiveTrack.Color = pColor; // Also Color the track? User screenshot showed white, but Primary is nicer. Let's stick to White for now to match request or Primary? User image had white line.
                 // Actually user image seemed to have white line.
                 // Let's use White for the track, but maybe change thumb stroke.
             }
             else
             {
                 FaderThumb.Stroke = Application.Current.Resources["Gray600"] as Color;
                 ActiveTrack.Color = Colors.Transparent; // Hide if 0? Or just keep white?
             }
             
             // Restore ActiveTrack color if > 0
             if(normalized > 0) ActiveTrack.Color = Colors.White;
        }
    }
    
    // Helper: Value -> 0..1 Position
    private double ValueToPosition(double val)
    {
        double min = Minimum;
        double max = Maximum;
        
        if (Math.Abs(max - min) < 0.0001) return 0;
        
        if (IsLogarithmic)
        {
            double safeMin = Math.Max(min, 0.0001);
            double safeMax = Math.Max(max, 0.0001);
            double safeVal = Math.Max(val, 0.0001);
            
            double logMin = Math.Log10(safeMin);
            double logMax = Math.Log10(safeMax);
            
            if (Math.Abs(logMax - logMin) < 0.0001) return 0;
            
            return (Math.Log10(safeVal) - logMin) / (logMax - logMin);
        }
        else
        {
            return (val - min) / (max - min);
        }
    }

    // Helper: 0..1 Position -> Value
    private double PositionToValue(double pos)
    {
        double min = Minimum;
        double max = Maximum;
        
        if (IsLogarithmic)
        {
             double safeMin = Math.Max(min, 0.0001);
             double safeMax = Math.Max(max, 0.0001);
             
             double logMin = Math.Log10(safeMin);
             double logMax = Math.Log10(safeMax);
             
             double logVal = pos * (logMax - logMin) + logMin;
             return Math.Pow(10, logVal);
        }
        else
        {
            return min + (pos * (max - min));
        }
    }
    
    private void UpdateValueLabel()
    {
       ValueLabelHost.Text = Value.ToString("F2");
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
         if (_totalHeight <= 0) return;
         
         double availableTravel = _totalHeight - FaderThumb.HeightRequest;
         if (availableTravel <= 0) return;

         if (e.StatusType == GestureStatus.Started)
         {
             _startValue = Value;
         }
         else if (e.StatusType == GestureStatus.Running)
         {
             // Dragging UP (Negative Y) -> Increase Value
             double pixelDelta = -e.TotalY; 
             double fractionDelta = pixelDelta / availableTravel;

             double currentPos = ValueToPosition(_startValue);
             double newPos = Math.Clamp(currentPos + fractionDelta, 0, 1);
             double newValue = PositionToValue(newPos);
             
             Value = newValue;
         }
    }

    private double _startValue;
    
    private void OnTapped(object sender, TappedEventArgs e)
    {
         // Click to jump?
         // Getting Y position relative to control is hard in MAUI without platform specifics or specific args
         // e.GetPosition? MAUI 8+
    }
}
