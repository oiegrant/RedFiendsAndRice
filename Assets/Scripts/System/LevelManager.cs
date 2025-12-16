using System.Collections;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace System
{
    public class LevelManager : MonoBehaviour
    {
        public MultiDie multiDiePrefab;
        public AbilityDie abilityDiePrefab;
        public GoldPiece goldPiecePrefab;
        public RoundManager roundManagerPrefab;
        private RoundManager currentRoundManager;
        public DiceSet diceSet;
        public Transform[] abilityDiceSpawnPoints;
        public Transform[] multiDiceSpawnPoints;
        public Transform goldSpawnPoint;
        public GameObject outlines;
        public Transform outlineSpawnPoint;
        public Transform sumUpLocation;
        public Transform enemyHitLocation;
        public List<EnemyData> levelEnemyData;
        
        void Awake()
        {
            //initialize ability die at some transform
            //initialize 2 multi die at a transform
            List<AbilityDie> abilityDice = new();
            List<MultiDie> multiDice = new();
            byte idInitializer = 0;

            for (int i = 0; i < MetaUpgradeData.startingAbilityDieCount; i++)
            {
                AbilityDie abilityDieInstance = Instantiate(abilityDiePrefab, abilityDiceSpawnPoints[i].position, Quaternion.identity);
                abilityDieInstance.rb.isKinematic = true;
                abilityDieInstance.diceId = idInitializer;
                idInitializer++;
                abilityDice.Add(abilityDieInstance);
                
            }

            for (int i = 0; i < MetaUpgradeData.startingMultiDiceCount; i++)
            {
                MultiDie multiDieInstance =
                    Instantiate(multiDiePrefab, multiDiceSpawnPoints[i].position, Quaternion.Euler(270, 0, 0)); 
                multiDieInstance.rb.isKinematic = true;
                multiDieInstance.diceId = idInitializer; 
                idInitializer++;
                multiDice.Add(multiDieInstance);
            }

            diceSet = new DiceSet
            {
                abilityDice = abilityDice,
                multiDice = multiDice,
            };

            SpecialAbility specialTest1 = new SpecialAbility
            {
                type =  EnemySpecialAbilityType.AddShield,
                magnitude = 10
            };

            EnemyData testEnemyData = new EnemyData
            {
                enemyName = "Enemy1",
                maxHealth = 100,
                currentHealth = 100,
                maxShield = 0,
                currentShield = 0,
                startingPhysicalDamage = 10,
                startingMagicDamage = 0,
                isBaseDamageIncrements = true,
                basePhysicalDamageIncrement = 4,
                baseMagicDamageIncrement = 0,
                specialType = specialTest1,
                specialChance = 10
            };
            levelEnemyData = new List<EnemyData>();
            levelEnemyData.Add(testEnemyData);
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

                EnemyData currentEnemyData = levelEnemyData[i];
            
                // Initialize round-specific data
                currentRoundManager.Initialize(goldSpawnPoint, goldPiecePrefab, multiDiceSpawnPoints, abilityDiceSpawnPoints, outlines, outlineSpawnPoint, sumUpLocation, enemyHitLocation, currentEnemyData);
            
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
            // Debug.DrawRay(transform.position, Vector3.forward * 5f, Color.blue, 0.1f);
            // Debug.DrawRay(transform.position, Vector3.up * 5f, Color.green, 0.1f);
            // Debug.DrawRay(transform.position, Vector3.right  * 5f, Color.red, 0.1f);
        }
        
        
    }
}