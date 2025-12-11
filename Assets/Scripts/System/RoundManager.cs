using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace System
{
    //A Round is the combat loop, where the player rolls dice to enact abilities, followed by enemy attack/ability
    //A Round ends when either the enemy or player is dead
    //There are no upgrades that occur during a round, the number of dice in the players set will be the same at the start and end of a round
    
    public class RoundManager : MonoBehaviour
    { 
        private InputSystem_Actions playerInputSystem;

        private Transform goldSpawnPoint;
        private GoldPiece goldPiecePrefab;

        private DiceSet diceSet;
        
        private int MAX_GOLD_ON_TABLE = 1000;
        private int currentGold = 0;
    
        [Header("Settings")]
        [SerializeField] private float velocityThreshold = 0.1f;
        [SerializeField] private float angularVelocityThreshold = 0.1f;
        [SerializeField] private float settleCheckInterval = 0.1f;
        [SerializeField] private float restThreshold = 0.1f; // Velocity threshold to consider "at rest"
        [SerializeField] private float restTime = 0.1f; // How long it must be still before considered at rest

    
        private bool waitingForInput = true;
        private bool isProcessingRound = false;

        private void Awake()
        {
            playerInputSystem = new InputSystem_Actions();
            Debug.unityLogger.logEnabled = true;
        }

        private void OnEnable()
        {
            playerInputSystem.Enable();
            playerInputSystem.Player.Space.started += spacePressed;
        }

        private void OnDisable()
        {
            playerInputSystem.Player.Space.started -= spacePressed;
            playerInputSystem.Disable();

        }
        
        private void spacePressed(InputAction.CallbackContext context)
        {
            if (waitingForInput)
            {
                waitingForInput = false;
            }
        }
        
        // Main entry point called by GameManager
        public IEnumerator StartRound(DiceSet diceSet, System.Action<RoundResult> onRoundComplete)
        {
            Debug.Log("Starting round");
            this.diceSet = diceSet;
            // Setup round
            
            // Setup enemy with appropriate difficulty
            // Initialize enemy
        
            // Run the game loop
            yield return StartCoroutine(GameLoop());
            
            Debug.Log("Finished round");
        
            // Create result
            RoundResult result = new RoundResult();
            result.victory = false;
            result.coinsEarned = 0;

            // Cleanup dice
            // CleanupDice();

            // Return result to GameManager
            // onRoundComplete?.Invoke(result);
        }
        
        private IEnumerator GameLoop()
        {
            isProcessingRound = true;
            while (isProcessingRound) //enemy alive 
            {
                // Wait for player input
                waitingForInput = true;

                while (waitingForInput)
                {
                    yield return null;
                }
                
                // Roll dice
                StartCoroutine(RollAllDice());
                
                // Wait for dice to settle and get results
                //diceId, faceindex
                yield return StartCoroutine(WaitForDiceToSettle());
                
                Dictionary<byte,int> faceUpMultiValues = GetDiceFaceUpMap();
                
                int faceIdx = FaceUpCalculator.GetUpwardFace(diceSet.abilityDice[0].gameObject);
                AbilityType ability = diceSet.abilityDice[0].faceData[faceIdx].abilityType;
                Debug.Log("Ability" + ability);


                // Calculate score using the results from the coroutine
                MultiplierResult totalScore = CalculateMultiplier(faceUpMultiValues);
                Debug.Log($"Total Base Multiplier: {totalScore.totalMultiplier}");
                Debug.Log($"Total Value: {totalScore.totalMultiplier}");
    
                // Animate each pair
                foreach (var pair in totalScore.pairs)
                {
                    Debug.Log($"Dice {pair.diceId1} + {pair.diceId2} = {pair.pairSum} (Crit: {pair.critted})");
                    // StartCoroutine(AnimatePair(pair));
                }

                ability = AbilityType.Gold;
                if (ability == AbilityType.Gold)
                {
                    StartCoroutine(DispenseGold((int)totalScore.totalMultiplier));
                }

                // Show score animation
                // yield return StartCoroutine(uiManager.AnimateScore(totalScore));

                // Apply damage to enemy
                // enemy.TakeDamage(totalScore);
                // uiManager.UpdateEnemyHealth(enemy.CurrentHealth, enemy.MaxHealth);
                //
                // Check if enemy is dead
                // if (enemy.IsDead())
                // {
                //     uiManager.ShowVictory();
                //     break;
                // }

                // Enemy attack
                // yield return new WaitForSeconds(0.5f);

                // Reset dice positions
                // ResetAllDice();

                // yield return new WaitForSeconds(1f);

                //Need to check if all dice are rolled, then it can proceed with round closure
                // isProcessingRound = false;
            }
        
            Debug.Log("Round Over");
        }

        public struct PairResult
        {
            public byte diceId1;
            public byte diceId2;
            public float pairSum;
            public bool critted;
        }

        public struct MultiplierResult
        {
            public float totalMultiplier;
            public List<PairResult> pairs;
        }

        private MultiplierResult CalculateMultiplier(Dictionary<byte,int> faceUpMultiValues)
        {
            float totalBaseMultiplier = 0;
            List<PairResult> pairs = new List<PairResult>();
    
            // Get all dice with their IDs
            List<(byte diceId, int value)> diceValues = new List<(byte, int)>();
            foreach (var multiDie in diceSet.multiDice)
            {
                int value = multiDie.faceData[faceUpMultiValues[multiDie.diceId]].baseValue;
                diceValues.Add((multiDie.diceId, value));
            }
    
            // Iterate through all unique pairs
            for (int i = 0; i < diceValues.Count; i++)
            {
                for (int j = i + 1; j < diceValues.Count; j++)
                {
                    float pairSum = diceValues[i].value + diceValues[j].value;
                    bool crit = false;
                    if (pairSum == 7.0f)
                    {
                        pairSum *= MetaUpgradeData.crit7;
                        crit = true;
                    }

                    if (pairSum == 11.0f)
                    {
                        pairSum *= MetaUpgradeData.crit11;
                        crit = true;
                    }
            
                    pairs.Add(new PairResult
                    {
                        diceId1 = diceValues[i].diceId,
                        diceId2 = diceValues[j].diceId,
                        pairSum = pairSum,
                        critted = crit,
                    });
            
                    totalBaseMultiplier += pairSum;
                }
            }
    
            return new MultiplierResult
            {
                totalMultiplier = totalBaseMultiplier,
                pairs = pairs
            };
        }

        private IEnumerator RollAllDice()
        {
            Debug.Log("Rolling all dice!!");
            // Roll ability dice
            foreach (var diceSetAbilityDie in diceSet.abilityDice)
            {
                diceSetAbilityDie.rb.isKinematic = false;
                diceSetAbilityDie.rb.AddForce(getRandomDiceLaunchAngle() * 2500, ForceMode.Impulse);
                diceSetAbilityDie.rb.AddTorque(UnityEngine.Random.insideUnitSphere * 500, ForceMode.Impulse);
                yield return new WaitForSeconds(0.2f); // Wait 1 second before next die
            }

            
            foreach (var multiDie in diceSet.multiDice)
            {
                multiDie.rb.isKinematic = false;
                multiDie.rb.AddForce(getRandomDiceLaunchAngle() * 2500, ForceMode.Impulse);
                multiDie.rb.AddTorque( UnityEngine.Random.insideUnitSphere * 500, ForceMode.Impulse);
                yield return new WaitForSeconds(0.1f); // Wait 1 second before next die
            }
        }

        private Vector3 getRandomDiceLaunchAngle()
        {

            Vector3 launchDirection = Vector3.Slerp(Vector3.left, Vector3.up, 10f / 90f).normalized;
            float randomPitch = UnityEngine.Random.Range(-5f, 5f);
            launchDirection = Quaternion.AngleAxis(randomPitch, Vector3.forward) * launchDirection;
            float randomYaw = UnityEngine.Random.Range(-5f, 5f);
            launchDirection = Quaternion.AngleAxis(randomYaw, Vector3.up) * launchDirection;
            
            return launchDirection;
        }
        
        private Vector3 getRandomGoldLaunchAngle()
        {
            Vector3 launchDirection = Vector3.forward; //goes to right of camera
            float randomPitch = UnityEngine.Random.Range(-5f, 5f);
            launchDirection = Quaternion.AngleAxis(randomPitch, Vector3.right) * launchDirection;

            // Add random rotation (±5 degrees) around the up axis (for front/back variation)
            float randomYaw = UnityEngine.Random.Range(-5f, 5f);
            launchDirection = Quaternion.AngleAxis(randomYaw, Vector3.up) * launchDirection;
            Debug.DrawRay(transform.position, launchDirection * 10f, Color.yellow, 0.1f);
            return launchDirection;
        }

        private IEnumerator DispenseGold(int newGoldCount)
        {
            newGoldCount *= 5;
            if (currentGold + newGoldCount > MAX_GOLD_ON_TABLE)
            {
                newGoldCount = MAX_GOLD_ON_TABLE - currentGold;
            }

            for (int i = 0; i < newGoldCount; i++)
            {
                if (currentGold >= MAX_GOLD_ON_TABLE)
                {
                    yield break; // Stops the coroutine completely
                }
                
                GoldPiece gp = Instantiate(goldPiecePrefab, goldSpawnPoint.position, Quaternion.identity);
                gp.rb.AddForce(getRandomGoldLaunchAngle() * 1100, ForceMode.Impulse);
                gp.rb.AddTorque( UnityEngine.Random.insideUnitSphere * 100, ForceMode.Impulse);
                currentGold++;
                Debug.Log("Gold = " + currentGold);
                yield return new WaitForSeconds(0.01f);
            }
        }
        
        private bool AllDiceSettled()
        {
            bool allSettled = true;
            foreach (var diceSetAbilityDie in diceSet.abilityDice)
            {
                if (diceSetAbilityDie.rb.linearVelocity.magnitude > velocityThreshold ||
                    diceSetAbilityDie.rb.angularVelocity.magnitude > angularVelocityThreshold)
                {
                    allSettled = false;
                    break;
                }

            }


            foreach (var multiDie in diceSet.multiDice)
            {
                if (multiDie.rb.linearVelocity.magnitude > velocityThreshold ||
                    multiDie.rb.angularVelocity.magnitude > angularVelocityThreshold)
                {
                    allSettled = false;
                    break;
                }
            }
            
            return allSettled;
        }
        
        private IEnumerator WaitForDiceToSettle()
        {
            yield return new WaitForSeconds(0.5f);
            
            // Check periodically if all dice have settled
            while (!AllDiceSettled())
            {
                yield return new WaitForSeconds(0.1f);
            }
            Debug.Log("DiceSettled");
        }
        
        private Dictionary<byte, int> GetDiceFaceUpMap()
        {
            Dictionary<byte, int> results = new Dictionary<byte, int>();
            
            foreach (var multiDie in diceSet.multiDice)
            {
                int faceIdx = FaceUpCalculator.GetUpwardFace(multiDie.gameObject);
                results.Add(multiDie.diceId, faceIdx);
            }
            
            return results;
        }
        
        private void ResetAllDice()
        {
            //TODO put all dice back in their starting position
        }

        public void Initialize(Transform goldSpawnPoint, GoldPiece goldPiecePrefab)
        {
            this.goldSpawnPoint = goldSpawnPoint;
            this.goldPiecePrefab = goldPiecePrefab;
        }

        // public void Update()
        // {
        //     Vector3 x = getRandomDiceLaunchAngle();
        //     Debug.DrawRay(transform.position, x * 10f, Color.blue, 0.1f);
        //     Vector3 g = getRandomGoldLaunchAngle();
        //     Debug.DrawRay(transform.position, g * 10f, Color.yellow, 0.1f);
        // }
    }
}