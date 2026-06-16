using Godot;

namespace RedFiendsAndRice.System;

// PORT NOTE: Unity version called `DOTween.Init()` here. Godot's built-in Tween
// system needs no init — every `Node.CreateTween()` call returns a fresh tween
// scoped to the SceneTree. If you want a global place for menu/state transitions,
// register this script as an autoload in project.godot.
//
// Original placeholders left intact for parity:
//   - Display main menu
//   - Handle main menu inputs
//   - On start new campaign
public partial class GameManager : Node
{
	public override void _Ready()
	{
	}
}
