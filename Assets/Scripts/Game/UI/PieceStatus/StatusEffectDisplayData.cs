using UnityEngine;

namespace Game.UI.PieceStatus
{
    public readonly struct StatusEffectDisplayData
    {
        public Sprite Icon { get; }

        public bool IsBuff
        {
            get;
        }

        public int RemainingTurns
        {
            get;
        }

        public int StackCount
        {
            get;
        }

        public StatusEffectDisplayData(
            Sprite icon,
            bool isBuff,
            int remainingTurns = 0,
            int stackCount = 1)
        {
            Icon =
                icon;

            IsBuff =
                isBuff;

            RemainingTurns =
                remainingTurns;

            StackCount =
                Mathf.Max(
                    stackCount,
                    1
                );
        }
    }

}