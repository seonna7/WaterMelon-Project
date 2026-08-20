using UnityEngine;

namespace Game.GamePlay.StatusEffects
{
    public sealed class PoisonStatusEffect : StatusEffect
    {
        public const string Id = "Poison";
        private readonly int damagePerTurn;

        public PoisonStatusEffect(
            int damagePerTurn,
            int durationTurns)
            : base(
                Id,
                durationTurns,
                StatusEffectTickTiming.TurnStart,
                StatusEffectCategory.Debuff)
        {
            this.damagePerTurn =
                Mathf.Max(
                    0,
                    damagePerTurn
                );
        }

        public override void OnTick(
            ChessPiece target,
            StatusEffectContext context)
        {
            if (target == null || target.IsDead || damagePerTurn <= 0)
                return;

            int hpBefore = target.CurrentHP;
            target.TakeDamage(damagePerTurn);

            Debug.Log(
                $"[StatusEffect] Poison | Target={target.name} | " +
                $"Damage={hpBefore - target.CurrentHP} | " +
                $"Remaining={RemainingTurns - 1}"
            );

            context.GameManager?.CheckWinCondition();
        }
    }
}
