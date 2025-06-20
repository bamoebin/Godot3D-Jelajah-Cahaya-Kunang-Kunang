// Scripts/managers/UIManager.cs - PERBAIKAN LENGKAP
using Godot;

public partial class UIManager : Node
{
    // Scene paths
    private const string MAIN_MENU_SCENE = "res://Scenes/UI/MainMenu.tscn";
    private const string MAIN_GAME_SCENE = "res://Scenes/MainScene/MainScene.tscn";
    private const string LOADING_SCENE = "res://Scenes/UI/LoadingScene.tscn";

    private const string PAUSE_MENU = "res://Scenes/UI/PauseMenu.tscn";
    private const string VICTORY_SCENE = "res://Scenes/UI/VictoryScene.tscn";
    private const string DEFEATED_SCENE = "res://Scenes/UI/DefeatedScene.tscn";
    private const string GUIDE_SCENE = "res://Scenes/UI/GuideScene.tscn";
    private const string ABOUT_SCENE = "res://Scenes/UI/AboutScene.tscn";
    private const string HUD_SCENE = "res://Scenes/UI/Hud.tscn";
    

    // Cached packed scenes
    private PackedScene _pauseMenuScene;
    private PackedScene _victoryScene;
    private PackedScene _defeatedScene;
    private PackedScene _guideOverlayScene;
    private PackedScene _aboutOverlayScene;

    // UI layers
    private CanvasLayer _hudLayer;
    private CanvasLayer _overlayLayer;

    private PackedScene _hudScene;
    private Control _gameplayHUD;

    // Current states
    private Control _currentOverlay;
    private bool _isGamePaused = false;
    private Vector3 _playerRespawnPosition = Vector3.Zero;

    [Signal] public delegate void GamePausedEventHandler(bool paused);
    [Signal] public delegate void SceneChangedEventHandler(string sceneName);
    private bool _wasMouseCaptured = false;


    public override void _Ready()
    {
        AddToGroup("ui_manager");
        SetupUILayers();
        CacheUIScenes();
        SetProcessInput(true);
        SetProcessMode(ProcessModeEnum.Always);

        GD.Print("✅ UIManager initialized and ready!");
    }

