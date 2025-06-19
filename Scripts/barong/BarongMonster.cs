using Godot;
using System;

public partial class BarongMonster : CharacterBody3D
{
	[Export] public float AIUpdateInterval = 0.2f; // Update AI setiap 200ms
	[Export] public float LightCullingDistance = 20.0f;
	[Export] public float MoveSpeed = 2.0f;
	[Export] public float ChangeDirectionTime = 3.0f;
	[Export] public float Gravity = 9.8f;
	[Export] public float DetectionRadius = 3.0f;
	[Export] public float IdleAnimationTime = 2.0f;
	[Export] public float SmashAnimationDuration = 2.7f;
	[Export] public float AttackCooldown = 2.0f;
	[Export] public float DefeatedTriggerDelay = 1.0f; // TAMBAHAN: Trigger defeated sebelum smash selesai
	[Export] public float RotationSpeed = 5.0f; // TAMBAHAN: Kecepatan rotasi monster
	
	// OPTIMASI: Kurangi update frequency
	
	private float _aiUpdateTimer = 0.0f;
	private PlayerController _player;
	private bool _isNearPlayer = false;

	// Referensi ke node
	private AnimationPlayer _animationPlayer;
	private Area3D _detectionArea;
	private OmniLight3D _redLight;
	private Node3D _spawner;
	
	// Variabel movement
	private Vector3 _targetPosition;
	private Vector3 _spawnAreaSize;
	private Vector3 _spawnAreaCenter;
	private float _directionTimer = 0.0f;
	private float _idleTimer = 0.0f;
	private float _attackCooldownTimer = 0.0f;
	
	// TAMBAHAN: Variabel untuk rotasi ke player
	private Vector3 _playerDirection = Vector3.Zero;
	private bool _needRotateToPlayer = false;
	
	// State management
	private enum MonsterState
	{
		Idle,
		Walking,
		RotatingToPlayer, // TAMBAHAN: State baru untuk rotasi ke player
		Attacking,
		Cooldown
	}
	private MonsterState _currentState = MonsterState.Idle;
	private bool _playerDetected = false;
	private string _currentAnimation = "";
	
	public override void _Ready()
	{
		// Dapatkan referensi node
		_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_detectionArea = GetNode<Area3D>("Area3D");
		_redLight = GetNode<OmniLight3D>("OmniLight3D");
		 _player = GetTree().GetFirstNodeInGroup("BocahJawa1") as PlayerController;
		
		// Setup cahaya merah (awalnya mati)
		if (_redLight != null)
		{
			_redLight.LightColor = new Color(1, 0, 0);
			_redLight.LightEnergy = 0.0f;
			_redLight.OmniRange = 5.0f;
		}
		
		// Connect signal detection
		if (_detectionArea != null)
		{
			_detectionArea.BodyEntered += OnPlayerDetected;
			_detectionArea.BodyExited += OnPlayerExited;
		}
		
		// Setup area spawn
		_spawner = GetParent<Node3D>();
		if (_spawner != null && _spawner is BarongSpawner spawner)
		{
			_spawnAreaSize = spawner.SpawnAreaSize;
			_spawnAreaCenter = spawner.GlobalPosition;
		}
		else
		{
			_spawnAreaSize = new Vector3(10, 3, 10);
			_spawnAreaCenter = GlobalPosition;
		}
		
		// Mulai dengan idle
		_currentState = MonsterState.Idle;
		PlayAnimation("idle", true);
		GetNewRandomTarget();
	}
	
	public override void _Process(double delta)
	{
		// OPTIMASI: Update AI dengan interval
		_aiUpdateTimer += (float)delta;
		if (_aiUpdateTimer >= AIUpdateInterval)
		{
			_aiUpdateTimer = 0.0f;
			UpdateAI();
		}
	}

