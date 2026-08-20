using UnityEngine;

namespace Game.GamePlay.StatusEffects
{
    public enum StatusEffectTickTiming
    {
        TurnStart,
        TurnEnd
    }

    public enum StatusEffectCategory
    {
        Buff,
        Debuff
    }

    public abstract class StatusEffect
    {
        public string EffectId
        {
            get;
        }

        public int RemainingTurns
        {
            get;
            private set;
        }

        public StatusEffectTickTiming TickTiming
        {
            get;
        }

        public StatusEffectCategory Category
        {
            get;
        }

        public bool IsBuff =>
            Category ==
            StatusEffectCategory.Buff;

        public bool IsDebuff =>
            Category ==
            StatusEffectCategory.Debuff;

        public bool IsExpired =>
            RemainingTurns <= 0;

        protected StatusEffect(
            string effectId,
            int durationTurns,
            StatusEffectTickTiming tickTiming,
            StatusEffectCategory category)
        {
            EffectId =
                effectId;

            RemainingTurns =
                Mathf.Max(
                    1,
                    durationTurns
                );

            TickTiming =
                tickTiming;

            Category =
                category;
        }

        public virtual void OnApplied(
            ChessPiece target,
            StatusEffectContext context)
        {
        }

        public abstract void OnTick(
            ChessPiece target,
            StatusEffectContext context);

        public virtual void OnRemoved(
            ChessPiece target,
            StatusEffectContext context)
        {
        }

        public virtual bool CanStackWith(
            StatusEffect other)
        {
            return false;
        }

        public void RefreshDuration(
            int durationTurns)
        {
            RemainingTurns =
                Mathf.Max(
                    RemainingTurns,
                    Mathf.Max(
                        1,
                        durationTurns
                    )
                );
        }

        internal void ConsumeTurn()
        {
            RemainingTurns =
                Mathf.Max(
                    0,
                    RemainingTurns - 1
                );
        }
    }
}