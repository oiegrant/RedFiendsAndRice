using System.Collections;
using System.Collections.Generic;
using Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.UI;

namespace System
{
    //A Round is the combat loop, where the player rolls dice to enact abilities, followed by enemy attack/ability
    //A Round ends when either the enemy or player is dead
    //There are no upgrades that occur during a round, the number of dice in the players set will be the same at the start and end of a round
    
    public class RoundManager : MonoBehaviour
    { 
        private InputSystem_Actions playerInputSystem;

        private DiceSet diceSet;
    
        [Header("Settings")]
        [SerializeField] private float velocityThreshold = 0.1f;
        [SerializeField] private float angularVelocityThreshold = 0.1f;
        [SerializeField] private float settleCheckInterval = 0.1f;
    
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
            playerInputSystem.Player.Attack.started += spacePressed;
        }

        private void OnDisable()
        {
            playerInputSystem.Player.Attack.started -= spacePressed;
            playerInputSystem.Disable();

        }
        
        private void spacePressed(InputAction.CallbackContext context)
        {
            Debug.Log("spacePressed");
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
                // Dictionary<int, int> diceResults = null;
                // yield return StartCoroutine(WaitForDiceToSettle(results => diceResults = results));
            
                // Calculate score using the results from the coroutine
                // int totalScore = CalculateScore(diceResults);
            
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
            
                isProcessingRound = false;
            }
        
            Debug.Log("Round Over");
        }

        private int CalculateScore(Dictionary<int, int> diceResults)
        {
            throw new NotImplementedException();
        }

        private IEnumerator RollAllDice()
        {
            Debug.Log("Rolling all dice!!");
            // Roll ability dice
            foreach (var diceSetAbilityDie in diceSet.abilityDice)
            {
                diceSetAbilityDie.rb.isKinematic = false;
                diceSetAbilityDie.rb.AddForce(Vector3.left * 3, ForceMode.Impulse);
                // diceSetAbilityDie.rb.AddTorque(UnityEngine.Random.insideUnitSphere * 5, ForceMode.Impulse);
            }
            
            foreach (var multiDie in diceSet.multiDice)
            {
                multiDie.rb.isKinematic = false;
                multiDie.rb.AddForce(Vector3.left * 3, ForceMode.Impulse);
                // multiDie.rb.AddTorque( UnityEngine.Random.insideUnitSphere * 5, ForceMode.Impulse);
                yield return new WaitForSeconds(0.3f); // Wait 1 second before next die
            }
        }
        
        private bool AllDiceSettled()
        {
            //TODO check if all dice settled
            return false;
        }
        
        private IEnumerator WaitForDiceToSettle(System.Action<Dictionary<int, int>> onComplete)
        {
            // Check periodically if all dice have settled
            while (!AllDiceSettled())
            {
                yield return new WaitForSeconds(0);
            }
            
            // Get dice values and pass them back via callback
            Dictionary<int, int> results = GetDiceValues();
            onComplete?.Invoke(results);
        }
        
        private Dictionary<int, int> GetDiceValues()
        {
            // Dictionary<int, int> results = new Dictionary<int, int>();
            //
            // for (int i = 0; i < dice.Count; i++)
            // {
            //     int faceValue = dice[i].GetFaceUpValue();
            //     results[i] = faceValue;
            //     Debug.Log($"Die {i}: {faceValue}");
            // }
        
            return new Dictionary<int, int>();
        }
        
        private void ResetAllDice()
        {
            //TODO put all dice back in their starting position
        }

        public void Initialize()
        {
            //TODO initialize round specific data (enemy, etc.)
        }
    }
}