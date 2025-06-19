// Scripts/ui/PauseMenuController.cs
using Godot;

public partial class PauseMenuController : BaseUIController
{
    protected override void SetupButtonActions()
    {
        base.SetupButtonActions();

        // Pause menu specific buttons
        AddButtonAction("BtnResume", OnResumePressed);
        AddButtonAction("BtnRestart", OnRestartPressed);
        AddButtonAction("BtnExittoMainMenu", OnMainMenuPressed);
        AddButtonAction("BtnExit", OnExitPressed);
        AddButtonAction("BtnGuide", OnGuidePressed);
    }

    // TAMBAHAN: Guide handler yang proper
    private void OnGuidePressed()
    {
        GD.Print("📖 Guide button pressed from Pause Menu!");
        
        // CRITICAL: Close pause overlay first, then show guide
        if (_uiManager != null)
        {
            // Method 1: Show guide as overlay (recommended)
            _uiManager.ShowGuideOverlay();
            
            // Method 2: Alternative - Full scene transition
            // _uiManager.ResumeGame(); 
            // _uiManager.ShowGuide();
        }
    }

    protected override void OnUIReady()
    {
        // Pause-specific setup
        GD.Print("⏸️ Pause menu displayed!");
    }
}