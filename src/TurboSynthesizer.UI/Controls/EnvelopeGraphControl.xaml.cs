using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace TurboSynthesizer.UI.Controls;

public partial class EnvelopeGraphControl : ContentView
{
    private readonly EnvelopeDrawable _drawable;

    public static readonly BindableProperty AttackProperty = BindableProperty.Create(nameof(Attack), typeof(double), typeof(EnvelopeGraphControl), 0.1, propertyChanged: OnPropertyChanged);
    public static readonly BindableProperty DecayProperty = BindableProperty.Create(nameof(Decay), typeof(double), typeof(EnvelopeGraphControl), 0.1, propertyChanged: OnPropertyChanged);
    public static readonly BindableProperty SustainProperty = BindableProperty.Create(nameof(Sustain), typeof(double), typeof(EnvelopeGraphControl), 0.5, propertyChanged: OnPropertyChanged);
    public static readonly BindableProperty ReleaseProperty = BindableProperty.Create(nameof(Release), typeof(double), typeof(EnvelopeGraphControl), 0.1, propertyChanged: OnPropertyChanged);

    public static readonly BindableProperty CurrentStageProperty = BindableProperty.Create(nameof(CurrentStage), typeof(string), typeof(EnvelopeGraphControl), "Idle", propertyChanged: OnPropertyChanged);
    public static readonly BindableProperty StageProgressProperty = BindableProperty.Create(nameof(StageProgress), typeof(double), typeof(EnvelopeGraphControl), 0.0, propertyChanged: OnPropertyChanged);

    public double Attack { get => (double)GetValue(AttackProperty); set => SetValue(AttackProperty, value); }
    public double Decay { get => (double)GetValue(DecayProperty); set => SetValue(DecayProperty, value); }
    public double Sustain { get => (double)GetValue(SustainProperty); set => SetValue(SustainProperty, value); }
    public double Release { get => (double)GetValue(ReleaseProperty); set => SetValue(ReleaseProperty, value); }
    public string CurrentStage { get => (string)GetValue(CurrentStageProperty); set => SetValue(CurrentStageProperty, value); }
    public double StageProgress { get => (double)GetValue(StageProgressProperty); set => SetValue(StageProgressProperty, value); }

    public EnvelopeGraphControl()
    {
        InitializeComponent();
        _drawable = new EnvelopeDrawable();
        GraphView.Drawable = _drawable;
    }

    private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is EnvelopeGraphControl ctrl)
        {
            ctrl._drawable.Attack = (float)ctrl.Attack;
            ctrl._drawable.Decay = (float)ctrl.Decay;
            ctrl._drawable.Sustain = (float)ctrl.Sustain;
            ctrl._drawable.Release = (float)ctrl.Release;
            ctrl._drawable.CurrentStage = ctrl.CurrentStage;
            ctrl._drawable.StageProgress = (float)ctrl.StageProgress;
            ctrl.GraphView.Invalidate();
        }
    }
}

public class EnvelopeDrawable : IDrawable
{
    public float Attack { get; set; } = 0.1f;
    public float Decay { get; set; } = 0.1f;
    public float Sustain { get; set; } = 0.5f;
    public float Release { get; set; } = 0.1f;
    public string CurrentStage { get; set; } = "Idle";
    public float StageProgress { get; set; } = 0.0f;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.LimeGreen;
        canvas.StrokeSize = 2;
        canvas.Antialias = true;

        float width = dirtyRect.Width;
        float height = dirtyRect.Height;
        
        // Normalize time: Total time = A + D + R + (HoldTime). 
        // For visualization, let's assume a fixed width for A+D+R and a gap for S.
        // A, D, R range 0-2s usually.
        // Let's us a relative scale where max width is ~5 sec? 
        // Or simpler: Assign 25% width per segment type max?
        // Let's try dynamic scaling based on total time, but capped.
        
        float totalTime = Attack + Decay + 1.0f + Release; // 1.0f for Sustain hold visual
        float scaleX = width / totalTime;

        float yBottom = height;
        float ySustain = height - (Sustain * height); 

        PathF path = new PathF();
        path.MoveTo(0, height);

        // Attack (0 to 1)
        float xA = Attack * scaleX;
        path.LineTo(xA, 0);

        // Decay (1 to S)
        float xD = xA + (Decay * scaleX);
        path.LineTo(xD, ySustain);

        // Sustain (Hold)
        float xS = xD + (1.0f * scaleX);
        path.LineTo(xS, ySustain);

        // Release (S to 0)
        float xR = xS + (Release * scaleX);
        path.LineTo(xR, height);

        canvas.DrawPath(path);
        
        // Fill
        path.LineTo(0, height);
        canvas.FillColor = Colors.LimeGreen.WithAlpha(0.2f);
        canvas.FillPath(path);

        // Draw Cursor
        if (CurrentStage != "Idle")
        {
            float cursorX = 0;
            switch (CurrentStage)
            {
                case "Attack":
                    cursorX = StageProgress * Attack * scaleX;
                    break;
                case "Decay":
                    cursorX = xA + StageProgress * Decay * scaleX;
                    break;
                case "Sustain":
                    cursorX = xD + StageProgress * 1.0f * scaleX;
                    break;
                case "Release":
                    cursorX = xS + StageProgress * Release * scaleX;
                    break;
            }

            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 2;
            canvas.DrawLine(cursorX, 0, cursorX, height);
            
            // Highlight current point
            float cursorY = height;
            // Simplified Y calculation matching the path
             switch (CurrentStage)
            {
                case "Attack": cursorY = height - (StageProgress * height); break;
                case "Decay": cursorY = (StageProgress * ySustain) + ((1.0f - StageProgress) * 0); break;
                case "Sustain": cursorY = ySustain; break;
                case "Release": cursorY = (StageProgress * height) + ((1.0f - StageProgress) * ySustain); break;
            }
            canvas.FillColor = Colors.White;
            canvas.FillCircle(cursorX, cursorY, 4);
        }
    }
}
