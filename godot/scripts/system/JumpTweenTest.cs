using Godot;

namespace RedFiendsAndRice.System;

// PORT NOTE — this is the Unity `tempDotween.cs` jump-demo, kept around so
// the team has a tiny reference for Godot's tween API in this codebase.
//
// Big idea differences from DOTween:
//   - DOTween.Sequence()        → Tween (returned by `node.CreateTween()`)
//   - .DOJump(target,p,n,t)     → no native equivalent — emulate with
//                                 horizontal TweenProperty + vertical
//                                 TweenMethod feeding a parabola.
//   - .SetEase(Ease.OutBack, k) → tween.SetTrans(Tween.TransitionType.Back)
//                                 .SetEase(Tween.EaseType.Out)
//                                 (overshoot factor is not directly
//                                 configurable — pick a different transition
//                                 if you need a tighter overshoot)
//   - .SetLoops(n, LoopType.Restart) → tween.SetLoops(n) (Restart is default).
//                                       For Yoyo: build a sub-sequence that
//                                       runs the reverse and call SetLoops on
//                                       the outer.
//   - .OnStepComplete(cb)       → connect to `Tween.LoopFinished` signal.
//   - .Kill()                   → `tween.Kill()` (same name, different namespace)
//
// `[Export]` replaces `[SerializeField]`. `[ExportGroup]` is the closest
// equivalent to `[Header]` for grouping in the inspector.
public partial class JumpTweenTest : Node3D
{
    [ExportGroup("Jump Parameters")]
    [Export] public Vector3 TargetOffset = new(-10f, 0f, 0f);
    [Export] public float JumpPower = 2f;
    [Export] public int NumJumps = 1;
    [Export] public float Duration = 1f;

    [ExportGroup("Overshoot Settings")]
    [Export] public Tween.TransitionType EaseTrans = Tween.TransitionType.Back;
    [Export] public Tween.EaseType EaseType = Tween.EaseType.Out;

    [ExportGroup("Loop Settings")]
    // PORT NOTE: Godot's `SetLoops(0)` means infinite. Unity's DOTween used -1
    // for infinite. So 0 ≡ Unity's -1 here.
    [Export] public int Loops = 0;

    private Vector3 _startPosition;
    private Tween? _jumpTween;

    public override void _Ready()
    {
        _startPosition = Position;
        StartJumpAnimation();
    }

    private void StartJumpAnimation()
    {
        _jumpTween?.Kill();

        Vector3 endPos = _startPosition + TargetOffset;

        _jumpTween = CreateTween();
        _jumpTween.SetLoops(Loops);
        _jumpTween.SetTrans(EaseTrans).SetEase(EaseType);

        // Run horizontal and vertical components in parallel.
        _jumpTween.SetParallel(true);

        // Horizontal interpolation.
        _jumpTween.TweenProperty(this, "position:x", endPos.X, Duration);
        _jumpTween.TweenProperty(this, "position:z", endPos.Z, Duration);

        // Vertical: parabolic arc per jump segment.
        // height(t) = start.y + 4 * jumpPower * t * (1 - t)   peaks at t=0.5
        float segmentDuration = Duration / Mathf.Max(1, NumJumps);
        for (int i = 0; i < NumJumps; i++)
        {
            float i1 = i; // capture
            _jumpTween.TweenMethod(
                Callable.From<float>(t =>
                {
                    float local = (t - i1 * segmentDuration) / segmentDuration;
                    float yOffset = 4f * JumpPower * local * (1f - local);
                    var p = Position;
                    p.Y = _startPosition.Y + yOffset;
                    Position = p;
                }),
                i * segmentDuration,
                (i + 1) * segmentDuration,
                segmentDuration);
        }

        // PORT NOTE: Unity OnStepComplete reset position on each loop iteration.
        // Godot's `Restart` loop type already snaps tweened properties back to
        // their starting values, so manual snapping is usually unnecessary.
        // If you specifically want a beat between loops, append a delay tween.
    }

    public override void _ExitTree()
    {
        _jumpTween?.Kill();
    }
}
