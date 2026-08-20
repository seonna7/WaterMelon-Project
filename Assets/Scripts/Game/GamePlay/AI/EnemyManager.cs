using Game.Core;
using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    public sealed class EnemyManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GridManager gridManager;

        [Header("Enemy")]
        [SerializeField]
        private PieceColor enemyColor =
            PieceColor.Black;

        [SerializeField]
        private Transform enemyRoot;

        [SerializeField]
        private bool registerSceneEnemies = true;

        private readonly List<ChessPiece> enemies = new();

        public PieceColor EnemyColor => enemyColor;
        public IReadOnlyList<ChessPiece> Enemies => enemies;

        public void Initialize(GameManager manager)
        {
            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<GridManager>();
            }

            if (registerSceneEnemies)
            {
                RegisterExistingEnemies();
            }
        }

        public void RegisterExistingEnemies()
        {
            ChessPiece[] scenePieces =
                FindObjectsByType<ChessPiece>(
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < scenePieces.Length; i++)
            {
                ChessPiece piece = scenePieces[i];

                if (piece != null &&
                    piece.Color == enemyColor)
                {
                    RegisterEnemy(piece);
                }
            }
        }

        public bool RegisterEnemy(ChessPiece enemy)
        {
            if (enemy == null ||
                enemy.Color != enemyColor)
            {
                return false;
            }

            CleanupNullReferences();

            if (enemies.Contains(enemy))
                return true;

            enemies.Add(enemy);

            Debug.Log(
                $"[EnemyManager] Registered | Enemy={enemy.name}"
            );

            return true;
        }

        public bool UnregisterEnemy(ChessPiece enemy)
        {
            if (enemy == null)
                return false;

            return enemies.Remove(enemy);
        }

        public ChessPiece SpawnEnemy(
            ChessPiece enemyPrefab,
            Vector2Int gridPosition)
        {
            if (enemyPrefab == null ||
                gridManager == null ||
                !gridManager.CanPlacePiece(gridPosition))
            {
                return null;
            }

            ChessPiece enemy = Instantiate(
                enemyPrefab,
                gridManager.GridToWorld(gridPosition),
                enemyPrefab.transform.rotation,
                enemyRoot
            );

            enemy.Initialize(enemyColor);

            if (!gridManager.PlacePiece(
                    enemy,
                    gridPosition))
            {
                Destroy(enemy.gameObject);
                return null;
            }

            RegisterEnemy(enemy);
            return enemy;
        }

        public List<ChessPiece> GetAliveEnemies()
        {
            CleanupNullReferences();

            List<ChessPiece> alive = new();

            for (int i = 0; i < enemies.Count; i++)
            {
                ChessPiece enemy = enemies[i];

                if (enemy != null &&
                    !enemy.IsDead &&
                    enemy.IsPlaced)
                {
                    alive.Add(enemy);
                }
            }

            return alive;
        }

        public bool HasAliveEnemies()
        {
            CleanupNullReferences();

            for (int i = 0; i < enemies.Count; i++)
            {
                ChessPiece enemy = enemies[i];

                if (enemy != null &&
                    !enemy.IsDead &&
                    enemy.IsPlaced)
                {
                    return true;
                }
            }

            return false;
        }

        private void CleanupNullReferences()
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i] == null)
                {
                    enemies.RemoveAt(i);
                }
            }
        }
    }
}