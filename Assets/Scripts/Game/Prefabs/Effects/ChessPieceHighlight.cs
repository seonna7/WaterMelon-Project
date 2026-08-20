using UnityEngine;

namespace Game.GamePlay.Prefabs.Effects
{
    public class ChessPieceHighlight : MonoBehaviour
    {
        [SerializeField]
        private Behaviour outlineBehaviour;

        public bool IsHighlighted { get; private set; }

        private void Awake()
        {
            SetHighlight(false);
        }

        public void SetHighlight(bool enable)
        {
            if (IsHighlighted == enable)
                return;

            IsHighlighted = enable;

            if (outlineBehaviour == null)
            {
                Debug.LogWarning(
                    $"[{nameof(ChessPieceHighlight)}] " +
                    $"{name}: outlineBehaviour is null"
                );

                return;
            }

            outlineBehaviour.enabled = enable;
        }
    }
}
