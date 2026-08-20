using Game.Core;
using Game.GamePlay.Fog;
using Game.GamePlay.Grid;
using Game.GamePlay.StatusEffects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * 적 팀의 턴 진행을 담당한다.
     *
     * 각 적 유닛의 처리 흐름:
     *
     * 1. 시야 데이터 갱신
     * 2. EnemyPerception 갱신
     * 3. EnemyBlackboard 갱신
     * 4. EnemyStateMachine 상태 결정
     * 5. EnemyDecisionMaker 행동 결정
     * 6. EnemyActionExecutor 행동 실행
     * 7. 행동 후 시야 갱신
     */
    public sealed class EnemyTurnController
        : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private EnemyManager enemyManager;

        [SerializeField]
        private StatusEffectManager statusEffectManager;

        [Header("Fog References")]
        [SerializeField]
        private VisionSystem visionSystem;

        [SerializeField]
        private FogOfWarSystem fogOfWarSystem;

        [Header("Perception")]
        [SerializeField]
        [Min(0)]
        private int temporaryVisionRange = 4;

        [SerializeField]
        [Min(0)]
        private int memoryDurationTurns = 3;

        [Header("Blackboard")]
        [SerializeField]
        [Range(0f, 1f)]
        private float lowHealthThreshold = 0.3f;

        [Header("Timing")]
        [SerializeField]
        [Min(0f)]
        private float turnStartDelay = 0.35f;

        [SerializeField]
        [Min(0f)]
        private float beforeActionDelay = 0.15f;

        [SerializeField]
        [Min(0f)]
        private float afterActionDelay = 0.25f;

        [Header("Debug")]
        [SerializeField]
        private bool logDecisionCandidates;

        [SerializeField]
        private bool logStateChanges = true;

        private readonly Dictionary<ChessPiece, EnemyMemory>
            memoriesByEnemy = new();

        private readonly Dictionary<ChessPiece, EnemyPerception>
            perceptionsByEnemy = new();

        private readonly Dictionary<ChessPiece, EnemyBlackboard>
            blackboardsByEnemy = new();

        private readonly List<ChessPiece>
            cleanupBuffer = new();

        private TurnManager turnManager;

        private EnemyDecisionMaker decisionMaker;

        private EnemyStateMachine stateMachine;

        private EnemyActionExecutor actionExecutor;

        private Coroutine enemyTurnRoutine;

        private bool isInitialized;

        private bool isProcessingTurn;

        private bool pendingEnemyTurnEnd;
        public bool IsProcessingTurn =>
            isProcessingTurn;

        #region Initialization

        public void Initialize(
            GameManager manager)
        {
            Unsubscribe();
            StopEnemyTurnRoutine();
            ClearAllAIRuntimeData();

            gameManager = manager;

            if (gameManager == null)
            {
                Debug.LogError(
                    "[EnemyTurnController] " +
                    "GameManager가 없습니다.",
                    this
                );

                isInitialized = false;
                return;
            }

            ResolveReferences();

            turnManager =
                gameManager.TurnManager;

            if (turnManager == null ||
                gridManager == null ||
                enemyManager == null)
            {
                Debug.LogError(
                    "[EnemyTurnController] " +
                    "필수 참조가 연결되지 않았습니다.",
                    this
                );

                isInitialized = false;
                return;
            }

            EnemyThreatAnalyzer threatAnalyzer =
                new EnemyThreatAnalyzer();

            EnemyPathFinder pathFinder =
                new EnemyPathFinder(
                    gridManager
                );

            EnemyUtilityEvaluator utilityEvaluator =
                new EnemyUtilityEvaluator(
                    EnemyUtilityWeights.CreateDefault(),
                    threatAnalyzer
                );

            stateMachine =
                new EnemyStateMachine(
                    threatAnalyzer,
                    EnemyStateSettings.CreateDefault()
                );

            decisionMaker =
                new EnemyDecisionMaker(
                    gridManager,
                    threatAnalyzer,
                    pathFinder,
                    utilityEvaluator
                );

            actionExecutor =
                new EnemyActionExecutor(
                    gameManager
                );

            isInitialized = true;

            Subscribe();

            fogOfWarSystem?
                .RefreshAllVisibility();

            Debug.Log(
                "[EnemyTurnController] " +
                "초기화 완료"
            );
        }

        private void ResolveReferences()
        {
            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<GridManager>();
            }

            if (enemyManager == null)
            {
                enemyManager =
                    FindFirstObjectByType<EnemyManager>();
            }

            if (statusEffectManager == null)
            {
                statusEffectManager =
                    FindFirstObjectByType<StatusEffectManager>();
            }

            if (visionSystem == null)
            {
                visionSystem =
                    FindFirstObjectByType<VisionSystem>();
            }

            if (fogOfWarSystem == null)
            {
                fogOfWarSystem =
                    FindFirstObjectByType<FogOfWarSystem>();
            }
        }

        #endregion

        #region Turn Event

        private void HandleTurnStarted(
            PlayerRuntimeData player,
            int turnNumber,
            PieceColor turnColor)
        {
            if (!isInitialized ||
                isProcessingTurn ||
                gameManager == null ||
                gameManager.IsGameEnded)
            {
                return;
            }

            if (turnColor !=
                enemyManager.EnemyColor)
            {
                return;
            }

            StopEnemyTurnRoutine();

            enemyTurnRoutine =
                StartCoroutine(
                    ExecuteEnemyTurnRoutine(
                        turnNumber
                    )
                );
        }

        #endregion

        #region Enemy Turn

        private IEnumerator ExecuteEnemyTurnRoutine(
            int turnNumber)
        {
            isProcessingTurn = true;
            pendingEnemyTurnEnd = true;

            Debug.Log(
                $"[EnemyTurnController] ★ 적 턴 시작 | " +
                $"Turn={turnNumber}"
            );

            if (turnStartDelay > 0f)
            {
                yield return new WaitForSeconds(
                    turnStartDelay
                );
            }

            CleanupRuntimeData();
            RefreshEnemyTeamVisibility();

            List<ChessPiece> enemies =
                enemyManager.GetAliveEnemies();

            Debug.Log(
                $"[EnemyTurnController] 적 수 = " +
                $"{enemies.Count}"
            );

            for (int i = 0;
                 i < enemies.Count;
                 i++)
            {
                if (gameManager == null ||
                    gameManager.IsGameEnded)
                {
                    break;
                }

                if (turnManager == null)
                    break;

                if (turnManager.CurrentTurnColor !=
                    enemyManager.EnemyColor)
                {
                    break;
                }

                ChessPiece enemy =
                    enemies[i];

                if (!CanProcessEnemy(enemy))
                    continue;

                Debug.Log(
                    $"[EnemyTurnController] " +
                    $"행동 시작 | Enemy={enemy.name}"
                );

                yield return ProcessSingleEnemyRoutine(
                    enemy,
                    turnNumber
                );

                Debug.Log(
                    $"[EnemyTurnController] " +
                    $"행동 종료 | Enemy={enemy.name}"
                );
            }

            FinishEnemyTurn();
        }
        private void FinishEnemyTurn()
        {
            if (!pendingEnemyTurnEnd)
                return;

            pendingEnemyTurnEnd = false;

            CleanupRuntimeData();

            isProcessingTurn = false;
            enemyTurnRoutine = null;

            if (gameManager == null ||
                gameManager.IsGameEnded)
            {
                return;
            }

            if (turnManager == null)
            {
                Debug.LogError(
                    "[EnemyTurnController] " +
                    "TurnManager가 없습니다."
                );

                return;
            }

            if (enemyManager == null)
            {
                Debug.LogError(
                    "[EnemyTurnController] " +
                    "EnemyManager가 없습니다."
                );

                return;
            }

            /*
             * 이미 다른 곳에서 턴을 넘겼다면
             * 다시 EndTurn하지 않는다.
             */
            if (!turnManager.IsTurnActive ||
                turnManager.CurrentTurnColor !=
                enemyManager.EnemyColor)
            {
                return;
            }

            Debug.Log(
                "[EnemyTurnController] " +
                "★ AI 행동 완료 → 적 턴 종료"
            );

            bool result =
                turnManager.EndTurn();

            Debug.Log(
                $"[EnemyTurnController] " +
                $"★ EndTurn Result={result}"
            );
        }

        private IEnumerator ProcessSingleEnemyRoutine(
            ChessPiece enemy,
            int turnNumber)
        {
            if (!CanProcessEnemy(enemy))
                yield break;

            RefreshVisibilityForEnemy(enemy);

            EnemyPerception perception =
                GetOrCreatePerception(enemy);

            perception.UpdatePerception(
                enemy,
                turnNumber
            );

            bool isStunned =
                statusEffectManager != null &&
                statusEffectManager.IsStunned(enemy);

            EnemyBlackboard blackboard =
                GetOrCreateBlackboard(enemy);

            blackboard.Refresh(
                turnNumber,
                turnManager.CurrentTurnColor,
                isStunned
            );

            EnemyAIState currentState =
                stateMachine.UpdateState(
                    blackboard
                );

            if (logStateChanges)
            {
                Debug.Log(
                    $"[EnemyTurnController] " +
                    $"State | " +
                    $"Enemy={enemy.name} | " +
                    $"State={currentState}"
                );
            }

            if (logDecisionCandidates)
            {
                LogActionCandidates(
                    blackboard
                );
            }

            EnemyAIAction selectedAction =
                decisionMaker.DecideAction(
                    blackboard
                );

            if (!selectedAction.IsValid)
            {
                selectedAction =
                    EnemyAIAction.CreateWait(
                        enemy
                    );

                blackboard.SetSelectedAction(
                    selectedAction
                );
            }

            Debug.Log(
                $"[EnemyTurnController] " +
                $"행동 결정 | " +
                $"Enemy={enemy.name} | " +
                $"State={currentState} | " +
                $"{selectedAction}"
            );

            if (beforeActionDelay > 0f)
            {
                yield return new WaitForSeconds(
                    beforeActionDelay
                );
            }

            EnemyAIExecutionResult executionResult =
                default;

            bool receivedResult = false;

            yield return actionExecutor.Execute(
                selectedAction,
                result =>
                {
                    executionResult = result;
                    receivedResult = true;
                }
            );

            if (receivedResult)
            {
                blackboard.SetExecutionResult(
                    executionResult
                );
            }

            if (!receivedResult)
            {
                Debug.LogWarning(
                    $"[EnemyTurnController] " +
                    $"행동 결과를 받지 못했습니다. | " +
                    $"Enemy={enemy.name}"
                );
            }
            else if (!executionResult.Success)
            {
                Debug.LogWarning(
                    $"[EnemyTurnController] " +
                    $"행동 실패 | " +
                    $"Enemy={enemy.name} | " +
                    $"Reason={executionResult.FailReason}"
                );
            }
            else
            {
                Debug.Log(
                    $"[EnemyTurnController] " +
                    $"행동 성공 | " +
                    $"Enemy={enemy.name} | " +
                    $"Action={selectedAction.ActionType}"
                );
            }

            RefreshVisibilityAfterAction();

            gameManager.CheckWinCondition();

            if (gameManager.IsGameEnded)
                yield break;

            if (afterActionDelay > 0f)
            {
                yield return new WaitForSeconds(
                    afterActionDelay
                );
            }
        }

        #endregion

        #region Fog And Vision

        private void RefreshVisibilityForEnemy(
            ChessPiece enemy)
        {
            if (fogOfWarSystem == null ||
                enemy == null)
            {
                return;
            }

            switch (fogOfWarSystem.AIVisibilityMode)
            {
                case AIVisibilityMode.Individual:
                    fogOfWarSystem
                        .RefreshObserverVisibility(
                            enemy
                        );
                    break;

                case AIVisibilityMode.TeamShared:
                    fogOfWarSystem
                        .RefreshTeamVisibility(
                            enemy.Color
                        );
                    break;
            }
        }

        private void RefreshVisibilityAfterAction()
        {
            fogOfWarSystem?
                .RefreshAllVisibility();
        }

        private void RefreshEnemyTeamVisibility()
        {
            if (fogOfWarSystem == null ||
                enemyManager == null)
            {
                return;
            }

            fogOfWarSystem.RefreshTeamVisibility(
                enemyManager.EnemyColor
            );
        }

        #endregion

        #region Perception, Memory And Blackboard

        private EnemyPerception GetOrCreatePerception(
            ChessPiece enemy)
        {
            if (perceptionsByEnemy.TryGetValue(
                    enemy,
                    out EnemyPerception perception))
            {
                return perception;
            }

            EnemyMemory memory =
                GetOrCreateMemory(enemy);

            perception =
                new EnemyPerception(
                    gridManager,
                    memory,
                    temporaryVisionRange
                );

            if (fogOfWarSystem != null)
            {
                perception.SetVisibilityEvaluator(
                    fogOfWarSystem.CanAISeePiece
                );

                perception.SetPositionVisibilityEvaluator(
                    fogOfWarSystem.CanAISeePosition
                );
            }
            else if (visionSystem != null)
            {
                perception.SetVisibilityEvaluator(
                    visionSystem.CanSee
                );

                perception.SetPositionVisibilityEvaluator(
                    visionSystem.CanSeePosition
                );
            }

            perceptionsByEnemy.Add(
                enemy,
                perception
            );

            return perception;
        }

        private EnemyMemory GetOrCreateMemory(
            ChessPiece enemy)
        {
            if (memoriesByEnemy.TryGetValue(
                    enemy,
                    out EnemyMemory memory))
            {
                return memory;
            }

            memory =
                new EnemyMemory(
                    memoryDurationTurns
                );

            memoriesByEnemy.Add(
                enemy,
                memory
            );

            return memory;
        }

        private EnemyBlackboard GetOrCreateBlackboard(
            ChessPiece enemy)
        {
            if (blackboardsByEnemy.TryGetValue(
                    enemy,
                    out EnemyBlackboard blackboard))
            {
                return blackboard;
            }

            EnemyMemory memory =
                GetOrCreateMemory(enemy);

            EnemyPerception perception =
                GetOrCreatePerception(enemy);

            blackboard =
                new EnemyBlackboard(
                    enemy,
                    memory,
                    perception,
                    lowHealthThreshold
                );

            EnemyPatrolAgent patrolAgent =
                enemy.GetComponent<EnemyPatrolAgent>();

            if (patrolAgent != null &&
                patrolAgent.PatrolRoute != null)
            {
                blackboard.SetPatrolRoute(
                    patrolAgent.PatrolRoute
                );
            }

            blackboardsByEnemy.Add(
                enemy,
                blackboard
            );

            return blackboard;
        }

        private void CleanupRuntimeData()
        {
            cleanupBuffer.Clear();

            CollectInvalidKeys(
                perceptionsByEnemy.Keys
            );

            CollectInvalidKeys(
                memoriesByEnemy.Keys
            );

            CollectInvalidKeys(
                blackboardsByEnemy.Keys
            );

            for (int i = 0;
                 i < cleanupBuffer.Count;
                 i++)
            {
                ChessPiece enemy =
                    cleanupBuffer[i];

                if (blackboardsByEnemy.TryGetValue(
                        enemy,
                        out EnemyBlackboard blackboard))
                {
                    blackboard.ClearRuntimeData(
                        clearMemory: false
                    );
                }

                if (perceptionsByEnemy.TryGetValue(
                        enemy,
                        out EnemyPerception perception))
                {
                    perception.Clear();
                }

                blackboardsByEnemy.Remove(enemy);
                perceptionsByEnemy.Remove(enemy);
                memoriesByEnemy.Remove(enemy);
            }

            cleanupBuffer.Clear();
        }

        private void CollectInvalidKeys(
            IEnumerable<ChessPiece> enemies)
        {
            foreach (ChessPiece enemy in enemies)
            {
                if (enemy != null &&
                    !enemy.IsDead &&
                    enemy.IsPlaced)
                {
                    continue;
                }

                if (!cleanupBuffer.Contains(enemy))
                {
                    cleanupBuffer.Add(enemy);
                }
            }
        }

        public void ClearAllAIMemory()
        {
            ClearAllAIRuntimeData();
        }

        private void ClearAllAIRuntimeData()
        {
            foreach (EnemyBlackboard blackboard
                     in blackboardsByEnemy.Values)
            {
                blackboard?.ClearRuntimeData(
                    clearMemory: true
                );
            }

            foreach (EnemyPerception perception
                     in perceptionsByEnemy.Values)
            {
                perception?.Clear();
            }

            blackboardsByEnemy.Clear();
            perceptionsByEnemy.Clear();
            memoriesByEnemy.Clear();
            cleanupBuffer.Clear();
        }

        #endregion

        #region Validation

        private static bool CanProcessEnemy(
            ChessPiece enemy)
        {
            return enemy != null &&
                   !enemy.IsDead &&
                   enemy.IsPlaced &&
                   !enemy.IsMoving;
        }

        #endregion

        #region Debug

        private void LogActionCandidates(
            EnemyBlackboard blackboard)
        {
            if (blackboard == null ||
                blackboard.Actor == null)
            {
                return;
            }

            List<EnemyAIAction> candidates =
                decisionMaker.EvaluateAllActions(
                    blackboard
                );

            Debug.Log(
                $"[EnemyTurnController] " +
                $"후보 목록 | " +
                $"Enemy={blackboard.Actor.name} | " +
                $"State={blackboard.CurrentState} | " +
                $"Count={candidates.Count}"
            );

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                Debug.Log(
                    $"[EnemyTurnController] " +
                    $"Candidate[{i}] | " +
                    $"{candidates[i]}"
                );
            }
        }

        #endregion

        #region Event Subscription

        private void Subscribe()
        {
            if (turnManager == null)
                return;

            turnManager.TurnStarted +=
                HandleTurnStarted;
        }

        private void Unsubscribe()
        {
            if (turnManager == null)
                return;

            turnManager.TurnStarted -=
                HandleTurnStarted;
        }

        #endregion

        #region Lifecycle

        private void StopEnemyTurnRoutine(
            bool finishTurn = false)
        {
            if (enemyTurnRoutine != null)
            {
                StopCoroutine(
                    enemyTurnRoutine
                );

                enemyTurnRoutine = null;
            }

            isProcessingTurn = false;

            if (finishTurn)
            {
                FinishEnemyTurn();
            }
        }
        private void OnDisable()
        {
            StopEnemyTurnRoutine();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            StopEnemyTurnRoutine();
            ClearAllAIRuntimeData();
        }

        #endregion
    }
}