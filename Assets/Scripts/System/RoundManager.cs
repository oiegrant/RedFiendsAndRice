using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Data;
using DG.Tweening;
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

        private Transform[] multiDiceSpawnPoints;
        private Transform[] abilityDiceSpawnPoints;
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

        [SerializeField] private Image outline1;
        [SerializeField] private Image outline2;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Vector3 outlineOffset = new Vector3(0, 0.5f, 0); // Offset above dice
        [SerializeField] private float outlineSize = 1.5f; // Size of the outline squares

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
                Dictionary<byte, MultiDie> diceMap = CreateDiceMap();


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

                //Get ability die face up
                int faceIdx = FaceUpCalculator.GetUpwardFace(abilityDie.gameObject);
                AbilityType ability = abilityDie.faceData[faceIdx].abilityType;


                // Calculate score using the results from the coroutine
                MultiplierResult totalScore = CalculateMultiplier(faceUpMultiValues);

                // Animate each pair
                foreach (var pair in totalScore.pairs)
                {
                    // StartCoroutine(AnimatePair(pair));
                }

                if (ability == AbilityType.Gold)
                {
                    StartCoroutine(DispenseGold((int)totalScore.totalMultiplier));
                }
                else
                {
                    //TODO implement ability switch
                }

                // Show score animation
                StartCoroutine(AnimateDicePairs(totalScore, diceMap));

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
                // Reset dice positions
                ResetMultiDie();

                // yield return new WaitForSeconds(1f);

                //Need to check if all dice are rolled, then it can proceed with round closure
                // isProcessingRound = false;
                waitingForInput = true;
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
            float randomPitch = UnityEngine.Random.Range(-3f, 3f);
            launchDirection = Quaternion.AngleAxis(randomPitch, Vector3.right) * launchDirection;

            // Add random rotation (±5 degrees) around the up axis (for front/back variation)
            float randomYaw = UnityEngine.Random.Range(-3f, 3f);
            launchDirection = Quaternion.AngleAxis(randomYaw, Vector3.up) * launchDirection;
            Debug.DrawRay(transform.position, launchDirection * 10f, Color.yellow, 0.1f);
            return launchDirection;
        }

        private IEnumerator DispenseGold(int newGoldCount)
        {
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
            Sequence masterSequence = DOTween.Sequence();

            // Step 1: All dice jump up (with stagger)
            for (int i = 0; i < diceSet.multiDice.Count; i++)
            {
                MultiDie curr = diceSet.multiDice[i];
                float delay = i * delayBetweenDice;
                curr.rb.isKinematic = true;

                masterSequence.Insert(
                    delay,
                    curr.transform.DOJump(
                            curr.transform.position + Vector3.up * 2,
                            jumpPower,
                            jumpCount,
                            jumpDuration
                        )
                        .SetEase(jumpEase)
                );
            }


            // Step 2: Wait 1 second (after all jumps complete)
            float totalJumpTime = (diceSet.multiDice.Count - 1) * delayBetweenDice + jumpDuration;
            // masterSequence.AppendInterval(0.1f);

            // Step 3: All dice move to final position and rotate (with stagger)
            float moveStartTime = totalJumpTime + 0f;
            for (int i = 0; i < diceSet.multiDice.Count; i++)
            {
                MultiDie curr = diceSet.multiDice[i];
                Transform finalPosition = multiDiceSpawnPoints[i];
                float delay = delayBetweenDice;

                masterSequence.Insert(
                    moveStartTime + delay,
                    curr.transform.DOMove(
                            finalPosition.position,
                            jumpDuration
                        )
                        .SetEase(jumpEase)
                );

                masterSequence.Insert(
                    moveStartTime + delay,
                    curr.transform.DORotateQuaternion(
                            Quaternion.Euler(270, 0, 0),
                            jumpDuration
                        )
                        .SetEase(jumpEase)
                );
            }
        }

        private void ResetAbilityDie(AbilityDie abilityDie)
        {
            Sequence masterSequence = DOTween.Sequence();


            abilityDie.rb.isKinematic = true;
            float delay = delayBetweenDice;
            Transform finalPosition = abilityDiceSpawnPoints[0];

            masterSequence.Insert(
                delay,
                abilityDie.transform.DOJump(
                        abilityDie.transform.position + Vector3.up * 2,
                        jumpPower,
                        jumpCount,
                        jumpDuration
                    )
                    .SetEase(jumpEase)
            );

            // Step 2: Wait 1 second (after all jumps complete)
            float totalJumpTime = jumpDuration;

            // Step 3: All dice move to final position and rotate (with stagger)
            float moveStartTime = totalJumpTime + 0f;
            masterSequence.Insert(
                moveStartTime + delay,
                abilityDie.transform.DOMove(
                        finalPosition.position,
                        jumpDuration
                    )
                    .SetEase(jumpEase)
            );

            masterSequence.Insert(
                moveStartTime + delay,
                abilityDie.transform.DORotateQuaternion(
                        Quaternion.identity,
                        jumpDuration
                    )
                    .SetEase(jumpEase)
            );

        }

        public IEnumerator AnimateDicePairs(MultiplierResult multiplierResult, Dictionary<byte, MultiDie> diceMap)
            {
                if (multiplierResult.pairs == null || multiplierResult.pairs.Count == 0)
                    return;

                // Create a main sequence
                Sequence mainSequence = DOTween.Sequence();

                SetOutlineAlpha(0);

                for (int i = 0; i < multiplierResult.pairs.Count; i++)
                {
                    PairResult pair = multiplierResult.pairs[i];

                    // Get the dice objects
                    if (!diceMap.TryGetValue(pair.diceId1, out MultiDie dice1) ||
                        !diceMap.TryGetValue(pair.diceId2, out MultiDie dice2))
                    {
                        Debug.LogWarning($"Could not find dice with IDs {pair.diceId1} or {pair.diceId2}");
                        continue;
                    }

                    // First pair - fade in
                    if (i == 0)
                    {
                        // Position outlines at dice locations
                        Vector3 worldPos1 = dice1.transform.position + outlineOffset;
                        Vector3 worldPos2 = dice2.transform.position + outlineOffset;

                        Vector2 screenPos1 = mainCamera.WorldToScreenPoint(worldPos1);
                        Vector2 screenPos2 = mainCamera.WorldToScreenPoint(worldPos2);

                        outline1.rectTransform.anchoredPosition = screenPos1;
                        outline2.rectTransform.anchoredPosition = screenPos2;

                        // Set size
                        outline1.rectTransform.sizeDelta = new Vector2(outlineSize * 100, outlineSize * 100);
                        outline2.rectTransform.sizeDelta = new Vector2(outlineSize * 100, outlineSize * 100);

                        // Fade in
                        mainSequence.Append(outline1.DOFade(1f, 0.3f));
                        mainSequence.Join(outline2.DOFade(1f, 0.3f));

                        // Wait 0.5 seconds
                        mainSequence.AppendInterval(0.5f);
                    }
                    else
                    {
                        // Tween to next pair of dice
                        Vector3 newPos1 = dice1.transform.position + outlineOffset;
                        Vector3 newPos2 = dice2.transform.position + outlineOffset;

                        Vector2 screenPos1 = mainCamera.WorldToScreenPoint(newPos1);
                        Vector2 screenPos2 = mainCamera.WorldToScreenPoint(newPos2);

                        mainSequence.Append(outline1.rectTransform.DOAnchorPos(screenPos1, 0.4f).SetEase(Ease.InOutQuad));
                        mainSequence.Join(outline2.rectTransform.DOAnchorPos(screenPos2, 0.4f).SetEase(Ease.InOutQuad));

                        // Wait 0.5 seconds before next pair
                        mainSequence.AppendInterval(0.5f);
                    }
                }

                // Fade out after all pairs
                mainSequence.Append(outline1.DOFade(0f, 0.3f));
                mainSequence.Join(outline2.DOFade(0f, 0.3f));
            }

            private void SetOutlineAlpha(float alpha)
            {
                Color color1 = outline1.color;
                color1.a = alpha;
                outline1.color = color1;

                Color color2 = outline2.color;
                color2.a = alpha;
                outline2.color = color2;
            }

            private Dictionary<byte, MultiDie> CreateDiceMap() {
                Dictionary<byte, MultiDie> results = new Dictionary<byte, MultiDie>();
                foreach (var multiDie in diceSet.multiDice)
                {
                    results.Add(multiDie.diceId, multiDie);
                }
                return results;

            }


        public void Initialize(Transform goldSpawnPoint, GoldPiece goldPiecePrefab, Transform[] multiDiceSpawnPoints, Transform[] abilityDiceSpawnPoints)
        {
            this.goldSpawnPoint = goldSpawnPoint;
            this.goldPiecePrefab = goldPiecePrefab;
            this.multiDiceSpawnPoints = multiDiceSpawnPoints;
            this.abilityDiceSpawnPoints = abilityDiceSpawnPoints;
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
