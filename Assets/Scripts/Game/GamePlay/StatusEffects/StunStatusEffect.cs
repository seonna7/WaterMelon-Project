namespace Game.GamePlay.StatusEffects
{
    public sealed class StunStatusEffect : StatusEffect
    {
        public const string Id = "Stun";

        public StunStatusEffect(
            int durationTurns)
            : base(
                Id,
                durationTurns,
                StatusEffectTickTiming.TurnStart,
                StatusEffectCategory.Debuff)
        {
        }

        public override void OnTick(
            ChessPiece target,
            StatusEffectContext context)
        {
            // 행동 차단은 StatusEffectManager.IsStunned()으로 판정한다.
        }
    }
}