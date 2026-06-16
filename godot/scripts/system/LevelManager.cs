using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using RedFiendsAndRice.Data;

namespace RedFiendsAndRice.System;

// A level is a certain 'biome' with some number of enemies - you fight individual enemies as a Round, beating all the rounds in a level moves you to the next level
public partial class LevelManager : Node3D
{
  [Export] public PackedScene MultiDieScene = null!;
  [Export] public PackedScene AbilityDieScene = null!;
  [Export] public PackedScene GoldPieceScene = null!; //TODO this won't change, doesn't need to be passed everytime as context
  [Export] public PackedScene RoundManagerScene = null!;
  [Export] public PackedScene OutlinesScene = null!; //TODO this won't change, doesn't need to be passed everytime as context

  [Export] public Node3D[] AbilityDiceSpawnPoints = null!; //TODO this won't change, doesn't need to be passed everytime as context
  [Export] public Node3D[] MultiDiceSpawnPoints = null!; //TODO this won't change, doesn't need to be passed everytime as context
  [Export] public Node3D GoldSpawnPoint = null!; //TODO this won't change, doesn't need to be passed everytime as context
  [Export] public Node3D OutlineSpawnPoint = null!; //TODO this won't change, doesn't need to be passed everytime as context

  public DiceSet DiceSet;
  public List<EnemyData> LevelEnemyData = new();

  private RoundManager? _currentRoundManager;

  public override void _Ready()
  {
	var abilityDice = new List<AbilityDie>();
	var multiDice = new List<MultiDie>();
	byte idInitializer = 0;

	for (int i = 0; i < MetaUpgradeData.StartingAbilityDieCount; i++)
	{
	  var abilityDieInstance = AbilityDieScene.Instantiate<AbilityDie>();
	  AddChild(abilityDieInstance);
	  abilityDieInstance.GlobalPosition = AbilityDiceSpawnPoints[i].GlobalPosition;
	  abilityDieInstance.Freeze = true;
	  abilityDieInstance.DiceId = idInitializer++;
	  abilityDice.Add(abilityDieInstance);
	}

	for (int i = 0; i < MetaUpgradeData.StartingMultiDiceCount; i++)
	{
	  var multiDieInstance = MultiDieScene.Instantiate<MultiDie>();
	  AddChild(multiDieInstance);
	  multiDieInstance.GlobalPosition = MultiDiceSpawnPoints[i].GlobalPosition;
	  multiDieInstance.GlobalRotation = new Vector3(Mathf.DegToRad(270), 0, 0);
	  multiDieInstance.Freeze = true;
	  multiDieInstance.DiceId = idInitializer++;
	  multiDice.Add(multiDieInstance);
	}

	DiceSet = new DiceSet
	{
	  AbilityDice = abilityDice,
	  MultiDice = multiDice,
	};

	LevelEnemyData = FileUtilities.LoadEnemyDataFile();
	_ = StartLevel();
  }

  public async Task StartLevel()
  {
	// TODO pick which level to start (a level is 5 enemies).
	for (int i = 0; i < LevelEnemyData.Count; i++)
	{
	  GD.Print("entering loop");
	  _currentRoundManager = RoundManagerScene.Instantiate<RoundManager>();
	  AddChild(_currentRoundManager);

	  EnemyData currentEnemyData = LevelEnemyData[i];

	  _currentRoundManager.Initialize(
	  GoldSpawnPoint,
	  GoldPieceScene,
	  MultiDiceSpawnPoints,
	  AbilityDiceSpawnPoints,
	  OutlinesScene,
	  OutlineSpawnPoint,
	  currentEnemyData);
	  GD.Print("starting round");
	  RoundResult result = await _currentRoundManager.StartRound(DiceSet);
	  GD.Print("Back to Level Manager, round finished");

	  if (result.Victory)
	  {
		// TODO open the upgrade menu
		// TODO let the player apply upgrades to die faces
	  }

	  // PORT NOTE: `Destroy(gameObject)` → `QueueFree()` (deferred) or
	  // `Free()` (immediate). Prefer `QueueFree` from inside async code so
	  // we don't free while still iterating tree state.
	  _currentRoundManager.QueueFree();
	  _currentRoundManager = null;

	  // ProcessRoundResult(result);
	}
  }
}
