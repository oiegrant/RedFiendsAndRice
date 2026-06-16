using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using RedFiendsAndRice.Data;

namespace RedFiendsAndRice.System;

// ===========================================================================
// PORT OVERVIEW — read this before you start rewriting.
//
// This is the most Unity-flavored file in the project. Everything that follows
// is a straight port preserving structure + parameter names; behavior was kept
// as close as practical, but several systems do not have direct Godot
// equivalents and are flagged inline with `PORT NOTE:` blocks. Expect to
// rewrite the animation/UI sections substantially.
//
// Major translations applied:
//
//   IEnumerator + yield return StartCoroutine(x)
//       → async Task + await x
//       → `yield return null` (one-frame wait)       → await ToSignal(GetTree(), "process_frame")
//       → `yield return new WaitForSeconds(t)`       → await ToSignal(GetTree().CreateTimer(t), "timeout")
//       → kicking a coroutine fire-and-forget         → `_ = MyAsync();`
//
//   InputSystem_Actions (Unity Input System asset)
//       → Godot InputMap action in project.godot (e.g., "ui_accept" or a
//         custom "roll_dice" action) + _UnhandledInput / Input.IsActionJustPressed.
//
//   UIManager.Instance (singleton)
//       → Once you register UIManager as an autoload, this access pattern
//         still compiles. Until then, calls inside this class will NRE.
//
//   DOTween Sequence / DOMove / DORotate / DOJump / DOFade / DOKill
//       → Built-in Tween from Node.CreateTween(). DOJump has no direct
//         equivalent — emulated below with a parabolic TweenMethod (see
//         JumpDieAsync). DOFade for UI colors → TweenProperty on `modulate:a`.
//
//   Rigidbody.AddForce(v, ForceMode.Impulse) / AddTorque
//       → RigidBody3D.ApplyImpulse(v) / ApplyTorqueImpulse(t).
//   Rigidbody.isKinematic = true
//       → RigidBody3D.Freeze = true.
//   Rigidbody.linearVelocity / angularVelocity
//       → RigidBody3D.LinearVelocity / AngularVelocity.
//
//   Random.insideUnitSphere
//       → custom helper, see RandomInUnitSphere().
//
//   Quaternion.Euler(x,y,z)
//       → Quaternion.FromEuler(new Vector3(deg→rad, deg→rad, deg→rad)).
//   Quaternion.AngleAxis(deg, axis)
//       → new Quaternion(axis.Normalized(), Mathf.DegToRad(deg)).
//   Quaternion.LookRotation(v)
//       → Basis.LookingAt(v).GetRotationQuaternion().
//   Quaternion.FromToRotation(a, b)
//       → not built-in; compute via axis-angle (cross+acos).
//   Vector3.ProjectOnPlane(v, n)
//       → v - n.Normalized() * v.Dot(n.Normalized()).
//   Transform.TransformDirection(v)
//       → node.GlobalBasis * v.
//
//   TextMeshProUGUI text/scale/etc.
//       → Label.Text. For animated 2D positioning, prefer Control.Position /
//         Scale (Tween TweenProperty paths "position", "scale", "modulate:a").
//
// LEGACY UI WORK:
//   The outline/sum-up/pair-text 3D-anchored UI is the most awkward chunk to
//   bring across. Unity's RectTransform.position can be set in world space and
//   the canvas projects it; Godot wants you to either (a) use a Sprite3D/Label3D
//   for in-world UI or (b) take Camera3D.UnprojectPosition() to map world →
//   screen and apply the result to a Control. Pick one; the code below leaves
//   the 3D-anchored sections as `// TODO ...` so you can re-decide rather
//   than blindly translate.
// ===========================================================================

public partial class RoundManager : Node3D
{
  // Inputs / spawn points injected via Initialize() — Unity referenced them
  // through Transform[] arrays; Node3D[] is the closest Godot type.
  private Node3D[] _multiDiceSpawnPoints = null!;
  private Node3D[] _abilityDiceSpawnPoints = null!;
  private Node3D _goldSpawnPoint = null!;
  private PackedScene _goldPieceScene = null!;

  private DiceSet _diceSet;

  private const int MaxGoldOnTable = 1000;
  private int _currentGold;

  // Outline UI — see LEGACY UI WORK note above. These are placeholders so
  // the file compiles; you'll likely replace with Sprite3D or screen-space
  // Control nodes.
  private Node? _outlineGo;
  private Control? _outline1;
  private Control? _outline2;
  private Node3D? _outlineSpawnPoint;

