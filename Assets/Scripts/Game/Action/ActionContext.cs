using Game.Core;
using Game.GamePlay.Grid;

namespace Game
{
    namespace Action
    {
        public class ActionContext
        {
            public GameManager GameManager { get; private set; }

            public GameContext GameContext { get; private set; }

            public TurnManager TurnManager { get; private set; }

            public GridManager Grid { get; private set; }

            public ActionContext(GameManager gameManager)
            {
                GameManager = gameManager;
                GameContext = gameManager.Context;
                TurnManager = gameManager.TurnManager;
                Grid = gameManager.Context.Grid;
            }
        }
    }
}
