using Game.Core;
using Game.GamePlay.AI;
using Game.GamePlay.Grid;
using Game.UI.PieceStatus;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Placement
{
    public sealed class BattlePieceSpawner
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private EnemyManager enemyManager;

        [SerializeField]
        private PieceWorldUIManager pieceWorldUIManager;

        [Header("Piece Roots")]
        [SerializeField]
        private Transform whiteRoot;

        [SerializeField]
        private Transform blackRoot;

        [Header("Spawn Data")]
        [SerializeField]
        private PieceSpawnData spawnData;

        [SerializeField]
        private bool spawnOnStart = true;

        private bool hasSpawned;

        private readonly List<ChessPiece>
            spawnedPieces = new();

        private readonly List<ChessPiece>
            unplacedPieces = new();

        public IReadOnlyList<ChessPiece>
            SpawnedPieces =>
                spawnedPieces;

        public IReadOnlyList<ChessPiece>
            UnplacedPieces =>
                unplacedPieces;

        public PieceSpawnData SpawnData =>
            spawnData;

        public bool HasSpawned =>
            hasSpawned;

        private void Awake()
        {
            DisableScenePreviewInstances();

            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<
                        GridManager>();
            }

            if (enemyManager == null)
            {
                enemyManager =
                    FindFirstObjectByType<
                        EnemyManager>();
            }
            if (pieceWorldUIManager == null)
            {
                pieceWorldUIManager =
                    FindFirstObjectByType<
                        PieceWorldUIManager>();
            }

        }

        private void DisableScenePreviewInstances()
        {
            PieceSpawnPreviewInstance[] previews =
                FindObjectsByType<
                    PieceSpawnPreviewInstance>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            for (int i = 0;
                 i < previews.Length;
                 i++)
            {
                PieceSpawnPreviewInstance preview =
                    previews[i];

                if (preview == null)
                    continue;

                GameObject previewObject =
                    preview.gameObject;

                previewObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (spawnOnStart)
            {
                SpawnAllPieces();
            }
        }

        /*
         * SpawnData에 정의된 말을 전부
         * 지정 위치에 바로 배치한다.
         *
         * 테스트 또는 완전 자동 배치용.
         */
        public void SpawnAllPieces()
        {
            if (hasSpawned)
            {
                Debug.LogWarning(
                    "[BattlePieceSpawner] " +
                    "이미 SpawnAllPieces가 실행되었습니다.",
                    this
                );

                return;
            }

            if (spawnData == null)
            {
                Debug.LogWarning(
                    "[BattlePieceSpawner] " +
                    "SpawnData가 없습니다."
                );

                return;
            }

            hasSpawned = true;

            for (int i = 0;
                 i < spawnData.Pieces.Count;
                 i++)
            {
                PieceSpawnEntry entry =
                    spawnData.Pieces[i];

                SpawnAndPlacePiece(
                    entry
                );
            }
        }

        /*
         * 지정 팀만 자동 생성 및 배치한다.
         *
         * 적 자동 배치에 유용하다.
         */
        public void SpawnTeam(
            PieceColor color)
        {
            if (spawnData == null)
                return;

            for (int i = 0;
                 i < spawnData.Pieces.Count;
                 i++)
            {
                PieceSpawnEntry entry =
                    spawnData.Pieces[i];

                if (entry.Color != color)
                    continue;

                SpawnAndPlacePiece(
                    entry
                );
            }
        }

        public void ResetSpawnGuard()
        {
            CleanupDestroyedPieces();

            if (spawnedPieces.Count > 0)
            {
                Debug.LogWarning(
                    "[BattlePieceSpawner] 생성된 말이 남아 있어 " +
                    "Spawn 상태를 초기화할 수 없습니다.",
                    this
                );

                return;
            }

            hasSpawned = false;
        }

        /*
         * 말을 생성하지만 보드에는 배치하지 않는다.
         *
         * 플레이어 Placement 단계에서 사용한다.
         */
        public ChessPiece CreatePiece(
            ChessPiece prefab,
            PieceColor color)
        {
            if (prefab == null)
                return null;

            Transform parent =
                GetPieceRoot(
                    color
                );

            ChessPiece piece =
                Instantiate(
                    prefab,
                    parent
                );

            /*
             * 아직 GridPosition이 확정되지 않았으므로
             * 팀 정보만 초기화한다.
             *
             * 현재 ChessPiece.Initialize가
             * PieceColor만 받는 버전 기준이다.
             */
            piece.Initialize(
                color
            );

            spawnedPieces.Add(
                piece
            );

            unplacedPieces.Add(
                piece
            );

            /*
             * 실제 배치 전까지 화면 밖에 숨긴다.
             */
            piece.gameObject.SetActive(
                false
            );

            Debug.Log(
                $"[BattlePieceSpawner] " +
                $"Piece Created | " +
                $"Piece={piece.name} | " +
                $"Team={color}"
            );

            return piece;
        }

        /*
         * CreatePiece()로 생성된 말을
         * 실제 그리드 위치에 배치한다.
         */
        public bool PlacePiece(
    ChessPiece piece,
    Vector2Int gridPosition)
        {
            if (piece == null ||
                gridManager == null)
            {
                return false;
            }

            if (piece.IsPlaced)
            {
                Debug.LogWarning(
                    $"[BattlePieceSpawner] " +
                    $"이미 배치된 말입니다. | " +
                    $"Piece={piece.name}"
                );

                return false;
            }

            if (!gridManager.IsInsideGrid(
                    gridPosition))
            {
                Debug.Log(
                    $"[BattlePieceSpawner] " +
                    $"그리드 밖입니다. | " +
                    $"Position={gridPosition}"
                );

                return false;
            }

            if (!gridManager.CanPlacePiece(
                    gridPosition))
            {
                Debug.Log(
                    $"[BattlePieceSpawner] " +
                    $"배치할 수 없는 위치입니다. | " +
                    $"Position={gridPosition}"
                );

                return false;
            }

            piece.gameObject.SetActive(
                true
            );

            bool placed =
                gridManager.PlacePiece(
                    piece,
                    gridPosition
                );

            if (!placed)
            {
                piece.gameObject.SetActive(
                    false
                );

                return false;
            }

            piece.transform.position =
                gridManager.GridToWorld(
                    gridPosition
                );

            unplacedPieces.Remove(
                piece
            );

            /*
             * Runtime 시스템 등록.
             */
            RegisterPieceToRuntimeSystem(
                piece
            );

            /*
             * =========================================
             * Piece World UI 등록
             * =========================================
             *
             * 말이 실제 Grid에 배치된 이후에 UI를 생성한다.
             *
             * PieceWorldUIManager.Start()에서
             * FindObjectsByType()에 의존하지 않으므로
             * Spawner 실행 순서 문제도 없어진다.
             */
            if (pieceWorldUIManager != null)
            {
                PieceWorldStatusUI ui =
                    pieceWorldUIManager.RegisterPiece(
                        piece
                    );

                if (ui == null)
                {
                    Debug.LogWarning(
                        $"[BattlePieceSpawner] " +
                        $"Piece UI 등록 실패 | " +
                        $"Piece={piece.name}"
                    );
                }
                else
                {
                    Debug.Log(
                        $"[BattlePieceSpawner] " +
                        $"Piece UI 등록 성공 | " +
                        $"Piece={piece.name} | " +
                        $"UI={ui.name}"
                    );
                }
            }
            else
            {
                Debug.LogWarning(
                    "[BattlePieceSpawner] " +
                    "PieceWorldUIManager가 없습니다."
                );
            }

            Debug.Log(
                $"[BattlePieceSpawner] " +
                $"Piece Placed | " +
                $"Piece={piece.name} | " +
                $"Team={piece.Color} | " +
                $"Position={gridPosition}"
            );

            return true;
        }
        /*
         * 생성과 배치를 한 번에 수행한다.
         *
         * 적 자동 배치나 테스트용.
         */
        public ChessPiece SpawnAndPlacePiece(
            PieceSpawnEntry entry)
        {
            if (entry == null ||
                entry.Prefab == null)
            {
                return null;
            }

            return SpawnAndPlacePiece(
                entry.Prefab,
                entry.Color,
                entry.SpawnPosition
            );
        }

        public ChessPiece SpawnAndPlacePiece(
            ChessPiece prefab,
            PieceColor color,
            Vector2Int gridPosition)
        {
            ChessPiece piece =
                CreatePiece(
                    prefab,
                    color
                );

            if (piece == null)
                return null;

            if (!PlacePiece(
                    piece,
                    gridPosition))
            {
                spawnedPieces.Remove(
                    piece
                );

                unplacedPieces.Remove(
                    piece
                );

                Destroy(
                    piece.gameObject
                );

                return null;
            }

            return piece;
        }

        /*
         * 아직 배치하지 않은 특정 팀 말을 반환한다.
         */
        public List<ChessPiece>
            GetUnplacedPieces(
                PieceColor color)
        {
            List<ChessPiece> result =
                new();

            for (int i = 0;
                 i < unplacedPieces.Count;
                 i++)
            {
                ChessPiece piece =
                    unplacedPieces[i];

                if (piece == null ||
                    piece.Color != color)
                {
                    continue;
                }

                result.Add(
                    piece
                );
            }

            return result;
        }

        public bool HasUnplacedPiece(
            PieceColor color)
        {
            for (int i = 0;
                 i < unplacedPieces.Count;
                 i++)
            {
                ChessPiece piece =
                    unplacedPieces[i];

                if (piece != null &&
                    piece.Color == color)
                {
                    return true;
                }
            }

            return false;
        }

        /*
         * 모든 말이 실제 보드에 배치됐는지 확인한다.
         */
        public bool AreAllPiecesPlaced()
        {
            CleanupDestroyedPieces();

            return unplacedPieces.Count == 0;
        }

        private void RegisterPieceToRuntimeSystem(
            ChessPiece piece)
        {
            if (piece == null)
                return;

            GameManager gameManager =
                FindFirstObjectByType<GameManager>();

            if (gameManager == null ||
                gameManager.Context == null)
            {
                Debug.LogError(
                    "[BattlePieceSpawner] " +
                    "GameManager 또는 GameContext가 없습니다."
                );

                return;
            }

            /*
             * 모든 말을 해당 PlayerRuntimeData에 등록한다.
             */
            PlayerRuntimeData owner =
                gameManager.Context.GetPlayer(
                    piece.Color
                );

            owner?.AddPiece(
                piece
            );

            /*
             * Black은 AI 관리 대상이므로
             * EnemyManager에도 추가 등록한다.
             */
            if (piece.Color ==
                PieceColor.Black)
            {
                enemyManager?.RegisterEnemy(
                    piece
                );
            }

            Debug.Log(
                $"[BattlePieceSpawner] Runtime Register | " +
                $"Piece={piece.name} | " +
                $"Team={piece.Color}"
            );
        }
        private Transform GetPieceRoot(
            PieceColor color)
        {
            return color ==
                   PieceColor.White
                ? whiteRoot
                : blackRoot;
        }

        private void CleanupDestroyedPieces()
        {
            for (int i =
                     spawnedPieces.Count - 1;
                 i >= 0;
                 i--)
            {
                if (spawnedPieces[i] == null)
                {
                    spawnedPieces.RemoveAt(
                        i
                    );
                }
            }

            for (int i =
                     unplacedPieces.Count - 1;
                 i >= 0;
                 i--)
            {
                if (unplacedPieces[i] == null)
                {
                    unplacedPieces.RemoveAt(
                        i
                    );
                }
            }
        }
    }
}