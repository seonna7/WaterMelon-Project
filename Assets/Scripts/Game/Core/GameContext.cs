using Game.GamePlay;
using Game.GamePlay.Grid;

namespace Game
{
    namespace Core
    {
        public class GameContext
        {
            public PlayerRuntimeData WhitePlayer { get; private set; }

            public PlayerRuntimeData BlackPlayer { get; private set; }

            public GridManager Grid { get; private set; }

            public GameContext(GridManager grid)
            {
                Grid = grid;

                WhitePlayer = new PlayerRuntimeData(PieceColor.White);
                BlackPlayer = new PlayerRuntimeData(PieceColor.Black);
            }

            public PlayerRuntimeData GetPlayer(PieceColor color)
            {
                return color == PieceColor.White ? WhitePlayer : BlackPlayer;
            }

            public PlayerRuntimeData GetOpponent(PieceColor color)
            {
                return color == PieceColor.White ? BlackPlayer : WhitePlayer;
            }
        }
    }
}
