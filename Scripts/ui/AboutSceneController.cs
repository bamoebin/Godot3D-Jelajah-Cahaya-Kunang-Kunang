// Scripts/ui/AboutSceneController.cs
using Godot;

public partial class AboutSceneController : BaseUIController
{
    protected override void SetupButtonActions()
    {
        base.SetupButtonActions();
        
        // Map button BtnBack untuk kembali ke scene sebelumnya
        AddButtonAction("BtnBack", OnBackPressed);
    }
    
    private void OnBackPressed()
    {
        // Smart back navigation
        var navManager = NavigationManager.Instance;
        if (navManager != null)
        {
            navManager.GoBack();
        }
        else
        {
            // Fallback ke MainMenu
            _uiManager?.ReturnToMainMenu();
        }
    }
    
    protected override void OnUIReady()
    {
        // About-specific setup
        GD.Print("ℹ️ About scene displayed!");
        
        // Optional: Setup about content, version info, etc.
    }
}