  [ExportGroup("Settings")]
  [Export] public float VelocityThreshold = 0.1f;
  [Export] public float AngularVelocityThreshold = 0.1f;
  [Export] public float SettleCheckInterval = 0.1f;
  [Export] public float RestThreshold = 0.1f;
  [Export] public float RestTime = 0.1f;

  private bool _waitingForInput = true;
  private bool _isProcessingRound;

  private Node3D? _sumUpLocation;
  private Label? _currentPairSumText;
  private Label? _totalSumText;
  private Node3D? _enemyHitLocation;
  private EnemyData _currentEnemyData;

  private int _currentPlayerHealth;
  private int _currentPlayerShield;

  private bool _roundVictory;

  // PORT NOTE — Input.
  //   Unity used the new InputSystem with a generated InputSystem_Actions
  //   class and subscribed to `Player.Space.started`. The Godot equivalent
  //   is to define a "roll_dice" (or reuse "ui_accept") action in
  //   project.godot → Input Map, then poll/handle it in _UnhandledInput or
  //   _PhysicsProcess.
  public override void _UnhandledInput(InputEvent @event)
  {
	if (@event.IsActionPressed("roll") && _waitingForInput)
	{
	  _waitingForInput = false;
	}
  }

  // PORT NOTE: Unity returned RoundResult via a callback (Action<RoundResult>).
  // Async/await is cleaner in C#-on-Godot — just return the task and let
  // LevelManager `await` it.
  public async Task<RoundResult> StartRound(DiceSet diceSet)
  {
	GD.Print("Starting round");
	_currentPlayerHealth = MetaUpgradeData.PlayerMaxHealth;
	_currentPlayerShield = 0;
	_diceSet = diceSet;

	InitializeEnemy();
	InitializePlayer();

	await GameLoop();

	var result = new RoundResult
	{
	  Victory = _roundVictory,
	  EndingCoins = _currentGold,
	};

	// Fire all reset animations in parallel; we don't await them. Same
	// semantics as Unity's `StartCoroutine(...)` without `yield return`.
	foreach (var die in diceSet.AbilityDice) _ = ResetAbilityDieAsync(die);
	_ = ResetMultiDiceAsync();

	CleanUpRoundManager();
	return result;
  }

  private void InitializePlayer()
  {
	// UIManager.Instance?.UpdatePlayerHealthValues(_currentPlayerHealth, MetaUpgradeData.PlayerMaxHealth);
	// UIManager.Instance?.UpdatePlayerShieldValues(0, MetaUpgradeData.PlayerMaxShield);
  }

  private void InitializeEnemy()
  {
	// UIManager.Instance?.UpdateEnemyHealthValues(_currentEnemyData.CurrentHealth, _currentEnemyData.MaxHealth);
	// UIManager.Instance?.UpdateEnemyShieldValues(_currentEnemyData.CurrentShield, _currentEnemyData.MaxShield);
  }

