using Game.Core;
using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Fog
{
    /*
     * 부쉬 안에 있는 유닛의 은신 여부를 판정한다.
     *
     * VisionSystem의 거리 및 Line Of Sight 판정을
     * 통과한 뒤 추가로 호출된다.
     *
     * 현재 기본 규칙:
     *
     * 1. 대상이 부쉬 밖이면 항상 보인다.
     * 2. 관찰자와 대상이 같은 팀이면 보인다.
     * 3. 관찰자가 대상 가까이에 있으면 보인다.
     * 4. 대상이 공격·스킬 등으로 노출 상태면 보인다.
     * 5. 위 조건을 만족하지 않는 부쉬 유닛은 보이지 않는다.
     */
    public sealed class BushStealthResolver
        : MonoBehaviour
    {
        private sealed class RevealData
        {
            public ChessPiece Target;

            /*
             * 이 턴 번호까지 대상이 노출된다.
             *
             * 예:
             * RevealedUntilTurn = 5
             * CurrentTurnNumber = 5
             * → 아직 보임
             *
             * CurrentTurnNumber = 6
             * → 노출 종료
             */
            public int RevealedUntilTurn;
        }

        [Header("References")]
        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private GameManager gameManager;

        [SerializeField]
        private VisionSystem visionSystem;

        [Header("Bush Visibility")]
        [Tooltip(
            "관찰자와 부쉬 안 대상 사이의 거리가 " +
            "이 값 이하이면 은신을 무시하고 발견합니다."
        )]
        [SerializeField]
        [Min(0)]
        private int closeRevealDistance = 1;

        [Tooltip(
            "같은 팀 유닛은 부쉬 은신과 관계없이 " +
            "항상 서로 볼 수 있습니다."
        )]
        [SerializeField]
        private bool alliesAlwaysVisible = true;

        [Tooltip(
            "관찰자도 부쉬 안에 있으면 가까운 부쉬 대상을 " +
            "조금 더 쉽게 발견하도록 합니다."
        )]
        [SerializeField]
        private bool bushObserverGetsBonus = true;

        [SerializeField]
        [Min(0)]
        private int bushObserverBonusDistance = 1;

        [Header("Reveal")]
        [Tooltip(
            "공격 또는 스킬 사용 후 기본 노출 지속 턴입니다."
        )]
        [SerializeField]
        [Min(0)]
        private int defaultRevealDurationTurns = 1;

        [Header("Debug")]
        [SerializeField]
        private bool logVisibilityChecks;

        private readonly Dictionary<
            ChessPiece,
            RevealData> revealDataByTarget =
                new();

        private readonly List<ChessPiece>
            cleanupBuffer = new();

        private TurnManager turnManager;

        private int currentTurnNumber;

        private void Awake()
        {
            ResolveReferences();
        }

        private void Start()
        {
            Initialize();
        }

        private void ResolveReferences()
        {
            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<
                        GridManager>();
            }

            if (gameManager == null)
            {
                gameManager =
                    FindFirstObjectByType<
                        GameManager>();
            }

            if (visionSystem == null)
            {
                visionSystem =
                    FindFirstObjectByType<
                        VisionSystem>();
            }
        }

        private void Initialize()
        {
            Unsubscribe();

            if (gameManager != null)
            {
                turnManager =
                    gameManager.TurnManager;
            }

            if (turnManager != null)
            {
                currentTurnNumber =
                    turnManager.CurrentTurnNumber;

                Subscribe();
            }

            /*
             * VisionSystem의 최종 유닛 가시 판정에
             * 부쉬 은신 규칙을 연결한다.
             */
            if (visionSystem != null)
            {
                visionSystem.SetTargetVisibilityRule(
                    IsTargetVisible
                );
            }
        }

        /*
         * VisionSystem에서 호출할 최종 부쉬 가시 판정이다.
         *
         * 거리와 장애물 시야 판정은 이미
         * VisionSystem에서 통과한 상태라고 가정한다.
         */
        public bool IsTargetVisible(
            ChessPiece observer,
            ChessPiece target)
        {
            if (!IsValidPiece(observer) ||
                !IsValidPiece(target))
            {
                return false;
            }

            if (observer == target)
                return true;

            /*
             * 아군끼리는 부쉬 은신을 적용하지 않는다.
             */
            if (alliesAlwaysVisible &&
                observer.Color == target.Color)
            {
                return true;
            }

            /*
             * 대상이 부쉬에 없다면
             * 추가 은신 판정 없이 보인다.
             */
            if (!IsInBush(target))
            {
                return true;
            }

            /*
             * 공격 또는 스킬 사용으로 노출된 대상이다.
             */
            if (IsRevealed(target))
            {
                LogResult(
                    observer,
                    target,
                    true,
                    "Revealed"
                );

                return true;
            }

            int revealDistance =
                closeRevealDistance;

            /*
             * 관찰자도 부쉬에 있으면
             * 발견 거리를 늘릴 수 있다.
             */
            if (bushObserverGetsBonus &&
                IsInBush(observer))
            {
                revealDistance +=
                    bushObserverBonusDistance;
            }

            int distance =
                ManhattanDistance(
                    observer.GridPosition,
                    target.GridPosition
                );

            bool visible =
                distance <= revealDistance;

            LogResult(
                observer,
                target,
                visible,
                visible
                    ? "CloseRange"
                    : "HiddenInBush"
            );

            return visible;
        }

        /*
         * 해당 유닛이 현재 부쉬 칸에 있는지 확인한다.
         */
        public bool IsInBush(
            ChessPiece piece)
        {
            if (!IsValidPiece(piece) ||
                gridManager == null)
            {
                return false;
            }

            return IsBushPosition(
                piece.GridPosition
            );
        }

        public bool IsBushPosition(
            Vector2Int position)
        {
            if (gridManager == null ||
                !gridManager.IsInsideGrid(
                    position))
            {
                return false;
            }

            return gridManager.GetTileTypeAt(
                       position
                   ) == TileType.Bush;
        }

        /*
         * 공격 또는 스킬을 사용한 유닛을
         * 기본 지속시간만큼 노출한다.
         */
        public void RevealTarget(
            ChessPiece target)
        {
            RevealTarget(
                target,
                defaultRevealDurationTurns
            );
        }

        /*
         * 지정한 턴 수 동안 대상을 노출한다.
         *
         * durationTurns = 0이면 현재 턴 동안만 노출된다.
         */
        public void RevealTarget(
            ChessPiece target,
            int durationTurns)
        {
            if (!IsValidPiece(target))
                return;

            durationTurns =
                Mathf.Max(
                    0,
                    durationTurns
                );

            int revealedUntilTurn =
                currentTurnNumber +
                durationTurns;

            if (revealDataByTarget.TryGetValue(
                    target,
                    out RevealData existingData))
            {
                existingData.RevealedUntilTurn =
                    Mathf.Max(
                        existingData
                            .RevealedUntilTurn,
                        revealedUntilTurn
                    );

                return;
            }

            revealDataByTarget.Add(
                target,
                new RevealData
                {
                    Target = target,

                    RevealedUntilTurn =
                        revealedUntilTurn
                }
            );

            Debug.Log(
                $"[BushStealthResolver] " +
                $"Target Revealed | " +
                $"Target={target.name} | " +
                $"UntilTurn={revealedUntilTurn}"
            );
        }

        public bool IsRevealed(
            ChessPiece target)
        {
            if (target == null)
                return false;

            if (!revealDataByTarget.TryGetValue(
                    target,
                    out RevealData revealData))
            {
                return false;
            }

            if (revealData == null ||
                revealData.Target == null)
            {
                revealDataByTarget.Remove(
                    target
                );

                return false;
            }

            return currentTurnNumber <=
                   revealData.RevealedUntilTurn;
        }

        public void ClearReveal(
            ChessPiece target)
        {
            if (target == null)
                return;

            revealDataByTarget.Remove(
                target
            );
        }

        public void ClearAllReveals()
        {
            revealDataByTarget.Clear();
            cleanupBuffer.Clear();
        }

        /*
         * TurnManager의 TurnStarted 이벤트에서 호출된다.
         * 만료된 노출 정보를 정리한다.
         */
        private void HandleTurnStarted(
            PlayerRuntimeData player,
            int turnNumber,
            PieceColor turnColor)
        {
            currentTurnNumber =
                turnNumber;

            CleanupExpiredReveals();
        }

        private void CleanupExpiredReveals()
        {
            cleanupBuffer.Clear();

            foreach (KeyValuePair<
                         ChessPiece,
                         RevealData> pair
                     in revealDataByTarget)
            {
                ChessPiece target =
                    pair.Key;

                RevealData data =
                    pair.Value;

                if (target == null ||
                    data == null ||
                    target.IsDead ||
                    !target.IsPlaced)
                {
                    cleanupBuffer.Add(
                        target
                    );

                    continue;
                }

                if (currentTurnNumber >
                    data.RevealedUntilTurn)
                {
                    cleanupBuffer.Add(
                        target
                    );
                }
            }

            for (int i = 0;
                 i < cleanupBuffer.Count;
                 i++)
            {
                revealDataByTarget.Remove(
                    cleanupBuffer[i]
                );
            }

            cleanupBuffer.Clear();
        }

        private void LogResult(
            ChessPiece observer,
            ChessPiece target,
            bool visible,
            string reason)
        {
            if (!logVisibilityChecks)
                return;

            Debug.Log(
                $"[BushStealthResolver] " +
                $"Observer={observer.name} | " +
                $"Target={target.name} | " +
                $"Visible={visible} | " +
                $"Reason={reason}"
            );
        }

        private static bool IsValidPiece(
            ChessPiece piece)
        {
            return piece != null &&
                   !piece.IsDead &&
                   piece.IsPlaced;
        }

        private static int ManhattanDistance(
            Vector2Int first,
            Vector2Int second)
        {
            return Mathf.Abs(
                       first.x - second.x
                   ) +
                   Mathf.Abs(
                       first.y - second.y
                   );
        }

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

        private void OnDestroy()
        {
            Unsubscribe();

            if (visionSystem != null)
            {
                visionSystem
                    .ClearTargetVisibilityRule();
            }

            ClearAllReveals();
        }
    }
}