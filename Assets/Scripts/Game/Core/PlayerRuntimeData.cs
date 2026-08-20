using Game.GamePlay;
using System.Collections.Generic;

namespace Game
{
    namespace Core
    {
        public class PlayerRuntimeData
        {
            public PieceColor TeamColor { get; private set; }

            public int CurrentGem { get; private set; }

            public List<ChessPiece> OwnedPieces { get; private set; } = new List<ChessPiece>();

            public PlayerRuntimeData(PieceColor teamColor)
            {
                TeamColor = teamColor;
                CurrentGem = 0;
            }

            public void SetGem(int amount)
            {
                if (amount < 0)
                    amount = 0;

                if (amount > GameRuleConfig.MaxGem)
                    amount = GameRuleConfig.MaxGem;

                CurrentGem = amount;
            }

            public void AddGem(int amount)
            {
                SetGem(CurrentGem + amount);
            }

            public bool CanSpendGem(int amount)
            {
                return CurrentGem >= amount;
            }

            public bool SpendGem(int amount)
            {
                if (amount < 0)
                    return false;

                if (CanSpendGem(amount) == false)
                    return false;

                CurrentGem -= amount;
                return true;
            }

            public void AddPiece(ChessPiece piece)
            {
                if (piece == null)
                    return;

                if (OwnedPieces.Contains(piece))
                    return;

                OwnedPieces.Add(piece);
            }

            public void RemovePiece(ChessPiece piece)
            {
                if (piece == null)
                    return;

                OwnedPieces.Remove(piece);
            }

            public bool HasAlivePieces()
            {
                for (int i = 0; i < OwnedPieces.Count; i++)
                {
                    ChessPiece piece = OwnedPieces[i];
                    if (piece != null && piece.IsDead == false)
                        return true;
                }

                return false;
            }
        }
    }
}
