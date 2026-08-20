using Game.Action;
using Game.GamePlay;
using Game.GamePlay.AI;
using Game.GamePlay.Grid;
using Game.GamePlay.StatusEffects;
using UnityEngine;

namespace Game.Core
{
    public class GameManager : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] private GridManager grid;

        [Header("Runtime Systems")]
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private StatusEffectManager statusEffectManager;
        [SerializeField] private EnemyTurnController enemyTurnController;

        public GameContext Context { get; private set; }
        public PhaseManager PhaseManager { get; private set; }
        public TurnManager TurnManager { get; private set; }
        public PieceActionController PieceActionController { get; private set; }

        public EnemyManager EnemyManager => enemyManager;
        public StatusEffectManager StatusEffectManager => statusEffectManager;

        public bool IsGameStarted { get; private set; }
        public bool IsGameEnded { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (grid == null)
                grid = FindFirstObjectByType<GridManager>();

            if (grid == null)
            {
                Debug.LogError(
                    "[GameManager] GridManager not found."
                );
                return;
            }

            Context = new GameContext(grid);
            PhaseManager = new PhaseManager();
            TurnManager = new TurnManager(Context);

            PieceActionController =
                new PieceActionController(
                    new ActionContext(this)
                );

            ResolveRuntimeSystems();

            statusEffectManager?.Initialize(this);
            enemyManager?.Initialize(this);
            enemyTurnController?.Initialize(this);

            IsGameStarted = false;
            IsGameEnded = false;
        }

        private void ResolveRuntimeSystems()
        {
            if (enemyManager == null)
                enemyManager = FindFirstObjectByType<EnemyManager>();

            if (statusEffectManager == null)
                statusEffectManager =
                    FindFirstObjectByType<StatusEffectManager>();

            if (enemyTurnController == null)
                enemyTurnController =
                    FindFirstObjectByType<EnemyTurnController>();
        }

        private void Start()
        {
            StartGame();
        }

        public void StartGame()
        {
            if (IsGameStarted)
                return;

            IsGameStarted = true;
            IsGameEnded = false;

            PhaseManager.SetPhase(GamePhase.Pick);

            Debug.Log("[GameManager] Game Start");
        }

        public void StartPlacementPhase()
        {
            if (IsGameEnded)
                return;

            PhaseManager.SetPhase(GamePhase.Placement);
        }

        public void StartBattlePhase()
        {
            if (IsGameEnded)
                return;

            PhaseManager.SetPhase(GamePhase.Battle);
            TurnManager.StartFirstTurn();
        }

        public void EndGame(PieceColor winner)
        {
            if (IsGameEnded)
                return;

            IsGameEnded = true;
            PhaseManager.SetPhase(GamePhase.End);

            Debug.Log(
                $"[GameManager] Game End | Winner={winner}"
            );
        }

        public void CheckWinCondition()
        {
            if (IsGameEnded || Context == null)
                return;

            bool whiteAlive =
                Context.WhitePlayer.HasAlivePieces();

            bool blackAlive =
                enemyManager != null
                    ? enemyManager.HasAliveEnemies()
                    : Context.BlackPlayer.HasAlivePieces();

            if (!whiteAlive && !blackAlive)
            {
                Debug.Log("[GameManager] Draw");
                IsGameEnded = true;
                PhaseManager.SetPhase(GamePhase.End);
                return;
            }

            if (!whiteAlive)
            {
                EndGame(PieceColor.Black);
                return;
            }

            if (!blackAlive)
                EndGame(PieceColor.White);
        }
    }
}