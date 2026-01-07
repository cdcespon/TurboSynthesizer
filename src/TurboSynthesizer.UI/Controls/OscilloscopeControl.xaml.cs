using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System;
using TurboSynthesizer.UI.ViewModels;

namespace TurboSynthesizer.UI.Controls;

public partial class OscilloscopeControl : ContentView
{
    private readonly OscilloscopeDrawable _drawable;
    private readonly IDispatcherTimer _timer;

    public static readonly BindableProperty WaveformTypeProperty = BindableProperty.Create(nameof(WaveformType), typeof(object), typeof(OscilloscopeControl), null, propertyChanged: OnWaveformChanged);
    public object WaveformType { get => GetValue(WaveformTypeProperty); set => SetValue(WaveformTypeProperty, value); }

    public static readonly BindableProperty LineColorProperty = BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(OscilloscopeControl), Colors.Cyan, propertyChanged: OnLineColorChanged);
    public Color LineColor { get => (Color)GetValue(LineColorProperty); set => SetValue(LineColorProperty, value); }

    public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(string), typeof(OscilloscopeControl), "Master");
    public string Source { get => (string)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }

    public static readonly BindableProperty AmplitudeProperty = BindableProperty.Create(nameof(Amplitude), typeof(float), typeof(OscilloscopeControl), 1.0f);
    public float Amplitude { get => (float)GetValue(AmplitudeProperty); set => SetValue(AmplitudeProperty, value); }

    public static readonly BindableProperty NoteNameProperty = BindableProperty.Create(nameof(NoteName), typeof(string), typeof(OscilloscopeControl), string.Empty);
    public string NoteName { get => (string)GetValue(NoteNameProperty); set => SetValue(NoteNameProperty, value); }
    
    // Mode: Live (uses Timer & GetScopeData) or Static (uses WaveformType)
    public bool IsStatic { get; set; } = false;

    public OscilloscopeControl()
    {
        InitializeComponent();
        _drawable = new OscilloscopeDrawable();
        ScopeView.Drawable = _drawable;
        
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33);
        _timer.Tick += OnTimerTick;
        // Timer only starts if not static, or manage start/stop
        Loaded += (s, e) => { if (!IsStatic) _timer.Start(); };
        Unloaded += (s, e) => _timer.Stop();
    }

    private static void OnWaveformChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is OscilloscopeControl ctrl)
        {
             // If we are currently silent, force a redraw of the static wave
             // Otherwise, the timer will handle it on next tick, but this makes it snappier
             ctrl.ScopeView.Invalidate();
        }
    }

    private static void OnLineColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is OscilloscopeControl ctrl && newValue is Color color)
        {
            ctrl._drawable.LineColor = color;
            ctrl.ScopeView.Invalidate();
        }
    }

    private void OnTimerTick(object sender, EventArgs e)
    {
        if (IsStatic) return;

        if (BindingContext is MainViewModel vm)
        {
            vm.GetScopeData(_drawable.Buffer, Source);
            _drawable.Amplitude = Amplitude;
            _drawable.NoteName = NoteName;
            ScopeView.Invalidate();
        }
    }
}

public class OscilloscopeDrawable : IDrawable
{
    public float[] Buffer { get; } = new float[512]; 
    public Color LineColor { get; set; } = Colors.Cyan;

    public void GenerateStaticWave(string typeName)
    {
        // Generate one cycle of waveform into Buffer
        for(int i=0; i<Buffer.Length; i++)
        {
            float t = (float)i / Buffer.Length; // 0 to 1
            float val = 0;
            double phase = t * 2 * Math.PI;

            switch(typeName)
            {
                case "Sine":
                    val = (float)Math.Sin(phase);
                    break;
                case "Square":
                    val = t < 0.5f ? 1f : -1f;
                    break;
                case "Saw":
                case "Sawtooth":
                    val = 1f - (2f * t);
                    break;
                case "Triangle":
                    val = 1f - (4f * (float)Math.Abs(t - 0.5));
                    break;
                case "Noise":
                    val = (float)(new Random().NextDouble() * 2 - 1);
                     break;
            }
            Buffer[i] = val;
        }
    }

    public float Amplitude { get; set; } = 1.0f;
    public string NoteName { get; set; } = string.Empty;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = LineColor;
        canvas.StrokeSize = 2;

        float midY = dirtyRect.Height / 2;
        float width = dirtyRect.Width;
        float step = width / (Buffer.Length - 1);

        var path = new PathF();
        
        for (int i = 0; i < Buffer.Length; i++)
        {
            float x = i * step;
            // Apply amplitude scaling
            float val = Buffer[i] * Amplitude;
            float y = midY - (val * midY * 0.8f);
            
            if (i == 0) path.MoveTo(x, y);
            else path.LineTo(x, y);
        }

        canvas.DrawPath(path);
    }
}