    private void SetMouseModeForGame()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        GD.Print("🖱️ Mouse mode: CAPTURED (Game)");
    }

    private void SetMouseModeForUI()
    {
        _wasMouseCaptured = Input.MouseMode == Input.MouseModeEnum.Captured;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GD.Print("🖱️ Mouse mode: VISIBLE (UI)");
    }

    private void RestoreMouseMode()
    {
        if (_wasMouseCaptured)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            GD.Print("🖱️ Mouse mode: RESTORED to CAPTURED");
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            GD.Print("🖱️ Mouse mode: RESTORED to VISIBLE");
        }
    }

    private void SetupUILayers()
    {
        _hudLayer = new CanvasLayer();
        _hudLayer.Layer = 1;
        _hudLayer.Name = "HUDLayer";
        AddChild(_hudLayer);

        _overlayLayer = new CanvasLayer();
        _overlayLayer.Layer = 3;
        _overlayLayer.Name = "OverlayLayer";
        AddChild(_overlayLayer);
    }

    private void CacheUIScenes()
    {
        try
        {
            // PERBAIKAN: Cek file exists sebelum load
            if (ResourceLoader.Exists(PAUSE_MENU))
            {
                _pauseMenuScene = GD.Load<PackedScene>(PAUSE_MENU);

            }
            else
            {
                GD.PrintErr($"❌ File not found: {PAUSE_MENU}");
            }

            if (ResourceLoader.Exists(VICTORY_SCENE))
            {
                _victoryScene = GD.Load<PackedScene>(VICTORY_SCENE);

            }
            else
            {
                GD.PrintErr($"❌ File not found: {VICTORY_SCENE}");
            }

            if (ResourceLoader.Exists(DEFEATED_SCENE))
            {
                _defeatedScene = GD.Load<PackedScene>(DEFEATED_SCENE);

            }
            else
            {
                GD.PrintErr($"❌ File not found: {DEFEATED_SCENE}");
            }

            if (ResourceLoader.Exists(GUIDE_SCENE))
            {
                _guideOverlayScene = GD.Load<PackedScene>(GUIDE_SCENE);

            }

            if (ResourceLoader.Exists(ABOUT_SCENE))
            {
                _aboutOverlayScene = GD.Load<PackedScene>(ABOUT_SCENE);

            }

            // TAMBAHAN: Cache HUD scene
            if (ResourceLoader.Exists(HUD_SCENE))
            {
                _hudScene = GD.Load<PackedScene>(HUD_SCENE);
                GD.Print($"✅ HUD scene cached: {HUD_SCENE}");
            }
            else
            {
                GD.PrintErr($"❌ HUD scene not found: {HUD_SCENE}");
            }

            GD.Print("✅ UI scenes caching complete");
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"❌ Failed to cache UI scenes: {e.Message}");
        }
    }

    public void ShowGameplayHUD()
    {
        if (_hudScene == null)
        {
            GD.PrintErr("❌ HUD scene not loaded!");
            return;
        }
        
        // Remove existing HUD jika ada
        HideGameplayHUD();
        
        // Instantiate HUD
        _gameplayHUD = _hudScene.Instantiate<Control>();
        _hudLayer.AddChild(_gameplayHUD);
        
        GD.Print("✅ Gameplay HUD displayed");
    }

    public void HideGameplayHUD()
    {
        if (_gameplayHUD != null)
        {
            _gameplayHUD.QueueFree();
            _gameplayHUD = null;
            GD.Print("🚫 Gameplay HUD hidden");
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel")) // ESC key
        {
            HandleEscapeInput();
        }

        // Debug keys
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.F1: // Force unpause
                    GD.Print("🧪 F1: Force unpause");
                    ForceUnpause();
                    break;

                case Key.F2: // Force mouse visible
                    GD.Print("🧪 F2: Force mouse visible");
                    SetMouseModeForUI();
                    break;

                case Key.F3: // Force mouse captured
                    GD.Print("🧪 F3: Force mouse captured");
                    SetMouseModeForGame();
                    break;
            }
        }
    }

    public void ForceUnpause()
    {
        _isGamePaused = false;
        GetTree().Paused = false;
        CloseAllOverlays();
        SetMouseModeForGame();

        GD.Print("🔓 Force unpause complete");
    }

    private void HandleEscapeInput()
    {
        // Check if we're in main menu
        if (IsInMainMenu())
        {
            GD.Print("ESC in main menu - ignored");
            return;
        }

        // Check current state and handle accordingly
        if (_isGamePaused)
        {
            GD.Print("ESC while paused - resuming");
            ResumeGame();
        }
        else
        {
            GD.Print("ESC while playing - pausing");
            PauseGame();
        }
    }

    // ====================== SCENE TRANSITIONS ======================

    public void LoadScene(string scenePath, bool showLoading = true)
    {
        GD.Print($"🎮 Loading scene: {scenePath}");

        if (showLoading)
        {
            LoadSceneWithLoading(scenePath);
        }
        else
        {
            if (ResourceLoader.Exists(scenePath))
            {
                GetTree().ChangeSceneToFile(scenePath);
                EmitSignal(SignalName.SceneChanged, scenePath);
                
                // TAMBAHAN: Auto-show HUD untuk MainScene
                if (scenePath.Contains("MainScene"))
                {
                    // Delay untuk memastikan scene fully loaded
                    GetTree().CreateTimer(0.5f).Timeout += ShowGameplayHUD;
                }
                else
                {
                    // Hide HUD untuk scene lain
                    HideGameplayHUD();
                }
                
                GD.Print($"✅ Scene changed to: {scenePath}");
            }
            else
            {
                GD.PrintErr($"❌ Scene file not found: {scenePath}");
            }
        }
    }

    private async void LoadSceneWithLoading(string scenePath)
    {
        if (!ResourceLoader.Exists(LOADING_SCENE))
        {
            GD.PrintErr($"❌ Loading scene not found: {LOADING_SCENE}");
            LoadScene(scenePath, false); // Fallback tanpa loading
            return;
        }

        GetTree().ChangeSceneToFile(LOADING_SCENE);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        if (ResourceLoader.Exists(scenePath))
        {
            var loader = ResourceLoader.LoadThreadedRequest(scenePath);

            while (ResourceLoader.LoadThreadedGetStatus(scenePath) != ResourceLoader.ThreadLoadStatus.Loaded)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            }

            var loadedScene = ResourceLoader.LoadThreadedGet(scenePath) as PackedScene;
            GetTree().ChangeSceneToPacked(loadedScene);

            EmitSignal(SignalName.SceneChanged, scenePath);
            GD.Print($"✅ Scene loaded with loading screen: {scenePath}");
        }
        else
        {
            GD.PrintErr($"❌ Target scene not found: {scenePath}");
        }
    }

    // ====================== GAME FLOW METHODS ======================

    public void StartNewGame()
    {
        GD.Print("🎮 Starting new game");

        // Reset game state
        ResetGameState();

        // Close UI and restore game mouse mode
        CloseAllOverlays();
        SetMouseModeForGame();

        LoadScene(MAIN_GAME_SCENE, true);
    }

    public void RestartGame()
    {
        GD.Print("🔄 Restarting game");

        // CRITICAL: Full state reset
        ResetGameState();
        CloseAllOverlays();

        // Ensure game is not paused
        if (GetTree().Paused)
        {
            GetTree().Paused = false;
            GD.Print("✅ Unpaused game for restart");
        }

        // Reset mouse mode for game
        SetMouseModeForGame();

        // Reset player position if available
        var player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
        if (player != null && _playerRespawnPosition != Vector3.Zero)
        {
            player.GlobalPosition = _playerRespawnPosition;
            GD.Print($"📍 Player respawned at {_playerRespawnPosition}");
        }

        // Force reload scene
        LoadScene(MAIN_GAME_SCENE, false);
    }

    public void ReturnToMainMenu()
    {
        GD.Print("🏠 Returning to main menu");

        // Reset all states
        ResetGameState();
        CloseAllOverlays();

        // UI needs visible mouse
        SetMouseModeForUI();

        LoadScene(MAIN_MENU_SCENE, false);
    }

    public void ExitGame()
    {
        GD.Print("👋 Exiting game");
        GetTree().Quit();
    }

    // ====================== PAUSE SYSTEM ======================

    public void PauseGame()
    {
        if (_isGamePaused) return;

        _isGamePaused = true;
        GetTree().Paused = true;

        // Switch to UI mouse mode
        SetMouseModeForUI();

        ShowOverlay(_pauseMenuScene);
        EmitSignal(SignalName.GamePaused, true);

        GD.Print("⏸️ Game paused with UI mouse mode");
    }

    public void ResumeGame()
    {
        if (!_isGamePaused) return;

        _isGamePaused = false;
        GetTree().Paused = false;

        // Restore game mouse mode
        SetMouseModeForGame();

        CloseCurrentOverlay();
        EmitSignal(SignalName.GamePaused, false);

        GD.Print("▶️ Game resumed with game mouse mode");
    }

    // ====================== GAME END SCENARIOS ======================

    public void ShowVictory()
    {
        GD.Print("🏆 Victory achieved!");

        // Pause game and switch to UI mode
        GetTree().Paused = true;
        SetMouseModeForUI();

        ShowOverlay(_victoryScene);

        GD.Print("🖱️ Victory screen with visible cursor");
    }

    public void ShowGuide()
    {
        GD.Print("📖 Showing guide");
        LoadScene(GUIDE_SCENE, false);
    }

    // Method untuk Guide dari PauseMenu (overlay)
    public void ShowGuideOverlay()
    {
        GD.Print("📖 Showing guide as overlay");
        ShowOverlay(_guideOverlayScene);
    }

    public void ShowAbout()
    {
        GD.Print("ℹ️ Showing about");
        LoadScene(ABOUT_SCENE, false);
    }

    public void ShowDefeat()
    {
        GD.Print("💀 Player defeated!");

        // Pause game and switch to UI mode
        GetTree().Paused = true;
        SetMouseModeForUI();

        ShowOverlay(_defeatedScene);

        GD.Print("🖱️ Defeat screen with visible cursor");
    }

    private void ResetGameState()
    {
        // Reset pause state
        _isGamePaused = false;

        // Ensure game is not paused
        GetTree().Paused = false;

        // Clear any lingering input states
        Input.ActionRelease("ui_cancel");

        GD.Print("✅ Game state reset complete");
    }

    
    // Method untuk About dari PauseMenu (overlay)
    public void ShowAboutOverlay()
    {
        GD.Print("ℹ️ Showing about as overlay");
        ShowOverlay(_aboutOverlayScene);
    }

    // ====================== OVERLAY MANAGEMENT ======================

    private void ShowOverlay(PackedScene overlayScene)
    {
        CloseCurrentOverlay();

        if (overlayScene == null)
        {
            GD.PrintErr("❌ Overlay scene is null!");
            return;
        }

        _currentOverlay = overlayScene.Instantiate<Control>();
        _overlayLayer.AddChild(_currentOverlay);

        // Ensure mouse is visible for UI
        SetMouseModeForUI();

        ConnectOverlayButtons(_currentOverlay);
        GD.Print($"✅ Overlay shown with visible cursor: {overlayScene.ResourcePath}");
    }

    private void ConnectOverlayButtons(Control overlay)
    {
        // PERBAIKAN: Connect berdasarkan nama button yang ada di MainMenu
        ConnectButtonIfExists(overlay, "BtnPlay", new Callable(this, nameof(StartNewGame)));
        ConnectButtonIfExists(overlay, "BtnExit", new Callable(this, nameof(ExitGame)));

        // For pause and other UI
        ConnectButtonIfExists(overlay, "BtnRestart", new Callable(this, nameof(RestartGame)));
        ConnectButtonIfExists(overlay, "BtnResume", new Callable(this, nameof(ResumeGame)));
        ConnectButtonIfExists(overlay, "BtnExittoMainMenu", new Callable(this, nameof(ReturnToMainMenu)));
        ConnectButtonIfExists(overlay, "BtnGuide", new Callable(this, nameof(ShowGuideOverlay)));
        ConnectButtonIfExists(overlay, "BtnAbout", new Callable(this, nameof(ShowAboutOverlay)));

        GD.Print("🔗 Button connections attempted");
    }

    private void ConnectButtonIfExists(Node parent, string buttonName, Callable method)
    {
        var button = FindButtonRecursive(parent, buttonName);
        if (button != null)
        {
            if (!button.IsConnected(BaseButton.SignalName.Pressed, method))
            {
                button.Connect(BaseButton.SignalName.Pressed, method);
                GD.Print($"✅ Connected {buttonName} ({button.GetType().Name}) to {method.Method}");
            }
            else
            {
                GD.Print($"⚠️ {buttonName} already connected");
            }
        }
        else
        {
            GD.Print($"⚠️ Button {buttonName} not found in overlay");
        }
    }

    public void HandleBackButton()
    {
        GD.Print("🔙 Back button pressed!");
        
        // Check current overlay type and handle accordingly
        if (_currentOverlay != null)
        {
            string overlayName = _currentOverlay.Name;
            
            if (overlayName.Contains("Guide") || overlayName.Contains("About"))
            {
                // If we're in guide/about overlay, go back to pause menu
                if (_isGamePaused)
                {
                    GD.Print("🔙 Back from Guide/About to Pause Menu");
                    PauseGame(); // This will show pause menu again
                }
                else
                {
                    GD.Print("🔙 Back from Guide/About to Main Menu");
                    ReturnToMainMenu();
                }
            }
            else
            {
                // Default back behavior
                ReturnToMainMenu();
            }
        }
    }

    private BaseButton FindButtonRecursive(Node parent, string buttonName)
    {
        // PERBAIKAN: Support both Button dan TextureButton
        if (parent.Name == buttonName && parent is BaseButton button)
        {
            return button;
        }

        foreach (Node child in parent.GetChildren())
        {
            var result = FindButtonRecursive(child, buttonName);
            if (result != null)
                return result;
        }

        return null;
    }

    private void CloseCurrentOverlay()
    {
        if (_currentOverlay != null)
        {
            _currentOverlay.QueueFree();
            _currentOverlay = null;
        }
    }

    private void CloseAllOverlays()
    {
        CloseCurrentOverlay();

        foreach (Node child in _overlayLayer.GetChildren())
        {
            child.QueueFree();
        }
    }

    // ====================== UTILITY METHODS ======================

    public void SetPlayerRespawnPosition(Vector3 position)
    {
        _playerRespawnPosition = position;
        GD.Print($"📍 Player respawn position set to {position}");
    }

    public bool IsGamePaused()
    {
        return _isGamePaused;
    }

    private bool IsInMainMenu()
    {
        var currentScene = GetTree().CurrentScene;
        return currentScene != null && currentScene.SceneFilePath.Contains("MainMenu");
    }

    // ====================== STATIC ACCESS ======================

    public static UIManager Instance
    {
        get
        {
            var tree = Engine.GetSingleton("SceneTree") as SceneTree;
            return tree?.GetFirstNodeInGroup("ui_manager") as UIManager;
        }
    }
}