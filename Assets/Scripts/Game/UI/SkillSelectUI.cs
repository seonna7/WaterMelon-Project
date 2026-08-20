using Game.GamePlay;
using Game.GamePlay.Skill;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.UI
{
    public class SkillSelectUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform menuRoot;

        [SerializeField] private Button skill1Button;

        [SerializeField] private Button skill2Button;

        [SerializeField] private Button cancelButton;

        [Header("Position")]
        [SerializeField]
        private Vector3 worldOffset =
            new Vector3(0f, 2.5f, 0f);

        private Camera targetCamera;

        private ChessPiece currentPiece;

        private void Awake()
        {
            targetCamera = Camera.main;

            Hide();
        }

        private void LateUpdate()
        {
            UpdatePosition();
        }

        public void BindSkill1Action(
            UnityAction action)
        {
            if (skill1Button == null)
                return;

            skill1Button.onClick.RemoveAllListeners();
            skill1Button.onClick.AddListener(action);
        }

        public void BindSkill2Action(
            UnityAction action)
        {
            if (skill2Button == null)
                return;

            skill2Button.onClick.RemoveAllListeners();
            skill2Button.onClick.AddListener(action);
        }

        public void BindCancelAction(
            UnityAction action)
        {
            if (cancelButton == null)
                return;

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(action);
        }

        public void Show(
            ChessPiece piece)
        {
            if (piece == null)
            {
                Hide();
                return;
            }

            currentPiece = piece;

            if (menuRoot != null)
            {
                menuRoot.gameObject.SetActive(true);
            }

            UpdateButtons();
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

        private void UpdateButtons()
        {
            if (currentPiece == null)
                return;

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
        }

        private void UpdatePosition()
        {
            if (currentPiece == null ||
                menuRoot == null ||
                targetCamera == null)
            {
                return;
            }

            Vector3 worldPosition =
                currentPiece.transform.position +
                worldOffset;

            Vector3 screenPosition =
                targetCamera.WorldToScreenPoint(
                    worldPosition
                );

            menuRoot.position =
                screenPosition;
        }
    }
}