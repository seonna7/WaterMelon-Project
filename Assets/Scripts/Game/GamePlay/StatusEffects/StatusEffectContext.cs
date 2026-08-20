using Game.Core;

namespace Game.GamePlay.StatusEffects
{
    public readonly struct StatusEffectContext
    {
        public GameManager GameManager { get; }
        public TurnManager TurnManager { get; }
        public int TurnNumber { get; }
        public PieceColor TurnColor { get; }

        public StatusEffectContext(
            GameManager gameManager,
            TurnManager turnManager,
            int turnNumber,
            PieceColor turnColor)
        {
            GameManager = gameManager;
            TurnManager = turnManager;
            TurnNumber = turnNumber;
            TurnColor = turnColor;
        }
    }
}