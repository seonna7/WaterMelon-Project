using UnityEngine;

namespace Game.GamePlay.Skill
{
    public struct SkillResult
    {
        public bool Success;

        public string FailReason;

        public ChessPiece Caster;

        public ChessPiece Target;

        public Vector2Int TargetPosition;

        public int AppliedDamage;

        public int AppliedHeal;

        public int AppliedShield;

        public bool PushApplied;

        public bool TargetKilled;

        public bool TargetRemoved;

        public static SkillResult CreateSuccess(
            ChessPiece caster,
            ChessPiece target = null,
            Vector2Int targetPosition = default)
        {
            return new SkillResult
            {
                Success = true,
                FailReason = string.Empty,
                Caster = caster,
                Target = target,
                TargetPosition = targetPosition
            };
        }

        public static SkillResult CreateFail(
            ChessPiece caster,
            string failReason)
        {
            return new SkillResult
            {
                Success = false,
                FailReason = failReason,
                Caster = caster,
                Target = null,
                TargetPosition = default
            };
        }
    }
}
