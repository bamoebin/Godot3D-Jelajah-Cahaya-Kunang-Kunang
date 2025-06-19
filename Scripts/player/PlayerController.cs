using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	[Export] public float Speed = 5.0f;
	[Export] public float JumpStrength = 4.5f;
	[Export] public float Gravity = 9.8f;
	[Export] public float RotationSpeed = 10.0f;
	[Export] public float IdleAnimationSwitchTime = 17.0f;

	//Lentera
	private Lentera _lentera;

	// Tambahkan variabel untuk referensi kamera dan CameraRig
	private Node3D _cameraRig;
	private Camera3D _camera;

	// Variabel untuk animasi lompat
	private bool _isJumpingAnimating = false;
	private float _jumpAnimationTimer = 0.0f;
	private const float JUMP_ANIMATION_DELAY = 0.3f; // Delay sebelum lompat fisik
	private bool _isInAir = false;
	private bool _wasOnFloor = true; // Tracking untuk mendeteksi landing

	// Variabel untuk fase animasi lompat
	private enum JumpPhase { None, TakeOff, MidAir, Landing }
	private JumpPhase _currentJumpPhase = JumpPhase.None;
	private float _jumpAnimationDuration = 0.0f; // Durasi animasi lompat
	private float _airTime = 0.0f; // Waktu di udara

	private AnimationPlayer _animationPlayer;
	private string _currentAnimation = "bocahJawa_animpack/idle1";
	private float _idleTimer = 0.0f;
	private bool _isIdle = true;
	private int _currentIdleAnimation = 1;
	private Vector3 _targetDirection = Vector3.Forward;

	private bool _inputEnabled = true;


	//interaksi dengan barong
	private bool _isDefeated = false;

	public override void _Ready()
	{
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		if (_animationPlayer == null)
		{
			GD.Print("ERROR: AnimationPlayer tidak ditemukan!");
		}
		else
		{
			// Dapatkan durasi animasi jump
			if (_animationPlayer.HasAnimation("bocahJawa_animpack/jump"))
			{
				_jumpAnimationDuration = _animationPlayer.GetAnimation("bocahJawa_animpack/jump").Length;
			}
		}

		_lentera = GetNodeOrNull<Lentera>("Armature/GeneralSkeleton/BoneAttachment3D/lentera");
		_cameraRig = GetNode<Node3D>("CameraRig");
		_camera = GetNodeOrNull<Camera3D>("CameraRig/SpringArm3D/Camera3D");

		// Debug untuk memastikan lentera ditemukan
		if (_lentera == null)
		{
			// Coba path alternatif atau pencarian rekursif
			_lentera = FindLenteraRecursive(this);
		}

		// Register with UIManager
		AddToGroup("player");

		// Set initial respawn position
		var uiManager = GetTree().GetFirstNodeInGroup("ui_manager") as UIManager;
		if (uiManager != null)
		{
			uiManager.SetPlayerRespawnPosition(GlobalPosition);
			uiManager.GamePaused += OnGamePaused;
			uiManager.SceneChanged += OnSceneChanged;
		}

	}

	private void OnGamePaused(bool paused)
	{
		_inputEnabled = !paused;
		GD.Print($"Player input {(paused ? "DISABLED" : "ENABLED")}");
	}
	
	private void OnSceneChanged(string sceneName)
	{
		// Reset player state when scene changes
		if (sceneName.Contains("MainScene"))
		{
			_inputEnabled = true;
			GD.Print("Player input enabled for game scene");
		}
		else
		{
			_inputEnabled = false;
			GD.Print("Player input disabled for non-game scene");
		}
	}

	// Method helper untuk mencari lentera secara rekursif
	private Lentera FindLenteraRecursive(Node node)
	{
		if (node is Lentera lentera)
			return lentera;

		foreach (Node child in node.GetChildren())
		{
			var result = FindLenteraRecursive(child);
			if (result != null)
				return result;
		}
		return null;
	}

	public override void _PhysicsProcess(double delta)
	{
		float deltaFloat = (float)delta;

		// Only process movement if input is enabled
		if (!_inputEnabled) return;

		if (_isDefeated)
		{
			return;
		}// Jangan process movement jika defeated
		 // Simpan status lantai sebelumnya untuk mendeteksi landing


		_wasOnFloor = IsOnFloor();

		HandleGravity(deltaFloat);
		HandleJump(deltaFloat);
		HandleMovement(deltaFloat);
		UpdateTargetDirectionFromCursor();

		// HandleIdleAnimations hanya dipanggil jika tidak sedang melompat
		if (_currentJumpPhase == JumpPhase.None)
		{
			HandleIdleAnimations(deltaFloat);
		}

		MoveAndSlide();

		// Deteksi landing setelah MoveAndSlide
		DetectLanding(_wasOnFloor);
	}

	// Metode baru untuk mendeteksi landing
	private void DetectLanding(bool wasOnFloor)
	{
		// Jika sebelumnya tidak di lantai, tapi sekarang di lantai = landing
		if (!wasOnFloor && IsOnFloor() && _currentJumpPhase == JumpPhase.MidAir)
		{
			_currentJumpPhase = JumpPhase.Landing;
			_airTime = 0.0f;

			// Reset semua status jump
			_isInAir = false;
			_currentJumpPhase = JumpPhase.None;

			// Mainkan animasi idle setelah landing
			PlayAnimation($"idle{_currentIdleAnimation}");
		}
	}

	// Menghitung arah target dari posisi kursor
	private void UpdateTargetDirectionFromCursor()
	{
		// Target direction mengikuti arah kamera
		PlayerCamera cameraScript = _cameraRig?.GetNode<PlayerCamera>(".");
		if (cameraScript != null)
		{
			_targetDirection = cameraScript.GetForwardDirection();
		}
		else
		{
			_targetDirection = -GlobalTransform.Basis.Z;
			_targetDirection.Y = 0;
			_targetDirection = _targetDirection.Normalized();
		}
	}

	// Method untuk rotasi instan yang akan dipanggil dari PlayerCamera
	public void InstantRotateToCamera()
	{
		if (_cameraRig == null) return;

		// Ambil arah forward kamera saat ini
		Vector3 cameraForward = -_cameraRig.GlobalTransform.Basis.Z;
		cameraForward.Y = 0;
		cameraForward = cameraForward.Normalized();

		// Langsung hadapkan karakter ke arah kamera
		if (cameraForward != Vector3.Zero)
		{
			float targetAngle = Mathf.Atan2(cameraForward.X, cameraForward.Z);
			Rotation = new Vector3(0, targetAngle, 0);

			// Update juga target direction
			_targetDirection = cameraForward;
		}
	}

	private void HandleGravity(float delta)
	{
		if (!IsOnFloor())
		{
			Velocity = new Vector3(Velocity.X, Velocity.Y - Gravity * delta, Velocity.Z);

			// Tandai karakter sedang di udara
			if (!_isInAir && _currentJumpPhase != JumpPhase.TakeOff)
			{
				_isInAir = true;
				_airTime = 0.0f;

				// Jika jatuh (bukan melompat), set fase ke MidAir dan mainkan animasi jump
				if (_currentJumpPhase == JumpPhase.None)
				{
					_currentJumpPhase = JumpPhase.MidAir;
					// Penting: mainkan animasi jump saat jatuh dari ketinggian
					PlayAnimation("jump");
				}
			}
		}

		// Update air time jika sedang di udara
		if (_isInAir)
		{
			_airTime += delta;
		}
	}

	private void HandleJump(float delta)
	{
		// Mulai animasi jump jika tombol ditekan dan karakter di tanah
		if (Input.IsActionJustPressed("jump") && IsOnFloor() && _currentJumpPhase == JumpPhase.None)
		{
			_isJumpingAnimating = true;
			_jumpAnimationTimer = 0.0f;
			_currentJumpPhase = JumpPhase.TakeOff;
			PlayAnimation("jump");
			_isIdle = false;
			return;
		}

		// Jika sedang dalam animasi takeoff, tunggu delay
		if (_currentJumpPhase == JumpPhase.TakeOff)
		{
			_jumpAnimationTimer += delta;

			// Setelah delay tercapai, lakukan lompatan fisik
			if (_jumpAnimationTimer >= JUMP_ANIMATION_DELAY)
			{
				Velocity = new Vector3(Velocity.X, JumpStrength, Velocity.Z);
				_currentJumpPhase = JumpPhase.MidAir;
				_isInAir = true;
				_airTime = 0.0f;
			}
		}

		// Handle animasi saat di udara - PERBAIKAN UTAMA
		if (_currentJumpPhase == JumpPhase.MidAir)
		{
			// Tentukan posisi ideal untuk pause (antara 0.5 - 0.7 biasanya bagian puncak lompatan)
			float idealPausePoint = _jumpAnimationDuration * 0.6f;

			// Jika animasi sudah mencapai atau melewati posisi ideal
			if (_animationPlayer.IsPlaying() && (float)_animationPlayer.CurrentAnimationPosition >= idealPausePoint)
			{
				// Pause di posisi ideal
				_animationPlayer.Pause();
			}

			// Jika animasi mendekati akhir atau sudah selesai, ulangi dan segera pause
			if (!_animationPlayer.IsPlaying() ||
				(float)_animationPlayer.CurrentAnimationPosition >= _jumpAnimationDuration * 0.9f)
			{
				// Mainkan ulang animasi jump dan segera seek ke posisi ideal
				_animationPlayer.Play("bocahJawa_animpack/jump");
				_animationPlayer.Seek(idealPausePoint);
				_animationPlayer.Pause();
			}
		}
	}

	private void HandleMovement(float delta)
	{
		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

		// PERBAIKAN: HAPUS SyncPlayerWithCamera dari sini untuk menghindari circular dependency
		// SyncPlayerWithCamera(); // HAPUS INI

		// PERBAIKAN: Gunakan arah KAMERA untuk movement
		PlayerCamera cameraScript = _cameraRig?.GetNode<PlayerCamera>(".");
		Vector3 cameraForward = Vector3.Zero;
		Vector3 cameraRight = Vector3.Zero;

		if (cameraScript != null)
		{
			// Gunakan basis kamera untuk movement direction
			Basis cameraBasis = _cameraRig.GlobalTransform.Basis;
			cameraForward = -cameraBasis.Z;  // Arah depan kamera
			cameraRight = cameraBasis.X;     // Arah kanan kamera

			// Proyeksikan ke bidang horizontal
			cameraForward.Y = 0;
			cameraRight.Y = 0;
			cameraForward = cameraForward.Normalized();
			cameraRight = cameraRight.Normalized();
		}
		else
		{
			// Fallback jika kamera tidak ditemukan
			Basis playerBasis = GlobalTransform.Basis;
			cameraForward = -playerBasis.Z;
			cameraRight = playerBasis.X;
			cameraForward.Y = 0;
			cameraRight.Y = 0;
			cameraForward = cameraForward.Normalized();
			cameraRight = cameraRight.Normalized();
		}

		// Movement berdasarkan arah kamera
		Vector3 moveDirection = Vector3.Zero;

		// Direct mapping berdasarkan arah kamera
		moveDirection += cameraForward * (-inputDirection.Y);  // W/S berdasarkan arah kamera
		moveDirection += cameraRight * inputDirection.X;       // A/D berdasarkan arah kamera

		if (moveDirection.Length() > 0)
		{
			moveDirection = moveDirection.Normalized();
			_isIdle = false;

			Velocity = new Vector3(moveDirection.X * Speed, Velocity.Y, moveDirection.Z * Speed);

			// Animasi berdasarkan input direction
			if (IsOnFloor() && _currentJumpPhase == JumpPhase.None)
			{
				if (inputDirection.Y < 0) PlayAnimation("walk_forward", true);   // W
				else if (inputDirection.Y > 0) PlayAnimation("walk_backward", true);  // S
				else if (inputDirection.X < 0) PlayAnimation("walk_left", true);      // A
				else if (inputDirection.X > 0) PlayAnimation("walk_right", true);     // D
			}
		}
		else
		{
			Velocity = new Vector3(0, Velocity.Y, 0);
			_isIdle = true;
		}
	}



	private void HandleIdleAnimations(float delta)
	{
		// Hanya jalankan idle animations jika di lantai dan tidak sedang dalam fase lompat
		if (_isIdle && IsOnFloor() && _currentJumpPhase == JumpPhase.None)
		{
			_idleTimer += delta;

			if (_idleTimer >= IdleAnimationSwitchTime)
			{
				_idleTimer = 0;
				_currentIdleAnimation = _currentIdleAnimation == 1 ? 2 : 1;
				PlayAnimation($"idle{_currentIdleAnimation}");
			}
			else if (_currentAnimation != $"bocahJawa_animpack/idle{_currentIdleAnimation}")
			{
				PlayAnimation($"idle{_currentIdleAnimation}");
			}
		}
		else
		{
			_idleTimer = 0;
		}
	}

	private void PlayAnimation(string animName, bool loop = false)
	{
		// Tambahkan prefix library ke nama animasi
		string fullAnimName = "bocahJawa_animpack/" + animName;

		// Mainkan animasi jika berbeda dari yang sedang diputar
		// atau jika kita ingin melakukan loop animasi yang sama
		if (_currentAnimation != fullAnimName || loop)
		{
			if (_animationPlayer.HasAnimation(fullAnimName))
			{
				_animationPlayer.Play(fullAnimName);
				_currentAnimation = fullAnimName;
			}
			else
			{
				GD.Print($"ERROR: Animasi '{fullAnimName}' tidak ditemukan!");
			}
		}
	}

	public void SyncToCameraY(float cameraY)
	{
		// Hanya update Y, X dan Z tetap
		Rotation = new Vector3(0, cameraY, 0);
	}

	//kontol
	public void RotatePlayerWithMouse(float deltaX)
	{
		// PERBAIKAN: Langsung rotate player tanpa kompleksitas tambahan
		float currentY = Rotation.Y;
		currentY += deltaX;
		Rotation = new Vector3(0, currentY, 0);
	}

	public override void _Input(InputEvent @event)
	{
		 if (!_inputEnabled) return;

		// Toggle kontrol mouse dengan tombol ESC
		if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Escape)
		{
			// Dapatkan referensi ke PlayerCamera
			PlayerCamera cameraScript = _cameraRig.GetNode<PlayerCamera>(".");
			if (cameraScript != null)
			{
				cameraScript.ToggleMouseMode();
			}
		}

		// TAMBAHAN: Reset camera ke belakang player dengan tombol C
		if (@event is InputEventKey cKey && cKey.Pressed && cKey.Keycode == Key.C)
		{
			PlayerCamera cameraScript = _cameraRig.GetNode<PlayerCamera>(".");
			if (cameraScript != null)
			{
				cameraScript.ResetCameraBehindPlayer();
			}
		}

	}

	// Tambahkan method untuk mengakses lentera dari script lain
	public Lentera GetLentera()
	{
		return _lentera;
	}

	//Interaksi dengan Barong
	public void TriggerDefeatedAnimation()
	{
		if (!_isDefeated)
		{
			_isDefeated = true;

			// Stop semua movement
			Velocity = Vector3.Zero;

			// Mainkan animasi defeated
			PlayAnimation("defeated");

			// Disable input sementara (opsional)
			// Atau trigger game over logic

			// Reset setelah beberapa detik (opsional)
			GetTree().CreateTimer(3.0f).Timeout += ResetPlayer;
		}

	}

	private void ResetPlayer()
	{
		_isDefeated = false;
		_currentJumpPhase = JumpPhase.None;
		PlayAnimation("idle1");
	}

	public void OnPlayerDefeated()
	{
		GD.Print("Player has been defeated!");

		var uiManager = GetTree().GetFirstNodeInGroup("ui_manager") as UIManager;
		if (uiManager != null)
		{
			uiManager.ShowDefeat();
		}
	}
}