	private void UpdateAI()
	{
		if (_player == null) return;

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
		bool shouldBeNear = distance <= LightCullingDistance;

		if (_isNearPlayer != shouldBeNear)
		{
			_isNearPlayer = shouldBeNear;
			
			// OPTIMASI: Aktifkan/nonaktifkan light berdasarkan jarak
			if (_redLight != null)
			{
				_redLight.LightEnergy = _isNearPlayer && _playerDetected ? 2.0f : 0.0f;
			}
			
			// OPTIMASI: Disable processing jika jauh
			SetProcessMode(_isNearPlayer ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled);
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		float deltaFloat = (float)delta;

		// Apply gravity selalu
		HandleGravity(deltaFloat);

		// Handle state berdasarkan kondisi
		switch (_currentState)
		{
			case MonsterState.Idle:
				HandleIdleState(deltaFloat);
				break;

			case MonsterState.Walking:
				HandleWalkingState(deltaFloat);
				break;

			case MonsterState.RotatingToPlayer: // TAMBAHAN: Handle rotasi ke player
				HandleRotatingToPlayer(deltaFloat);
				break;

			case MonsterState.Attacking:
				// Tidak ada movement saat attacking
				break;

			case MonsterState.Cooldown:
				HandleCooldownState(deltaFloat);
				break;
		}

		MoveAndSlide();
	}
	
	private void HandleGravity(float delta)
	{
		if (!IsOnFloor())
		{
			Velocity = new Vector3(Velocity.X, Velocity.Y - Gravity * delta, Velocity.Z);
		}
	}
	
	private void HandleIdleState(float delta)
	{
		_idleTimer += delta;
		
		if (_idleTimer >= IdleAnimationTime)
		{
			_currentState = MonsterState.Walking;
			_idleTimer = 0.0f;
			_directionTimer = 0.0f;
			PlayAnimation("walk_forward", true);
			GetNewRandomTarget();
		}
	}
	
	private void HandleWalkingState(float delta)
	{
		_directionTimer += delta;
		
		if (_directionTimer >= ChangeDirectionTime)
		{
			if (GD.Randf() < 0.3f)
			{
				_currentState = MonsterState.Idle;
				PlayAnimation("idle", true);
				Velocity = new Vector3(0, Velocity.Y, 0);
				return;
			}
			else
			{
				GetNewRandomTarget();
				_directionTimer = 0;
			}
		}
		
		// Gerakkan monster ke target
		Vector3 direction = (_targetPosition - GlobalPosition);
		direction.Y = 0;
		direction = direction.Normalized();
		
		Vector3 horizontalVelocity = direction * MoveSpeed;
		Velocity = new Vector3(horizontalVelocity.X, Velocity.Y, horizontalVelocity.Z);
		
		// Rotasi menghadap arah gerak
		if (horizontalVelocity.Length() > 0.1f)
		{
			float targetRotation = Mathf.Atan2(horizontalVelocity.X, horizontalVelocity.Z);
			Rotation = new Vector3(0, targetRotation, 0);
		}
		
		EnforceAreaBoundary();
		
		Vector3 horizontalDistance = new Vector3(
			GlobalPosition.X - _targetPosition.X,
			0,
			GlobalPosition.Z - _targetPosition.Z
		);
		
		if (horizontalDistance.Length() < 1.5f)
		{
			GetNewRandomTarget();
		}
	}
	
	// TAMBAHAN: Method untuk handle rotasi ke player
	private void HandleRotatingToPlayer(float delta)
	{
		// Stop movement
		Velocity = new Vector3(0, Velocity.Y, 0);
		
		// Hitung rotasi target ke player
		Vector3 directionToPlayer = _playerDirection;
		directionToPlayer.Y = 0;
		directionToPlayer = directionToPlayer.Normalized();
		
		float targetRotation = Mathf.Atan2(directionToPlayer.X, directionToPlayer.Z);
		float currentRotation = Rotation.Y;
		
		// Smooth rotation ke player
		float rotationDifference = Mathf.AngleDifference(currentRotation, targetRotation);
		
		if (Mathf.Abs(rotationDifference) > 0.1f)
		{
			float rotationStep = RotationSpeed * delta;
			float newRotation = currentRotation + Mathf.Sign(rotationDifference) * Mathf.Min(rotationStep, Mathf.Abs(rotationDifference));
			Rotation = new Vector3(0, newRotation, 0);
		}
		else
		{
			// Rotasi selesai, mulai attack
			Rotation = new Vector3(0, targetRotation, 0);
			StartAttackSequence();
		}
	}
	
	// TAMBAHAN: Method untuk memulai sequence attack
	private void StartAttackSequence()
	{
		_currentState = MonsterState.Attacking;
		
		// Aktifkan cahaya merah
		if (_redLight != null)
		{
			var tween = CreateTween();
			tween.TweenProperty(_redLight, "light_energy", 3.0f, 0.2f);
		}
		
		// Mainkan animasi smash
		PlayAnimation("smash", false);
		
		// PERBAIKAN: Trigger defeated SEBELUM smash selesai untuk transisi yang smooth
		GetTree().CreateTimer(DefeatedTriggerDelay).Timeout += TriggerPlayerDefeated;
		
		// Setelah smash selesai, masuk ke cooldown
		GetTree().CreateTimer(SmashAnimationDuration).Timeout += StartCooldown;
	}
	
	private void HandleCooldownState(float delta)
	{
		_attackCooldownTimer += delta;
		
		if (_attackCooldownTimer >= AttackCooldown)
		{
			_currentState = MonsterState.Idle;
			_attackCooldownTimer = 0.0f;
			_playerDetected = false;
			PlayAnimation("idle", true);
			
			if (_redLight != null)
			{
				var tween = CreateTween();
				tween.TweenProperty(_redLight, "light_energy", 0.0f, 0.5f);
			}
		}
	}
	
	private void GetNewRandomTarget()
	{
		float halfWidth = _spawnAreaSize.X / 2;
		float halfLength = _spawnAreaSize.Z / 2;
		
		float randomX = (float)GD.RandRange(-halfWidth, halfWidth);
		float randomZ = (float)GD.RandRange(-halfLength, halfLength);
		
		_targetPosition = _spawnAreaCenter + new Vector3(randomX, GlobalPosition.Y, randomZ);
	}
	
	private void EnforceAreaBoundary()
	{
		Vector3 halfSize = _spawnAreaSize / 2;
		Vector3 localPos = GlobalPosition - _spawnAreaCenter;
		
		bool needNewTarget = false;
		
		if (Mathf.Abs(localPos.X) > halfSize.X)
		{
			localPos.X = halfSize.X * Mathf.Sign(localPos.X);
			needNewTarget = true;
		}
		
		if (Mathf.Abs(localPos.Z) > halfSize.Z)
		{
			localPos.Z = halfSize.Z * Mathf.Sign(localPos.Z);
			needNewTarget = true;
		}
		
		if (needNewTarget)
		{
			GlobalPosition = new Vector3(
				_spawnAreaCenter.X + localPos.X,
				GlobalPosition.Y,
				_spawnAreaCenter.Z + localPos.Z
			);
			GetNewRandomTarget();
		}
	}
	
	private void OnPlayerDetected(Node3D body)
	{
		if (body is PlayerController player && !_playerDetected && 
			_currentState != MonsterState.Attacking && 
			_currentState != MonsterState.Cooldown &&
			_currentState != MonsterState.RotatingToPlayer)
		{
			_playerDetected = true;
			
			// PERBAIKAN: Hitung arah ke player dan mulai rotasi
			_playerDirection = (player.GlobalPosition - GlobalPosition).Normalized();
			_currentState = MonsterState.RotatingToPlayer;
			
			// Stop walking animation, play idle saat rotasi
			PlayAnimation("idle", true);
		}
	}
	
	private void OnPlayerExited(Node3D body)
	{
		if (body is PlayerController player && _currentState != MonsterState.Attacking)
		{
			_playerDetected = false;
		}
	}
	
	// PERBAIKAN: Method terpisah untuk trigger defeated
	private void TriggerPlayerDefeated()
	{
		// Cari player yang masih dalam detection area
		var bodies = _detectionArea.GetOverlappingBodies();
		
		foreach (Node3D body in bodies)
		{
			if (body is PlayerController player)
			{
				player.TriggerDefeatedAnimation();
				//menunggu sampai animasi selesai
				GetTree().CreateTimer(DefeatedTriggerDelay).Timeout += () =>
				player.OnPlayerDefeated();
				break;
			}
		}
	}
	
	// TAMBAHAN: Method untuk memulai cooldown
	private void StartCooldown()
	{
		_currentState = MonsterState.Cooldown;
		_attackCooldownTimer = 0.0f;
		PlayAnimation("idle", true);
	}
	
	private void PlayAnimation(string animName, bool loop = false)
	{
		string fullAnimName = "Barong_animpack/" + animName;
		
		if (_currentAnimation != fullAnimName)
		{
			if (_animationPlayer != null && _animationPlayer.HasAnimation(fullAnimName))
			{
				_animationPlayer.Play(fullAnimName);
				
				if (loop)
				{
					_animationPlayer.GetAnimation(fullAnimName).LoopMode = Animation.LoopModeEnum.Linear;
				}
				else
				{
					_animationPlayer.GetAnimation(fullAnimName).LoopMode = Animation.LoopModeEnum.None;
				}
				
				_currentAnimation = fullAnimName;
			}
		}
	}
	
	public void ResetMonster()
	{
		_playerDetected = false;
		_currentState = MonsterState.Idle;
		_attackCooldownTimer = 0.0f;
		PlayAnimation("idle", true);
		_idleTimer = 0.0f;
		
		if (_redLight != null)
		{
			_redLight.LightEnergy = 0.0f;
		}
	}
}
