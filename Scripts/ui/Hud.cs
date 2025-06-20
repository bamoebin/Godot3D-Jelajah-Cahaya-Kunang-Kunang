using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Hud : Control
{
    // Export untuk debugging dan fine-tuning
    [Export] public bool ShowKunangCounter = true;
    [Export] public bool ShowDistanceTracker = true;
    [Export] public bool ShowLenteraProgress = true;
    [Export] public float UpdateInterval = 0.5f;
    
    // HAPUS NodePath Export - gunakan auto-detection
    // [Export] public NodePath FinishAreaPath; // HAPUS INI
    
    // References ke UI elements
    private Label _jarakLabel;
    private Label _jumlahKunangLabel;
    private TextureProgressBar _lenteraProgressBar;
    
    // References ke game objects
    private PlayerController _player;
    private Lentera _lentera;
    private Node3D _finishArea;
    
    // Update timer untuk optimasi
    private Timer _updateTimer;
    
    // Cache untuk optimasi
    private int _lastKunangCount = -1;
    private float _lastDistance = -1f;
    private float _lastLenteraProgress = -1f;
    
    public override void _Ready()
    {
        // Get UI element references
        GetUIReferences();
        
        // Find game object references dengan delay untuk memastikan scene loaded
        CallDeferred(nameof(FindGameReferencesDeferred));
        
        // Setup update timer
        SetupUpdateTimer();
        
        GD.Print("✅ HUD initialized successfully");
    }
    
    private void FindGameReferencesDeferred()
    {
        // Wait a frame untuk memastikan MainScene fully loaded
        GetTree().ProcessFrame += FindGameReferences;
    }
    
    private void GetUIReferences()
    {
        // Get UI elements berdasarkan struktur scene Anda
        _jarakLabel = GetNode<Label>("Jarak");
        _jumlahKunangLabel = GetNode<Label>("jumlahkunang");
        _lenteraProgressBar = GetNode<TextureProgressBar>("Node2D/TextureProgressBar");
        
        // Validate UI elements
        if (_jarakLabel == null) GD.PrintErr("❌ Jarak label not found!");
        if (_jumlahKunangLabel == null) GD.PrintErr("❌ JumlahKunang label not found!");
        if (_lenteraProgressBar == null) GD.PrintErr("❌ TextureProgressBar not found!");
        
        // Setup initial values
        if (_jarakLabel != null) _jarakLabel.Text = "🏠 Jarak: ---m";
        if (_jumlahKunangLabel != null) _jumlahKunangLabel.Text = "🔥 Kunang-kunang: 0";
        if (_lenteraProgressBar != null)
        {
            _lenteraProgressBar.MinValue = 0;
            _lenteraProgressBar.MaxValue = 100;
            _lenteraProgressBar.Value = 100;
        }
    }
    
    private void FindGameReferences()
    {
        // SOLUSI 1: Find player menggunakan multiple methods
        _player = FindPlayerMultipleMethods();
        
        if (_player != null)
        {
            GD.Print($"✅ Player found: {_player.Name}");
            
            // Get lentera from player
            _lentera = _player.GetLentera();
            if (_lentera == null)
            {
                GD.PrintErr("❌ Lentera not found on player!");
            }
            else
            {
                GD.Print("✅ Lentera found");
            }
        }
        else
        {
            GD.PrintErr("❌ Player not found!");
        }
        
        // SOLUSI 2: Find finish area menggunakan multiple methods
        _finishArea = FindFinishAreaMultipleMethods();
        
        if (_finishArea != null)
        {
            GD.Print($"✅ Finish area found: {_finishArea.Name}");
        }
        else
        {
            GD.PrintErr("❌ Finish area not found!");
        }
        
        // Initial update setelah semua reference ditemukan
        UpdateAllDisplays();
    }
    
    private PlayerController FindPlayerMultipleMethods()
    {
        PlayerController player = null;
        
        // Method 1: Cari berdasarkan group yang sudah ada
        player = GetTree().GetFirstNodeInGroup("BocahJawa1") as PlayerController;
        if (player != null) return player;
        
        player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        if (player != null) return player;
        
        // Method 2: Cari di current scene
        var currentScene = GetTree().CurrentScene;
        if (currentScene != null)
        {
            player = FindPlayerInScene(currentScene);
            if (player != null) return player;
        }
        
        // Method 3: Cari berdasarkan nama node langsung
        var playerNode = GetTree().CurrentScene?.FindChild("BocahJawa1", true, false);
        if (playerNode is PlayerController playerController)
        {
            return playerController;
        }
        
        return null;
    }
    
    private Node3D FindFinishAreaMultipleMethods()
    {
        Node3D finishArea = null;
        var currentScene = GetTree().CurrentScene;
        
        if (currentScene == null) return null;
        
        // Method 1: Cari berdasarkan nama-nama umum
        var possibleNames = new[] { 
            "FinishArea", "Rumah", "House", "Finish", "Goal", 
            "Victory", "VictoryArea", "EndArea", "Target",
            "FinishZone", "HomeArea", "Destination"
        };
        
        foreach (string name in possibleNames)
        {
            finishArea = currentScene.FindChild(name, true, false) as Node3D;
            if (finishArea != null)
            {
                GD.Print($"✅ Found finish area by name: {finishArea.Name}");
                return finishArea;
            }
        }
        
        // Method 2: Cari berdasarkan group
        var possibleGroups = new[] { 
            "finish_area", "victory_area", "goal", "house", "rumah" 
        };
        
        foreach (string groupName in possibleGroups)
        {
            var nodes = GetTree().GetNodesInGroup(groupName);
            if (nodes.Count > 0 && nodes[0] is Node3D node3D)
            {
                GD.Print($"✅ Found finish area by group: {groupName}");
                return node3D;
            }
        }
        
        // Method 3: Cari berdasarkan script type
        finishArea = FindNodeByType<Area3D>(currentScene, "finish");
        if (finishArea != null) return finishArea;
        
        finishArea = FindNodeByType<StaticBody3D>(currentScene, "finish");
        if (finishArea != null) return finishArea;
        
        // Method 4: Cari yang memiliki collision shape khusus (misalnya trigger area)
        finishArea = FindFinishAreaByCollision(currentScene);
        
        return finishArea;
    }
    
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
    
    private T FindNodeByType<T>(Node parent, string nameHint = "") where T : Node
    {
        if (parent is T result && (string.IsNullOrEmpty(nameHint) || 
            parent.Name.ToString().ToLower().Contains(nameHint.ToLower())))
        {
            return result;
        }
        
        foreach (Node child in parent.GetChildren())
        {
            var found = FindNodeByType<T>(child, nameHint);
            if (found != null) return found;
        }
        
        return null;
    }
    
    private Node3D FindFinishAreaByCollision(Node parent)
    {
        // Cari Area3D yang kemungkinan adalah finish area
        if (parent is Area3D area && area.Name.ToString().ToLower().Contains("VictoryArea"))
        {
            return area;
        }
        
        // Cari CollisionShape3D dengan nama yang mengindikasikan finish
        if (parent is CollisionShape3D collision)
        {
            var parentNode = collision.GetParent();
            if (parentNode is Node3D node3D && 
                (node3D.Name.ToString().ToLower().Contains("CollisionShape3D") || 
                 node3D.Name.ToString().ToLower().Contains("goal") ||
                 node3D.Name.ToString().ToLower().Contains("victory")))
            {
                return node3D;
            }
        }
        
        foreach (Node child in parent.GetChildren())
        {
            var result = FindFinishAreaByCollision(child);
            if (result != null) return result;
        }
        
        return null;
    }
    
    private void SetupUpdateTimer()
    {
        _updateTimer = new Timer();
        _updateTimer.WaitTime = UpdateInterval;
        _updateTimer.Timeout += UpdateAllDisplays;
        _updateTimer.Autostart = true;
        AddChild(_updateTimer);
    }
    
    private void UpdateAllDisplays()
    {
        if (ShowKunangCounter) UpdateKunangCounter();
        if (ShowDistanceTracker) UpdateDistanceTracker();
        if (ShowLenteraProgress) UpdateLenteraProgress();
    }
    
    private void UpdateKunangCounter()
    {
        if (_jumlahKunangLabel == null) return;
        
        try
        {
            var spawners = GetTree().GetNodesInGroup("kunang_spawners");
            int totalActive = 0;
            int totalCapacity = 0;
            int spawnerCount = 0;
            
            foreach (Node spawner in spawners)
            {
                if (spawner is KunangSpawner kunangSpawner)
                {
                    totalActive += kunangSpawner.GetActiveKunangCount();
                    totalCapacity += kunangSpawner.GetTotalKunangCount();
                    spawnerCount++;
                }
            }
            
            // Only update if changed for performance
            int currentCount = totalActive;
            if (currentCount != _lastKunangCount)
            {
                _lastKunangCount = currentCount;
                _jumlahKunangLabel.Text = $"🔥 Kunang-kunang: {totalActive}/{totalCapacity} ({spawnerCount} area)";
                
                // Color coding berdasarkan density
                if (totalCapacity > 0)
                {
                    float density = (float)totalActive / totalCapacity;
                    if (density > 0.7f)
                        _jumlahKunangLabel.Modulate = Colors.LightGreen;
                    else if (density > 0.3f)
                        _jumlahKunangLabel.Modulate = Colors.Yellow;
                    else
                        _jumlahKunangLabel.Modulate = Colors.OrangeRed;
                }
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"Error updating kunang counter: {e.Message}");
        }
    }
    
    private void UpdateDistanceTracker()
    {
        if (_jarakLabel == null || _player == null || _finishArea == null) return;
        
        try
        {
            float distance = _player.GlobalPosition.DistanceTo(_finishArea.GlobalPosition);
            
            // Only update if changed significantly for performance
            if (Mathf.Abs(distance - _lastDistance) > 0.5f)
            {
                _lastDistance = distance;
                _jarakLabel.Text = $"🏠 Jarak ke Rumah: {distance:F1}m";
                
                // Color coding berdasarkan jarak
                if (distance < 10f)
                    _jarakLabel.Modulate = Colors.LightGreen; // Dekat
                else if (distance < 30f)
                    _jarakLabel.Modulate = Colors.Yellow;     // Sedang
                else
                    _jarakLabel.Modulate = Colors.White;      // Jauh
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"Error updating distance tracker: {e.Message}");
        }
    }
    
    private void UpdateLenteraProgress()
    {
        if (_lenteraProgressBar == null || _lentera == null) return;
        
        try
        {
            float currentEnergy = _lentera.GetCurrentEnergy();
            float maxEnergy = _lentera.GetMaxEnergy();
            float percentage = (currentEnergy / maxEnergy) * 100f;
            
            // Only update if changed significantly for performance
            if (Mathf.Abs(percentage - _lastLenteraProgress) > 1f)
            {
                _lastLenteraProgress = percentage;
                _lenteraProgressBar.Value = percentage;
                
                // Color coding berdasarkan persentase
                if (percentage < 20)
                    _lenteraProgressBar.TintProgress = Colors.Red;
                else if (percentage < 50)
                    _lenteraProgressBar.TintProgress = Colors.Yellow;
                else
                    _lenteraProgressBar.TintProgress = Colors.LightGreen;
            }
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"Error updating lentera progress: {e.Message}");
        }
    }
    
    // Public methods untuk manual setup jika diperlukan
    public void SetFinishArea(Node3D finishArea)
    {
        _finishArea = finishArea;
        GD.Print($"✅ Finish area manually set: {finishArea?.Name ?? "null"}");
    }
    
    public void ForceRefreshReferences()
    {
        FindGameReferences();
    }
    
    public void ForceUpdateAll()
    {
        _lastKunangCount = -1;
        _lastDistance = -1f;
        _lastLenteraProgress = -1f;
        UpdateAllDisplays();
    }
}