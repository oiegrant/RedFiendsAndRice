using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Sequence = DG.Tweening.Sequence;

namespace System
{
    //A Round is the combat loop, where the player rolls dice to enact abilities, followed by enemy attack/ability
    //A Round ends when either the enemy or player is dead
    //There are no upgrades that occur during a round, the number of dice in the players set will be the same at the start and end of a round
    
    public class RoundManager : MonoBehaviour
    { 
        private InputSystem_Actions playerInputSystem;

        private Transform[] multiDiceSpawnPoints;
        private Transform[] abilityDiceSpawnPoints;
        private Transform goldSpawnPoint;
        private GoldPiece goldPiecePrefab;

        private DiceSet diceSet;
        
        private int MAX_GOLD_ON_TABLE = 5000;
        private int currentGold = 0;

        private Image outline1;
        private Image outline2;
        private Transform outlineSpawnPoint;
    
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
                AbilityDie abilityDie = diceSet.abilityDice[0];
                Dictionary<byte, MultiDie> multiDieDict = CreateMultiDieDict();
                
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
                
                Dictionary<byte,int> faceUpMultiValues =  GetDiceFaceUpMap();
                
                //Get ability die face up
                int faceIdx = FaceUpCalculator.GetUpwardFace(abilityDie.gameObject);
                AbilityType ability = abilityDie.faceData[faceIdx].abilityType;


                // Calculate score using the results from the coroutine
                MultiplierResult totalScore = CalculateMultiplier(faceUpMultiValues);
  
                yield return StartCoroutine(AnimateAllPairsCoroutine(totalScore, multiDieDict));
                
                ability = AbilityType.Gold;
                if (ability == AbilityType.Gold)
                {
                    yield return StartCoroutine(DispenseGold((int)totalScore.totalMultiplier));
                }
                else
                {
                    //TODO implement ability switch
                }

                // Show score animation
                // yield return StartCoroutine(uiManager.AnimateScore(totalScore));

                // Apply damage to enemy
                // enemy.TakeDamage(totalScore);
                // uiManager.UpdateEnemyHealth(enemy.CurrentHealth, enemy.MaxHealth);
                
                // Check if enemy is dead
                // if (enemy.IsDead())
                // {
                //     uiManager.ShowVictory();
                //     break;
                // }

                // Enemy attack
                // yield return new WaitForSeconds(0.5f);

                ResetAbilityDie(abilityDie);
                ResetMultiDie();

                // yield return new WaitForSeconds(1f);

                //Need to check if all dice are rolled, then it can proceed with round closure
                // isProcessingRound = false;
                ReturnOutlinesToSpawnPoint();
                waitingForInput = true;
            }
        
