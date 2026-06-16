using Godot;
using RedFiendsAndRice.Data;

namespace RedFiendsAndRice.System;

// PORT NOTE — UI is the biggest semantic gap in this project.
//
// Unity widgets used here    →    Godot equivalent
//   TextMeshProUGUI                Label  (or RichTextLabel for markup)
//   UnityEngine.UI.Slider          ProgressBar  (or HSlider/VSlider if interactive)
//   UnityEngine.UI.Image           TextureRect  (for arbitrary sprite)
//   GameObject panel root          Control  (or PanelContainer, VBoxContainer, etc.)
//   Image.sprite                   TextureRect.Texture
//   GameObject.SetActive(bool)     node.Visible = bool   (or node.ProcessMode)
//
// The Unity script discovered children by name via GetComponentsInChildren<T>()
// then `.Find(x => x.name.Contains(...))`. That's brittle and not idiomatic
// in Godot — prefer either:
//   1) `[Export]` each Label/ProgressBar/TextureRect directly so they show
//      up in the inspector and you wire them by drag-and-drop, OR
//   2) `GetNode<Label>("EnemyHealthPanel/HealthCounter")` with explicit paths,
//      using `% unique names` to avoid hard-coding the full tree.
//
// I went with option (1) below — explicit [Export] slots. You'll wire them in
// the editor once the UI scene is built. The fallback name-search is removed
// intentionally; if a slot is null at runtime, GD.PushError says which one.
//
// PORT NOTE — Singleton pattern.
//   The Unity `Instance` pattern works in Godot but fights the scene-tree
//   lifecycle (the static reference can point to a freed node after a scene
//   change). The idiomatic fix is to register this script as an autoload in
//   project.godot:
//
//       [autoload]
//       UIManager="*res://scripts/system/UIManager.cs"
//
//   Then access it as `UIManager.Instance` (kept here for parity) or via
//   `GetNode<UIManager>("/root/UIManager")`. Until autoload is set up, the
//   Instance getter falls back to a tree search, mirroring Unity's
//   FindFirstObjectByType.
public partial class UIManager : Control
{
    public static UIManager? Instance { get; private set; }

    [Export] public Label? GoldCounterText;

    [Export] public Label? EnemyHealthCounter;
    [Export] public Label? EnemyShieldCounter;
    [Export] public ProgressBar? EnemyHealthSlider;
    [Export] public ProgressBar? EnemyShieldSlider;

    [Export] public Label? PlayerHealthCounter;
    [Export] public Label? PlayerShieldCounter;
    [Export] public ProgressBar? PlayerHealthSlider;
    [Export] public ProgressBar? PlayerShieldSlider;

    [Export] public Label? SingleDamageAmount;
    [Export] public TextureRect? SingleDamageTypeImage;
    [Export] public Label? DoubleOneDamageAmount;
    [Export] public TextureRect? DoubleOneDamageTypeImage;
    [Export] public Label? DoubleTwoDamageAmount;
    [Export] public TextureRect? DoubleTwoDamageTypeImage;

    [Export] public Texture2D? PhysicalSymbol;
    [Export] public Texture2D? MagicSymbol;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready()
    {
        ClearEnemyAttackPanel();
    }

    public void UpdateGoldCounter(float newValue)
    {
        if (GoldCounterText == null)
        {
            GD.PushWarning("UIManager: GoldCounterText not assigned");
            return;
        }
        GoldCounterText.Text = ((int)newValue).ToString();
    }

    public void UpdateEnemyHealthValues(int newValue, int totalHealth)
        => SetCounterAndBar(EnemyHealthCounter, EnemyHealthSlider, newValue, totalHealth, "enemy health");

    public void UpdateEnemyShieldValues(int newValue, int totalShield)
        => SetCounterAndBar(EnemyShieldCounter, EnemyShieldSlider, newValue, totalShield, "enemy shield");

    public void UpdatePlayerHealthValues(int newValue, int totalHealth)
        => SetCounterAndBar(PlayerHealthCounter, PlayerHealthSlider, newValue, totalHealth, "player health");

    public void UpdatePlayerShieldValues(int newValue, int totalShield)
        => SetCounterAndBar(PlayerShieldCounter, PlayerShieldSlider, newValue, totalShield, "player shield");

    // Unity version repeated this four times with separate null-checks per widget.
    // Consolidated for the port; behavior is identical.
    private static void SetCounterAndBar(Label? counter, ProgressBar? bar, int value, int max, string label)
    {
        if (counter == null) GD.PushWarning($"UIManager: {label} counter not assigned");
        else counter.Text = value.ToString();

        if (bar == null) GD.PushWarning($"UIManager: {label} bar not assigned");
        else
        {
            bar.MaxValue = max;
            bar.Value = value;
        }
    }

    public void UpdateSingleEnemyAttackUI(EnemyAbilityType abilityType, int magnitude)
    {
        if (SingleDamageAmount == null || SingleDamageTypeImage == null) return;

        SingleDamageAmount.Text = $"{magnitude}";
        SingleDamageTypeImage.Texture = abilityType switch
        {
            EnemyAbilityType.Melee => PhysicalSymbol,
            EnemyAbilityType.Magic => MagicSymbol,
            _ => SingleDamageTypeImage.Texture,
        };
        SingleDamageAmount.Visible = true;
        SingleDamageTypeImage.Visible = true;
    }

    public void UpdateDoubleEnemyAttackUI(int physicalDamageMagnitude, int magicDamageMagnitude)
    {
        if (DoubleOneDamageAmount == null || DoubleOneDamageTypeImage == null ||
            DoubleTwoDamageAmount == null || DoubleTwoDamageTypeImage == null) return;

        DoubleOneDamageAmount.Text = $"{physicalDamageMagnitude}";
        DoubleOneDamageTypeImage.Texture = PhysicalSymbol;

        DoubleTwoDamageAmount.Text = $"{magicDamageMagnitude}";
        DoubleTwoDamageTypeImage.Texture = MagicSymbol;

        DoubleOneDamageAmount.Visible = true;
        DoubleOneDamageTypeImage.Visible = true;
        DoubleTwoDamageAmount.Visible = true;
        DoubleTwoDamageTypeImage.Visible = true;
    }

    public void ClearEnemyAttackPanel()
    {
        // PORT NOTE: Unity used `enabled = false` to hide individual UI components
        // without affecting the GameObject. Godot's nearest equivalent is `Visible`.
        // If you want the layout container to keep reserving space (Unity's behavior
        // when only `enabled` is toggled), use `Modulate.A = 0` instead and keep
        // Visible = true.
        if (SingleDamageAmount != null) SingleDamageAmount.Visible = false;
        if (SingleDamageTypeImage != null) SingleDamageTypeImage.Visible = false;
        if (DoubleOneDamageAmount != null) DoubleOneDamageAmount.Visible = false;
        if (DoubleOneDamageTypeImage != null) DoubleOneDamageTypeImage.Visible = false;
        if (DoubleTwoDamageAmount != null) DoubleTwoDamageAmount.Visible = false;
        if (DoubleTwoDamageTypeImage != null) DoubleTwoDamageTypeImage.Visible = false;
    }
}
