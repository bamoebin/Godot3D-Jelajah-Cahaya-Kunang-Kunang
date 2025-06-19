using Godot;
using System;

public partial class KunangKunang : CharacterBody3D
{
	[Export] public float MoveSpeed = 2.0f;
	[Export] public float ChangeDirectionTime = 3.0f;
	[Export] public float MinFlightHeight = 0.5f;
	[Export] public float MaxFlightHeight = 3.0f;
	[Export] public float EnergyValue = 10.0f;
	[Export] public bool UseBothLights = false; // PERBAIKAN: Default false untuk menghindari dobel cahaya
	
	// PERBAIKAN: Nilai cahaya yang realistis untuk kunang-kunang
	[Export] public float LightEnergyMin = 0.05f;  // Sangat rendah
	[Export] public float LightEnergyMax = 0.15f;  // Masih rendah
	[Export] public float LightPulsateSpeed = 1.0f; // Kecepatan pulsasi
	[Export] public float LightRange = 1.5f;       // Jangkauan pendek
	[Export] public float LightAttenuation = 3.0f; // Atenuasi tinggi untuk transisi halus
	[Export] public Color LightColor = new Color(0.7f, 1.0f, 0.5f, 1.0f); // Hijau kekuningan lembut

	// Referensi ke node
	private OmniLight3D _light1;
	private OmniLight3D _light2;
	private Area3D _area;
	private Node3D _spawner;

	// Variabel gerakan
	private Vector3 _targetPosition;
	private Vector3 _spawnAreaSize;
	private Vector3 _spawnAreaCenter;
	private float _directionTimer = 0.0f;
	private bool _isCollected = false;

	[Export] public float LightCullingDistance = 15.0f; // Jarak maksimal light aktif
	[Export] public float ProcessCullingDistance = 25.0f; // Jarak maksimal processing aktif
	[Export] public float UpdateInterval = 0.1f; // Update setiap 100ms, bukan setiap frame
	
	//Optimasi untuk performa
	private PlayerController _player;
	private float _updateTimer = 0.0f;
	private bool _isLightActive = true;
	private bool _isProcessingActive = true;
	private float _lastDistanceCheck = 0.0f;

	public override void _Ready()
	{
		// Setup referensi node
		_light1 = GetNodeOrNull<OmniLight3D>("OmniLight3D");
		_light2 = GetNodeOrNull<OmniLight3D>("OmniLight3D2");
		_area = GetNode<Area3D>("Area3D");

		_player = GetTree().GetFirstNodeInGroup("BocahJawa1") as PlayerController;
		_updateTimer = (float)GD.RandRange(0.0f, UpdateInterval);


		// PERBAIKAN: Setup cahaya kunang-kunang yang realistis
		SetupFireflyLight(_light1);
		if (UseBothLights && _light2 != null)
		{
			SetupFireflyLight(_light2);
		}

		// Setup collision detection
		if (_area != null)
		{
			_area.BodyEntered += OnBodyEntered;
		}

		// Setup spawner reference
		_spawner = GetParent() as Node3D;
		if (_spawner is KunangSpawner spawner)
		{
			_spawnAreaSize = spawner.SpawnAreaSize;
			_spawnAreaCenter = spawner.GlobalPosition;
		}

		// Set target awal
		GetNewRandomTarget();
	}

	// TAMBAHAN: Method untuk setup cahaya kunang-kunang
	private void SetupFireflyLight(OmniLight3D light)
	{
		if (light == null) return;

		// PERBAIKAN: Pengaturan cahaya yang realistis
		light.LightEnergy = LightEnergyMin;
		light.OmniRange = LightRange;
		light.OmniAttenuation = LightAttenuation;
		light.LightColor = LightColor;
		
		// PERBAIKAN: Pengaturan tambahan untuk menghindari ledakan cahaya
		light.LightSpecular = 0.1f; // Kurangi specular reflection
		light.ShadowEnabled = false; // Nonaktifkan shadow untuk performa
	}

	public override void _Process(double delta)
	{
		if (_isCollected) return;

		// OPTIMASI: Update dengan interval, bukan setiap frame
		_updateTimer += (float)delta;
		if (_updateTimer >= UpdateInterval)
		{
			_updateTimer = 0.0f;
			UpdateLOD();
		}

		// OPTIMASI: Hanya update glow jika light aktif dan dekat
		if (_isLightActive && _isProcessingActive)
		{
			UpdateFireflyGlow(delta);
		}
	}
	
