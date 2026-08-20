using Game.GamePlay.Skill;
using UnityEngine;

namespace Game.GamePlay.AI
{
    public enum EnemyAIActionType
    {
        None,
        Move,
        DirectAttack,
        UseSkill,
        Wait
    }

    /*
     * EnemyDecisionMaker가 선택한 행동 정보를
     * EnemyActionExecutor로 전달하기 위한 데이터다.
     *
     * 이 구조체 자체는 행동을 실행하지 않는다.
     */
    public readonly struct EnemyAIAction
    {
        public EnemyAIActionType ActionType
        {
            get;
        }

        public ChessPiece Actor
        {
            get;
        }

        public ChessPiece TargetPiece
        {
            get;
        }

        public Vector2Int TargetPosition
        {
            get;
        }

        public SkillSlot SkillSlot
        {
            get;
        }

        /*
         * Utility AI에서 행동을 비교할 때 사용한다.
         *
         * 현재는 직접 지정하며,
         * 추후 EnemyUtilityEvaluator가 계산한다.
         */
        public float UtilityScore
        {
            get;
        }

        public bool HasActor =>
            Actor != null;

        public bool HasTargetPiece =>
            TargetPiece != null;

        public bool IsValid =>
            ActionType != EnemyAIActionType.None &&
            Actor != null;

        private EnemyAIAction(
            EnemyAIActionType actionType,
            ChessPiece actor,
            ChessPiece targetPiece,
            Vector2Int targetPosition,
            SkillSlot skillSlot,
            float utilityScore)
        {
            ActionType = actionType;
            Actor = actor;
            TargetPiece = targetPiece;
            TargetPosition = targetPosition;
            SkillSlot = skillSlot;
            UtilityScore = utilityScore;
        }

        public static EnemyAIAction CreateNone()
        {
            return new EnemyAIAction(
                EnemyAIActionType.None,
                null,
                null,
                default,
                default,
                0f
            );
        }

        public static EnemyAIAction CreateMove(
            ChessPiece actor,
            Vector2Int targetPosition,
            float utilityScore = 0f)
        {
            return new EnemyAIAction(
                EnemyAIActionType.Move,
                actor,
                null,
                targetPosition,
                default,
                utilityScore
            );
        }

        public static EnemyAIAction CreateDirectAttack(
            ChessPiece actor,
            ChessPiece targetPiece,
            float utilityScore = 0f)
        {
            Vector2Int targetPosition =
                targetPiece != null
                    ? targetPiece.GridPosition
                    : default;

            return new EnemyAIAction(
                EnemyAIActionType.DirectAttack,
                actor,
                targetPiece,
                targetPosition,
                default,
                utilityScore
            );
        }

        public static EnemyAIAction CreateSkill(
            ChessPiece actor,
            SkillSlot skillSlot,
            Vector2Int targetPosition,
            ChessPiece targetPiece = null,
            float utilityScore = 0f)
        {
            return new EnemyAIAction(
                EnemyAIActionType.UseSkill,
                actor,
                targetPiece,
                targetPosition,
                skillSlot,
                utilityScore
            );
        }

        public static EnemyAIAction CreateWait(
            ChessPiece actor,
            float utilityScore = 0f)
        {
            return new EnemyAIAction(
                EnemyAIActionType.Wait,
                actor,
                null,
                actor != null
                    ? actor.GridPosition
                    : default,
                default,
                utilityScore
            );
        }

        public EnemyAIAction WithUtilityScore(
            float utilityScore)
        {
            return new EnemyAIAction(
                ActionType,
                Actor,
                TargetPiece,
                TargetPosition,
                SkillSlot,
                utilityScore
            );
        }

        public override string ToString()
        {
            string actorName =
                Actor != null
                    ? Actor.name
                    : "None";

            string targetName =
                TargetPiece != null
                    ? TargetPiece.name
                    : "None";

            return
                $"Action={ActionType}, " +
                $"Actor={actorName}, " +
                $"Target={targetName}, " +
                $"Position={TargetPosition}, " +
                $"SkillSlot={SkillSlot}, " +
                $"Score={UtilityScore:F2}";
        }
    }
}