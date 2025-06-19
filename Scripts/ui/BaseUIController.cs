// Scripts/ui/BaseUIController.cs - BASE CLASS UNTUK SEMUA UI
using Godot;
using System;
using System.Collections.Generic;

public partial class BaseUIController : Control
{
    protected UIManager _uiManager;
    protected Dictionary<string, Action> _buttonActions;
    
    public override void _Ready()
    {
        // Ensure mouse is visible when UI is shown
        if (Input.MouseMode != Input.MouseModeEnum.Visible)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            GD.Print($"🖱️ Mouse set to visible for {GetType().Name}");
        }

        // Get UIManager reference
        _uiManager = GetTree().GetFirstNodeInGroup("ui_manager") as UIManager;
        if (_uiManager == null)
        {
            GD.PrintErr($"❌ UIManager not found in {GetType().Name}!");
            return;
        }
        
        // Setup button actions
        SetupButtonActions();
        
        // Auto-connect all buttons
        ConnectAllButtons();
        
        // Call derived class setup
        OnUIReady();
        
        GD.Print($"✅ {GetType().Name} initialized successfully!");
    }
    
    protected virtual void SetupButtonActions()
    {
        _buttonActions = new Dictionary<string, Action>();
    }

    protected virtual void OnUIReady()
    {
        // Override in derived classes untuk custom setup
        // Force mouse visible untuk semua UI
        Input.MouseMode = Input.MouseModeEnum.Visible;
    }
    
    protected void AddButtonAction(string buttonName, Action action)
    {
        _buttonActions[buttonName] = action;
    }
    
    private void ConnectAllButtons()
    {
        foreach (var kvp in _buttonActions)
        {
            ConnectButtonIfExists(kvp.Key, kvp.Value);
        }
    }
    
    private void ConnectButtonIfExists(string buttonName, Action callback)
    {
        var button = FindButtonRecursive(this, buttonName);
        if (button != null)
        {
            button.Pressed += callback;
            GD.Print($"✅ Connected {buttonName} ({button.GetType().Name}) in {GetType().Name}");
        }
        else
        {
            GD.Print($"⚠️ Button {buttonName} not found in {GetType().Name}");
        }
    }
    
    private BaseButton FindButtonRecursive(Node parent, string buttonName)
    {
        if (parent.Name == buttonName && parent is BaseButton button)
            return button;
            
        foreach (Node child in parent.GetChildren())
        {
            var result = FindButtonRecursive(child, buttonName);
            if (result != null) return result;
        }
        
        return null;
    }
    
    // ====================== COMMON BUTTON ACTIONS ======================
    
    protected void OnPlayPressed()
    {
        GD.Print("🎮 Play/Start button pressed!");
        
        // Ensure clean state before starting
        if (_uiManager != null)
        {
            _uiManager.ForceUnpause();
            _uiManager.StartNewGame();
        }
    }
    
    protected void OnResumePressed()
    {
        GD.Print("▶️ Resume button pressed!");
        _uiManager?.ResumeGame();
    }
    
    protected void OnRestartPressed()
    {
        GD.Print("🔄 Restart button pressed!");
        
        // Force clean restart
        if (_uiManager != null)
        {
            _uiManager.ForceUnpause();
            _uiManager.RestartGame();
        }
    }

    protected void OnGuidePressed()
    {
        GD.Print("📖 Guide button pressed!");
        _uiManager?.ShowGuide();
    }
    
    protected void OnAboutPressed()
    {
        GD.Print("ℹ️ About button pressed!");
        _uiManager?.ShowAbout();
    }
    
    protected void OnMainMenuPressed()
    {
        GD.Print("🏠 Main Menu button pressed!");
        _uiManager?.ReturnToMainMenu();
    }
    
    protected void OnExitPressed()
    {
        GD.Print("👋 Exit button pressed!");
        _uiManager?.ExitGame();
    }
    
    protected void OnTryAgainPressed()
    {
        GD.Print("🔄 Try Again button pressed!");
        _uiManager?.RestartGame();
    }
    
    protected void OnPlayAgainPressed()
    {
        GD.Print("🔄 Play Again button pressed!");
        _uiManager?.RestartGame();
    }
    
    // ====================== DEBUGGING ======================
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.F5)
            {
                DebugButtonStatus();
            }
        }
    }
    
    private void DebugButtonStatus()
    {
        GD.Print($"=== {GetType().Name.ToUpper()} BUTTON DEBUG ===");
        GD.Print($"UIManager: {(_uiManager != null ? "✅ Found" : "❌ Missing")}");
        GD.Print($"Button Actions Count: {_buttonActions?.Count ?? 0}");
        
        if (_buttonActions != null)
        {
            foreach (var kvp in _buttonActions)
            {
                var button = FindButtonRecursive(this, kvp.Key);
                GD.Print($"{kvp.Key}: {(button != null ? "✅ Found" : "❌ Missing")}");
            }
        }
    }
}