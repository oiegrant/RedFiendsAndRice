using System;
using DG.Tweening;
using UnityEngine;

namespace DefaultNamespace
{
    public class tempDotween : MonoBehaviour
    {
        [Header("Jump Parameters")]
        [SerializeField] private Vector3 targetPosition = new Vector3(-10f, 0f, 0f);
        [SerializeField] private float jumpPower = 2f;
        [SerializeField] private int numJumps = 1;
        [SerializeField] private float duration = 1f;
    
        [Header("Overshoot Settings")]
        [SerializeField] private Ease easeType = Ease.OutBack;
        [SerializeField] private float overshoot = 1.70158f; // Default OutBack overshoot value
    
        [Header("Loop Settings")]
        [SerializeField] private LoopType loopType = LoopType.Restart;
        [SerializeField] private int loops = -1; // -1 = infinite
    
        private Vector3 startPosition;
        private Tween jumpTween;

        void Start()
        {
            startPosition = transform.position;
            StartJumpAnimation();
        }

        void StartJumpAnimation()
        {
            // Kill any existing tween
            jumpTween?.Kill();
        
            // Create the jump tween with overshoot
            jumpTween = transform.DOJump(
                    startPosition + targetPosition,
                    jumpPower,
                    numJumps,
                    duration
                )
                .SetEase(easeType, overshoot)
                .SetLoops(loops, loopType)
                .OnStepComplete(() => {
                    // Reset to start position on each loop if using Restart
                    if (loopType == LoopType.Restart)
                    {
                        transform.position = startPosition;
                    }
                });
        }

        void OnDestroy()
        {
            jumpTween?.Kill();
        }
    }
}