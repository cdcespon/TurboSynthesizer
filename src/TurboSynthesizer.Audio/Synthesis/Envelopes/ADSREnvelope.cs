using System;

namespace TurboSynthesizer.Audio.Synthesis.Envelopes;

public class ADSREnvelope
{
    public float Attack { get; set; } = 0.01f;      // seconds
    public float Decay { get; set; } = 0.1f;        // seconds
    public float Sustain { get; set; } = 0.5f;      // 0.0 - 1.0
    public float Release { get; set; } = 0.3f;      // seconds
    
    public enum EnvelopeStage { Idle, Attack, Decay, Sustain, Release }
    public EnvelopeStage Stage { get; private set; } = EnvelopeStage.Idle;
    public float CurrentLevel => _currentLevel;
    public float StageProgress { get; private set; } = 0.0f; // 0.0 to 1.0 within the current stage

    private float _currentLevel;
    private int _sampleRate = 48000;
    
    // Coefficients for exponential curves
    private float _attackCoef;
    private float _decayCoef;
    private float _releaseCoef;
    private float _attackBase;
    private float _decayBase;
    private float _releaseBase;

    public ADSREnvelope()
    {
        UpdateCoefficients();
    }

    public void SetSampleRate(int sampleRate)
    {
        _sampleRate = sampleRate;
        UpdateCoefficients();
    }
    
    // Call this whenever A, D, R change
    public void UpdateCoefficients()
    {
        _attackCoef = CalcCoef(Attack, 1.5f); // Target 1.5 to reach 1.0 faster
        _attackBase = (1.0f + _attackCoef) * (1.0f - _attackCoef);
        
        _decayCoef = CalcCoef(Decay, 0.0f); // Target 0
        _decayBase = (Sustain - 0.0f) * (1.0f - _decayCoef);
        
        _releaseCoef = CalcCoef(Release, 0.0f);
        _releaseBase = -0.0f * (1.0f - _releaseCoef); // Target 0
    }

    private float CalcCoef(float time, float targetRatio)
    {
         // Simple exponential approach: 
         // Value = Target + (Value - Target) * exp(-1 / (time * sampleRate))
         // However, standard digital envelope implemetations use:
         // out = input + coef * (out - input) which is a 1-pole filter.
         // Let's use simple linear for now for stability, or basic exp.
         
         // Actually, let's implement linear for simplicity first to ensure it works,
         // then upgrade to exponential if needed for "snappiness".
         // Wait, the plan asked for exponential.
         
         // For exponential approach to a target:
         // newLevel = currentLevel + speed * (target - currentLevel)
         // speed = 1 - exp(-1 / (time * Rate))
         
         return 1.0f - MathF.Exp(-1.0f / (time * _sampleRate)); 
    }

    public void NoteOn()
    {
        Stage = EnvelopeStage.Attack;
        _currentLevel = 0.0f; // Alternatively continue from current level if retriggering
    }

    public void NoteOff()
    {
        if (Stage != EnvelopeStage.Idle)
        {
            Stage = EnvelopeStage.Release;
        }
    }

    public float Process()
    {
        switch (Stage)
        {
            case EnvelopeStage.Idle:
                _currentLevel = 0.0f;
                StageProgress = 0.0f;
                break;
                
            case EnvelopeStage.Attack:
                _currentLevel += 1.0f / (Attack * _sampleRate);
                if (_currentLevel >= 1.0f)
                {
                    _currentLevel = 1.0f;
                    Stage = EnvelopeStage.Decay;
                    StageProgress = 0.0f;
                }
                else
                {
                    StageProgress = _currentLevel;
                }
                break;
                
            case EnvelopeStage.Decay:
                // Normalize progress from 1.0 to Sustain
                float range = 1.0f - Sustain;
                if (range > 0.001f)
                    StageProgress = (1.0f - _currentLevel) / range;
                else
                    StageProgress = 1.0f;

                _currentLevel += (Sustain - _currentLevel) * (1.0f - MathF.Exp(-1.0f / (Decay * _sampleRate / 3.0f)));
                
                if (MathF.Abs(_currentLevel - Sustain) < 0.001f)
                {
                    _currentLevel = Sustain;
                    Stage = EnvelopeStage.Sustain;
                    StageProgress = 0.0f;
                }
                break;
                
            case EnvelopeStage.Sustain:
                _currentLevel = Sustain;
                StageProgress = 0.5f; // Static middle of sustain hold visual
                break;
                
            case EnvelopeStage.Release:
                // Normalize progress from Sustain to 0.0
                if (Sustain > 0.001f)
                    StageProgress = (Sustain - _currentLevel) / Sustain;
                else
                    StageProgress = 1.0f - _currentLevel; // Fallback

                _currentLevel += (0.0f - _currentLevel) * (1.0f - MathF.Exp(-1.0f / (Release * _sampleRate / 3.0f)));
                 
                if (_currentLevel < 0.001f)
                {
                    _currentLevel = 0.0f;
                    Stage = EnvelopeStage.Idle;
                    StageProgress = 0.0f;
                }
                break;
        }
        
        return _currentLevel;
    }
}
