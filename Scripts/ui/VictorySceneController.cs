// Scripts/ui/VictorySceneController.cs
using Godot;

public partial class VictorySceneController : BaseUIController
{
    protected override void SetupButtonActions()
    {
        base.SetupButtonActions();
        
        // Common button names untuk victory screen
        AddButtonAction("BtnPlayAgain", OnPlayAgainPressed);
        AddButtonAction("BtnRestart", OnRestartPressed);
        AddButtonAction("BtnMainMenu", OnMainMenuPressed);
        AddButtonAction("BtnExittoMainMenu", OnMainMenuPressed);
        AddButtonAction("BtnExit", OnExitPressed);
    }
    
    protected override void OnUIReady()
    {
        // Victory-specific setup
        GD.Print("🏆 Victory screen displayed!");
        
        // Optional: Play victory sound, animation, etc.
    }
}