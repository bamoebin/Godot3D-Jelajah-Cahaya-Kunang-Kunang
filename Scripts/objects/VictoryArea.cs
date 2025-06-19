using Godot;

public partial class VictoryArea : Area3D
{
    [Export] public bool ShowDebugMessage = true;
    
    private bool _hasTriggered = false;
    
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        
        if (ShowDebugMessage)
        {
            GD.Print($"Victory area ready at position: {GlobalPosition}");
        }
    }
    
    private void OnBodyEntered(Node3D body)
    {
        if (_hasTriggered) return;
        
        if (body is PlayerController player)
        {
            _hasTriggered = true;
            
            GD.Print("Player reached victory area!");
            
            // Get UIManager and show victory
            var uiManager = GetTree().GetFirstNodeInGroup("ui_manager") as UIManager;
            if (uiManager != null)
            {
                uiManager.ShowVictory();
            }
            else
            {
                GD.PrintErr("UIManager not found! Cannot show victory screen.");
            }
        }
    }
}