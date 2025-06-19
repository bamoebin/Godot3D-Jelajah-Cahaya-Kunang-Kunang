// Scripts/ui/DefeatedSceneController.cs
using Godot;

public partial class DefeatedSceneController : BaseUIController
{
    protected override void SetupButtonActions()
    {
        base.SetupButtonActions();
        
        // Common button names untuk defeat screen
        AddButtonAction("BtnTryAgain", OnTryAgainPressed);
        AddButtonAction("BtnRestart", OnRestartPressed);
        AddButtonAction("BtnMainMenu", OnMainMenuPressed);
        AddButtonAction("BtnExittoMainMenu", OnMainMenuPressed);
        AddButtonAction("BtnExit", OnExitPressed);
    }
    
    protected override void OnUIReady()
    {
        // Defeat-specific setup
        GD.Print("💀 Defeat screen displayed!");
        
        // Optional: Play defeat sound, screen effects, etc.
    }
}