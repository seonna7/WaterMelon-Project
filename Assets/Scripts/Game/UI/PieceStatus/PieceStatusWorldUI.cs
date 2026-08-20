using Game.GamePlay;
using Game.GamePlay.StatusEffects;
using UnityEngine;

namespace Game.UI.PieceStatus
{
    public sealed class PieceWorldStatusUI
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private RectTransform rectTransform;

        [SerializeField]
        private GameObject visualRoot;

        [SerializeField]
        private Canvas rootCanvas;

        [Header("World Tracking")]
        [SerializeField]
        private Vector3 worldOffset =
            new Vector3(
                0f,
                2.5f,
                0f
            );
        [SerializeField]
        private PieceHealthUI healthUI;

        [SerializeField]
        private PieceTypeIconUI typeIconUI;

        [SerializeField]
        private PieceStatusEffectUI statusEffectUI;

        private StatusEffectManager statusEffectManager;
        [SerializeField]
        private Vector2 screenOffset =
            Vector2.zero;

        [Header("Visibility")]
        [SerializeField]
        private bool hideWhenBehindCamera =
            true;

        [SerializeField]
        private bool hideWhenPieceDead =
            true;

        private ChessPiece targetPiece;

        private Camera targetCamera;

        private bool isInitialized;

        public ChessPiece TargetPiece =>
            targetPiece;

        public bool IsInitialized =>
            isInitialized;

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform =
                    transform as RectTransform;
            }

            if (rootCanvas == null)
            {
                rootCanvas =
                    GetComponentInParent<Canvas>();
            }

            SetVisible(
                false
            );
        }

        public void InitializePreview(
    ChessPiece piece,
    Camera camera)
        {
            targetPiece =
                piece;

            targetCamera =
                camera;

            if (rectTransform == null)
            {
                rectTransform =
                    transform as RectTransform;
            }

            if (rootCanvas == null)
            {
                rootCanvas =
                    GetComponentInParent<Canvas>();
            }

            /*
             * Edit Mode에서는
             * StatusEffectManager / TurnManager 등
             * Runtime 시스템을 건드리지 않는다.
             *
             * Serialized된 ChessPiece 데이터만 읽어서
             * 표시한다.
             */

            healthUI?.Initialize(
                targetPiece
            );

            typeIconUI?.Initialize(
                targetPiece
            );

            /*
             * 상태이상은 현재 Runtime 데이터이므로
             * Edit Mode Preview에서는 비운다.
             *
             * 나중에 Editor Preview Status를 따로
             * 만들고 싶다면 확장 가능.
             */
            statusEffectUI?.Clear();

            UpdateScreenPosition();

            SetVisible(
                true
            );
        }

        /*
         * UI Prefab을 특정 체스말에 연결한다.
         */
        public void Initialize(
            ChessPiece piece,
            Camera camera)
        {
            targetPiece =
                piece;

            targetCamera =
                camera != null
                    ? camera
                    : Camera.main;

            if (rectTransform == null)
            {
                rectTransform =
                    transform as RectTransform;
            }

            if (rootCanvas == null)
            {
                rootCanvas =
                    GetComponentInParent<Canvas>();
            }

            isInitialized =
                targetPiece != null &&
                targetCamera != null &&
                rectTransform != null &&
                rootCanvas != null;

            if (!isInitialized)
            {
                Debug.LogWarning(
                    "[PieceWorldStatusUI] " +
                    "초기화에 실패했습니다."
                );

                SetVisible(
                    false
                );

                return;
            }
            statusEffectManager =
    FindFirstObjectByType<
        StatusEffectManager>();

            healthUI?.Initialize(
                targetPiece
            );

            typeIconUI?.Initialize(
                targetPiece
            );

            statusEffectUI?.Initialize(
                targetPiece,
                statusEffectManager
            );


            /*
             * 처음 연결하는 순간
             * 즉시 올바른 위치로 이동.
             */
            UpdateScreenPosition();

            SetVisible(
                true
            );
        }

        /*
         * 추적 대상을 다른 말로 바꿀 때 사용.
         */
        public void SetTarget(
            ChessPiece piece)
        {
            targetPiece =
                piece;

            isInitialized =
                targetPiece != null &&
                targetCamera != null &&
                rectTransform != null &&
                rootCanvas != null;

            if (!isInitialized)
            {
                SetVisible(
                    false
                );

                return;
            }

            UpdateScreenPosition();

            SetVisible(
                true
            );
        }

        private void LateUpdate()
        {
            if (!isInitialized)
                return;

            if (targetPiece == null)
            {
                SetVisible(
                    false
                );

                return;
            }

            /*
             * 사망 시 UI 제거 여부.
             *
             * 나중에 사망 애니메이션 동안
             * 잠시 유지하도록 변경 가능.
             */
            if (hideWhenPieceDead &&
                targetPiece.IsDead)
            {
                SetVisible(
                    false
                );

                return;
            }

            UpdateScreenPosition();
        }

        private void UpdateScreenPosition()
        {
            if (targetPiece == null ||
                targetCamera == null ||
                rectTransform == null ||
                rootCanvas == null)
            {
                return;
            }

            Vector3 worldPosition =
                targetPiece.transform.position +
                worldOffset;

            Vector3 screenPosition =
                targetCamera.WorldToScreenPoint(
                    worldPosition
                );

            /*
             * 카메라 뒤쪽에 있는 경우
             * WorldToScreenPoint의 Z가 음수가 된다.
             */
            if (hideWhenBehindCamera &&
                screenPosition.z <= 0f)
            {
                SetVisible(
                    false
                );

                return;
            }

            SetVisible(
                true
            );

            /*
             * Screen Space - Overlay와
             * Screen Space - Camera를 모두 대응한다.
             */
            Camera canvasCamera =
                rootCanvas.renderMode ==
                RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera;

            RectTransform canvasRect =
                rootCanvas.transform
                    as RectTransform;

            if (canvasRect == null)
                return;

            bool converted =
                RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPosition,
                        canvasCamera,
                        out Vector2 localPoint
                    );

            if (!converted)
                return;

            rectTransform
                .anchoredPosition =
                    localPoint +
                    screenOffset;
        }

        public void SetWorldOffset(
            Vector3 offset)
        {
            worldOffset =
                offset;
        }

        public void SetScreenOffset(
            Vector2 offset)
        {
            screenOffset =
                offset;
        }

        public void SetCamera(
            Camera camera)
        {
            targetCamera =
                camera;

            if (targetCamera == null)
            {
                targetCamera =
                    Camera.main;
            }
        }

        public void SetVisible(
            bool visible)
        {
            if (visualRoot == null)
                return;

            visualRoot.SetActive(
                visible
            );
        }

        /*
         * Pooling을 사용할 때
         * UI 인스턴스를 초기 상태로 되돌린다.
         */
        public void Release()
        {
            targetPiece =
                null;

            isInitialized =
                false;

            SetVisible(
                false
            );
        }
    }
}