using System.Collections;
using Data;
using UnityEngine;

namespace System
{
    public class LevelManager : MonoBehaviour
    {
        private MultiDie multiDiePrefab;
        private AbilityDie abilityDiePrefab;
        private RoundManager roundManager;
        private DiceSet diceSet;
        
        void Awake()
        {
            
        }

        void Start()
        {
            StartLevel();
        }
        
        public IEnumerator StartLevel()
        {
            RoundResult result = new RoundResult();

            yield return StartCoroutine(
                roundManager.StartRound(diceSet, roundResult => result = roundResult));

        }
        
        
    }
}