  private async Task GameLoop()
  {
	_isProcessingRound = true;
	while (_isProcessingRound)
	{
	  AbilityDie abilityDie = _diceSet.AbilityDice[0];
	  Dictionary<byte, MultiDie> multiDieDict = CreateMultiDieDict();

	  // Get next enemy action
	  bool enemySpecialThisRound = false; // TODO drive from EnemyData
	  int currentPhysicalAttackDamage = 0;
	  int currentMagicAttackDamage = 0;

	  if (enemySpecialThisRound)
	  {
		// TODO compute the special-ability payload
	  }
	  else
	  {
		currentPhysicalAttackDamage = _currentEnemyData.startingPhysicalDamage;
		_currentEnemyData.startingPhysicalDamage += _currentEnemyData.basePhysicalDamageIncrement;

		currentMagicAttackDamage = _currentEnemyData.startingMagicDamage;
		_currentEnemyData.startingMagicDamage += _currentEnemyData.baseMagicDamageIncrement;
	  }

	  // Display next enemy action in display bar
	  if (!enemySpecialThisRound)
	  {
		// if (currentPhysicalAttackDamage > 0 && currentMagicAttackDamage > 0)
		// {
		//     UIManager.Instance?.UpdateDoubleEnemyAttackUI(currentPhysicalAttackDamage, currentMagicAttackDamage);
		// }
		// else if (currentPhysicalAttackDamage > 0)
		// {
		//     UIManager.Instance?.UpdateSingleEnemyAttackUI(EnemyAbilityType.Melee, currentPhysicalAttackDamage);
		// }
		// else if (currentMagicAttackDamage > 0)
		// {
		//     UIManager.Instance?.UpdateSingleEnemyAttackUI(EnemyAbilityType.Magic, currentMagicAttackDamage);
		// }
	  }
	  GD.Print("coming to the input wait");
	  // Wait for player input
	  _waitingForInput = true;
	  while (_waitingForInput)
	  {
		await Task.Yield();
	  }
	  GD.Print("SPACE PRESSED!");
	  _ = RollAllDiceAsync(); // fire-and-forget, mirrors Unity's StartCoroutine without yield
	  await WaitForDiceToSettleAsync();

	  // Dictionary<byte, int> faceUpMultiValues = GetDiceFaceUpMap();
	  //
	  // // Ability die face up
	  // int faceIdx = FaceUpCalculator.GetUpwardFace(abilityDie);
	  // AbilityType ability = abilityDie.FaceData[faceIdx].AbilityType;
	  //
	  // MultiplierResult totalScore = CalculateMultiplier(faceUpMultiValues);
	  // // TODO animate ability UP
	  //
	  // await AnimateAllPairsAsync(totalScore, multiDieDict, ability);
	  //
	  // GD.Print("Total Mult = " + totalScore.TotalMultiplier);
	  // GD.Print("Total Base = " + AbilityTypeValuesMap.Map[ability]);
	  //
	  // int finalScore = (int)totalScore.TotalMultiplier * AbilityTypeValuesMap.Map[ability];
	  //
	  // if (ability == AbilityType.Gold)
	  // {
	  //     await DispenseGoldAsync(finalScore);
	  // }
	  // else if (ability == AbilityType.Sword)
	  // {
	  //     _currentEnemyData.CurrentHealth -= finalScore;
	  //     UIManager.Instance?.UpdateEnemyHealthValues(_currentEnemyData.CurrentHealth, _currentEnemyData.MaxHealth);
	  // }
	  // else if (ability == AbilityType.Shield)
	  // {
	  //     GD.Print("Shield Before = " + _currentPlayerShield);
	  //     _currentPlayerShield += finalScore;
	  //     GD.Print("Shield After = " + _currentPlayerShield);
	  //     UIManager.Instance?.UpdatePlayerShieldValues(_currentPlayerShield, MetaUpgradeData.PlayerMaxShield);
	  // }
	  //
	  // // TODO animate ability DOWN
	  //
	  // if (_currentEnemyData.CurrentHealth <= 0)
	  // {
	  //     // TODO display enemy death animation
	  //     _roundVictory = true;
	  //     _isProcessingRound = false;
	  //     break;
	  // }
	  //
	  // if (_currentPlayerHealth <= 0)
	  // {
	  //     // TODO display player death animation
	  //     _isProcessingRound = false;
	  //     break;
	  // }
	  //
	  // if (!enemySpecialThisRound)
	  // {
	  //     if (currentPhysicalAttackDamage > 0 && currentMagicAttackDamage > 0)
	  //     {
	  //         EnemyAttackAction(currentPhysicalAttackDamage, currentMagicAttackDamage);
	  //     }
	  //     // else branch in the Unity source was entirely commented out — see git history
	  // }
	  //
	  // // Run both die-reset animations in parallel and await both.
	  // Task resetAbility = ResetAbilityDieAsync(abilityDie);
	  // Task resetMulti = ResetMultiDiceAsync();
	  // await Task.WhenAll(resetAbility, resetMulti);
	  //
	  // ReturnOutlinesToSpawnPoint();
	  // UIManager.Instance?.ClearEnemyAttackPanel();
	  // _waitingForInput = true;
	}
  }

  // The three Animate*EnemyAttack* helpers below operated on a screen-space
  // Image via DOTween. They're stubbed because the migration story for
  // RectTransform animations is "rebuild as Control tweens" — see LEGACY UI
  // WORK at top of file.

  private Task AnimateSingleEnemyAttackHitAsync(Control copiedImage)
  {
	// TODO scale up → fly to player location → fade out
	return Task.CompletedTask;
  }

  private Task FadeOutDisintegrateAsync(Control copiedImage)
  {
	// PORT NOTE: DOFade(0f, 0.6f).SetEase(OutCubic).WaitForCompletion() →
	// var t = CreateTween();
	// t.TweenProperty(copiedImage, "modulate:a", 0f, 0.6f)
	//  .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
	// await ToSignal(t, Tween.SignalName.Finished);
	return Task.CompletedTask;
  }

