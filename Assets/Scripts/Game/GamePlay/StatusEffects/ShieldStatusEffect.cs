using UnityEngine;

namespace Game.GamePlay.StatusEffects
{
    public sealed class ShieldStatusEffect
        : StatusEffect
    {
        public const string Id =
            "Shield";

        private int shieldAmount;

        public int ShieldAmount =>
            shieldAmount;

        public bool HasShield =>
            shieldAmount > 0;

        public ShieldStatusEffect(
            int shieldAmount,
            int durationTurns)
            : base(
                Id,
                durationTurns,
                StatusEffectTickTiming.TurnStart,
                StatusEffectCategory.Buff)
        {
            this.shieldAmount =
                Mathf.Max(
                    0,
                    shieldAmount
                );
        }

        public override void OnApplied(
            ChessPiece target,
            StatusEffectContext context)
        {
            if (target == null)
                return;

            Debug.Log(
                $"[StatusEffect] Shield Applied | " +
                $"Target={target.name} | " +
                $"Shield={shieldAmount} | " +
                $"Duration={RemainingTurns}"
            );
        }

        public override void OnTick(
            ChessPiece target,
            StatusEffectContext context)
        {
            /*
             * Shield는 턴마다 별도 효과를
             * 발생시키지 않는다.
             *
             * StatusEffectManager에서
             * RemainingTurns만 감소한다.
             */
        }

        public override void OnRemoved(
            ChessPiece target,
            StatusEffectContext context)
        {
            if (target == null)
                return;

            Debug.Log(
                $"[StatusEffect] Shield Removed | " +
                $"Target={target.name}"
            );

            shieldAmount =
                0;
        }

        /*
         * 들어오는 피해를 Shield가 먼저 흡수한다.
         *
         * 반환값:
         * Shield 적용 후 실제 HP에 들어가야 할 피해.
         *
         * 예:
         *
         * Shield = 5
         * Damage = 8
         *
         * Shield → 0
         * 반환값 → 3
         */
        public int AbsorbDamage(
            int incomingDamage)
        {
            if (incomingDamage <= 0)
                return 0;

            if (shieldAmount <= 0)
                return incomingDamage;

            int absorbedDamage =
                Mathf.Min(
                    shieldAmount,
                    incomingDamage
                );

            shieldAmount -=
                absorbedDamage;

            int remainingDamage =
                incomingDamage -
                absorbedDamage;

            Debug.Log(
                $"[StatusEffect] Shield Absorb | " +
                $"Incoming={incomingDamage} | " +
                $"Absorbed={absorbedDamage} | " +
                $"ShieldRemaining={shieldAmount} | " +
                $"DamageRemaining={remainingDamage}"
            );

            return remainingDamage;
        }

        public override bool CanStackWith(
            StatusEffect other)
        {
            /*
             * 현재는 Shield 중첩 불가.
             *
             * 동일 Shield가 다시 들어오면
             * StatusEffectManager의 기존 로직에 따라
             * 지속시간 Refresh 대상으로 사용한다.
             */
            return false;
        }
    }
}