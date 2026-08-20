using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * AI가 인식한 상대 유닛의 위협도와
     * 공격 우선순위를 계산한다.
     *
     * 이 클래스는 다음 작업을 하지 않는다.
     *
     * - 시야 판정
     * - 이동 경로 계산
     * - 공격 실행
     * - 스킬 실행
     *
     * EnemyPerception이 전달한 대상만 평가한다.
     */
    public sealed class EnemyThreatAnalyzer
    {
        private readonly EnemyThreatWeights
            weights;

        public EnemyThreatAnalyzer()
            : this(
                EnemyThreatWeights
                    .CreateDefault()
            )
        {
        }

        public EnemyThreatAnalyzer(
            EnemyThreatWeights weights)
        {
            this.weights = weights;
        }

        /*
         * 관찰자 기준으로 대상 하나의
         * 최종 위협 점수를 계산한다.
         */
        public float EvaluateThreat(
            ChessPiece observer,
            ChessPiece target)
        {
            if (!IsValidTarget(
                    observer,
                    target))
            {
                return float.MinValue;
            }

            float score = 0f;

            score += EvaluateAttackPower(
                target
            );

            score += EvaluateLowHealth(
                target
            );

            score += EvaluateDistance(
                observer,
                target
            );

            score += EvaluateKillOpportunity(
                observer,
                target
            );

            score += EvaluateTargetCondition(
                target
            );

            return score;
        }

        /*
         * 전달받은 대상 중 가장 높은 위협 점수를 가진
         * 상대를 찾는다.
         *
         * visibleTargets에는 EnemyPerception이
         * 현재 볼 수 있다고 판정한 대상만 전달해야 한다.
         */
        public bool TryGetHighestThreatTarget(
            ChessPiece observer,
            IReadOnlyList<ChessPiece>
                visibleTargets,
            out ChessPiece highestThreatTarget,
            out float highestThreatScore)
        {
            highestThreatTarget = null;

            highestThreatScore =
                float.MinValue;

            if (observer == null ||
                visibleTargets == null)
            {
                return false;
            }

            for (int i = 0;
                 i < visibleTargets.Count;
                 i++)
            {
                ChessPiece target =
                    visibleTargets[i];

                float score =
                    EvaluateThreat(
                        observer,
                        target
                    );

                if (score <=
                    highestThreatScore)
                {
                    continue;
                }

                highestThreatScore = score;

                highestThreatTarget =
                    target;
            }

            return highestThreatTarget != null;
        }

        /*
         * 여러 대상의 위협도 결과를
         * 점수가 높은 순서대로 반환한다.
         */
        public List<EnemyThreatResult>
            EvaluateAll(
                ChessPiece observer,
                IReadOnlyList<ChessPiece>
                    visibleTargets)
        {
            List<EnemyThreatResult> results =
                new();

            if (observer == null ||
                visibleTargets == null)
            {
                return results;
            }

            for (int i = 0;
                 i < visibleTargets.Count;
                 i++)
            {
                ChessPiece target =
                    visibleTargets[i];

                if (!IsValidTarget(
                        observer,
                        target))
                {
                    continue;
                }

                float score =
                    EvaluateThreat(
                        observer,
                        target
                    );

                results.Add(
                    new EnemyThreatResult(
                        target,
                        score
                    )
                );
            }

            results.Sort(
                CompareThreatDescending
            );

            return results;
        }

        /*
         * 공격력이 높은 상대에게 높은 점수를 준다.
         */
        private float EvaluateAttackPower(
            ChessPiece target)
        {
            return Mathf.Max(
                       0,
                       target.AttackPower
                   ) *
                   weights.AttackPowerWeight;
        }

        /*
         * 체력이 낮은 상대는 제거하기 쉬우므로
         * 공격 우선순위를 높인다.
         *
         * CurrentHP가 0에 가까울수록 높은 점수다.
         */
        private float EvaluateLowHealth(
            ChessPiece target)
        {
            if (target.MaxHP <= 0)
                return 0f;

            float healthRatio =
                Mathf.Clamp01(
                    (float)target.CurrentHP /
                    target.MaxHP
                );

            float missingHealthRatio =
                1f - healthRatio;

            return missingHealthRatio *
                   weights.LowHealthWeight;
        }

        /*
         * 가까운 대상일수록 높은 점수를 준다.
         *
         * 최대 거리 점수에서 맨해튼 거리를 차감한다.
         */
        private float EvaluateDistance(
            ChessPiece observer,
            ChessPiece target)
        {
            int distance =
                ManhattanDistance(
                    observer.GridPosition,
                    target.GridPosition
                );

            float distanceScore =
                weights.MaximumDistanceScore -
                distance *
                weights.DistancePenaltyPerTile;

            return Mathf.Max(
                0f,
                distanceScore
            );
        }

        /*
         * 관찰자의 기본 공격력으로 즉시 처치할 수 있으면
         * 추가 점수를 준다.
         *
         * 실제 공격 성공 여부는
         * EnemyDecisionMaker와 PieceActionController가
         * 별도로 검사한다.
         */
        private float EvaluateKillOpportunity(
            ChessPiece observer,
            ChessPiece target)
        {
            if (observer.AttackPower <= 0)
                return 0f;

            return observer.AttackPower >=
                   target.CurrentHP
                ? weights.KillOpportunityBonus
                : 0f;
        }

        /*
         * 현재 대상 상태에 관한 기본 보정이다.
         *
         * 추후 버프, 디버프, 왕 여부,
         * 목표물 여부 등을 이곳에 추가할 수 있다.
         */
        private float EvaluateTargetCondition(
            ChessPiece target)
        {
            if (target.IsMoving)
            {
                return weights.MovingTargetBonus;
            }

            return 0f;
        }

        private static bool IsValidTarget(
            ChessPiece observer,
            ChessPiece target)
        {
            return observer != null &&
                   target != null &&
                   observer != target &&
                   !observer.IsDead &&
                   observer.IsPlaced &&
                   !target.IsDead &&
                   target.IsPlaced &&
                   observer.Color !=
                   target.Color;
        }

        private static int CompareThreatDescending(
            EnemyThreatResult first,
            EnemyThreatResult second)
        {
            return second.ThreatScore.CompareTo(
                first.ThreatScore
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
    }

    /*
     * 위협도 계산에 사용할 가중치 모음이다.
     *
     * 지금은 일반 구조체지만,
     * 적 종류마다 다른 값을 사용하려면 추후
     * ScriptableObject로 분리할 수 있다.
     */
    public readonly struct EnemyThreatWeights
    {
        public float AttackPowerWeight
        {
            get;
        }

        public float LowHealthWeight
        {
            get;
        }

        public float MaximumDistanceScore
        {
            get;
        }

        public float DistancePenaltyPerTile
        {
            get;
        }

        public float KillOpportunityBonus
        {
            get;
        }

        public float MovingTargetBonus
        {
            get;
        }

        public EnemyThreatWeights(
            float attackPowerWeight,
            float lowHealthWeight,
            float maximumDistanceScore,
            float distancePenaltyPerTile,
            float killOpportunityBonus,
            float movingTargetBonus)
        {
            AttackPowerWeight =
                Mathf.Max(
                    0f,
                    attackPowerWeight
                );

            LowHealthWeight =
                Mathf.Max(
                    0f,
                    lowHealthWeight
                );

            MaximumDistanceScore =
                Mathf.Max(
                    0f,
                    maximumDistanceScore
                );

            DistancePenaltyPerTile =
                Mathf.Max(
                    0f,
                    distancePenaltyPerTile
                );

            KillOpportunityBonus =
                Mathf.Max(
                    0f,
                    killOpportunityBonus
                );

            MovingTargetBonus =
                Mathf.Max(
                    0f,
                    movingTargetBonus
                );
        }

        public static EnemyThreatWeights
            CreateDefault()
        {
            return new EnemyThreatWeights(
                attackPowerWeight: 3f,
                lowHealthWeight: 40f,
                maximumDistanceScore: 30f,
                distancePenaltyPerTile: 3f,
                killOpportunityBonus: 80f,
                movingTargetBonus: 5f
            );
        }
    }

    /*
     * 대상 하나의 위협도 평가 결과다.
     */
    public readonly struct EnemyThreatResult
    {
        public ChessPiece Target
        {
            get;
        }

        public float ThreatScore
        {
            get;
        }

        public EnemyThreatResult(
            ChessPiece target,
            float threatScore)
        {
            Target = target;

            ThreatScore =
                threatScore;
        }

        public override string ToString()
        {
            string targetName =
                Target != null
                    ? Target.name
                    : "None";

            return
                $"Target={targetName}, " +
                $"ThreatScore={ThreatScore:F2}";
        }
    }
}