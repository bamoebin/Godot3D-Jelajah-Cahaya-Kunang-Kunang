using Godot;

public partial class MainScene : Node
{
    [Export] public Node3D FinishArea; // Assign di Inspector ke VictoryArea
    
    public override void _Ready()
    {
        GD.Print("🎮 MainScene loaded");
        
        // Setup finish area untuk HUD
        SetupFinishAreaForHUD();
        
        // Optional: Setup any other game initialization
        InitializeGame();
    }
    
    private void SetupFinishAreaForHUD()
    {
        // Auto-find VictoryArea jika tidak di-assign
        if (FinishArea == null)
        {
            FinishArea = GetNodeOrNull<Node3D>("VictoryArea");
        }
        
        if (FinishArea != null)
        {
            // Register ke group untuk HUD detection
            FinishArea.AddToGroup("finish_area");
            GD.Print($"✅ Finish area registered: {FinishArea.Name}");
        }
        else
        {
            GD.PrintErr("❌ VictoryArea not found!");
        }
    }
    
    private void InitializeGame()
    {
        // Force refresh HUD jika ada
        var hud = GetNodeOrNull<Control>("UI/Hud");
        if (hud != null)
        {
            GD.Print("✅ HUD found in MainScene");
            
            // Force refresh references
            if (hud.HasMethod("ForceRefreshReferences"))
            {
                hud.Call("ForceRefreshReferences");
            }
        }
        else
        {
            GD.PrintErr("❌ HUD not found in MainScene! Please add it manually.");
        }
    }
}