  private void EnemyAttackAction(int currentPhysicalAttackDamage, int currentMagicAttackDamage)
  {
	if (currentPhysicalAttackDamage > _currentPlayerShield)
	{
	  int damageToHealth = currentPhysicalAttackDamage - _currentPlayerShield;
	  _currentPlayerShield = 0;
	  _currentPlayerHealth -= damageToHealth;
	}
	else
	{
	  _currentPlayerShield -= currentPhysicalAttackDamage;
	}

	// Magic damage always hits health
	_currentPlayerHealth -= currentMagicAttackDamage;

	UIManager.Instance?.UpdatePlayerShieldValues(_currentPlayerShield, MetaUpgradeData.PlayerMaxShield);
	UIManager.Instance?.UpdatePlayerHealthValues(_currentPlayerHealth, MetaUpgradeData.PlayerMaxHealth);
  }

  private async Task AnimateAllPairsAsync(MultiplierResult totalScore, Dictionary<byte, MultiDie> multiDieDict, AbilityType abilityType)
  {
	bool isFirstPair = true;
	float totalScoreText = 0f;
	// PORT NOTE: the `RectTransform.DOScale(0, 0.01f)` instantly-collapse trick is
	// doing a "force zero" via tween. In Godot just set scale directly:
	//   _totalSumText.Scale = Vector2.Zero;
	if (_totalSumText != null) _totalSumText.Scale = Vector2.Zero;
	InitializeTotalScoreText(totalScoreText);

	foreach (var pair in totalScore.Pairs)
	{
	  MultiDie die1 = multiDieDict[pair.DiceId1];
	  MultiDie die2 = multiDieDict[pair.DiceId2];
	  await JumpOutlinesToDicePositionsAsync(die1, die2, isFirstPair);
	  await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
	  await ShowPairSumTextAsync(die1, die2, pair.PairSum);
	  totalScoreText += pair.PairSum;
	  isFirstPair = false;
	  await MovePairSumToTotalAsync();
	  if (_totalSumText != null) _totalSumText.Text = totalScoreText.ToString();
	  await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
	}

	await FinalScoreToEnemyAsync();
  }

  private async Task FinalScoreToEnemyAsync()
  {

	// const float moveToEnemyTime = 0.5f;
	// if (_totalSumText != null && _enemyHitLocation != null)
	// {
	//     // PORT NOTE: Unity moved a RectTransform to a 3D world position. In Godot
	//     // you'd project the 3D point to screen space first:
	//     //   Vector2 screen = GetViewport().GetCamera3D().UnprojectPosition(_enemyHitLocation.GlobalPosition);
	//     // then TweenProperty on `position` to `screen`.
	//     var t = CreateTween().SetParallel(true);
	//     t.TweenProperty(_totalSumText, "scale", Vector2.Zero, moveToEnemyTime)
	//         .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
	//     await ToSignal(t, Tween.SignalName.Finished);
	// }
	// else
	// {
	//     await ToSignal(GetTree().CreateTimer(0.4), SceneTreeTimer.SignalName.Timeout);
	// }
  }

  private void InitializeTotalScoreText(float totalScoreText)
  {
	if (_totalSumText == null || _sumUpLocation == null) return;
	// TODO project sumUpLocation.GlobalPosition to screen and assign to Position.
	_totalSumText.Text = totalScoreText.ToString();
	_totalSumText.Visible = true;
	var t = CreateTween();
	t.TweenProperty(_totalSumText, "scale", Vector2.One, 0.5f)
	  .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
  }

  private async Task ShowPairSumTextAsync(MultiDie die1, MultiDie die2, float pairSum)
  {
	if (_currentPairSumText == null) return;
	// TODO project (die1.GlobalPosition + Vector3.Up * 2) onto camera; assign to Position.
	_currentPairSumText.Text = pairSum.ToString();
	_currentPairSumText.Modulate = new Color(_currentPairSumText.Modulate, 1f);
	_currentPairSumText.Scale = Vector2.Zero;
	_currentPairSumText.Visible = true;

	var t = CreateTween();
	t.TweenProperty(_currentPairSumText, "scale", Vector2.One, 0.2f)
	  .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);

