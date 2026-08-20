using Game.GamePlay.Skill;
using Game.UI.PieceStatus;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class PieceActionMenuUI : MonoBehaviour
    {
        [SerializeField]
        private RectTransform menuRoot;

        [SerializeField]
        private Button moveButton;

        [SerializeField]
        private Button skill1Button;

        [SerializeField]
        private Button skill2Button;

        [SerializeField]
        private Vector3 worldOffset =
            new Vector3(0f, 2f, 0f);

        private Camera targetCamera;

        [SerializeField]
        private PieceWorldUIManager pieceWorldUIManager;

        private Game.GamePlay.ChessPiece currentPiece;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (pieceWorldUIManager == null)
            {
                pieceWorldUIManager =
                    FindFirstObjectByType<PieceWorldUIManager>();
            }

            Hide();
        }

        private void LateUpdate()
        {
            if (currentPiece == null ||
                menuRoot == null ||
                targetCamera == null)
            {
                return;
            }

            UpdatePosition();
        }

        public void Show(
            Game.GamePlay.ChessPiece piece)
        {
            if (piece == null)
            {
                Hide();
                return;
            }

            currentPiece = piece;

            if (pieceWorldUIManager != null)
            {
                pieceWorldUIManager.HidePieceUI(
                    currentPiece
                );
            }

            menuRoot.gameObject.SetActive(true);

            if (skill1Button != null)
            {
                skill1Button.interactable =
                    currentPiece.HasSkill(
                        SkillSlot.Skill1
                    );
            }

            if (skill2Button != null)
            {
                skill2Button.interactable =
                    currentPiece.HasSkill(
                        SkillSlot.Skill2
                    );
            }

            UpdatePosition();
        }

        public void Hide()
        {
            currentPiece = null;

            if (menuRoot != null)
            {
                menuRoot.gameObject.SetActive(false);
            }
        }

        private void UpdatePosition()
        {
            if (currentPiece == null ||
                menuRoot == null ||
                targetCamera == null)
            {
                return;
            }

            Vector3 worldPos =
                currentPiece.transform.position +
                worldOffset;

            Vector3 screenPos =
                targetCamera.WorldToScreenPoint(
                    worldPos
                );

            menuRoot.position =
                screenPos;
        }

        public void BindMoveAction(
            UnityEngine.Events.UnityAction action)
        {
            if (moveButton == null)
                return;

            moveButton.onClick.RemoveAllListeners();
            moveButton.onClick.AddListener(action);
        }

        public void BindSkill1Action(
            UnityEngine.Events.UnityAction action)
        {
            if (skill1Button == null)
                return;

            skill1Button.onClick.RemoveAllListeners();
            skill1Button.onClick.AddListener(action);
        }

        public void BindSkill2Action(
            UnityEngine.Events.UnityAction action)
        {
            if (skill2Button == null)
                return;

            skill2Button.onClick.RemoveAllListeners();
            skill2Button.onClick.AddListener(action);
        }

        public Game.GamePlay.ChessPiece GetCurrentPiece()
        {
            return currentPiece;
        }
    }
}