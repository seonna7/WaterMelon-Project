using UnityEngine;

namespace Game.GamePlay.Placement
{
    [DisallowMultipleComponent]
    public sealed class PieceSpawnPreviewInstance
        : MonoBehaviour
    {
        [SerializeField]
        private PieceSpawnPreviewController owner;

        [SerializeField]
        private string entryId;

        public PieceSpawnPreviewController Owner =>
            owner;

        public string EntryId =>
            entryId;

        public void Bind(
            PieceSpawnPreviewController targetOwner,
            string targetEntryId)
        {
            owner = targetOwner;
            entryId = targetEntryId;
        }
    }
}