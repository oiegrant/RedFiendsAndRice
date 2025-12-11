using DG.Tweening;
using UnityEngine;

namespace System
{
    public class GameManager : MonoBehaviour
    {
        private void Awake()
        { 
            DOTween.Init();
        }
    }
}