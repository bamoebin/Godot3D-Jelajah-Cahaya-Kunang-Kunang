using Godot;
using System;

public partial class PlayerCamera : Node3D
{
    [Export] public float MinZoom = 2.0f;
    [Export] public float MaxZoom = 10.0f;
    [Export] public float ZoomSpeed = 2f;
    [Export] public float SmoothSpeed = 2f;
    [Export] public float MouseSensitivity = 0.003f;
    [Export] public float CameraSnapSpeed = 8.0f;

    private PlayerController _playerController;
    private float _currentZoom = 5.0f;
    private float _targetZoom = 5.0f;
    private SpringArm3D _springArm;
    private Camera3D _camera;

    private float _currentYRotation = 0.0f;
    private bool _isRightClickMode = false;
    private bool _isSnappingToPlayer = false;
    private Vector2 _screenCenter = Vector2.Zero;
    private bool _isInitialized = false;
    
    // PERBAIKAN: Simplified camera system
    private float _cameraOffsetFromPlayer = Mathf.Pi; // 180 derajat di belakang
    private float _rightClickStartRotation = 0.0f;

    public override void _Ready()
    {
        _springArm = GetNode<SpringArm3D>("SpringArm3D");
        _camera = _springArm.GetNode<Camera3D>("Camera3D");
        _springArm.SpringLength = _currentZoom;
        
        _springArm.Rotation = new Vector3(-0.15f, 0, 0);
        _playerController = GetParent<PlayerController>();
        
        // PERBAIKAN: Set posisi kamera baku (selalu di belakang karakter)
        _currentYRotation = _playerController.Rotation.Y + _cameraOffsetFromPlayer;
        Rotation = new Vector3(0, _currentYRotation, 0);
        
        Input.MouseMode = Input.MouseModeEnum.Captured;
        InitializeMousePosition();
    }

    private void InitializeMousePosition()
    {
        Rect2 viewportRect = GetViewport().GetVisibleRect();
        _screenCenter = viewportRect.Size / 2;
        Input.WarpMouse(_screenCenter);
        
        GetTree().ProcessFrame += () => {
            _isInitialized = true;
        };
    }

    public override void _Process(double delta)
    {
        // Smooth zoom
        _currentZoom = Mathf.Lerp(_currentZoom, _targetZoom, SmoothSpeed * (float)delta);
        _springArm.SpringLength = _currentZoom;
        
        // PERBAIKAN: Simplified camera behavior
        if (_isSnappingToPlayer)
        {
            // Snap ke posisi berdasarkan rotasi player saat right click dimulai
            float targetRotation = _rightClickStartRotation + _cameraOffsetFromPlayer;
            _currentYRotation = Mathf.LerpAngle(_currentYRotation, targetRotation, CameraSnapSpeed * (float)delta);
            Rotation = new Vector3(0, _currentYRotation, 0);
            
            float angleDiff = Mathf.Abs(Mathf.AngleDifference(_currentYRotation, targetRotation));
            if (angleDiff < 0.05f)
            {
                _currentYRotation = targetRotation;
                Rotation = new Vector3(0, _currentYRotation, 0);
                _isSnappingToPlayer = false;
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            HandleMouseButton(mouseEvent);
        }

        if (@event is InputEventMouseMotion mouseMotion)
        {
            HandleMouseMotionFixed(mouseMotion);
        }
    }

    private void HandleMouseButton(InputEventMouseButton mouseEvent)
    {
        if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
        {
            _targetZoom = Mathf.Clamp(_targetZoom - ZoomSpeed, MinZoom, MaxZoom);
        }
        else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
        {
            _targetZoom = Mathf.Clamp(_targetZoom + ZoomSpeed, MinZoom, MaxZoom);
        }
        else if (mouseEvent.ButtonIndex == MouseButton.Right)
        {
            if (mouseEvent.Pressed)
            {
                _isRightClickMode = true;
                _isSnappingToPlayer = false;
                
                // PERBAIKAN: Simpan rotasi player saat right click dimulai
                _rightClickStartRotation = _playerController.Rotation.Y;
            }
            else
            {
                _isRightClickMode = false;
                _isSnappingToPlayer = true;
            }
        }
    }

    private void HandleMouseMotionFixed(InputEventMouseMotion mouseMotion)
    {
        if (!_isInitialized)
            return;

        Vector2 mouseDelta = mouseMotion.Relative;
        
        if (mouseDelta.LengthSquared() < 0.1f)
            return;

        float deltaX = -mouseDelta.X * MouseSensitivity;

        if (_isRightClickMode)
        {
            // MODE RIGHT CLICK: Hanya gerakkan kamera, player tidak ikut
            _currentYRotation += deltaX;
            Rotation = new Vector3(0, _currentYRotation, 0);
        }
        else
        {
            // MODE NORMAL: Mouse mengontrol player langsung, kamera mengikuti
            if (_playerController != null && !_isSnappingToPlayer)
            {
                // PERBAIKAN: Rotate player langsung, kamera akan mengikuti di _Process
                _playerController.RotatePlayerWithMouse(deltaX);
            }
        }
    }

    public void ToggleMouseMode()
    {
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    public Vector3 GetForwardDirection()
    {
        Vector3 forward = -GlobalTransform.Basis.Z;
        forward.Y = 0;
        return forward.Normalized();
    }
    
    public bool IsInRightClickMode()
    {
        return _isRightClickMode;
    }
    
    public void ResetCameraBehindPlayer()
    {
        _currentYRotation = _playerController.Rotation.Y + _cameraOffsetFromPlayer;
        Rotation = new Vector3(0, _currentYRotation, 0);
        _isSnappingToPlayer = false;
    }
    
    public void ForceReinitialize()
    {
        _isInitialized = false;
        InitializeMousePosition();
    }
}