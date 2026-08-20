using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * AI가 현재 한 번의 행동으로 이동할 위치를 선택한다.
     *
     * 실제 이동 가능 범위 계산:
     * ChessPiece.GetPossibleMoves()
     *
     * 실제 이동 실행:
     * EnemyActionExecutor
     *
     * EnemyPathFinder는 이동 후보를 평가하고
     * 가장 적합한 칸만 반환한다.
     */
    public sealed class EnemyPathFinder
    {
        private readonly GridManager gridManager;

        private readonly EnemyPathWeights weights;

        private readonly List<Vector2Int>
            candidateBuffer = new();

        public EnemyPathFinder(
            GridManager gridManager)
            : this(
                gridManager,
                EnemyPathWeights.CreateDefault()
            )
        {
        }

        public EnemyPathFinder(
            GridManager gridManager,
            EnemyPathWeights weights)
        {
            this.gridManager = gridManager;
            this.weights = weights;
        }

        /*
         * 목표 위치에 가장 가까워지는 이동 칸을 찾는다.
         *
         * 현재 위치보다 목표와 가까워지는 칸이 없으면
         * false를 반환한다.
         */
        public bool TryFindMoveToward(
            ChessPiece actor,
            Vector2Int destination,
            out Vector2Int bestPosition)
        {
            bestPosition =
                actor != null
                    ? actor.GridPosition
                    : default;

            if (!IsValidActor(actor) ||
                !IsValidDestination(destination))
            {
                return false;
            }

            CollectMoveCandidates(actor);

            if (candidateBuffer.Count == 0)
                return false;

            int currentDistance =
                ManhattanDistance(
                    actor.GridPosition,
                    destination
                );

            float bestScore =
                float.MinValue;

            bool found = false;

            for (int i = 0;
                 i < candidateBuffer.Count;
                 i++)
            {
                Vector2Int candidate =
                    candidateBuffer[i];

                int candidateDistance =
                    ManhattanDistance(
                        candidate,
                        destination
                    );

                /*
                 * 목표에서 더 멀어지는 이동은 제외한다.
                 */
                if (candidateDistance >=
                    currentDistance)
                {
                    continue;
                }

                float score =
                    EvaluateTowardScore(
                        actor,
                        candidate,
                        destination
                    );

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPosition = candidate;
                found = true;
            }

            return found;
        }

        /*
         * 목표와 가까워지는 칸이 없는 경우에도
         * 가능한 칸 중 가장 가까운 위치를 반환한다.
         *
         * 장애물에 막혀 우회해야 하는 상황에서 사용한다.
         */
        public bool TryFindClosestReachableMove(
            ChessPiece actor,
            Vector2Int destination,
            out Vector2Int bestPosition)
        {
            bestPosition =
                actor != null
                    ? actor.GridPosition
                    : default;

            if (!IsValidActor(actor) ||
                !IsValidDestination(destination))
            {
                return false;
            }

            CollectMoveCandidates(actor);

            if (candidateBuffer.Count == 0)
                return false;

            float bestScore =
                float.MinValue;

            bool found = false;

            for (int i = 0;
                 i < candidateBuffer.Count;
                 i++)
            {
                Vector2Int candidate =
                    candidateBuffer[i];

                float score =
                    EvaluateTowardScore(
                        actor,
                        candidate,
                        destination
                    );

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPosition = candidate;
                found = true;
            }

            return found;
        }

        /*
         * 특정 대상으로부터 가장 멀어지는 칸을 선택한다.
         *
         * 체력이 낮은 적이나 원거리형 AI의
         * 후퇴 행동에 사용할 수 있다.
         */
        public bool TryFindMoveAway(
            ChessPiece actor,
            Vector2Int dangerPosition,
            out Vector2Int bestPosition)
        {
            bestPosition =
                actor != null
                    ? actor.GridPosition
                    : default;

            if (!IsValidActor(actor) ||
                !IsValidDestination(dangerPosition))
            {
                return false;
            }

            CollectMoveCandidates(actor);

            if (candidateBuffer.Count == 0)
                return false;

            int currentDistance =
                ManhattanDistance(
                    actor.GridPosition,
                    dangerPosition
                );

            float bestScore =
                float.MinValue;

            bool found = false;

            for (int i = 0;
                 i < candidateBuffer.Count;
                 i++)
            {
                Vector2Int candidate =
                    candidateBuffer[i];

                int candidateDistance =
                    ManhattanDistance(
                        candidate,
                        dangerPosition
                    );

                if (candidateDistance <=
                    currentDistance)
                {
                    continue;
                }

                float score =
                    EvaluateRetreatScore(
                        actor,
                        candidate,
                        dangerPosition
                    );

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPosition = candidate;
                found = true;
            }

            return found;
        }

        /*
         * 특정 목표 주변에서 지정한 거리를 유지할 수 있는
         * 이동 칸을 선택한다.
         *
         * 원거리 공격 유닛이나 지원형 AI에 사용할 수 있다.
         */
        public bool TryFindMoveAtPreferredRange(
            ChessPiece actor,
            Vector2Int targetPosition,
            int preferredRange,
            out Vector2Int bestPosition)
        {
            bestPosition =
                actor != null
                    ? actor.GridPosition
                    : default;

            if (!IsValidActor(actor) ||
                !IsValidDestination(targetPosition))
            {
                return false;
            }

            preferredRange =
                Mathf.Max(0, preferredRange);

            CollectMoveCandidates(actor);

            if (candidateBuffer.Count == 0)
                return false;

            float bestScore =
                float.MinValue;

            bool found = false;

            for (int i = 0;
                 i < candidateBuffer.Count;
                 i++)
            {
                Vector2Int candidate =
                    candidateBuffer[i];

                int distance =
                    ManhattanDistance(
                        candidate,
                        targetPosition
                    );

                int rangeDifference =
                    Mathf.Abs(
                        distance -
                        preferredRange
                    );

                float score =
                    -rangeDifference *
                    weights
                        .PreferredRangeDifferencePenalty;

                score +=
                    EvaluateCommonPositionScore(
                        actor,
                        candidate
                    );

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPosition = candidate;
                found = true;
            }

            return found;
        }

        /*
         * 기억한 위치에 도착한 뒤 주변을 조사할 때 사용할
         * 수색 위치를 선택한다.
         */
        public bool TryFindSearchMove(
            ChessPiece actor,
            Vector2Int searchCenter,
            out Vector2Int bestPosition)
        {
            bestPosition =
                actor != null
                    ? actor.GridPosition
                    : default;

            if (!IsValidActor(actor) ||
                !IsValidDestination(searchCenter))
            {
                return false;
            }

            CollectMoveCandidates(actor);

            if (candidateBuffer.Count == 0)
                return false;

            float bestScore =
                float.MinValue;

            bool found = false;

            for (int i = 0;
                 i < candidateBuffer.Count;
                 i++)
            {
                Vector2Int candidate =
                    candidateBuffer[i];

                int distance =
                    ManhattanDistance(
                        candidate,
                        searchCenter
                    );

                /*
                 * 기억 위치와 너무 멀리 떨어진 칸은 제외한다.
                 */
                if (distance >
                    weights.MaximumSearchDistance)
                {
                    continue;
                }

                float score =
                    -distance *
                    weights.SearchDistancePenalty;

                score +=
                    EvaluateCommonPositionScore(
                        actor,
                        candidate
                    );

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPosition = candidate;
                found = true;
            }

            return found;
        }

        public List<EnemyPathResult>
            EvaluateMovesToward(
                ChessPiece actor,
                Vector2Int destination)
        {
            List<EnemyPathResult> results =
                new();

            if (!IsValidActor(actor) ||
                !IsValidDestination(destination))
            {
                return results;
            }

            CollectMoveCandidates(actor);

            for (int i = 0;
                 i < candidateBuffer.Count;
                 i++)
            {
                Vector2Int candidate =
                    candidateBuffer[i];

                float score =
                    EvaluateTowardScore(
                        actor,
                        candidate,
                        destination
                    );

                results.Add(
                    new EnemyPathResult(
                        candidate,
                        score,
                        ManhattanDistance(
                            candidate,
                            destination
                        )
                    )
                );
            }

            results.Sort(
                CompareScoreDescending
            );

            return results;
        }

        private void CollectMoveCandidates(
            ChessPiece actor)
        {
            candidateBuffer.Clear();

            if (!IsValidActor(actor))
                return;

            List<Vector2Int> possibleMoves =
                actor.GetPossibleMoves(
                    gridManager
                );

            if (possibleMoves == null)
                return;

            for (int i = 0;
                 i < possibleMoves.Count;
                 i++)
            {
                Vector2Int position =
                    possibleMoves[i];

                if (!IsValidMovePosition(
                        position))
                {
                    continue;
                }

                if (candidateBuffer.Contains(
                        position))
                {
                    continue;
                }

                candidateBuffer.Add(
                    position
                );
            }
        }

        private float EvaluateTowardScore(
            ChessPiece actor,
            Vector2Int candidate,
            Vector2Int destination)
        {
            int distance =
                ManhattanDistance(
                    candidate,
                    destination
                );

            float score =
                -distance *
                weights.DistancePenaltyPerTile;

            score +=
                EvaluateCommonPositionScore(
                    actor,
                    candidate
                );

            return score;
        }

        private float EvaluateRetreatScore(
            ChessPiece actor,
            Vector2Int candidate,
            Vector2Int dangerPosition)
        {
            int distance =
                ManhattanDistance(
                    candidate,
                    dangerPosition
                );

            float score =
                distance *
                weights.RetreatDistanceReward;

            score +=
                EvaluateCommonPositionScore(
                    actor,
                    candidate
                );

            return score;
        }

        /*
         * 목표 방향과 관계없이 공통으로 적용되는 위치 점수다.
         *
         * 추후 이곳에 다음 요소를 연결할 수 있다.
         *
         * - 부쉬 은신 보너스
         * - 안개 내부 위치 보너스
         * - 적 공격 범위 위험도
         * - 아군 근접 보너스
         * - 고지대 보너스
         * - 지형 이동 비용
         */
        private float EvaluateCommonPositionScore(
            ChessPiece actor,
            Vector2Int candidate)
        {
            float score = 0f;

            score +=
                EvaluateBoardEdgePenalty(
                    candidate
                );

            score +=
                EvaluateAdjacentAllyScore(
                    actor,
                    candidate
                );

            return score;
        }

        /*
         * 맵 가장자리는 밀치기로 탈락할 위험이 있으므로
         * 기본적으로 약한 감점을 준다.
         */
        private float EvaluateBoardEdgePenalty(
            Vector2Int position)
        {
            if (gridManager == null)
                return 0f;

            bool isEdge =
                position.x == 0 ||
                position.y == 0 ||
                position.x ==
                    gridManager.GridWidth - 1 ||
                position.y ==
                    gridManager.GridHeight - 1;

            return isEdge
                ? -weights.BoardEdgePenalty
                : 0f;
        }

        /*
         * 이동 목적지 주변에 같은 팀 말이 있으면
         * 약한 보너스를 부여한다.
         */
        private float EvaluateAdjacentAllyScore(
            ChessPiece actor,
            Vector2Int position)
        {
            if (actor == null ||
                gridManager == null)
            {
                return 0f;
            }

            int adjacentAllies = 0;

            for (int i = 0;
                 i < CardinalDirections.Length;
                 i++)
            {
                Vector2Int adjacentPosition =
                    position +
                    CardinalDirections[i];

                if (!gridManager.IsInsideGrid(
                        adjacentPosition))
                {
                    continue;
                }

                ChessPiece adjacentPiece =
                    gridManager.GetPieceAt(
                        adjacentPosition
                    );

                if (adjacentPiece == null ||
                    adjacentPiece.IsDead ||
                    adjacentPiece == actor)
                {
                    continue;
                }

                if (adjacentPiece.Color ==
                    actor.Color)
                {
                    adjacentAllies++;
                }
            }

            return adjacentAllies *
                   weights.AdjacentAllyBonus;
        }

        private bool IsValidMovePosition(
            Vector2Int position)
        {
            return gridManager != null &&
                   gridManager.IsInsideGrid(
                       position
                   ) &&
                   gridManager.IsEmpty(
                       position
                   );
        }

        private bool IsValidDestination(
            Vector2Int destination)
        {
            return gridManager != null &&
                   gridManager.IsInsideGrid(
                       destination
                   );
        }

        private bool IsValidActor(
            ChessPiece actor)
        {
            return actor != null &&
                   !actor.IsDead &&
                   actor.IsPlaced &&
                   !actor.IsMoving &&
                   gridManager != null;
        }

        private static int
            CompareScoreDescending(
                EnemyPathResult first,
                EnemyPathResult second)
        {
            return second.Score.CompareTo(
                first.Score
            );
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

        private static readonly Vector2Int[]
            CardinalDirections =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };
    }

    public readonly struct EnemyPathResult
    {
        public Vector2Int Position
        {
            get;
        }

        public float Score
        {
            get;
        }

        public int DistanceToDestination
        {
            get;
        }

        public EnemyPathResult(
            Vector2Int position,
            float score,
            int distanceToDestination)
        {
            Position = position;
            Score = score;

            DistanceToDestination =
                distanceToDestination;
        }

        public override string ToString()
        {
            return
                $"Position={Position}, " +
                $"Score={Score:F2}, " +
                $"Distance={DistanceToDestination}";
        }
    }

    public readonly struct EnemyPathWeights
    {
        public float DistancePenaltyPerTile
        {
            get;
        }

        public float RetreatDistanceReward
        {
            get;
        }

        public float PreferredRangeDifferencePenalty
        {
            get;
        }

        public float BoardEdgePenalty
        {
            get;
        }

        public float AdjacentAllyBonus
        {
            get;
        }

        public int MaximumSearchDistance
        {
            get;
        }

        public float SearchDistancePenalty
        {
            get;
        }

        public EnemyPathWeights(
            float distancePenaltyPerTile,
            float retreatDistanceReward,
            float preferredRangeDifferencePenalty,
            float boardEdgePenalty,
            float adjacentAllyBonus,
            int maximumSearchDistance,
            float searchDistancePenalty)
        {
            DistancePenaltyPerTile =
                Mathf.Max(
                    0f,
                    distancePenaltyPerTile
                );

            RetreatDistanceReward =
                Mathf.Max(
                    0f,
                    retreatDistanceReward
                );

            PreferredRangeDifferencePenalty =
                Mathf.Max(
                    0f,
                    preferredRangeDifferencePenalty
                );

            BoardEdgePenalty =
                Mathf.Max(
                    0f,
                    boardEdgePenalty
                );

            AdjacentAllyBonus =
                Mathf.Max(
                    0f,
                    adjacentAllyBonus
                );

            MaximumSearchDistance =
                Mathf.Max(
                    0,
                    maximumSearchDistance
                );

            SearchDistancePenalty =
                Mathf.Max(
                    0f,
                    searchDistancePenalty
                );
        }

        public static EnemyPathWeights
            CreateDefault()
        {
            return new EnemyPathWeights(
                distancePenaltyPerTile: 10f,
                retreatDistanceReward: 8f,
                preferredRangeDifferencePenalty: 12f,
                boardEdgePenalty: 8f,
                adjacentAllyBonus: 2f,
                maximumSearchDistance: 3,
                searchDistancePenalty: 4f
            );
        }
    }
}