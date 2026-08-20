namespace Game.GamePlay.Attack
{
    public sealed class AttackResult
    {
        public bool Success { get; }
        public AttackFailReason FailReason { get; }
        public ChessPiece Attacker { get; }
        public ChessPiece Target { get; }
        public int Damage { get; }
        public int TargetHPBefore { get; }
        public int TargetHPAfter { get; }
        public bool TargetKilled { get; }
        public bool TargetRemoved { get; }

        private AttackResult(
            bool success,
            AttackFailReason failReason,
            ChessPiece attacker,
            ChessPiece target,
            int damage,
            int targetHPBefore,
            int targetHPAfter,
            bool targetKilled,
            bool targetRemoved)
        {
            Success = success;
            FailReason = failReason;
            Attacker = attacker;
            Target = target;
            Damage = damage;
            TargetHPBefore = targetHPBefore;
            TargetHPAfter = targetHPAfter;
            TargetKilled = targetKilled;
            TargetRemoved = targetRemoved;
        }

        public static AttackResult CreateSuccess(
            ChessPiece attacker,
            ChessPiece target,
            int damage,
            int targetHPBefore,
            int targetHPAfter,
            bool targetKilled,
            bool targetRemoved)
        {
            return new AttackResult(
                true,
                AttackFailReason.None,
                attacker,
                target,
                damage,
                targetHPBefore,
                targetHPAfter,
                targetKilled,
                targetRemoved
            );
        }

        public static AttackResult CreateFail(
            AttackFailReason failReason,
            ChessPiece attacker = null,
            ChessPiece target = null)
        {
            int currentHP = target != null ? target.CurrentHP : 0;

            return new AttackResult(
                false,
                failReason,
                attacker,
                target,
                0,
                currentHP,
                currentHP,
                false,
                false
            );
        }
    }
}