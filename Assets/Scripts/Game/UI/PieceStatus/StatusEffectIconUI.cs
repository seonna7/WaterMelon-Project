using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.PieceStatus
{
    public sealed class StatusEffectIconUI
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Image backgroundImage;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TMP_Text durationText;

        [SerializeField]
        private TMP_Text stackText;

        [Header("Colors")]
        [SerializeField]
        private Color buffColor =
            new Color(
                1f,
                0.85f,
                0.1f,
                1f
            );

        [SerializeField]
        private Color debuffColor =
            new Color(
                1f,
                0.45f,
                0.05f,
                1f
            );

        private bool isBuff;

        public bool IsBuff =>
            isBuff;

        public void Initialize(
            Sprite icon,
            bool buff,
            int remainingTurns = 0,
            int stackCount = 1)
        {
            isBuff =
                buff;

            SetIcon(
                icon
            );

            SetBackgroundType(
                buff
            );

            SetDuration(
                remainingTurns
            );

            SetStack(
                stackCount
            );

            gameObject.SetActive(
                true
            );
        }

        public void SetIcon(
            Sprite icon)
        {
            if (iconImage == null)
                return;

            iconImage.sprite =
                icon;

            iconImage.enabled =
                icon != null;
        }

        public void SetBackgroundType(
            bool buff)
        {
            isBuff =
                buff;

            if (backgroundImage == null)
                return;

            backgroundImage.color =
                buff
                    ? buffColor
                    : debuffColor;
        }

        public void SetDuration(
            int remainingTurns)
        {
            if (durationText == null)
                return;

            /*
             * 0 이하라면 지속시간 표시 안 함.
             *
             * 영구 버프 / 지속시간 없는 상태에도 사용 가능.
             */
            if (remainingTurns <= 0)
            {
                durationText.text =
                    string.Empty;

                durationText.gameObject
                    .SetActive(false);

                return;
            }

            durationText.gameObject
                .SetActive(true);

            durationText.text =
                remainingTurns.ToString();
        }

        public void SetStack(
            int stackCount)
        {
            if (stackText == null)
                return;

            /*
             * 1스택은 굳이 표시하지 않는다.
             */
            if (stackCount <= 1)
            {
                stackText.text =
                    string.Empty;

                stackText.gameObject
                    .SetActive(false);

                return;
            }

            stackText.gameObject
                .SetActive(true);

            stackText.text =
                $"x{stackCount}";
        }

        public void Clear()
        {
            isBuff =
                false;

            if (iconImage != null)
            {
                iconImage.sprite =
                    null;

                iconImage.enabled =
                    false;
            }

            if (durationText != null)
            {
                durationText.text =
                    string.Empty;

                durationText.gameObject
                    .SetActive(false);
            }

            if (stackText != null)
            {
                stackText.text =
                    string.Empty;

                stackText.gameObject
                    .SetActive(false);
            }

            gameObject.SetActive(
                false
            );
        }
    }
}