using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * AI 행동 후보들의 효용 점수를 계산하고
     * 가장 가치가 높은 행동을 선택한다.
     *
     * 담당:
     * - Move 점수 계산
     * - DirectAttack 점수 계산
     * - Skill 점수 계산
     * - Wait 점수 계산
     * - 행동 후보 비교
     *
     * 담당하지 않음:
     * - 시야 판정
     * - 공격 가능 범위 판정
     * - 이동 가능 범위 계산
     * - 실제 행동 실행
     *
     * EnemyDecisionMaker가 유효한 후보만 생성한 뒤
     * 이 클래스에 전달해야 한다.
     */
    public sealed class EnemyUtilityEvaluator
    {
        private readonly EnemyUtilityWeights weights;

        private readonly EnemyThreatAnalyzer
            threatAnalyzer;

        public EnemyUtilityEvaluator()
            : this(
                EnemyUtilityWeights.CreateDefault(),
                new EnemyThreatAnalyzer()
            )
        {
        }

        public EnemyUtilityEvaluator(
            EnemyUtilityWeights weights,
            EnemyThreatAnalyzer threatAnalyzer)
        {
            this.weights = weights;

            this.threatAnalyzer =
                threatAnalyzer ??
                new EnemyThreatAnalyzer();
        }

        /*
         * 행동 하나의 최종 효용 점수를 계산한다.
         *
         * 계산된 점수가 포함된 새로운 EnemyAIAction을
         * 반환한다.
         */
        public EnemyAIAction Evaluate(
            EnemyAIAction action)
        {
            if (!action.IsValid)
            {
                return EnemyAIAction.CreateNone();
            }

            float score;

            switch (action.ActionType)
            {
                case EnemyAIActionType.Move:
                    score =
                        EvaluateMove(action);
                    break;

                case EnemyAIActionType.DirectAttack:
                    score =
                        EvaluateDirectAttack(action);
                    break;

                case EnemyAIActionType.UseSkill:
                    score =
                        EvaluateSkill(action);
                    break;

                case EnemyAIActionType.Wait:
                    score =
                        EvaluateWait(action);
                    break;

                default:
                    return EnemyAIAction.CreateNone();
            }

            return action.WithUtilityScore(
                score
            );
        }

        /*
         * 여러 행동 후보 중 가장 높은 점수의
         * 행동을 반환한다.
         */
        public EnemyAIAction SelectBestAction(
            IReadOnlyList<EnemyAIAction>
                candidates)
        {
            if (candidates == null ||
                candidates.Count == 0)
            {
                return EnemyAIAction.CreateNone();
            }

            EnemyAIAction bestAction =
                EnemyAIAction.CreateNone();

            float bestScore =
                float.MinValue;

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                EnemyAIAction candidate =
                    candidates[i];

                if (!candidate.IsValid)
                    continue;

                EnemyAIAction evaluated =
                    Evaluate(candidate);

                if (!evaluated.IsValid)
                    continue;

                if (evaluated.UtilityScore <=
                    bestScore)
                {
                    continue;
                }

                bestScore =
                    evaluated.UtilityScore;

                bestAction = evaluated;
            }

            return bestAction;
        }

        /*
         * 모든 후보를 평가한 뒤
         * 점수가 높은 순서대로 반환한다.
         *
         * AI 디버깅 로그나 행동 분석 UI에 사용할 수 있다.
         */
        public List<EnemyAIAction>
            EvaluateAndSort(
                IReadOnlyList<EnemyAIAction>
                    candidates)
        {
            List<EnemyAIAction> results =
                new();

            if (candidates == null)
                return results;

            for (int i = 0;
                 i < candidates.Count;
                 i++)
            {
                EnemyAIAction candidate =
                    candidates[i];

                if (!candidate.IsValid)
                    continue;

                EnemyAIAction evaluated =
                    Evaluate(candidate);

                if (evaluated.IsValid)
                {
                    results.Add(evaluated);
                }
            }

            results.Sort(
                CompareUtilityDescending
            );

            return results;
        }

        private float EvaluateMove(
            EnemyAIAction action)
        {
            ChessPiece actor =
                action.Actor;

            if (!IsValidActor(actor))
                return float.MinValue;

            float score =
                weights.MoveBaseScore;

            /*
             * DecisionMaker나 PathFinder가 미리 넣은 점수를
             * 경로 평가 보너스로 사용한다.
             */
            score +=
                action.UtilityScore *
                weights.ExternalScoreWeight;

            /*
             * 맵 가장자리로 이동하면 밀치기 위험이 있으므로
             * 약한 감점을 준다.
             *
             * 정확한 보드 크기 판정은 EnemyPathFinder가
             * 담당하므로 여기서는 음수 좌표만 방어한다.
             */
            if (action.TargetPosition.x < 0 ||
                action.TargetPosition.y < 0)
            {
                score -=
                    weights.InvalidPositionPenalty;
            }

            /*
             * 체력이 낮을수록 이동 행동의 가치가 조금 증가한다.
             *
             * 추후 MoveToward와 Retreat 행동을 구분하면
             * 후퇴 행동에만 적용하도록 변경할 수 있다.
             */
            float lowHealthRatio =
                GetMissingHealthRatio(actor);

            score +=
                lowHealthRatio *
                weights.LowHealthMoveBonus;

            return score;
        }

        private float EvaluateDirectAttack(
            EnemyAIAction action)
        {
            ChessPiece actor =
                action.Actor;

            ChessPiece target =
                action.TargetPiece;

            if (!IsValidOpponent(
                    actor,
                    target))
            {
                return float.MinValue;
            }

            float score =
                weights.DirectAttackBaseScore;

            /*
             * 위협도가 높은 상대를 공격하는 행동에
             * 추가 점수를 준다.
             */
            float threatScore =
                threatAnalyzer.EvaluateThreat(
                    actor,
                    target
                );

            if (threatScore >
                float.MinValue)
            {
                score +=
                    threatScore *
                    weights.ThreatScoreWeight;
            }

            /*
             * 이번 기본 공격으로 처치 가능하면
             * 높은 보너스를 부여한다.
             */
            if (actor.AttackPower >=
                target.CurrentHP)
            {
                score +=
                    weights.KillBonus;
            }

            /*
             * 대상의 체력이 낮을수록
             * 마무리 공격의 가치가 증가한다.
             */
            score +=
                GetMissingHealthRatio(target) *
                weights.WoundedTargetBonus;

            /*
             * 자신의 체력이 매우 낮으면
             * 근접 공격 행동에 약한 감점을 준다.
             */
            score -=
                GetMissingHealthRatio(actor) *
                weights.LowHealthAttackPenalty;

            score +=
                action.UtilityScore *
                weights.ExternalScoreWeight;

            return score;
        }

        private float EvaluateSkill(
            EnemyAIAction action)
        {
            ChessPiece actor =
                action.Actor;

            if (!IsValidActor(actor))
                return float.MinValue;

            if (actor.GetSkill(
                    action.SkillSlot) == null)
            {
                return float.MinValue;
            }

            float score =
                weights.SkillBaseScore;

            ChessPiece target =
                action.TargetPiece;

            if (target != null)
            {
                if (target.Color !=
                    actor.Color)
                {
                    score +=
                        EvaluateHostileSkillTarget(
                            actor,
                            target
                        );
                }
                else
                {
                    score +=
                        EvaluateFriendlySkillTarget(
                            target
                        );
                }
            }
            else
            {
                /*
                 * 대상 유닛이 없는 설치형·지역형 스킬은
                 * 기본 위치 스킬 보너스를 사용한다.
                 */
                score +=
                    weights.PositionSkillBonus;
            }

            /*
             * Skill1과 Skill2의 기본 우선순위를
             * 서로 다르게 둘 수 있다.
             */
            switch (action.SkillSlot)
            {
                case Game.GamePlay.Skill
                    .SkillSlot.Skill1:

                    score +=
                        weights.Skill1Bonus;
                    break;

                case Game.GamePlay.Skill
                    .SkillSlot.Skill2:

                    score +=
                        weights.Skill2Bonus;
                    break;
            }

            score +=
                action.UtilityScore *
                weights.ExternalScoreWeight;

            return score;
        }

        private float EvaluateHostileSkillTarget(
            ChessPiece actor,
            ChessPiece target)
        {
            if (!IsValidOpponent(
                    actor,
                    target))
            {
                return float.MinValue;
            }

            float score =
                weights.HostileSkillTargetBonus;

            float threatScore =
                threatAnalyzer.EvaluateThreat(
                    actor,
                    target
                );

            if (threatScore >
                float.MinValue)
            {
                score +=
                    threatScore *
                    weights.ThreatScoreWeight;
            }

            score +=
                GetMissingHealthRatio(target) *
                weights.WoundedTargetBonus;

            return score;
        }

        private float EvaluateFriendlySkillTarget(
            ChessPiece target)
        {
            if (target == null ||
                target.IsDead ||
                !target.IsPlaced)
            {
                return float.MinValue;
            }

            /*
             * 회복·보호·버프 스킬의 정확한 종류는
             * SkillStrategy 정보가 확장된 후 구분한다.
             *
             * 현재는 체력이 낮은 아군을 대상으로 하는
             * 스킬에 높은 점수를 부여한다.
             */
            return
                weights.FriendlySkillTargetBonus +
                GetMissingHealthRatio(target) *
                weights.InjuredAllyBonus;
        }

        private float EvaluateWait(
            EnemyAIAction action)
        {
            ChessPiece actor =
                action.Actor;

            if (!IsValidActor(actor))
                return float.MinValue;

            float score =
                weights.WaitBaseScore;

            /*
             * 체력이 낮다고 대기만 반복하지 않도록
             * Wait에는 매우 작은 체력 보정만 적용한다.
             */
            score +=
                GetMissingHealthRatio(actor) *
                weights.LowHealthWaitBonus;

            return score;
        }

        private static float GetMissingHealthRatio(
            ChessPiece piece)
        {
            if (piece == null ||
                piece.MaxHP <= 0)
            {
                return 0f;
            }

            float healthRatio =
                Mathf.Clamp01(
                    (float)piece.CurrentHP /
                    piece.MaxHP
                );

            return 1f - healthRatio;
        }

        private static bool IsValidActor(
            ChessPiece actor)
        {
            return actor != null &&
                   !actor.IsDead &&
                   actor.IsPlaced &&
                   !actor.IsMoving;
        }

        private static bool IsValidOpponent(
            ChessPiece actor,
            ChessPiece target)
        {
            return IsValidActor(actor) &&
                   target != null &&
                   !target.IsDead &&
                   target.IsPlaced &&
                   target != actor &&
                   target.Color != actor.Color;
        }

        private static int
            CompareUtilityDescending(
                EnemyAIAction first,
                EnemyAIAction second)
        {
            return second.UtilityScore
                .CompareTo(
                    first.UtilityScore
                );
        }
    }

    /*
     * Utility AI의 행동별 가중치다.
     *
     * 추후 적 타입별 ScriptableObject로 옮기면
     * 공격형, 수비형, 지원형 AI 성향을
     * 데이터만으로 생성할 수 있다.
     */
    public readonly struct EnemyUtilityWeights
    {
        public float MoveBaseScore
        {
            get;
        }

        public float DirectAttackBaseScore
        {
            get;
        }

        public float SkillBaseScore
        {
            get;
        }

        public float WaitBaseScore
        {
            get;
        }

        public float ExternalScoreWeight
        {
            get;
        }

        public float ThreatScoreWeight
        {
            get;
        }

        public float KillBonus
        {
            get;
        }

        public float WoundedTargetBonus
        {
            get;
        }

        public float LowHealthAttackPenalty
        {
            get;
        }

        public float LowHealthMoveBonus
        {
            get;
        }

        public float LowHealthWaitBonus
        {
            get;
        }

        public float HostileSkillTargetBonus
        {
            get;
        }

        public float FriendlySkillTargetBonus
        {
            get;
        }

        public float InjuredAllyBonus
        {
            get;
        }

        public float PositionSkillBonus
        {
            get;
        }

        public float Skill1Bonus
        {
            get;
        }

        public float Skill2Bonus
        {
            get;
        }

        public float InvalidPositionPenalty
        {
            get;
        }

        public EnemyUtilityWeights(
            float moveBaseScore,
            float directAttackBaseScore,
            float skillBaseScore,
            float waitBaseScore,
            float externalScoreWeight,
            float threatScoreWeight,
            float killBonus,
            float woundedTargetBonus,
            float lowHealthAttackPenalty,
            float lowHealthMoveBonus,
            float lowHealthWaitBonus,
            float hostileSkillTargetBonus,
            float friendlySkillTargetBonus,
            float injuredAllyBonus,
            float positionSkillBonus,
            float skill1Bonus,
            float skill2Bonus,
            float invalidPositionPenalty)
        {
            MoveBaseScore = moveBaseScore;

            DirectAttackBaseScore =
                directAttackBaseScore;

            SkillBaseScore =
                skillBaseScore;

            WaitBaseScore =
                waitBaseScore;

            ExternalScoreWeight =
                Mathf.Max(
                    0f,
                    externalScoreWeight
                );

            ThreatScoreWeight =
                Mathf.Max(
                    0f,
                    threatScoreWeight
                );

            KillBonus =
                Mathf.Max(
                    0f,
                    killBonus
                );

            WoundedTargetBonus =
                Mathf.Max(
                    0f,
                    woundedTargetBonus
                );

            LowHealthAttackPenalty =
                Mathf.Max(
                    0f,
                    lowHealthAttackPenalty
                );

            LowHealthMoveBonus =
                Mathf.Max(
                    0f,
                    lowHealthMoveBonus
                );

            LowHealthWaitBonus =
                Mathf.Max(
                    0f,
                    lowHealthWaitBonus
                );

            HostileSkillTargetBonus =
                Mathf.Max(
                    0f,
                    hostileSkillTargetBonus
                );

            FriendlySkillTargetBonus =
                Mathf.Max(
                    0f,
                    friendlySkillTargetBonus
                );

            InjuredAllyBonus =
                Mathf.Max(
                    0f,
                    injuredAllyBonus
                );

            PositionSkillBonus =
                Mathf.Max(
                    0f,
                    positionSkillBonus
                );

            Skill1Bonus =
                skill1Bonus;

            Skill2Bonus =
                skill2Bonus;

            InvalidPositionPenalty =
                Mathf.Max(
                    0f,
                    invalidPositionPenalty
                );
        }

        public static EnemyUtilityWeights
            CreateDefault()
        {
            return new EnemyUtilityWeights(
                moveBaseScore: 30f,
                directAttackBaseScore: 80f,
                skillBaseScore: 90f,
                waitBaseScore: 1f,

                externalScoreWeight: 0.25f,
                threatScoreWeight: 0.5f,

                killBonus: 120f,
                woundedTargetBonus: 40f,

                lowHealthAttackPenalty: 20f,
                lowHealthMoveBonus: 15f,
                lowHealthWaitBonus: 2f,

                hostileSkillTargetBonus: 20f,
                friendlySkillTargetBonus: 15f,
                injuredAllyBonus: 60f,

                positionSkillBonus: 10f,

                skill1Bonus: 0f,
                skill2Bonus: 5f,

                invalidPositionPenalty: 1000f
            );
        }
    }
}