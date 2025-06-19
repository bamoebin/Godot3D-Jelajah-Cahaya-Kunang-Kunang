using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class BarongSpawner : Node3D
{
    [Export] public PackedScene BarongScene;
    [Export] public int SpawnCount = 3;
    [Export] public Vector3 SpawnAreaSize = new Vector3(15, 4, 15);
    [Export] public float MinHeight = 0.0f;
    [Export] public float MaxHeight = 2.0f;
    [Export] public bool ShowDebugArea = true;
    
    // TAMBAHAN: Optimasi settings
    [Export] public bool UseOptimization = true; // Toggle untuk enable/disable optimasi
    [Export] public float SpawnRadius = 25.0f; // Radius spawn dari player
    [Export] public int MaxActiveCount = 2; // Maksimal Barong aktif bersamaan (kurangi dari SpawnCount)
    [Export] public float CheckInterval = 3.0f; // Interval check distance (detik)
    
    private MeshInstance3D _debugMesh;
    
    // OPTIMASI: Pool system untuk reuse Barong
    private List<BarongMonster> _barongPool = new();
    private List<BarongMonster> _activeBarong = new();
    private PlayerController _player;
    private Timer _checkTimer;
    private bool _isPlayerNear = false;
    
    public override void _Ready()
    {
        if (ShowDebugArea)
        {
            CreateDebugMesh();
        }
        
        if (UseOptimization)
        {
            // OPTIMASI: Setup system optimasi
            SetupOptimizedSpawning();
        }
        else
        {
            // FALLBACK: Gunakan spawning biasa
            SpawnBarongMonsters();
        }
    }
    
    // TAMBAHAN: Setup system optimasi
    private void SetupOptimizedSpawning()
    {
        // PERBAIKAN: Cari player dengan berbagai metode
        _player = FindPlayer();
        
        if (_player == null)
        {
            GD.PrintErr($"Player tidak ditemukan untuk {Name}! Menggunakan spawning biasa...");
            SpawnBarongMonsters();
            return;
        }
        
        GD.Print($"Player found for {Name}: {_player.Name}");
        
        // OPTIMASI: Buat pool Barong
        CreateBarongPool();
        
        // OPTIMASI: Setup timer untuk check distance
        _checkTimer = new Timer();
        _checkTimer.WaitTime = CheckInterval;
        _checkTimer.Timeout += CheckPlayerDistance;
        _checkTimer.Autostart = true;
        AddChild(_checkTimer);
        
        GD.Print($"Optimized Barong spawning setup complete for {Name}");
    }
    
    // TAMBAHAN: Method untuk mencari player
    private PlayerController FindPlayer()
    {
        // Metode 1: Cari berdasarkan nama node langsung
        var player = GetTree().CurrentScene.GetNode<PlayerController>("BocahJawa1");
        if (player != null)
        {
            return player;
        }
        
        // Metode 2: Cari berdasarkan group
        string[] possibleGroups = { "player", "Player", "BocahJawa1", "PlayerController" };
        
        foreach (string groupName in possibleGroups)
        {
            var playerFromGroup = GetTree().GetFirstNodeInGroup(groupName) as PlayerController;
            if (playerFromGroup != null)
            {
                return playerFromGroup;
            }
        }
        
        // Metode 3: Cari manual di scene tree
        return FindPlayerInScene(GetTree().CurrentScene);
    }
    
    // TAMBAHAN: Cari player manual di scene tree
    private PlayerController FindPlayerInScene(Node node)
    {
        if (node is PlayerController player)
            return player;
            
        foreach (Node child in node.GetChildren())
        {
            var result = FindPlayerInScene(child);
            if (result != null)
                return result;
        }
        
        return null;
    }
    
    // OPTIMASI: Buat pool Barong untuk reuse
    private void CreateBarongPool()
    {
        if (BarongScene == null)
        {
            GD.PrintErr($"BarongScene belum di-assign untuk {Name}!");
            return;
        }
        
        // Buat pool object yang bisa digunakan kembali
        for (int i = 0; i < SpawnCount; i++)
        {
            var barong = BarongScene.Instantiate<BarongMonster>();
            
            // OPTIMASI: Disable processing dan visibility secara default
            barong.SetProcessMode(Node.ProcessModeEnum.Disabled);
            barong.Visible = false;
            
            // Posisikan secara acak dalam area
            Vector3 randomPos = GetRandomPosition();
            barong.Position = randomPos;
            barong.RotationDegrees = new Vector3(0, (float)GD.RandRange(0, 360), 0);
            
            AddChild(barong);
            _barongPool.Add(barong);
        }
        
        GD.Print($"Created Barong pool with {_barongPool.Count} objects for {Name}");
    }
    
    // OPTIMASI: Check jarak player dan activate/deactivate Barong
    private void CheckPlayerDistance()
    {
        if (_player == null)
        {
            GD.Print($"Player reference lost for {Name}, trying to find again...");
            _player = FindPlayer();
            if (_player == null) return;
        }
        
        float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);
        bool shouldBeNear = distanceToPlayer <= SpawnRadius;
        
        // Debug info (comment out for production)
        // GD.Print($"{Name} - Distance to player: {distanceToPlayer:F1}, Should be near: {shouldBeNear}");
        
        if (_isPlayerNear != shouldBeNear)
        {
            _isPlayerNear = shouldBeNear;
            
            if (_isPlayerNear)
            {
                ActivateBarong();
            }
            else
            {
                DeactivateAllBarong();
            }
        }
    }
    
    // OPTIMASI: Aktifkan sebagian Barong saat player dekat
    private void ActivateBarong()
    {
        int currentActive = _activeBarong.Count;
        int neededActivation = Mathf.Min(MaxActiveCount - currentActive, _barongPool.Count);
        
        if (neededActivation > 0)
        {
            GD.Print($"{Name} - Activating {neededActivation} Barong (player near)");
        }
        
        for (int i = 0; i < neededActivation; i++)
        {
            var barong = _barongPool[0];
            _barongPool.RemoveAt(0);
            _activeBarong.Add(barong);
            
            // Aktifkan Barong
            barong.SetProcessMode(Node.ProcessModeEnum.Inherit);
            barong.Visible = true;
            
            // OPTIMASI: Reset posisi secara acak untuk variasi
            Vector3 newPos = GetRandomPosition();
            barong.Position = newPos;
            
            // OPTIMASI: Reset state Barong jika ada method untuk itu
            if (barong.HasMethod("ResetToSpawnState"))
            {
                barong.Call("ResetToSpawnState");
            }
        }
    }
    
    // OPTIMASI: Deaktifkan semua Barong saat player jauh
    private void DeactivateAllBarong()
    {
        if (_activeBarong.Count > 0)
        {
            GD.Print($"{Name} - Deactivating {_activeBarong.Count} Barong (player far)");
        }
        
        foreach (var barong in _activeBarong)
        {
            // Deaktifkan Barong
            barong.SetProcessMode(Node.ProcessModeEnum.Disabled);
            barong.Visible = false;
            
            // Kembalikan ke pool
            _barongPool.Add(barong);
        }
        _activeBarong.Clear();
    }
    
    // HELPER: Generate posisi acak dalam area spawn
    private Vector3 GetRandomPosition()
    {
        float halfWidth = SpawnAreaSize.X / 2;
        float halfLength = SpawnAreaSize.Z / 2;
        
        float randomX = (float)GD.RandRange(-halfWidth, halfWidth);
        float randomZ = (float)GD.RandRange(-halfLength, halfLength);
        float randomY = (float)GD.RandRange(MinHeight, MaxHeight);
        
        return new Vector3(randomX, randomY, randomZ);
    }
    
    // FALLBACK: Method spawning lama untuk compatibility
    public void SpawnBarongMonsters()
    {
        if (BarongScene == null)
        {
            GD.PrintErr($"BarongScene belum di-assign untuk {Name}!");
            return;
        }
        
        GD.Print($"{Name} - Spawning {SpawnCount} Barong using old method");
        
        for (int i = 0; i < SpawnCount; i++)
        {
            var barong = BarongScene.Instantiate<BarongMonster>();
            AddChild(barong);
            
            // Posisikan secara acak
            Vector3 randomPos = GetRandomPosition();
            barong.Position = randomPos;
            barong.RotationDegrees = new Vector3(0, (float)GD.RandRange(0, 360), 0);
        }
    }
    
    private void CreateDebugMesh()
    {
        _debugMesh = new MeshInstance3D();
        BoxMesh boxMesh = new BoxMesh();
        boxMesh.Size = SpawnAreaSize;
        
        _debugMesh.Mesh = boxMesh;
        
        StandardMaterial3D material = new StandardMaterial3D();
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.AlbedoColor = new Color(0.8f, 0.2f, 0.2f, 0.2f); // Merah untuk monster
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded; // OPTIMASI: Unshaded untuk performa
        material.NoDepthTest = true;
        
        _debugMesh.MaterialOverride = material;
        AddChild(_debugMesh);
    }
    
    public int GetActiveBarongCount()
    {
        return UseOptimization ? _activeBarong.Count : GetChildren().Cast<Node>().OfType<BarongMonster>().Count();
    }
    
    public int GetTotalBarongCount()
    {
        return UseOptimization ? (_activeBarong.Count + _barongPool.Count) : GetChildren().Cast<Node>().OfType<BarongMonster>().Count();
    }
    
    
    // TAMBAHAN: Cleanup saat spawner dihapus
    public override void _ExitTree()
    {
        if (_checkTimer != null)
        {
            _checkTimer.QueueFree();
        }
    }
    
    // TAMBAHAN: Method untuk force activate/deactivate (untuk debugging)
    public void ForceActivateAll()
    {
        if (!UseOptimization) return;
        
        while (_barongPool.Count > 0 && _activeBarong.Count < SpawnCount)
        {
            var barong = _barongPool[0];
            _barongPool.RemoveAt(0);
            _activeBarong.Add(barong);
            
            barong.SetProcessMode(Node.ProcessModeEnum.Inherit);
            barong.Visible = true;
        }
    }
    
    public void ForceDeactivateAll()
    {
        if (!UseOptimization) return;
        
        DeactivateAllBarong();
    }
}