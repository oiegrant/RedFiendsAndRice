using System.Collections;
using System.Collections.Generic;
using Data;
using NUnit.Framework;
using UnityEngine;

namespace System
{
    public class LevelManager : MonoBehaviour
    {
        public MultiDie multiDiePrefab;
        public AbilityDie abilityDiePrefab;
        public RoundManager roundManagerPrefab;
        private RoundManager currentRoundManager;
        public DiceSet diceSet;
        public Transform[] abilityDiceSpawnPoints;
        public Transform[] multiDiceSpawnPoints;
        
        void Awake()
        {
            //initialize ability die at some transform
            //initialize 2 multi die at a transform
            List<AbilityDie> abilityDice = new(); 
            List<MultiDie> multiDice = new();

            for (int i = 0; i < MetaUpgradeData.startingAbilityDieCount; i++)
            {
                AbilityDie abilityDieInstance = Instantiate(abilityDiePrefab, abilityDiceSpawnPoints[i].position, Quaternion.identity);
                abilityDieInstance.rb.isKinematic = true;
                abilityDice.Add(abilityDieInstance);
            }

            for (int i = 0; i < MetaUpgradeData.startingMultiDiceCount; i++)
            {
                MultiDie multiDieInstance =
                    Instantiate(multiDiePrefab, multiDiceSpawnPoints[i].position, Quaternion.Euler(270, 0, 0)); 
                multiDieInstance.rb.isKinematic = true;
                multiDice.Add(multiDieInstance);
            }

            diceSet = new DiceSet
            {
                abilityDice = abilityDice,
                multiDice = multiDice,
            };

        }

        void Start()
        {
            StartCoroutine(StartLevel());
        }
        
        public IEnumerator StartLevel()
        {

            for (int i = 0; i < 5; i++)
            {
                // Create new RoundManager for this round
                currentRoundManager = Instantiate(roundManagerPrefab, transform);
            
                // Initialize round-specific data
                currentRoundManager.Initialize();
            
                RoundResult result = new RoundResult();
                yield return StartCoroutine(
                    currentRoundManager.StartRound(diceSet, roundResult => result = roundResult));
                Debug.Log("Back to Level Manager, round finished");
            
                // Clean up after round
                Destroy(currentRoundManager.gameObject);
                currentRoundManager = null;
            
                // Process result before next round
                // ProcessRoundResult(result);
            }

        }
        
        void Update()
        {
            Debug.DrawRay(transform.position, Vector3.forward * 5f, Color.blue, 0.1f);
            Debug.DrawRay(transform.position, Vector3.up * 5f, Color.green, 0.1f);
            Debug.DrawRay(transform.position, Vector3.right  * 5f, Color.red, 0.1f);
        }
        
        
    }
}