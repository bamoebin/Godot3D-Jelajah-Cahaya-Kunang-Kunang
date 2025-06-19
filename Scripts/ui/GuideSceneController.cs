// Scripts/ui/GuideSceneController.cs
using Godot;

public partial class GuideSceneController : BaseUIController
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
        // Guide-specific setup
        GD.Print("📖 Guide scene displayed!");
        
        // Optional: Setup guide content, scrolling, etc.
    }
}