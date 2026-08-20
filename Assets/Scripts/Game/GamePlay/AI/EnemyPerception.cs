using Game.GamePlay.Grid;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * 하나의 AI 유닛이 현재 인식할 수 있는
     * 상대 유닛과 위치를 계산한다.
     *
     * 개별 시야와 팀 공유 시야의 실제 정책은
     * FogOfWarSystem에 위임한다.
     */
    public sealed class EnemyPerception
    {
        private readonly GridManager gridManager;

        private readonly EnemyMemory memory;

        private readonly List<ChessPiece>
            visibleOpponents = new();

        private readonly List<Vector2Int>
            visiblePositions = new();

        /*
         * 특정 유닛을 볼 수 있는지 판정한다.
         *
         * 연결 예:
         * FogOfWarSystem.CanAISeePiece
         */
        private Func<
            ChessPiece,
            ChessPiece,
            bool> visibilityEvaluator;

        /*
         * 특정 위치를 볼 수 있는지 판정한다.
         *
         * 연결 예:
         * FogOfWarSystem.CanAISeePosition
         */
        private Func<
            ChessPiece,
            Vector2Int,
            bool> positionVisibilityEvaluator;

        private int temporaryVisionRange;

        public IReadOnlyList<ChessPiece>
            VisibleOpponents =>
                visibleOpponents;

        public IReadOnlyList<Vector2Int>
            VisiblePositions =>
                visiblePositions;

        public EnemyMemory Memory =>
            memory;

        public int TemporaryVisionRange =>
            temporaryVisionRange;

        public EnemyPerception(
            GridManager gridManager,
            EnemyMemory memory,
            int temporaryVisionRange = 4)
        {
            this.gridManager =
                gridManager;

            this.memory =
                memory ??
                new EnemyMemory();

            this.temporaryVisionRange =
                Mathf.Max(
                    0,
                    temporaryVisionRange
                );
        }

        /*
         * 현재 인식 정보를 전부 갱신한다.
         *
         * 반드시 AI가 행동을 결정하기 직전에 호출한다.
         */
        public void UpdatePerception(
            ChessPiece observer,
            int currentTurn)
        {
            visibleOpponents.Clear();
            visiblePositions.Clear();

            memory.BeginPerceptionUpdate();

            if (!IsValidObserver(observer))
            {
                memory.RemoveExpiredMemories(
                    currentTurn
                );

                return;
            }

            BuildVisiblePositions(
                observer
            );

            FindVisibleOpponents(
                observer,
                currentTurn
            );

            memory.RemoveExpiredMemories(
                currentTurn
            );
        }

        public bool CanSeeTarget(
            ChessPiece observer,
            ChessPiece target)
        {
            if (!IsValidObserver(observer) ||
                !IsValidTarget(
                    observer,
                    target))
            {
                return false;
            }

            /*
             * FogOfWarSystem 또는 VisionSystem이
             * 연결된 경우 해당 판정을 사용한다.
             */
            if (visibilityEvaluator != null)
            {
                return visibilityEvaluator.Invoke(
                    observer,
                    target
                );
            }

            /*
             * 외부 판정기가 없을 때만
             * 임시 거리 기반 판정을 사용한다.
             */
            return IsInsideTemporaryVision(
                observer.GridPosition,
                target.GridPosition
            );
        }

        public bool CanSeePosition(
            ChessPiece observer,
            Vector2Int position)
        {
            if (!IsValidObserver(observer) ||
                gridManager == null ||
                !gridManager.IsInsideGrid(
                    position))
            {
                return false;
            }

            /*
             * 개별 또는 팀 공유 위치 시야 판정을 사용한다.
             */
            if (positionVisibilityEvaluator != null)
            {
                return positionVisibilityEvaluator
                    .Invoke(
                        observer,
                        position
                    );
            }

            return IsInsideTemporaryVision(
                observer.GridPosition,
                position
            );
        }

        public bool IsCurrentlyVisible(
            ChessPiece target)
        {
            if (target == null)
                return false;

            return visibleOpponents.Contains(
                target
            );
        }

        public bool IsPositionCurrentlyVisible(
            Vector2Int position)
        {
            return visiblePositions.Contains(
                position
            );
        }

        public bool TryGetNearestVisibleOpponent(
            ChessPiece observer,
            out ChessPiece nearestOpponent)
        {
            nearestOpponent = null;

            if (!IsValidObserver(observer))
                return false;

            int nearestDistance =
                int.MaxValue;

            for (int i = 0;
                 i < visibleOpponents.Count;
                 i++)
            {
                ChessPiece target =
                    visibleOpponents[i];

                if (target == null ||
                    target.IsDead ||
                    !target.IsPlaced)
                {
                    continue;
                }

                int distance =
                    ManhattanDistance(
                        observer.GridPosition,
                        target.GridPosition
                    );

                if (distance >=
                    nearestDistance)
                {
                    continue;
                }

                nearestDistance =
                    distance;

                nearestOpponent =
                    target;
            }

            return nearestOpponent != null;
        }

        /*
         * 유닛 가시 판정기를 연결한다.
         */
        public void SetVisibilityEvaluator(
            Func<
                ChessPiece,
                ChessPiece,
                bool> evaluator)
        {
            visibilityEvaluator =
                evaluator;
        }

        /*
         * 위치 가시 판정기를 연결한다.
         */
        public void SetPositionVisibilityEvaluator(
            Func<
                ChessPiece,
                Vector2Int,
                bool> evaluator)
        {
            positionVisibilityEvaluator =
                evaluator;
        }

        public void ClearVisibilityEvaluator()
        {
            visibilityEvaluator = null;
        }

        public void
            ClearPositionVisibilityEvaluator()
        {
            positionVisibilityEvaluator = null;
        }

        public void ClearVisibilityEvaluators()
        {
            visibilityEvaluator = null;
            positionVisibilityEvaluator = null;
        }

        public void SetTemporaryVisionRange(
            int visionRange)
        {
            temporaryVisionRange =
                Mathf.Max(
                    0,
                    visionRange
                );
        }

        public void Clear()
        {
            visibleOpponents.Clear();
            visiblePositions.Clear();

            memory.Clear();

            ClearVisibilityEvaluators();
        }

        /*
         * 외부 위치 판정기가 있다면
         * 그리드 전체에서 해당 AI가 볼 수 있는 위치를 수집한다.
         *
         * 외부 판정기가 없으면 임시 시야 거리 주변만 조사한다.
         */
        private void BuildVisiblePositions(
            ChessPiece observer)
        {
            if (gridManager == null)
                return;

            if (positionVisibilityEvaluator != null)
            {
                BuildVisiblePositionsFromEvaluator(
                    observer
                );

                return;
            }

            BuildTemporaryVisiblePositions(
                observer
            );
        }

        private void
            BuildVisiblePositionsFromEvaluator(
                ChessPiece observer)
        {
            for (int x = 0;
                 x < gridManager.GridWidth;
                 x++)
            {
                for (int y = 0;
                     y < gridManager.GridHeight;
                     y++)
                {
                    Vector2Int position =
                        new Vector2Int(
                            x,
                            y
                        );

                    if (!CanSeePosition(
                            observer,
                            position))
                    {
                        continue;
                    }

                    visiblePositions.Add(
                        position
                    );
                }
            }
        }

        private void BuildTemporaryVisiblePositions(
            ChessPiece observer)
        {
            Vector2Int observerPosition =
                observer.GridPosition;

            int minimumX =
                Mathf.Max(
                    0,
                    observerPosition.x -
                    temporaryVisionRange
                );

            int maximumX =
                Mathf.Min(
                    gridManager.GridWidth - 1,
                    observerPosition.x +
                    temporaryVisionRange
                );

            int minimumY =
                Mathf.Max(
                    0,
                    observerPosition.y -
                    temporaryVisionRange
                );

            int maximumY =
                Mathf.Min(
                    gridManager.GridHeight - 1,
                    observerPosition.y +
                    temporaryVisionRange
                );

            for (int x = minimumX;
                 x <= maximumX;
                 x++)
            {
                for (int y = minimumY;
                     y <= maximumY;
                     y++)
                {
                    Vector2Int position =
                        new Vector2Int(
                            x,
                            y
                        );

                    if (!CanSeePosition(
                            observer,
                            position))
                    {
                        continue;
                    }

                    visiblePositions.Add(
                        position
                    );
                }
            }
        }

        /*
         * 보이는 위치에 존재하는 상대를 조사한다.
         *
         * 위치가 보이더라도 부쉬 은신으로 유닛 자체가
         * 보이지 않을 수 있으므로 CanSeeTarget()을
         * 반드시 다시 호출한다.
         */
        private void FindVisibleOpponents(
            ChessPiece observer,
            int currentTurn)
        {
            if (gridManager == null)
                return;

            for (int i = 0;
                 i < visiblePositions.Count;
                 i++)
            {
                Vector2Int position =
                    visiblePositions[i];

                ChessPiece target =
                    gridManager.GetPieceAt(
                        position
                    );

                if (!IsValidTarget(
                        observer,
                        target))
                {
                    continue;
                }

                if (!CanSeeTarget(
                        observer,
                        target))
                {
                    continue;
                }

                if (!visibleOpponents.Contains(
                        target))
                {
                    visibleOpponents.Add(
                        target
                    );
                }

                memory.RememberVisibleTarget(
                    target,
                    currentTurn
                );
            }
        }

        private bool IsInsideTemporaryVision(
            Vector2Int observerPosition,
            Vector2Int targetPosition)
        {
            int distance =
                ManhattanDistance(
                    observerPosition,
                    targetPosition
                );

            return distance <=
                   temporaryVisionRange;
        }

        private static bool IsValidObserver(
            ChessPiece observer)
        {
            return observer != null &&
                   !observer.IsDead &&
                   observer.IsPlaced;
        }

        private static bool IsValidTarget(
            ChessPiece observer,
            ChessPiece target)
        {
            return target != null &&
                   !target.IsDead &&
                   target.IsPlaced &&
                   observer != target &&
                   observer.Color !=
                   target.Color;
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
    }
}