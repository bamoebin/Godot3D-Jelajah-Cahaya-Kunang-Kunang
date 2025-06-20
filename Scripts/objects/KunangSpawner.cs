using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class KunangSpawner : Node3D
{
	[Export] public PackedScene KunangScene;
	[Export] public int SpawnCount = 10;
	[Export] public Vector3 SpawnAreaSize = new Vector3(10, 4, 10);
	[Export] public float MinHeight = 0.5f;
	[Export] public float MaxHeight = 3.0f;
	[Export] public bool ShowDebugArea = true;

	// OPTIMASI: Tambahan untuk performance
	[Export] public bool UseOptimization = true; // Toggle untuk enable/disable optimasi
	[Export] public int MaxActiveCount = 5; // Maksimal aktif bersamaan
	[Export] public float SpawnRadius = 20.0f; // Radius spawn dari player

	// Untuk debugging
	private MeshInstance3D _debugMesh;

	// OPTIMASI: Pool system (hanya jika UseOptimization = true)
	private List<KunangKunang> _kunangPool = new();
	private List<KunangKunang> _activeKunang = new();
	private PlayerController _player;
	private Timer _spawnTimer;

	public override void _Ready()
	{
		AddToGroup("kunang_spawners");

		if (UseOptimization)
		{
			// PERBAIKAN: Setup optimized spawning
			SetupOptimizedSpawning();
		}
		else
		{
			// FALLBACK: Gunakan method spawning lama yang pasti jalan
			SpawnKunangKunang();
		}
	}

	// Method untuk HUD tracking
	// Method untuk HUD tracking (pastikan ada)
public int GetActiveKunangCount()
{
	return UseOptimization ? _activeKunang.Count : 
		   GetChildren().Cast<Node>().OfType<KunangKunang>().Count();
}

public int GetTotalKunangCount()
{
	return UseOptimization ? (_activeKunang.Count + _kunangPool.Count) : SpawnCount;
}

	// TAMBAHAN: Method untuk setup optimized spawning
	private void SetupOptimizedSpawning()
	{
		// PERBAIKAN: Cari player dengan berbagai kemungkinan group name
		_player = FindPlayer();

		if (_player == null)
		{
			GD.PrintErr("Player tidak ditemukan! Menggunakan spawning biasa...");
			SpawnKunangKunang();
			return;
		}

		GD.Print($"Player ditemukan: {_player.Name}");

		// OPTIMASI: Buat pool object
		CreateKunangPool();

		// OPTIMASI: Setup timer untuk update spawning
		_spawnTimer = new Timer();
		_spawnTimer.WaitTime = 2.0f; // Check setiap 2 detik
		_spawnTimer.Timeout += UpdateKunangSpawning;
		_spawnTimer.Autostart = true;
		AddChild(_spawnTimer);

		GD.Print($"Optimized spawning setup complete for {Name}");
	}

	// PERBAIKAN: Method untuk mencari player dengan berbagai kemungkinan
	private PlayerController FindPlayer()
	{
		// Coba berbagai kemungkinan group name
		string[] possibleGroups = { "player", "Player", "BocahJawa", "BocahJawa1", "PlayerController" };

		foreach (string groupName in possibleGroups)
		{
			var player = GetTree().GetFirstNodeInGroup(groupName) as PlayerController;
			if (player != null)
			{
				GD.Print($"Player found in group: {groupName}");
				return player;
			}
		}

		// Jika tidak ada group, cari berdasarkan type
		var allNodes = GetTree().GetNodesInGroup("player");
		if (allNodes.Count == 0)
		{
			// Cari manual di scene tree
			return FindPlayerInScene(GetTree().CurrentScene);
		}

		return allNodes[0] as PlayerController;
	}

	// TAMBAHAN: Cari player manual di scene tree
	private PlayerController FindPlayerInScene(Node node)
	{
		if (node is PlayerController player)
			return player;

		foreach (Node child in node.GetChildren())
		{
			var result = FindPlayerInScene(child);
			if (result != null)
				return result;
		}

		return null;
	}

	private void CreateKunangPool()
	{
		if (KunangScene == null)
		{
			GD.PrintErr("KunangScene belum di-assign!");
			return;
		}

		// OPTIMASI: Buat pool object yang bisa digunakan kembali
		for (int i = 0; i < SpawnCount; i++)
		{
			var kunang = KunangScene.Instantiate<KunangKunang>();
			kunang.SetProcessMode(Node.ProcessModeEnum.Disabled);
			kunang.Visible = false;
			AddChild(kunang);
			_kunangPool.Add(kunang);
		}

		GD.Print($"Created kunang pool with {_kunangPool.Count} objects");
	}

	private void UpdateKunangSpawning()
	{
		if (_player == null)
		{
			GD.Print("Player reference lost, trying to find again...");
			_player = FindPlayer();
			if (_player == null) return;
		}

		float distanceToPlayer = GlobalPosition.DistanceTo(_player.GlobalPosition);

		// Debug info
		// GD.Print($"Distance to player: {distanceToPlayer}, Spawn radius: {SpawnRadius}");

		// OPTIMASI: Hanya spawn jika player dekat
		if (distanceToPlayer <= SpawnRadius)
		{
			SpawnNearbyKunang();
		}
		else
		{
			DespawnFarKunang();
		}
	}

	private void SpawnNearbyKunang()
	{
		int currentActive = _activeKunang.Count;
		int neededSpawn = Mathf.Min(MaxActiveCount - currentActive, _kunangPool.Count);

		if (neededSpawn > 0)
		{
			GD.Print($"Spawning {neededSpawn} kunang-kunang");
		}

		for (int i = 0; i < neededSpawn; i++)
		{
			var kunang = _kunangPool[0];
			_kunangPool.RemoveAt(0);
			_activeKunang.Add(kunang);

			// Aktifkan kunang-kunang
			kunang.SetProcessMode(Node.ProcessModeEnum.Inherit);
			kunang.Visible = true;

			// Posisikan secara acak
			Vector3 randomPos = GetRandomPosition();
			kunang.GlobalPosition = randomPos;
		}
	}

	private void DespawnFarKunang()
	{
		if (_activeKunang.Count > 0)
		{
			GD.Print($"Despawning {_activeKunang.Count} kunang-kunang (player too far)");
		}

		// Nonaktifkan semua kunang-kunang yang aktif
		foreach (var kunang in _activeKunang)
		{
			kunang.SetProcessMode(Node.ProcessModeEnum.Disabled);
			kunang.Visible = false;
			_kunangPool.Add(kunang);
		}
		_activeKunang.Clear();
	}

	private Vector3 GetRandomPosition()
	{
		float halfWidth = SpawnAreaSize.X / 2;
		float halfLength = SpawnAreaSize.Z / 2;

		float randomX = (float)GD.RandRange(-halfWidth, halfWidth);
		float randomZ = (float)GD.RandRange(-halfLength, halfLength);
		float randomY = (float)GD.RandRange(MinHeight, MaxHeight);

		return GlobalPosition + new Vector3(randomX, randomY, randomZ);
	}

	// FALLBACK: Method spawning lama yang pasti jalan
	public void SpawnKunangKunang()
	{
		if (KunangScene == null)
		{
			GD.PrintErr("KunangScene belum di-assign!");
			return;
		}

		GD.Print($"Spawning {SpawnCount} kunang-kunang using old method");

		for (int i = 0; i < SpawnCount; i++)
		{
			// Instance kunang-kunang
			var kunang = KunangScene.Instantiate<KunangKunang>();
			AddChild(kunang);

			// Posisikan secara acak dalam area
			Vector3 randomPos = GetRandomPosition();
			kunang.GlobalPosition = randomPos;
		}
	}





}
