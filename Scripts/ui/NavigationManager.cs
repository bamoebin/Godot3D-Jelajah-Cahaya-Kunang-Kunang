// Scripts/managers/NavigationManager.cs
using Godot;
using System.Collections.Generic;

public partial class NavigationManager : Node
{
    private static NavigationManager _instance;
    public static NavigationManager Instance => _instance;
    
    private Stack<string> _sceneHistory = new Stack<string>();
    private string _currentScene = "";
    
    public override void _Ready()
    {
        _instance = this;
        AddToGroup("navigation_manager");
        
        // Listen to scene changes
        var uiManager = GetTree().GetFirstNodeInGroup("ui_manager") as UIManager;
        if (uiManager != null)
        {
            uiManager.SceneChanged += OnSceneChanged;
        }
    }
    
    private void OnSceneChanged(string scenePath)
    {
        // Add previous scene to history (if not already current)
        if (!string.IsNullOrEmpty(_currentScene) && _currentScene != scenePath)
        {
            _sceneHistory.Push(_currentScene);
            GD.Print($"📚 Added to history: {_currentScene}");
        }
        
        _currentScene = scenePath;
        GD.Print($"📍 Current scene: {_currentScene}");
    }
    
    public void GoBack()
    {
        if (_sceneHistory.Count > 0)
        {
            string previousScene = _sceneHistory.Pop();
            GD.Print($"🔙 Going back to: {previousScene}");
            
            var uiManager = GetTree().GetFirstNodeInGroup("ui_manager") as UIManager;
            uiManager?.LoadScene(previousScene, false);
        }
        else
        {
            GD.Print("🔙 No previous scene, going to MainMenu");
            
            var uiManager = GetTree().GetFirstNodeInGroup("ui_manager") as UIManager;
            uiManager?.ReturnToMainMenu();
        }
    }
    
    public void ClearHistory()
    {
        _sceneHistory.Clear();
        GD.Print("🗑️ Navigation history cleared");
    }
}