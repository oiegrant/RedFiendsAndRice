using System.Collections;
using System.Collections.Generic;
using Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace System
{
    public class RoundManager : MonoBehaviour
    { 
        private InputSystem_Actions playerInputSystem;
        
        [Header("Dice")]
        [SerializeField] private List<MultiDie> dice = new List<MultiDie>();
        
        [Header("UI")]
        [SerializeField] private UIManager uiManager;
    
        [Header("Settings")]
        [SerializeField] private float velocityThreshold = 0.1f;
        [SerializeField] private float angularVelocityThreshold = 0.1f;
        [SerializeField] private float settleCheckInterval = 0.1f;
    
        private bool waitingForInput = false;
        private bool isProcessingRound = false;

        private void Awake()
        {
            playerInputSystem = new InputSystem_Actions();
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
            if (waitingForInput)
            {
                waitingForInput = false;
            }
        }
        
        // Main entry point called by GameManager
        public IEnumerator StartRound(DiceSet diceSet, System.Action<RoundResult> onRoundComplete)
        {
            // Setup round
        
            // Spawn dice
            // SpawnDice(dicePrefabs);
        
            // Setup enemy with appropriate difficulty
            // Initialize enemy
        
            // Run the game loop
            yield return StartCoroutine(GameLoop());
        
            // Create result
            RoundResult result = new RoundResult();
            result.victory = false;
            result.coinsEarned = 0;

            // Cleanup dice
            // CleanupDice();

            // Return result to GameManager
            // onRoundComplete?.Invoke(result);
        }
        
        private void Start()
        {
            StartCoroutine(GameLoop());
        }
        
        private IEnumerator GameLoop()
        {
            while (true) //enemy alive 
            {
                isProcessingRound = true;
                // Wait for player input
                waitingForInput = true;
                yield return new WaitUntil(() => !waitingForInput);
                
                // Roll dice
                // RollAllDice(dice);
            
                // Wait for dice to settle and get results
                Dictionary<int, int> diceResults = null;
                yield return StartCoroutine(WaitForDiceToSettle(results => diceResults = results));
            
                // Calculate score using the results from the coroutine
                int totalScore = CalculateScore(diceResults);
            
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
                yield return new WaitForSeconds(0.5f);
            
                // Reset dice positions
                ResetAllDice();
            
                yield return new WaitForSeconds(1f);
            
                isProcessingRound = false;
            }
        
            Debug.Log("Round Over");
        }

        private int CalculateScore(Dictionary<int, int> diceResults)
        {
            throw new NotImplementedException();
        }

        private void RollAllDice()
        {
            //TODO dice roll logic
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
        
    }
}