            Debug.Log("Round Over");
        }
        
        private void ReturnOutlinesToSpawnPoint()
        {
            outline1.rectTransform.DOMove(outlineSpawnPoint.position, 0.5f).SetEase(Ease.InCubic)
                .OnComplete(() => outline1.gameObject.SetActive(false));
            outline2.rectTransform.DOMove(outlineSpawnPoint.position, 0.5f).SetEase(Ease.InCubic)
                .OnComplete(() => outline2.gameObject.SetActive(false));
        }
        
        private void AnimateAllPairs(MultiplierResult totalScore,  Dictionary<byte, MultiDie> multiDieDict)
        {
            StartCoroutine(AnimateAllPairsCoroutine(totalScore, multiDieDict));
        }

        private IEnumerator AnimateAllPairsCoroutine(MultiplierResult totalScore,  Dictionary<byte, MultiDie> multiDieDict)
        {
            bool isFirstPair = true;
            foreach (var pair in totalScore.pairs)
            {
                MultiDie die1 = multiDieDict[pair.diceId1];
                MultiDie die2 = multiDieDict[pair.diceId2];
        
                JumpOutlinesToDicePositions(die1, die2, isFirstPair);
                isFirstPair = false;
        
                // Wait for the animation to complete (0.5s duration)
                yield return new WaitForSeconds(0.5f);
            }
        }
        
        private void JumpOutlinesToDicePositions(MultiDie die1, MultiDie die2, bool isFirstPair)
        {
            int face1 = FaceUpCalculator.GetUpwardFace(die1.gameObject);
            int face2 = FaceUpCalculator.GetUpwardFace(die2.gameObject);
            Vector3 die1LocalNormal = DiceFaceNormals.D6[face1].normalized;
            Vector3 die2LocalNormal = DiceFaceNormals.D6[face2].normalized;
            Vector3 die1WorldNormal = die1.transform.TransformDirection(die1LocalNormal);
            Vector3 die2WorldNormal = die2.transform.TransformDirection(die2LocalNormal);

            // Calculate target positions and rotations
            Vector3 targetPos1 = die1.transform.position + die1WorldNormal * 0.35f;
            Vector3 targetPos2 = die2.transform.position + die2WorldNormal * 0.35f;
            Quaternion targetRot1 = GetRotationAlignedWithNormal(die1.transform, die1WorldNormal, face1);
            Quaternion targetRot2 = GetRotationAlignedWithNormal(die2.transform, die2WorldNormal, face2);

            // Only set initial position and visibility on first pair
            if (isFirstPair)
            {
                // Start outlines at the same position but higher up (for the jump effect)
                outline1.rectTransform.position = targetPos1 + Vector3.up * 2f;
                outline2.rectTransform.position = targetPos2 + Vector3.up * 2f;
        
                // Make them fully visible
                outline1.color = new Color(outline1.color.r, outline1.color.g, outline1.color.b, 1f);
                outline2.color = new Color(outline2.color.r, outline2.color.g, outline2.color.b, 1f);
        
                // Enable the images
                outline1.gameObject.SetActive(true);
                outline2.gameObject.SetActive(true);
            }

            // Always animate to new positions and rotations
            outline1.rectTransform.DOMove(targetPos1, 0.5f).SetEase(Ease.Linear);
            outline1.rectTransform.DORotateQuaternion(targetRot1, 0.5f);
    
            outline2.rectTransform.DOMove(targetPos2, 0.5f).SetEase(Ease.OutCubic);
            outline2.rectTransform.DORotateQuaternion(targetRot2, 0.5f);
        }

        
        private void FadeInOutlinesAtDicePositions(MultiDie die1, MultiDie die2)
        {

            int face1 = FaceUpCalculator.GetUpwardFace(die1.gameObject);
            int face2 = FaceUpCalculator.GetUpwardFace(die2.gameObject);
            Vector3 die1LocalNormal = DiceFaceNormals.D6[face1].normalized;
            Vector3 die2LocalNormal = DiceFaceNormals.D6[face2].normalized;
            Vector3 die1WorldNormal = die1.transform.TransformDirection(die1LocalNormal);
            Vector3 die2WorldNormal = die2.transform.TransformDirection(die2LocalNormal);
            outline1.rectTransform.rotation = GetRotationAlignedWithNormal(die1.transform, die1WorldNormal, face1);
            outline2.rectTransform.rotation = GetRotationAlignedWithNormal(die2.transform, die2WorldNormal, face2);
            outline1.rectTransform.position = die1.transform.position + die1WorldNormal * 0.35f;
            outline2.rectTransform.position = die2.transform.position + die2WorldNormal * 0.35f;
            outline1.rectTransform.rotation = GetRotationAlignedWithNormal(die1.transform, die1WorldNormal, face1);
            outline2.rectTransform.rotation = GetRotationAlignedWithNormal(die2.transform, die2WorldNormal, face2);

        }
        
        private Quaternion GetRotationAlignedWithNormal(Transform dieTransform, Vector3 worldNormal, int idx)
        {
            // Start with rotation that points in the normal direction
            Quaternion baseRotation = Quaternion.LookRotation(worldNormal);
    
            // Get one of the die's axes (let's use right/x-axis) in world space
            Vector3 dieRight = dieTransform.right;

            if (idx == 1 || idx == 4)
            {
                dieRight = dieTransform.forward;
            }
            
            // Project it onto the plane perpendicular to the normal
            Vector3 projectedRight = Vector3.ProjectOnPlane(dieRight, worldNormal);
    
            // Calculate the rotation around the normal
            if (projectedRight.sqrMagnitude > 0.001f)
            {
                Quaternion alignRotation = Quaternion.FromToRotation(baseRotation * Vector3.right, projectedRight);
                return alignRotation * baseRotation;
            }
    
            return baseRotation;
        }

        private Dictionary<byte, MultiDie> CreateMultiDieDict()
        {
            Dictionary<byte, MultiDie> ret = new Dictionary<byte, MultiDie>();
            foreach (var multiDie in diceSet.multiDice)
            {
                ret.Add(multiDie.diceId, multiDie);
            }
            return ret; 
        }

        // private string AnimatePair(MultiDie die1, MultiDie die2)
        // {
        //     
        // }


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
            float randomPitch = UnityEngine.Random.Range(-3f, 3f);
            launchDirection = Quaternion.AngleAxis(randomPitch, Vector3.right) * launchDirection;

            // Add random rotation (±5 degrees) around the up axis (for front/back variation)
            float randomYaw = UnityEngine.Random.Range(-3f, 3f);
            launchDirection = Quaternion.AngleAxis(randomYaw, Vector3.up) * launchDirection;
            // Debug.DrawRay(transform.position, launchDirection * 10f, Color.yellow, 0.1f);
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
        
        [Header("Dice Reset Animation")]
        [SerializeField] private float jumpPower = 1.5f;
        [SerializeField] private int jumpCount = 1;
        [SerializeField] private float jumpDuration = 5f;
        [SerializeField] private float delayBetweenDice = 0.1f;
        
        [SerializeField] private Ease jumpEase = Ease.OutCubic;
        
        
        
        private void ResetMultiDie()
        {
            // Kill any existing tweens on these dice
            foreach (var die in diceSet.multiDice)
            {
                die.transform.DOKill();
            }

            // Step 1: All dice jump up (with stagger)
            for (int i = 0; i < diceSet.multiDice.Count; i++)
            {
                MultiDie curr = diceSet.multiDice[i];
                float jumpDelay = i * delayBetweenDice;
                curr.rb.isKinematic = true;

                curr.transform.DOJump(
                        curr.transform.position + Vector3.up * 2,
                        jumpPower,
                        jumpCount,
                        jumpDuration
                    )
                    .SetDelay(jumpDelay)
                    .SetEase(jumpEase)
                    .SetAutoKill(true);
            }

            // Step 3: All dice move to final position and rotate (with stagger)
            float totalJumpTime = (diceSet.multiDice.Count - 1) * delayBetweenDice + jumpDuration;
            float moveStartTime = totalJumpTime + 0f;
    
            for (int i = 0; i < diceSet.multiDice.Count; i++)
            {
                MultiDie curr = diceSet.multiDice[i];
                Transform finalPosition = multiDiceSpawnPoints[i];
                float moveDelay = moveStartTime + delayBetweenDice;

                curr.transform.DOMove(
                        finalPosition.position,
                        jumpDuration
                    )
                    .SetDelay(moveDelay)
                    .SetEase(jumpEase)
                    .SetAutoKill(true);

                curr.transform.DORotateQuaternion(
                        Quaternion.Euler(270, 0, 0),
                        jumpDuration
                    )
                    .SetDelay(moveDelay)
                    .SetEase(jumpEase)
                    .SetAutoKill(true);
            }
        }
        
        private void ResetAbilityDie(AbilityDie abilityDie)
        {
            // Kill any existing tweens on this die
            abilityDie.transform.DOKill();
    
            abilityDie.rb.isKinematic = true;
            float jumpDelay = delayBetweenDice;
            Transform finalPosition = abilityDiceSpawnPoints[0];
    
            // Step 1: Jump up
            abilityDie.transform.DOJump(
                    abilityDie.transform.position + Vector3.up * 2,
                    jumpPower,
                    jumpCount,
                    jumpDuration
                )
                .SetDelay(jumpDelay)
                .SetEase(jumpEase)
                .SetAutoKill(true);
    
            // Step 2: Calculate timing
            float totalJumpTime = jumpDuration;
            float moveStartTime = totalJumpTime + 0f;
            float moveDelay = moveStartTime + jumpDelay;
    
            // Step 3: Move to final position
            abilityDie.transform.DOMove(
                    finalPosition.position,
                    jumpDuration
                )
                .SetDelay(moveDelay)
                .SetEase(jumpEase)
                .SetAutoKill(true);

            // Step 3: Rotate to identity
            abilityDie.transform.DORotateQuaternion(
                    Quaternion.identity,
                    jumpDuration
                )
                .SetDelay(moveDelay)
                .SetEase(jumpEase)
                .SetAutoKill(true);
        }

        public void Initialize(Transform goldSpawnPoint, GoldPiece goldPiecePrefab, Transform[] multiDiceSpawnPoints, Transform[] abilityDiceSpawnPoints, GameObject outlines, Transform outlineSpawnPoint)
        {
            this.goldSpawnPoint = goldSpawnPoint;
            this.goldPiecePrefab = goldPiecePrefab;
            this.multiDiceSpawnPoints = multiDiceSpawnPoints;
            this.abilityDiceSpawnPoints = abilityDiceSpawnPoints;
            GameObject outlinesGO = Instantiate(outlines, outlineSpawnPoint.position, Quaternion.identity);
            
            Image[] outlinearr = outlinesGO.GetComponentsInChildren<Image>();
            outline1 = outlinearr[0];
            outline2 = outlinearr[1];
            this.outlineSpawnPoint = outlineSpawnPoint; 

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