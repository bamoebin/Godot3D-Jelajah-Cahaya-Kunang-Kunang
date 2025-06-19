using Godot;

public partial class LoadingScene : Control
{
    private AnimatedSprite2D _spinner;
    private Label _loadingLabel;
    private ProgressBar _progressBar;
    
    public override void _Ready()
    {
        _spinner = GetNodeOrNull<AnimatedSprite2D>("LoadingContainer/LoadingContent/LoadingSpinner");
        _loadingLabel = GetNodeOrNull<Label>("LoadingContainer/LoadingContent/LoadingLabel");
        _progressBar = GetNodeOrNull<ProgressBar>("LoadingContainer/LoadingContent/ProgressBar");
        
        if (_spinner != null)
        {
            _spinner.Play("spin");
        }
        
        if (_loadingLabel != null)
        {
            AnimateLoadingText();
        }
    }
    
    private async void AnimateLoadingText()
    {
        string baseText = "Loading";
        int dotCount = 0;
        
        while (IsInsideTree())
        {
            if (_loadingLabel != null)
            {
                _loadingLabel.Text = baseText + new string('.', dotCount);
                dotCount = (dotCount + 1) % 4;
            }
            
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        }
    }
}