	await ToSignal(GetTree().CreateTimer(0.25), SceneTreeTimer.SignalName.Timeout);
  }

  private async Task MovePairSumToTotalAsync()
  {
	if (_currentPairSumText == null || _sumUpLocation == null) return;
	// TODO move via screen-space projection of _sumUpLocation.
	var t = CreateTween().SetParallel(true);
	t.TweenProperty(_currentPairSumText, "modulate:a", 0f, 0.6f)
	  .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
	await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
  }

  private void ReturnOutlinesToSpawnPoint()
  {
	if (_outline1 == null || _outline2 == null || _outlineSpawnPoint == null) return;
	// PORT NOTE: OnComplete(() => SetActive(false)) →
	//   Tween.Finished signal:
	//   t.Finished += () => outline.Visible = false;
	// TODO project _outlineSpawnPoint.GlobalPosition to screen-space target.
	var t1 = CreateTween();
	t1.Finished += () => _outline1.Visible = false;
	var t2 = CreateTween();
	t2.Finished += () => _outline2.Visible = false;
  }

  private async Task JumpOutlinesToDicePositionsAsync(MultiDie die1, MultiDie die2, bool isFirstPair)
  {
	if (_outline1 == null || _outline2 == null) return;

	int face1 = FaceUpCalculator.GetUpwardFace(die1);
	int face2 = FaceUpCalculator.GetUpwardFace(die2);
	Vector3 die1LocalNormal = DiceFaceNormals.D6[face1].Normalized();
	Vector3 die2LocalNormal = DiceFaceNormals.D6[face2].Normalized();
	Vector3 die1WorldNormal = die1.GlobalBasis * die1LocalNormal;
	Vector3 die2WorldNormal = die2.GlobalBasis * die2LocalNormal;

	Vector3 targetPos1 = die1.GlobalPosition + die1WorldNormal * 0.35f;
	Vector3 targetPos2 = die2.GlobalPosition + die2WorldNormal * 0.35f;
	// ReSharper disable once UnusedVariable
	Quaternion targetRot1 = GetRotationAlignedWithNormal(die1, die1WorldNormal, face1);
	// ReSharper disable once UnusedVariable
	Quaternion targetRot2 = GetRotationAlignedWithNormal(die2, die2WorldNormal, face2);

	if (isFirstPair)
	{
	  // TODO project (targetPos1 + Vector3.Up * 2) to screen → set Control.Position.
	  _outline1.Modulate = new Color(_outline1.Modulate, 1f);
	  _outline2.Modulate = new Color(_outline2.Modulate, 1f);
	  _outline1.Visible = true;
	  _outline2.Visible = true;
	}

	// TODO animate to projected target positions + apply targetRot to RotationDegrees.
	await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
  }

  // PORT NOTE — Quaternion math.
  //   Unity's Quaternion.LookRotation(forward) builds a rotation whose Z-axis
  //   points along `forward`. Godot's Basis.LookingAt builds a basis whose
  //   *negative* Z points at the target (camera-style). The end behavior is
  //   the same as long as you stay consistent throughout the codebase.
  //   FromToRotation is not built-in; computed via cross+acos below.
  private Quaternion GetRotationAlignedWithNormal(Node3D dieTransform, Vector3 worldNormal, int idx)
  {
	Quaternion baseRotation = Basis.LookingAt(worldNormal).GetRotationQuaternion();

	Vector3 dieRight = dieTransform.GlobalBasis.X; // "right"
	if (idx == 1 || idx == 4)
	{
	  dieRight = -dieTransform.GlobalBasis.Z; // Unity's forward → Godot -Z
	}

	Vector3 projectedRight = ProjectOnPlane(dieRight, worldNormal);
	if (projectedRight.LengthSquared() > 0.001f)
	{
	  Vector3 baseRight = baseRotation * Vector3.Right;
	  Quaternion alignRotation = FromToRotation(baseRight, projectedRight);
	  return alignRotation * baseRotation;
	}
	return baseRotation;
  }

  private static Vector3 ProjectOnPlane(Vector3 v, Vector3 planeNormal)
  {
	Vector3 n = planeNormal.Normalized();
	return v - n * v.Dot(n);
  }

  private static Quaternion FromToRotation(Vector3 from, Vector3 to)
  {
	Vector3 a = from.Normalized();
	Vector3 b = to.Normalized();
	float dot = Mathf.Clamp(a.Dot(b), -1f, 1f);
	if (dot > 0.9999f) return Quaternion.Identity;
	if (dot < -0.9999f)
	{
	  // 180° rotation around any axis orthogonal to `a`.
	  Vector3 axis = a.Cross(Vector3.Up);
	  if (axis.LengthSquared() < 1e-6f) axis = a.Cross(Vector3.Right);
	  return new Quaternion(axis.Normalized(), Mathf.Pi);
	}
	Vector3 cross = a.Cross(b);
	return new Quaternion(cross.X, cross.Y, cross.Z, 1f + dot).Normalized();
  }

  private Dictionary<byte, MultiDie> CreateMultiDieDict()
  {
	var ret = new Dictionary<byte, MultiDie>();
	foreach (var multiDie in _diceSet.MultiDice)
	{
	  ret.Add(multiDie.DiceId, multiDie);
	}
	return ret;
  }

  public struct PairResult
  {
	public byte DiceId1;
	public byte DiceId2;
	public float PairSum;
	public bool Critted;
  }

  public struct MultiplierResult
  {
	public float TotalMultiplier;
	public List<PairResult> Pairs;
  }

  private MultiplierResult CalculateMultiplier(Dictionary<byte, int> faceUpMultiValues)
  {
	float totalBaseMultiplier = 0;
	var pairs = new List<PairResult>();

	var diceValues = new List<(byte DiceId, int Value)>();
	foreach (var multiDie in _diceSet.MultiDice)
	{
	  int value = multiDie.FaceData[faceUpMultiValues[multiDie.DiceId]].BaseValue;
	  diceValues.Add((multiDie.DiceId, value));
	}

	for (int i = 0; i < diceValues.Count; i++)
	{
	  for (int j = i + 1; j < diceValues.Count; j++)
	  {
		float pairSum = diceValues[i].Value + diceValues[j].Value;
		bool crit = false;
		if (Mathf.IsEqualApprox(pairSum, 7f))
		{
		  pairSum *= MetaUpgradeData.Crit7;
		  crit = true;
		}
		if (Mathf.IsEqualApprox(pairSum, 11f))
		{
		  pairSum *= MetaUpgradeData.Crit11;
		  crit = true;
		}

		pairs.Add(new PairResult
		{
		  DiceId1 = diceValues[i].DiceId,
		  DiceId2 = diceValues[j].DiceId,
		  PairSum = pairSum,
		  Critted = crit,
		});

		totalBaseMultiplier += pairSum;
	  }
	}

	return new MultiplierResult
	{
	  TotalMultiplier = totalBaseMultiplier,
	  Pairs = pairs,
	};
  }

  private async Task RollAllDiceAsync()
  {
	GD.Print("Rolling all dice!!");

	foreach (var abilityDie in _diceSet.AbilityDice)
	{
	  abilityDie.Freeze = false;
	  // PORT NOTE: ForceMode.Impulse → ApplyImpulse (already velocity-scaled).
	  // ForceMode.Force would be ApplyForce — but Unity used Impulse here.
	  abilityDie.ApplyImpulse(GetRandomDiceLaunchAngle() * 2500f);
	  abilityDie.ApplyTorqueImpulse(RandomInUnitSphere() * 500f);
	  await ToSignal(GetTree().CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
	}

	foreach (var multiDie in _diceSet.MultiDice)
	{
	  multiDie.Freeze = false;
	  multiDie.ApplyImpulse(GetRandomDiceLaunchAngle() * 2500f);
	  multiDie.ApplyTorqueImpulse(RandomInUnitSphere() * 500f);
	  await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
	}
  }

  // PORT NOTE: Vector3.Slerp + Quaternion.AngleAxis → Godot equivalents:
  //   - Slerp on a normalized Vector3 is identical (Godot has Vector3.Slerp).
  //   - AngleAxis(deg, axis) → new Quaternion(axis.Normalized(), DegToRad(deg)).
  private Vector3 GetRandomDiceLaunchAngle()
  {
	Vector3 launchDirection = Vector3.Left.Slerp(Vector3.Up, 10f / 90f).Normalized();
	float randomPitch = (float)GD.RandRange(-5.0, 5.0);
	launchDirection = new Quaternion(Vector3.Back, Mathf.DegToRad(randomPitch)) * launchDirection;
	// PORT NOTE: Vector3.forward (Unity) = (0,0,1). Godot's Vector3.Forward
	// = (0,0,-1). I used Vector3.Back here to preserve Unity's "Z+" semantics
	// for the rotation axis. If you instead want "the camera-forward of the
	// current camera," compute it from the camera transform.
	float randomYaw = (float)GD.RandRange(-5.0, 5.0);
	launchDirection = new Quaternion(Vector3.Up, Mathf.DegToRad(randomYaw)) * launchDirection;
	return launchDirection;
  }

  private Vector3 GetRandomGoldLaunchAngle()
  {
	// See PORT NOTE above re: forward axis sign convention.
	Vector3 launchDirection = Vector3.Back; // Unity's "Vector3.forward"
	float randomPitch = (float)GD.RandRange(-3.0, 3.0);
	launchDirection = new Quaternion(Vector3.Right, Mathf.DegToRad(randomPitch)) * launchDirection;
	float randomYaw = (float)GD.RandRange(-3.0, 3.0);
	launchDirection = new Quaternion(Vector3.Up, Mathf.DegToRad(randomYaw)) * launchDirection;
	return launchDirection;
  }

  private async Task DispenseGoldAsync(int newGoldCount)
  {
	if (_currentGold + newGoldCount > MaxGoldOnTable)
	{
	  newGoldCount = MaxGoldOnTable - _currentGold;
	}

	for (int i = 0; i < newGoldCount; i++)
	{
	  if (_currentGold >= MaxGoldOnTable) return;

	  var gp = _goldPieceScene.Instantiate<GoldPiece>();
	  AddChild(gp);
	  gp.GlobalPosition = _goldSpawnPoint.GlobalPosition;
	  gp.ApplyImpulse(GetRandomGoldLaunchAngle() * 1100f);
	  gp.ApplyTorqueImpulse(RandomInUnitSphere() * 100f);
	  _currentGold++;
	  UIManager.Instance?.UpdateGoldCounter(_currentGold);
	  await ToSignal(GetTree().CreateTimer(0.01), SceneTreeTimer.SignalName.Timeout);
	}
  }

  private bool AllDiceSettled()
  {
	foreach (var abilityDie in _diceSet.AbilityDice)
	{
	  if (abilityDie.LinearVelocity.Length() > VelocityThreshold ||
	  abilityDie.AngularVelocity.Length() > AngularVelocityThreshold)
	  {
		return false;
	  }
	}

	foreach (var multiDie in _diceSet.MultiDice)
	{
	  if (multiDie.LinearVelocity.Length() > VelocityThreshold ||
	  multiDie.AngularVelocity.Length() > AngularVelocityThreshold)
	  {
		return false;
	  }
	}
	return true;
  }

  private async Task WaitForDiceToSettleAsync()
  {
	await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
	while (!AllDiceSettled())
	{
	  await ToSignal(GetTree().CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
	}
	GD.Print("DiceSettled");
  }

  private Dictionary<byte, int> GetDiceFaceUpMap()
  {
	var results = new Dictionary<byte, int>();
	foreach (var multiDie in _diceSet.MultiDice)
	{
	  int faceIdx = FaceUpCalculator.GetUpwardFace(multiDie);
	  results.Add(multiDie.DiceId, faceIdx);
	}
	return results;
  }

  // -----------------------------------------------------------------------
  // Dice reset animations.
  // PORT NOTE — DOTween Sequences modelled here as Godot Tweens.
  //
  //   DOTween.Sequence()              → CreateTween()
  //   .Insert(time, tween)            → SetParallel(true) and TweenInterval +
  //                                     TweenProperty interleaved manually;
  //                                     Godot has no direct `Insert(at, t)`.
  //   .Append(tween)                  → sequential TweenProperty (default mode).
  //   .Join(tween)                    → parallel with previous; SetParallel(true).
  //   .AppendInterval(t)              → TweenInterval(t).
  //   .WaitForCompletion()            → await ToSignal(t, Tween.SignalName.Finished).
  //   DOJump                          → see JumpAsync (parabolic emulation).
  // -----------------------------------------------------------------------
  private const float JumpPower = 1.5f;
  private const int JumpCount = 1;
  private const float JumpDuration = 0.5f;
  private const float DelayBetweenDice = 0.04f;
  private static readonly Tween.TransitionType JumpTrans = Tween.TransitionType.Cubic;
  private static readonly Tween.EaseType JumpEase = Tween.EaseType.Out;

  private async Task ResetMultiDiceAsync()
  {
	// PORT NOTE: Unity ran every die's "jump + fly home" inside a single
	// DOTween Sequence using Insert(timeOffset, tween). Godot's Tween has no
	// Insert API, so we run one Tween per die (each handles its own timing
	// via TweenInterval) and Task.WhenAll for the await semantics.
	var tasks = new List<Task>(_diceSet.MultiDice.Count);
	float allJumpsEndTime = (_diceSet.MultiDice.Count - 1) * DelayBetweenDice + JumpDuration;
	float moveStartTime = allJumpsEndTime + DelayBetweenDice;

	for (int i = 0; i < _diceSet.MultiDice.Count; i++)
	{
	  MultiDie curr = _diceSet.MultiDice[i];
	  Node3D finalPosition = _multiDiceSpawnPoints[i];
	  curr.Freeze = true;

	  float jumpStartTime = i * DelayBetweenDice;
	  // Quaternion.Euler(270,0,0) → Godot equivalent in radians.
	  Vector3 finalEuler = new(Mathf.DegToRad(270), 0, 0);
	  tasks.Add(JumpAndMoveAsync(curr, finalPosition.GlobalPosition, finalEuler,
	  jumpStartTime, moveStartTime));
	}

	await Task.WhenAll(tasks);
  }

  private async Task ResetAbilityDieAsync(AbilityDie abilityDie)
  {
	abilityDie.Freeze = true;
	Node3D finalPosition = _abilityDiceSpawnPoints[0];
	await JumpAndMoveAsync(abilityDie, finalPosition.GlobalPosition, Vector3.Zero,
	  jumpStartTime: DelayBetweenDice, moveStartTime: DelayBetweenDice + JumpDuration);
  }

  // Common scheduling helper for both ability + multi die resets.
  private async Task JumpAndMoveAsync(Node3D node, Vector3 finalPos, Vector3 finalEulerRad,
   float jumpStartTime, float moveStartTime)
  {
	if (jumpStartTime > 0)
	  await ToSignal(GetTree().CreateTimer(jumpStartTime), SceneTreeTimer.SignalName.Timeout);

	Vector3 jumpStart = node.GlobalPosition;
	Vector3 jumpTarget = jumpStart + Vector3.Up * 2f;

	await JumpAsync(node, jumpTarget, JumpPower, JumpDuration);

	// Sync the moveStartTime — we already burned jumpStartTime + JumpDuration.
	float remainingDelay = moveStartTime - (jumpStartTime + JumpDuration);
	if (remainingDelay > 0)
	  await ToSignal(GetTree().CreateTimer(remainingDelay), SceneTreeTimer.SignalName.Timeout);

	var t = node.CreateTween().SetParallel(true);
	t.TweenProperty(node, "global_position", finalPos, JumpDuration)
	  .SetTrans(JumpTrans).SetEase(JumpEase);
	t.TweenProperty(node, "global_rotation", finalEulerRad, JumpDuration)
	  .SetTrans(JumpTrans).SetEase(JumpEase);
	await ToSignal(t, Tween.SignalName.Finished);
  }

  // PORT NOTE — DOJump emulator.
  //   Models a parabolic arc from current position to `target` peaking at
  //   `height` above the midpoint. NumJumps==1, since none of the callers
  //   used >1. Drives position via TweenMethod (Godot's "interpolate over a
  //   callable" tween primitive — no direct Unity analogue).
  private async Task JumpAsync(Node3D node, Vector3 target, float height, float duration)
  {
	Vector3 start = node.GlobalPosition;
	var t = node.CreateTween();
	t.TweenMethod(
	  Callable.From<float>(p =>
	  {
		Vector3 pos = start.Lerp(target, p);
		pos.Y += 4f * height * p * (1f - p); // parabola, peak at p=0.5
		node.GlobalPosition = pos;
	  }),
	  0f, 1f, duration)
	  .SetTrans(JumpTrans).SetEase(JumpEase);
	await ToSignal(t, Tween.SignalName.Finished);
  }

  public void Initialize(Node3D goldSpawnPoint, PackedScene goldPieceScene,
   Node3D[] multiDiceSpawnPoints, Node3D[] abilityDiceSpawnPoints,
   PackedScene outlinesScene, Node3D outlineSpawnPoint,
   EnemyData currentEnemyData)
  {
	GD.Print("in Initialize");
	_goldSpawnPoint = goldSpawnPoint;
	_goldPieceScene = goldPieceScene;
	_multiDiceSpawnPoints = multiDiceSpawnPoints;
	_abilityDiceSpawnPoints = abilityDiceSpawnPoints;


	_outlineGo = outlinesScene.Instantiate();
	AddChild(_outlineGo);
	if (_outlineGo is Node3D outlineNode3D)
	{
	  outlineNode3D.GlobalPosition = outlineSpawnPoint.GlobalPosition;
	}

	// PORT NOTE — Unity used GetComponentsInChildren<Image>() to find both
	// outlines by index and TextMeshProUGUI by name-substring match. Replace
	// with explicit named child paths once the outline scene exists, e.g.:
	//   _outline1 = _outlineGo.GetNodeOrNull<Control>("Outline1");
	//   _currentPairSumText = _outlineGo.GetNodeOrNull<Label>("IncrementText");
	// Left null here so the file compiles; populate when the prefab is built.

	_outlineSpawnPoint = outlineSpawnPoint;
	_currentEnemyData = currentEnemyData;
	GD.Print("in Initialize");
  }

  private void CleanUpRoundManager()
  {
	_outlineGo?.QueueFree();
	_outlineGo = null;
  }

  // PORT NOTE: Unity.Random.insideUnitSphere returns a point in the unit
  // ball, not on its surface. Reproduce with rejection sampling for an
  // unbiased distribution.
  private static Vector3 RandomInUnitSphere()
  {
	while (true)
	{
	  var v = new Vector3(
	  (float)GD.RandRange(-1.0, 1.0),
	  (float)GD.RandRange(-1.0, 1.0),
	  (float)GD.RandRange(-1.0, 1.0));
	  if (v.LengthSquared() <= 1f) return v;
	}
  }
}
