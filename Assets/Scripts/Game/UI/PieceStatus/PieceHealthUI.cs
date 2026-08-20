using Game.GamePlay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.PieceStatus
{
    [ExecuteAlways]
    public sealed class PieceHealthUI
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Image healthFill;

        [SerializeField]
        private TMP_Text healthText;

        private ChessPiece targetPiece;

        private int cachedCurrentHP = -1;

        private int cachedMaxHP = -1;

        public ChessPiece TargetPiece =>
            targetPiece;

        private void Awake()
        {
            ConfigureHealthFill();
        }

        private void OnValidate()
        {
            ConfigureHealthFill();
        }

        public void Initialize(
            ChessPiece piece)
        {
            ConfigureHealthFill();

            targetPiece =
                piece;

            cachedCurrentHP =
                -1;

            cachedMaxHP =
                -1;

            Refresh(
                true
            );
        }

        private void Update()
        {
            if (targetPiece == null)
                return;

            if (cachedCurrentHP !=
                    targetPiece.CurrentHP ||
                cachedMaxHP !=
                    targetPiece.MaxHP)
            {
                Refresh();
            }
        }

        public void Refresh(
            bool force = false)
        {
            if (targetPiece == null)
            {
                Clear();
                return;
            }

            int currentHP =
                Mathf.Max(
                    targetPiece.CurrentHP,
                    0
                );

            int maxHP =
                Mathf.Max(
                    targetPiece.MaxHP,
                    1
                );

            if (!force &&
                cachedCurrentHP ==
                    currentHP &&
                cachedMaxHP ==
                    maxHP)
            {
                return;
            }

            cachedCurrentHP =
                currentHP;

            cachedMaxHP =
                maxHP;

            /*
             * HP Fill
             */
            if (healthFill != null)
            {
                float ratio =
                    Mathf.Clamp01(
                        (float)currentHP /
                        maxHP
                    );

                healthFill.fillAmount =
                    ratio;
            }

            /*
             * HP Text
             *
             * 예:
             * 18 / 30
             */
            if (healthText != null)
            {
                healthText.text =
                    $"{currentHP} / {maxHP}";
            }
        }

        public void SetTarget(
            ChessPiece piece)
        {
            Initialize(
                piece
            );
        }

        public void Clear()
        {
            targetPiece =
                null;

            cachedCurrentHP =
                -1;

            cachedMaxHP =
                -1;

            if (healthFill != null)
            {
                healthFill.fillAmount =
                    0f;
            }

            if (healthText != null)
            {
                healthText.text =
                    string.Empty;
            }
        }

        private void ConfigureHealthFill()
        {
            if (healthFill == null)
                return;

            healthFill.type =
                Image.Type.Filled;

            healthFill.fillMethod =
                Image.FillMethod.Horizontal;

            /*
             * 0 = Left.
             * 체력이 감소하면 오른쪽부터 색 영역이 사라진다.
             */
            healthFill.fillOrigin =
                (int)Image.OriginHorizontal.Left;
        }
    }
}