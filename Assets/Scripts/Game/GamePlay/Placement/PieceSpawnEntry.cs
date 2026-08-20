using System;
using UnityEngine;

namespace Game.GamePlay.Placement
{
    [System.Serializable]
    public sealed class PieceSpawnEntry
    {
        [SerializeField]
        private string entryId;

        [SerializeField]
        private ChessPiece prefab;

        [SerializeField]
        private PieceColor color;

        [SerializeField]
        private Vector2Int spawnPosition;

        public string EntryId =>
            entryId;

        public ChessPiece Prefab =>
            prefab;

        public PieceColor Color =>
            color;

        public Vector2Int SpawnPosition =>
            spawnPosition;

        public PieceSpawnEntry(
            ChessPiece prefab,
            PieceColor color,
            Vector2Int spawnPosition)
        {
            entryId =
                Guid.NewGuid().ToString("N");

            this.prefab = prefab;
            this.color = color;
            this.spawnPosition = spawnPosition;
        }

        public void EnsureId()
        {
            if (!string.IsNullOrWhiteSpace(entryId))
                return;

            entryId =
                Guid.NewGuid().ToString("N");
        }

        public void SetPrefab(ChessPiece value)
        {
            prefab = value;
        }

        public void SetColor(PieceColor value)
        {
            color = value;
        }

        public void SetSpawnPosition(Vector2Int value)
        {
            spawnPosition = value;
        }
    }
}