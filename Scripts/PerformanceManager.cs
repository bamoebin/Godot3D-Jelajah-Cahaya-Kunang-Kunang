// Scripts/managers/PerformanceManager.cs - BARU
using Godot;

public partial class PerformanceManager : Node
{
    [Export] public int MaxActiveLights = 20; // Batas maksimal light aktif
    [Export] public bool EnableLightLOD = true;
    [Export] public bool EnableAILOD = true;

    [Export] public bool EnableAdaptiveQuality = true;
    [Export] public int TargetFPS = 60;
    [Export] public float QualityCheckInterval = 1.0f;

    private float _fpsCheckTimer = 0.0f;
    private float _averageFPS = 60.0f;

    private int _currentActiveLights = 0;

    public override void _Process(double delta)
    {
        _fpsCheckTimer += (float)delta;
        if (_fpsCheckTimer >= QualityCheckInterval)
        {
            _fpsCheckTimer = 0.0f;
            _averageFPS = (float)Engine.GetFramesPerSecond();

            if (EnableAdaptiveQuality)
            {
                AdaptQualityBasedOnPerformance();
            }

            // Debug info
            if (Time.GetTicksMsec() % 5000 < 100) // Setiap 5 detik
            {
                GD.Print($"FPS: {_averageFPS:F1}, Active Lights: {_currentActiveLights}/{MaxActiveLights}");
            }
        }
    }
    public override void _Ready()
    {
        // Set sebagai singleton
        GetTree().SetAutoAcceptQuit(false);
    }

    public bool CanActivateLight()
    {
        return _currentActiveLights < MaxActiveLights;
    }

    public void RegisterLight()
    {
        _currentActiveLights++;
    }

    public void UnregisterLight()
    {
        _currentActiveLights = Mathf.Max(0, _currentActiveLights - 1);
    }

    private void AdaptQualityBasedOnPerformance()
    {
        if (_averageFPS < TargetFPS * 0.8f) // Jika FPS < 80% target
        {
            // Kurangi quality
            MaxActiveLights = Mathf.Max(10, MaxActiveLights - 2);
            GetTree().CallGroup("spawners", "ReduceQuality");
        }
        else if (_averageFPS > TargetFPS * 1.1f) // Jika FPS > 110% target
        {
            // Tingkatkan quality
            MaxActiveLights = Mathf.Min(30, MaxActiveLights + 1);
            GetTree().CallGroup("spawners", "IncreaseQuality");
        }
    }
}