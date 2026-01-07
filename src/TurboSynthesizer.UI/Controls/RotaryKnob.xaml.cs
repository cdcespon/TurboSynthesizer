using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;

namespace TurboSynthesizer.UI.Controls;

public partial class RotaryKnob : ContentView
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(nameof(Value), typeof(double), typeof(RotaryKnob), 0.0, 
            propertyChanged: OnValueChanged);

    public static readonly BindableProperty MinimumProperty =
        BindableProperty.Create(nameof(Minimum), typeof(double), typeof(RotaryKnob), 0.0);

    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(nameof(Maximum), typeof(double), typeof(RotaryKnob), 100.0);

    public static readonly BindableProperty LabelProperty =
        BindableProperty.Create(nameof(Label), typeof(string), typeof(RotaryKnob), string.Empty);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    // Internal property for rotation binding
    public static readonly BindableProperty RotationAngleProperty =
        BindableProperty.Create(nameof(RotationAngle), typeof(double), typeof(RotaryKnob), 0.0);
        
    public double RotationAngle
    {
        get => (double)GetValue(RotationAngleProperty);
        set => SetValue(RotationAngleProperty, value);
    }

    public RotaryKnob()
    {
        InitializeComponent();
        UpdateRotation();
    }

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is RotaryKnob knob)
        {
            knob.UpdateRotation();
        }
    }

    private void UpdateRotation()
    {
        // Map Value (Min to Max) to Rotation (-135 to 135 degrees)
        double range = Maximum - Minimum;
        if (range <= 0) return;

        double normalized = (Value - Minimum) / range;
        double angle = -135 + (normalized * 270);
        
        // We need to access the visual element to rotate it. 
        // In XAML we bound BoxView Rotation to something.
        // Let's use FindByName or data binding.
        // Actually, I didn't set x:Name for the indicator. 
        // Let's implement a Bindable Property "RotationAngle" and bind to it in XAML.
        
        // Simpler: Just update the binding context or a property.
        // I will use 'RotationAngle' property.
        
        // Wait, 'Rotation' is a property of VisualElement. I shouldn't shadow it?
        // BoxView Rotation="{Binding Rotation...}" refers to the ContentView's Rotation property? 
        // No, I need a specific property for the knob indicator rotation.
        // Let's call it "IndicatorRotation".
        
        // Actually in XAML I used Source={RelativeSource AncestorType...} Path=Rotation.
        // This refers to the ContentView's Rotation, which rotates the WHOLE control. That's wrong.
        
        // I need to fix XAML to bind to a custom property `IndicatorRotation`.
        
        IndicatorRotation = angle;
    }

    public static readonly BindableProperty IndicatorRotationProperty =
        BindableProperty.Create(nameof(IndicatorRotation), typeof(double), typeof(RotaryKnob), -135.0);

    public double IndicatorRotation
    {
        get => (double)GetValue(IndicatorRotationProperty);
        set => SetValue(IndicatorRotationProperty, value);
    }

    private double _previousY;

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _previousY = e.TotalY;
                break;
                
            case GestureStatus.Running:
                double delta = _previousY - e.TotalY; // Drag up increases value
                _previousY = e.TotalY;
                
                // Sensitivity
                double change = delta * ((Maximum - Minimum) / 200.0);
                
                double newValue = Value + change;
                Value = Math.Clamp(newValue, Minimum, Maximum);
                break;
                
            case GestureStatus.Completed:
                break;
        }
    }
}