	// TAMBAHAN: LOD System untuk optimasi
	private void UpdateLOD()
	{
		if (_player == null) return;

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
		_lastDistanceCheck = distance;

		// OPTIMASI: Light culling berdasarkan jarak
		bool shouldLightBeActive = distance <= LightCullingDistance;
		if (_isLightActive != shouldLightBeActive)
		{
			_isLightActive = shouldLightBeActive;
			SetLightEnabled(_isLightActive);
		}

		// OPTIMASI: Processing culling berdasarkan jarak
		bool shouldProcessingBeActive = distance <= ProcessCullingDistance;
		if (_isProcessingActive != shouldProcessingBeActive)
		{
			_isProcessingActive = shouldProcessingBeActive;
			SetProcessMode(shouldProcessingBeActive ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled);
		}
	}

private void SetLightEnabled(bool enabled)
	{
		if (_light1 != null)
		{
			_light1.LightEnergy = enabled ? LightEnergyMin : 0.0f;
		}
		if (_light2 != null && UseBothLights)
		{
			_light2.LightEnergy = enabled ? LightEnergyMin : 0.0f;
		}
	}

	// TAMBAHAN: Method terpisah untuk update glow effect
	private void UpdateFireflyGlow(double delta)
	{
		// PERBAIKAN: Pulsasi yang lebih halus dan natural
		float time = (float)Time.GetTicksMsec() / 1000.0f * LightPulsateSpeed;

		// Gunakan sin wave yang lebih halus dengan smoothstep
		float rawPulsate = (Mathf.Sin(time) + 1.0f) / 2.0f; // 0 to 1
		float smoothPulsate = Mathf.SmoothStep(0.2f, 0.8f, rawPulsate); // Batas pulsasi lebih sempit

		float lightEnergy = Mathf.Lerp(LightEnergyMin, LightEnergyMax, smoothPulsate);

		// Apply ke cahaya
		if (_light1 != null)
		{
			_light1.LightEnergy = lightEnergy;
		}

		if (_light2 != null && UseBothLights)
		{
			_light2.LightEnergy = lightEnergy;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isCollected || !_isProcessingActive)
			return;

		float deltaFloat = (float)delta;

		// Update timer pergantian arah
		_directionTimer += deltaFloat;
		if (_directionTimer >= ChangeDirectionTime)
		{
			GetNewRandomTarget();
			_directionTimer = 0;
		}

		// Arahkan ke posisi target
		Vector3 direction = (_targetPosition - GlobalPosition).Normalized();

		// Gerakkan kunang-kunang
		Vector3 velocity = direction * MoveSpeed;
		Velocity = velocity;

		// Buat kunang-kunang menghadap arah gerak
		if (velocity.Length() > 0.1f)
		{
			LookAt(GlobalPosition + velocity);
		}

		MoveAndSlide();

		// Cek jika keluar dari batas area, paksa kembali
		EnforceAreaBoundary();
	}

	private void GetNewRandomTarget()
	{
		Vector3 halfSize = _spawnAreaSize / 2;
		
		float randomX = (float)GD.RandRange(-halfSize.X, halfSize.X);
		float randomZ = (float)GD.RandRange(-halfSize.Z, halfSize.Z);
		float randomY = (float)GD.RandRange(MinFlightHeight, MaxFlightHeight);
		
		_targetPosition = _spawnAreaCenter + new Vector3(randomX, randomY, randomZ);
	}

	private void EnforceAreaBoundary()
	{
		Vector3 halfSize = _spawnAreaSize / 2;
		Vector3 localPos = GlobalPosition - _spawnAreaCenter;

		// Cek batas X
		if (Mathf.Abs(localPos.X) > halfSize.X)
		{
			localPos.X = halfSize.X * Mathf.Sign(localPos.X);
			GetNewRandomTarget();
		}

		// Cek batas Y
		if (localPos.Y < MinFlightHeight || localPos.Y > MaxFlightHeight)
		{
			localPos.Y = Mathf.Clamp(localPos.Y, MinFlightHeight, MaxFlightHeight);
			GetNewRandomTarget();
		}

		// Cek batas Z
		if (Mathf.Abs(localPos.Z) > halfSize.Z)
		{
			localPos.Z = halfSize.Z * Mathf.Sign(localPos.Z);
			GetNewRandomTarget();
		}

		GlobalPosition = _spawnAreaCenter + localPos;
	}

	private void OnBodyEntered(Node3D body)
	{
		// Cek jika player yang masuk
		if (body is PlayerController player)
		{
			// Jika player memiliki lentera
			Lentera lentera = player.GetLentera();
			if (lentera != null && !_isCollected)
			{
				// Tambahkan energi ke lentera
				lentera.AddEnergy(EnergyValue);

				// Efek kunang-kunang tertangkap
				_isCollected = true;

				// PERBAIKAN: Matikan cahaya sebelum animasi menghilang
				if (_light1 != null) _light1.LightEnergy = 0;
				if (_light2 != null) _light2.LightEnergy = 0;

				// Animasi menghilang
				var tween = CreateTween();
				tween.TweenProperty(this, "scale", Vector3.Zero, 0.3f);
				tween.TweenCallback(Callable.From(QueueFree));
			}
			else
			{
				GD.Print("Lentera not found or kunang-kunang already collected!");
			}
		}
	}
}
