// Scripts/ui/MainMenuController.cs - SIMPLIFIED VERSION
using Godot;

public partial class MainMenuController : BaseUIController
{
    protected override void SetupButtonActions()
    {
        base.SetupButtonActions();
        
        // Map button names to actions
        AddButtonAction("BtnPlay", OnPlayPressed);
        AddButtonAction("BtnGuide", OnGuidePressed);
        AddButtonAction("BtnAbout", OnAboutPressed);
        AddButtonAction("BtnExit", OnExitPressed);
    }
    
    
}