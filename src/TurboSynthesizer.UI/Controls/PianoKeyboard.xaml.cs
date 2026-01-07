using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace TurboSynthesizer.UI.Controls;

public partial class PianoKeyboard : ContentView
{
    public static readonly BindableProperty NoteOnCommandProperty =
        BindableProperty.Create(nameof(NoteOnCommand), typeof(ICommand), typeof(PianoKeyboard));

    public static readonly BindableProperty NoteOffCommandProperty =
        BindableProperty.Create(nameof(NoteOffCommand), typeof(ICommand), typeof(PianoKeyboard));

    public ICommand NoteOnCommand
    {
        get => (ICommand)GetValue(NoteOnCommandProperty);
        set => SetValue(NoteOnCommandProperty, value);
    }

    public ICommand NoteOffCommand
    {
        get => (ICommand)GetValue(NoteOffCommandProperty);
        set => SetValue(NoteOffCommandProperty, value);
    }

    public PianoKeyboard()
    {
        InitializeComponent();
        GenerateKeys();
        this.SizeChanged += OnKeyboardSizeChanged;
    }

    private void OnKeyboardSizeChanged(object sender, EventArgs e)
    {
        // Adjust black keys based on white key width
        if (KeysContainer.ColumnDefinitions.Count == 0 || Width <= 0) return;
        
        double whiteKeyWidth = Width / KeysContainer.ColumnDefinitions.Count;
        double blackKeyWidth = whiteKeyWidth * 0.65;
        double blackKeyOffset = blackKeyWidth / 2;

        foreach(var child in KeysContainer.Children)
        {
            if (child is BoxView key && key.ZIndex == 10) // Black Key
            {
                key.WidthRequest = blackKeyWidth;
                key.TranslationX = blackKeyOffset;
            }
        }
    }

    private void GenerateKeys()
    {
        KeysContainer.Children.Clear();
        KeysContainer.ColumnDefinitions.Clear();

        // Standard Piano Range: A0 (21) to C8 (108) -> 88 keys
        int startNote = 21; 
        int endNote = 108;   

        // Count white keys to define columns
        int whiteKeyCount = 0;
        for (int i = startNote; i <= endNote; i++)
        {
            if (!IsBlackKey(i)) whiteKeyCount++;
        }
        
        // Remove fixed width request on container
        KeysContainer.WidthRequest = -1;

        // Create Grid Columns
        for (int i = 0; i < whiteKeyCount; i++)
        {
             KeysContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        int currentWhiteKeyIndex = 0;
        for (int note = startNote; note <= endNote; note++)
        {
            bool isBlack = IsBlackKey(note);
            var key = CreateKey(note, isBlack);

            if (!isBlack)
            {
                // White key
                Grid.SetColumn(key, currentWhiteKeyIndex);
                Grid.SetRow(key, 0);
                KeysContainer.Children.Add(key);
                currentWhiteKeyIndex++; 
            }
            else
            {
                // Black key
                if (currentWhiteKeyIndex > 0)
                {
                    Grid.SetColumn(key, currentWhiteKeyIndex - 1); 
                    key.HorizontalOptions = LayoutOptions.End;
                    // Initial defaults, will be updated by OnKeyboardSizeChanged
                    key.WidthRequest = 10; 
                    key.TranslationX = 5; 
                    key.ZIndex = 10;
                    KeysContainer.Children.Add(key);
                }
            }
        }
    }

    private View CreateKey(int note, bool isBlack)
    {
        var button = new BoxView
        {
            Color = isBlack ? Colors.Black : Colors.White,
            HeightRequest = isBlack ? 95 : 150,
            VerticalOptions = LayoutOptions.Start,
            CornerRadius = isBlack ? new CornerRadius(0, 0, 2, 2) : new CornerRadius(0, 0, 4, 4),
            ZIndex = isBlack ? 10 : 0
        };
        
        if (!isBlack)
        {
            button.WidthRequest = -1; // Auto fill
            button.HorizontalOptions = LayoutOptions.Fill;
            button.Color = Colors.WhiteSmoke;
            button.Margin = new Thickness(0.5, 0); 
        }

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => 
        {
            NoteOnCommand?.Execute(note);
            button.Color = Colors.LightBlue;
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(200), () => 
            {
                button.Color = isBlack ? Colors.Black : Colors.WhiteSmoke;
                NoteOffCommand?.Execute(note);
            });
        };
        
        button.GestureRecognizers.Add(tapGesture);
        
        return button;
    }

    private bool IsBlackKey(int note)
    {
        int n = note % 12;
        return n == 1 || n == 3 || n == 6 || n == 8 || n == 10;
    }
}
