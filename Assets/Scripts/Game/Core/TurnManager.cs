using Game.GamePlay;
using UnityEngine;

namespace Game.Core
{
    public sealed class TurnManager
    {
        public int CurrentTurnNumber { get; private set; }
        public PieceColor CurrentTurnColor { get; private set; } = PieceColor.White;
        public TurnState CurrentTurnState { get; private set; } = TurnState.None;
        public bool IsTurnActive => CurrentTurnState == TurnState.TurnStarted;

        public event System.Action<PlayerRuntimeData, int, PieceColor> TurnStarting;
        public event System.Action<PlayerRuntimeData, int, PieceColor> TurnStarted;
        public event System.Action<PlayerRuntimeData, int, PieceColor> TurnEnding;
        public event System.Action<PlayerRuntimeData, int, PieceColor> TurnEnded;

        private readonly GameContext context;

        public TurnManager(GameContext gameContext)
        {
            context = gameContext;
        }

        public PlayerRuntimeData GetCurrentPlayer()
        {
            return context.GetPlayer(CurrentTurnColor);
        }

        public PlayerRuntimeData GetOpponentPlayer()
        {
            return context.GetOpponent(CurrentTurnColor);
        }

        public void StartFirstTurn(PieceColor firstColor = PieceColor.White)
        {
            if (CurrentTurnState != TurnState.None)
                return;

            CurrentTurnNumber = 1;
            CurrentTurnColor = firstColor;
            BeginTurn();
        }

        public bool EndTurn()
        {
            if (!IsTurnActive)
                return false;

            PlayerRuntimeData endingPlayer = GetCurrentPlayer();
            CurrentTurnState = TurnState.TurnEnded;

            TurnEnding?.Invoke(
                endingPlayer,
                CurrentTurnNumber,
                CurrentTurnColor
            );

            Debug.Log(
                $"[TurnManager] Turn End | Turn={CurrentTurnNumber} | " +
                $"Player={CurrentTurnColor}"
            );

            TurnEnded?.Invoke(
                endingPlayer,
                CurrentTurnNumber,
                CurrentTurnColor
            );

            AdvanceToNextTurn();
            return true;
        }

        private void AdvanceToNextTurn()
        {
            CurrentTurnColor =
                CurrentTurnColor == PieceColor.White
                    ? PieceColor.Black
                    : PieceColor.White;

            CurrentTurnNumber++;
            BeginTurn();
        }

        private void BeginTurn()
        {
            PlayerRuntimeData currentPlayer = GetCurrentPlayer();
            CurrentTurnState = TurnState.TurnStarted;

            TurnStarting?.Invoke(
                currentPlayer,
                CurrentTurnNumber,
                CurrentTurnColor
            );

            currentPlayer.AddGem(GameRuleConfig.TurnGemIncrease);

            Debug.Log(
                $"[TurnManager] Turn Start | Turn={CurrentTurnNumber} | " +
                $"Player={CurrentTurnColor} | Gem={currentPlayer.CurrentGem}"
            );

            if (ShouldTriggerMapShrink())
            {
                Debug.Log(
                    $"[TurnManager] Map Shrink Trigger | Turn={CurrentTurnNumber}"
                );
            }

            TurnStarted?.Invoke(
                currentPlayer,
                CurrentTurnNumber,
                CurrentTurnColor
            );
        }

        public bool ShouldTriggerMapShrink()
        {
            if (CurrentTurnNumber <= 0)
                return false;

            return CurrentTurnNumber %
                   GameRuleConfig.ShrinkTurnInterval == 0;
        }
    }
}