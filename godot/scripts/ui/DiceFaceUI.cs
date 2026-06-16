using System;
using System.Collections.Generic;
using Godot;
using RedFiendsAndRice.Data;

namespace RedFiendsAndRice.UI;

[GlobalClass]
public partial class DiceFaceUI : Control
{
	[Export] private Label FaceIndexText = null!;
	[Export] private Label UpgradesText = null!;
	[Export] private Button FaceButton = null!;

	private int _faceIndex;
	private Action<int>? _onClickCallback;
	private readonly List<string> _appliedUpgrades = new();

	public void Initialize(int index, Action<int> clickCallback)
	{
		_faceIndex = index;
		_onClickCallback = clickCallback;

		FaceIndexText.Text = $"Face {index + 1}";
		UpdateUpgradesDisplay();

		FaceButton.Pressed += OnFaceClick;
	}

	private void OnFaceClick()
	{
		_onClickCallback?.Invoke(_faceIndex);
	}

	public void UpdateVisual(ShopItem appliedUpgrade)
	{
		_appliedUpgrades.Add(appliedUpgrade.DisplayName);
		UpdateUpgradesDisplay();
	}

	private void UpdateUpgradesDisplay()
	{
		UpgradesText.Text = _appliedUpgrades.Count == 0
			? "No upgrades"
			: string.Join("\n", _appliedUpgrades);
	}
}
