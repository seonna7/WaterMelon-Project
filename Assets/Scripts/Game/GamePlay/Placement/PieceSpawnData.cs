using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Placement
{
    [CreateAssetMenu(
        fileName = "PieceSpawnData",
        menuName = "Game/Battle/Piece Spawn Data"
    )]
    public sealed class PieceSpawnData
        : ScriptableObject
    {
        [SerializeField]
        private List<PieceSpawnEntry> pieces =
            new();

        public IReadOnlyList<PieceSpawnEntry> Pieces =>
            pieces;

        private void OnValidate()
        {
            EnsureEntryIds();
        }

        public PieceSpawnEntry AddPiece(
            ChessPiece prefab,
            PieceColor color,
            Vector2Int spawnPosition)
        {
            if (prefab == null)
                return null;

            PieceSpawnEntry entry =
                new PieceSpawnEntry(
                    prefab,
                    color,
                    spawnPosition);

            pieces.Add(entry);
            return entry;
        }

        public bool RemovePiece(PieceSpawnEntry entry)
        {
            return entry != null &&
                   pieces.Remove(entry);
        }

        public bool RemovePieceById(string entryId)
        {
            PieceSpawnEntry entry =
                FindPieceById(entryId);

            return RemovePiece(entry);
        }

        public PieceSpawnEntry FindPieceById(string entryId)
        {
            if (string.IsNullOrWhiteSpace(entryId))
                return null;

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceSpawnEntry entry = pieces[i];

                if (entry != null &&
                    entry.EntryId == entryId)
                {
                    return entry;
                }
            }

            return null;
        }

        public PieceSpawnEntry FindPiece(
            ChessPiece prefab,
            PieceColor color,
            Vector2Int spawnPosition)
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                PieceSpawnEntry entry = pieces[i];

                if (entry != null &&
                    entry.Prefab == prefab &&
                    entry.Color == color &&
                    entry.SpawnPosition == spawnPosition)
                {
                    return entry;
                }
            }

            return null;
        }

        public bool UpdatePosition(
            string entryId,
            Vector2Int position)
        {
            PieceSpawnEntry entry =
                FindPieceById(entryId);

            if (entry == null)
                return false;

            entry.SetSpawnPosition(position);
            return true;
        }

        public void EnsureEntryIds()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                pieces[i]?.EnsureId();
            }
        }

        public void Clear()
        {
            pieces.Clear();
        }
    }
}