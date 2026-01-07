using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TurboSynthesizer.Audio.Engine;
using TurboSynthesizer.Data.Services;
using TurboSynthesizer.Data.Entities; // Needed for Preset types if accessed directly

namespace TurboSynthesizer.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SynthEngine _synthEngine = null!;
    private readonly PresetService _presetService = null!;

    private readonly Core.Models.SynthParameters _parameters = new();
    
    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private System.Collections.ObjectModel.ObservableCollection<TurboSynthesizer.Data.Entities.Preset> _presets = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUserPreset))]
    private TurboSynthesizer.Data.Entities.Preset? _selectedPreset;

    [ObservableProperty]
    private string _currentNotes = string.Empty;

    public bool IsUserPreset => SelectedPreset?.Category == "User";

    private readonly System.Collections.Generic.List<int> _activeNoteList = new();

    [ObservableProperty]
    private int _keyboardOctave = 4;

    private readonly System.Collections.Generic.Dictionary<string, int> _keyboardMap = new()
    {
        { "A", 0 },  // C
        { "W", 1 },  // C#
        { "S", 2 },  // D
        { "E", 3 },  // D#
        { "D", 4 },  // E
        { "F", 5 },  // F
        { "T", 6 },  // F#
        { "G", 7 },  // G
        { "Y", 8 },  // G#
        { "H", 9 },  // A
        { "U", 10 }, // A#
        { "J", 11 }, // B
        { "K", 12 }, // C (Next Octave)
        { "O", 13 }, // C#
        { "L", 14 }  // D
    };

    private readonly System.Collections.Generic.HashSet<string> _pressedKeys = new();
    
    // --- Oscillators ---
    [ObservableProperty] private string _osc1Waveform = "Sine";
    [ObservableProperty] private string _osc2Waveform = "Saw";
    [ObservableProperty] private double _mixOsc1 = 0.5;
    [ObservableProperty] private double _mixOsc2 = 0.5;

    // --- Filter ---
    [ObservableProperty] private double _filterCutoff = 5000;
    [ObservableProperty] private double _filterResonance = 0;
    [ObservableProperty] private double _filterEnvAmount = 0;
    [ObservableProperty] private double _filterLfoAmount = 0;

    // --- Amp Envelope ---
    [ObservableProperty] private double _ampAttack = 0.01;
    [ObservableProperty] private double _ampDecay = 0.1;
    [ObservableProperty] private double _ampSustain = 0.7;
    [ObservableProperty] private double _ampRelease = 0.2;

    // --- Filter Envelope ---
    [ObservableProperty] private double _filterAttack = 0.01;
    [ObservableProperty] private double _filterDecay = 0.2;
    [ObservableProperty] private double _filterSustain = 0.5;
    [ObservableProperty] private double _filterRelease = 0.2;

    // --- Envelope Live State ---
    [ObservableProperty] private string _ampEnvStage = "Idle";
    [ObservableProperty] private double _ampEnvProgress = 0;
    [ObservableProperty] private string _filterEnvStage = "Idle";
    [ObservableProperty] private double _filterEnvProgress = 0;

    // --- LFO ---
    [ObservableProperty] private double _lfoRate = 1.0;

    // --- Effects ---
    [ObservableProperty] private double _delayTime = 0.4;
    [ObservableProperty] private double _delayFeedback = 0.3;
    [ObservableProperty] private double _delayMix = 0.0;
    [ObservableProperty] private double _reverbSize = 0.8;
    [ObservableProperty] private double _reverbDamp = 0.5;
    [ObservableProperty] private double _reverbMix = 0.0;

    // --- MIDI & Modulation ---
    [ObservableProperty] private ObservableCollection<string> _midiDevices = new();
    [ObservableProperty] private int _selectedMidiDeviceIndex = -1;
    [ObservableProperty] private ObservableCollection<Core.Modulation.ModulationRoute> _modulationRoutes = new();

    partial void OnSelectedPresetChanged(TurboSynthesizer.Data.Entities.Preset? value)
    {
        if (value != null)
        {
            LoadPresetCommand.Execute(null);
        }
    }

    partial void OnSelectedMidiDeviceIndexChanged(int value)
    {
        if (value >= 0)
        {
            _synthEngine.MidiService.Start(value);
            StatusMessage = $"MIDI Device {value} Started";
        }
        else
        {
            _synthEngine.MidiService.Stop();
        }
    }

    public MainViewModel()
    {
        try
        {
             var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
             System.IO.File.WriteAllText(System.IO.Path.Combine(desktop, "alive.txt"), "MainViewModel Started");

            _synthEngine = new SynthEngine();
            _presetService = new PresetService(); // DI normally
            
            // Sequential initialization to prevent race conditions
            Task.Run(async () => {
                try
                {
                    await _presetService.InitializeAsync();
                    await LoadPresetsAsync();
                    
                    _synthEngine.Start();
                    
                    // Init Engine with default values after DB load if any
                    MainThread.BeginInvokeOnMainThread(() => {
                        UpdateEngine();
                        LoadMidiDevices();
                        RefreshModulationRoutes();
                        StatusMessage = "TurboSynth Ready | Musical Typing ON";
                    });
                }
                catch (Exception ex)
                {
                    MainThread.BeginInvokeOnMainThread(() => {
                        StatusMessage = $"Init Error: {ex.Message}";
                    });
                }
            });

            // Subscribe to MIDI Service events
            _synthEngine.MidiService.NoteOn += (note, vel) => MainThread.BeginInvokeOnMainThread(() => NoteOn(note));
            _synthEngine.MidiService.NoteOff += (note) => MainThread.BeginInvokeOnMainThread(() => NoteOff(note));
            _synthEngine.MidiService.ControlChange += (cc, val) => MainThread.BeginInvokeOnMainThread(() => HandleControlChange(cc, val));
        }
        catch (Exception ex)
        {
            // Emergency logging
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var logPath = System.IO.Path.Combine(desktop, "turbosynth_crash.txt");
            System.IO.File.WriteAllText(logPath, $"Crash in MainViewModel: {ex}");
            StatusMessage = "Audio Init Failed!";
        }
    }

    // --- Partial Property Change Handlers ---
    partial void OnOsc1WaveformChanged(string value) => UpdateEngine();
    partial void OnOsc2WaveformChanged(string value) => UpdateEngine();
    partial void OnMixOsc1Changed(double value) => UpdateEngine();
    partial void OnMixOsc2Changed(double value) => UpdateEngine();
    
    partial void OnFilterCutoffChanged(double value) => UpdateEngine();
    partial void OnFilterResonanceChanged(double value) => UpdateEngine();
    partial void OnFilterEnvAmountChanged(double value) => UpdateEngine();
    partial void OnFilterLfoAmountChanged(double value) => UpdateEngine();
    
    partial void OnAmpAttackChanged(double value) => UpdateEngine();
    partial void OnAmpDecayChanged(double value) => UpdateEngine();
    partial void OnAmpSustainChanged(double value) => UpdateEngine();
    partial void OnAmpReleaseChanged(double value) => UpdateEngine();

    partial void OnFilterAttackChanged(double value) => UpdateEngine();
    partial void OnFilterDecayChanged(double value) => UpdateEngine();
    partial void OnFilterSustainChanged(double value) => UpdateEngine();
    partial void OnFilterReleaseChanged(double value) => UpdateEngine();

    partial void OnLfoRateChanged(double value) => UpdateEngine();

    partial void OnDelayTimeChanged(double value) => UpdateEngine();
    partial void OnDelayFeedbackChanged(double value) => UpdateEngine();
    partial void OnDelayMixChanged(double value) => UpdateEngine();
    partial void OnReverbSizeChanged(double value) => UpdateEngine();
    partial void OnReverbDampChanged(double value) => UpdateEngine();
    partial void OnReverbMixChanged(double value) => UpdateEngine();

    private void UpdateEngine()
    {
        if (_synthEngine == null) return;

        _parameters.Osc1Waveform = Osc1Waveform;
        _parameters.Osc2Waveform = Osc2Waveform;
        _parameters.MixOsc1 = (float)MixOsc1;
        _parameters.MixOsc2 = (float)MixOsc2;
        
        _parameters.FilterCutoff = (float)FilterCutoff;
        _parameters.FilterResonance = (float)FilterResonance;
        _parameters.FilterEnvAmount = (float)FilterEnvAmount;
        _parameters.FilterLfoAmount = (float)FilterLfoAmount;
        
        _parameters.AmpAttack = (float)AmpAttack;
        _parameters.AmpDecay = (float)AmpDecay;
        _parameters.AmpSustain = (float)AmpSustain;
        _parameters.AmpRelease = (float)AmpRelease;
        
        _parameters.FilterAttack = (float)FilterAttack;
        _parameters.FilterDecay = (float)FilterDecay;
        _parameters.FilterSustain = (float)FilterSustain;
        _parameters.FilterRelease = (float)FilterRelease;
        
        _parameters.LfoFrequency = (float)LfoRate;
        
        _parameters.DelayTime = (float)DelayTime;
        _parameters.DelayFeedback = (float)DelayFeedback;
        _parameters.DelayMix = (float)DelayMix;
        
        _parameters.ReverbRoomSize = (float)ReverbSize;
        _parameters.ReverbDamping = (float)ReverbDamp;
        _parameters.ReverbMix = (float)ReverbMix;
        
        _synthEngine.SetParameters(_parameters);
    }

    [RelayCommand]
    private void NoteOn(int note)
    {
        StatusMessage = $"Note On: {note}";
        _synthEngine.NoteOn(note, 100);
        
        lock(_activeNoteList)
        {
            if (!_activeNoteList.Contains(note))
                _activeNoteList.Add(note);
            UpdateCurrentNotes();
        }
    }

    [RelayCommand]
    private void NoteOff(int note)
    {
        StatusMessage = $"Note Off: {note}";
        _synthEngine.NoteOff(note);

        lock(_activeNoteList)
        {
            _activeNoteList.Remove(note);
            UpdateCurrentNotes();
        }
    }

    public void HandleKeyPress(string key)
    {
        key = key.ToUpper();
        if (key == "Z")
        {
             KeyboardOctave = Math.Max(0, KeyboardOctave - 1);
             return;
        }
        if (key == "X")
        {
             KeyboardOctave = Math.Min(8, KeyboardOctave + 1);
             return;
        }

        if (_keyboardMap.TryGetValue(key, out int offset))
        {
            if (_pressedKeys.Contains(key)) return;
            
            _pressedKeys.Add(key);
            int noteNumber = (KeyboardOctave + 1) * 12 + offset;
            StatusMessage = $"Musical Typing: {GetNoteName(noteNumber)}";
            NoteOn(noteNumber);
        }
    }

    public void HandleKeyRelease(string key)
    {
        key = key.ToUpper();
        if (_keyboardMap.TryGetValue(key, out int offset))
        {
            _pressedKeys.Remove(key);
            int noteNumber = (KeyboardOctave + 1) * 12 + offset;
            NoteOff(noteNumber);
        }
    }

    private void HandleControlChange(int cc, int value)
    {
        // Simple mapping for now
        // Normalized value (0-1)
        double norm = value / 127.0;

        switch (cc)
        {
            case 1:  // Mod Wheel
            case 74: // Brightness
                FilterCutoff = 20 + (norm * 19980);
                break;
            case 71: // Resonance
                FilterResonance = norm;
                break;
            case 7:  // Volume
                // We don't have a master volume yet, let's map to Reverb Mix as proxy or just status
                ReverbMix = norm;
                break;
        }

        StatusMessage = $"MIDI CC {cc}: {value}";
    }

    private void UpdateCurrentNotes()
    {
        if (_activeNoteList.Count == 0)
        {
            CurrentNotes = string.Empty;
            return;
        }

        var noteNames = _activeNoteList.ConvertAll(n => GetNoteName(n));
        CurrentNotes = string.Join(" + ", noteNames);
    }

    private string GetNoteName(int noteNumber)
    {
        string[] names = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int octave = (noteNumber / 12) - 1;
        string name = names[noteNumber % 12];
        return $"{name}{octave}";
    }

    [RelayCommand]
    private async Task SavePresetAsync()
    {
        try
        {
            await _presetService.SavePresetAsync($"Preset {DateTime.Now:HH:mm:ss}", "User", _parameters);
            StatusMessage = "Preset Saved";
            await LoadPresetsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Save Failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadPresetAsync()
    {
        try
        {
            if (SelectedPreset == null) return;
            
            var p = _presetService.DeserializeParameters(SelectedPreset.ParametersJson);
            
            Osc1Waveform = p.Osc1Waveform;
            Osc2Waveform = p.Osc2Waveform;
            MixOsc1 = p.MixOsc1;
            MixOsc2 = p.MixOsc2;
            
            FilterCutoff = p.FilterCutoff;
            FilterResonance = p.FilterResonance;
            FilterEnvAmount = p.FilterEnvAmount;
            FilterLfoAmount = p.FilterLfoAmount;
            
            AmpAttack = p.AmpAttack;
            AmpDecay = p.AmpDecay;
            AmpSustain = p.AmpSustain;
            AmpRelease = p.AmpRelease;
            
            FilterAttack = p.FilterAttack;
            FilterDecay = p.FilterDecay;
            FilterSustain = p.FilterSustain;
            FilterRelease = p.FilterRelease;
            
            LfoRate = p.LfoFrequency;
            
            DelayTime = p.DelayTime;
            DelayFeedback = p.DelayFeedback;
            DelayMix = p.DelayMix;
            
            ReverbSize = p.ReverbRoomSize;
            ReverbDamp = p.ReverbDamping;
            ReverbMix = p.ReverbMix;

            StatusMessage = $"Loaded: {SelectedPreset.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load Failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeletePresetAsync()
    {
        try
        {
            if (SelectedPreset == null || SelectedPreset.Category == "Factory") return;

            await _presetService.DeletePresetAsync(SelectedPreset.Id);
            StatusMessage = $"Deleted: {SelectedPreset.Name}";
            await LoadPresetsAsync();
            SelectedPreset = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete Failed: {ex.Message}";
        }
    }
    
    // Command to refresh list
    public async Task LoadPresetsAsync()
    {
        try
        {
            var list = await _presetService.GetAllPresetsAsync();
            MainThread.BeginInvokeOnMainThread(() => {
                Presets.Clear();
                foreach(var p in list) Presets.Add(p);
                
                if (Presets.Count > 0 && SelectedPreset == null)
                    SelectedPreset = Presets[0];

                StatusMessage = list.Count > 0 ? "Synth Ready" : "No Presets Found";
            });
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                StatusMessage = $"DB Error: {ex.Message}";
            });
        }
    }

    private void LoadMidiDevices()
    {
        var devices = _synthEngine.MidiService.GetInputDevices();
        MidiDevices.Clear();
        foreach(var d in devices) MidiDevices.Add(d);
        
        if (MidiDevices.Count > 0)
            SelectedMidiDeviceIndex = 0; // Auto-select first
    }

    public void RefreshModulationRoutes()
    {
        ModulationRoutes.Clear();
        foreach(var r in _synthEngine.ModulationMatrix.Routes)
        {
            ModulationRoutes.Add(r);
        }
    }

    public void GetScopeData(float[] buffer, string source = "Master")
    {
        _synthEngine.GetScopeData(buffer, source);
        UpdateEnvelopeStatus();
    }

    private void UpdateEnvelopeStatus()
    {
        var status = _synthEngine.GetEnvelopeStatus();
        AmpEnvStage = status.ampStage;
        AmpEnvProgress = status.ampProgress;
        FilterEnvStage = status.filtStage;
        FilterEnvProgress = status.filtProgress;
    }
}
