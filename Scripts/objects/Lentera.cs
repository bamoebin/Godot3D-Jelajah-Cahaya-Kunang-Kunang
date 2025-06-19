using Godot;
using System;

public partial class Lentera : Node3D
{
    [Export] public float MaxEnergy = 100.0f;        // Maksimal amunisi cahaya
    [Export] public float CurrentEnergy = 100.0f;    // Amunisi saat ini (mulai penuh)
    [Export] public float EnergyDecayRate = 2.0f;    // Kecepatan amunisi habis per detik
    [Export] public float MinLightEnergy = 0f;     // Cahaya minimum saat amunisi hampir habis
    [Export] public float MaxLightEnergy = 3.0f;     // Cahaya maksimum saat amunisi penuh
    [Export] public bool UseBothLights = true; // Flag untuk menggunakan kedua lampu karena memakai 2 OmniLight3D
    
    // Referensi ke node cahaya
    private OmniLight3D _light1;
       private OmniLight3D _light2;
    
    // UI untuk menampilkan energi (opsional)
    private Control _energyUI;
    private TextureProgressBar _energyBar;
    
    public override void _Ready()
    {
        _light1 = GetNode<OmniLight3D>("world/OmniLight3D");
        _light2 = GetNode<OmniLight3D>("world/OmniLight3D2");
        
        // Inisialisasi UI jika ada
        _energyUI = GetNodeOrNull<Control>("/root/Main/UI/EnergyUI");
        if (_energyUI != null)
        {
            _energyBar = _energyUI.GetNode<TextureProgressBar>("EnergyBar");
        }
        
        // Set nilai awal
        UpdateLightEnergy();
        UpdateUI();
    }
    
    public override void _Process(double delta)
    {
        // Kurangi energi seiring waktu
        CurrentEnergy = Mathf.Max(0, CurrentEnergy - EnergyDecayRate * (float)delta);
        
        // Update cahaya
        UpdateLightEnergy();
        
        // Update UI
        UpdateUI();
    }

    // Method untuk menambah energi saat menyentuh kunang-kunang
    public void AddEnergy(float amount)
    {
        CurrentEnergy = Mathf.Min(MaxEnergy, CurrentEnergy + amount);
        UpdateLightEnergy();
        UpdateUI();
    }
    
    private void UpdateLightEnergy()
    {
        // Hitung energi cahaya berdasarkan energi saat ini
        float energyRatio = CurrentEnergy / MaxEnergy;
        float newLightEnergy = Mathf.Lerp(MinLightEnergy, MaxLightEnergy, energyRatio);
        
        // Set energi cahaya untuk kedua lampu
        if (_light1 != null)
        {
            _light1.LightEnergy = newLightEnergy;
            
            // Perubahan warna opsional (dari kuning ke merah saat energi sedikit)
            if (energyRatio < 0.2f)
            {
                _light1.LightColor = new Color(1, energyRatio * 3, 0); // Jadi lebih merah saat energi sedikit
            }
            else
            {
                _light1.LightColor = new Color(1, 0.9f, 0.7f); // Warna normal
            }
        }
        
        if (_light2 != null && UseBothLights)
        {
            _light2.LightEnergy = newLightEnergy;
            
            // Perubahan warna yang sama untuk light kedua
            if (energyRatio < 0.2f)
            {
                _light2.LightColor = new Color(1, energyRatio * 3, 0);
            }
            else
            {
                _light2.LightColor = new Color(1, 0.9f, 0.7f);
            }
        }
    }
    
    private void UpdateUI()
    {
        // Update UI jika ada
        if (_energyBar != null)
        {
            _energyBar.Value = CurrentEnergy;
            _energyBar.MaxValue = MaxEnergy;
        }
    }
    
    // Getter untuk energi saat ini (untuk akses dari script lain)
    public float GetCurrentEnergy()
    {
        return CurrentEnergy;
    }
    
    // Getter untuk energi maksimum
    public float GetMaxEnergy()
    {
        return MaxEnergy;
    }
}