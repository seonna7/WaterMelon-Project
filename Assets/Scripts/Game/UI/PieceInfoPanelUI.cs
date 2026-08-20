using Game.GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public sealed class PieceInfoPanelUI
        : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField]
        private GameObject panelRoot;

        [Header("Basic")]
        [SerializeField]
        private TMP_Text pieceNameText;

        [SerializeField]
        private TMP_Text actionTypeText;

        [SerializeField]
        private TMP_Text movementStrategyText;

        [Header("HP")]
        [SerializeField]
        private Slider hpSlider;

        [SerializeField]
        private TMP_Text hpText;

        private ChessPiece currentPiece;

        public ChessPiece CurrentPiece =>
            currentPiece;

        private void Awake()
        {
            Hide();
        }

        private void Update()
        {
            /*
             * 현재 선택된 말의 HP가
             * 전투 도중 변할 수 있으므로 갱신.
             *
             * 추후 이벤트 방식으로 변경 가능.
             */
            if (currentPiece == null)
                return;

            if (currentPiece.IsDead)
            {
                Refresh();
                return;
            }

            RefreshHP();
        }

        public void Show(
            ChessPiece piece)
        {
            if (piece == null)
            {
                Hide();
                return;
            }

            currentPiece =
                piece;

            if (panelRoot != null)
            {
                panelRoot.SetActive(
                    true
                );
            }

            Refresh();
        }

        public void Hide()
        {
            currentPiece =
                null;

            if (panelRoot != null)
            {
                panelRoot.SetActive(
                    false
                );
            }
        }

        public void Refresh()
        {
            if (currentPiece == null)
            {
                Hide();
                return;
            }

            RefreshBasicInfo();
            RefreshHP();
        }

        private void RefreshBasicInfo()
        {
            if (pieceNameText != null)
            {
                pieceNameText.text =
                    currentPiece.name
                        .Replace(
                            "(Clone)",
                            ""
                        );
            }

            if (actionTypeText != null)
            {
                actionTypeText.text =
                    currentPiece
                        .ActionType
                        .ToString();
            }

            if (movementStrategyText != null)
            {
                movementStrategyText.text =
                    currentPiece
                        .MoveStrategyName;
            }
        }

        private void RefreshHP()
        {
            int currentHP =
                currentPiece.CurrentHP;

            int maxHP =
                currentPiece.MaxHP;

            if (hpSlider != null)
            {
                hpSlider.minValue = 0f;

                hpSlider.maxValue =
                    Mathf.Max(
                        maxHP,
                        1
                    );

                hpSlider.value =
                    currentHP;
            }

            if (hpText != null)
            {
                hpText.text =
                    $"{currentHP} / {maxHP}";
            }
        }
    }
}