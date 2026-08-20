using Game.GamePlay;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.PieceStatus
{

    public sealed class PieceWorldUIManager
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private RectTransform uiRoot;

        [SerializeField]
        private PieceWorldStatusUI
            pieceStatusUIPrefab;

        private readonly Dictionary<
            ChessPiece,
            PieceWorldStatusUI>
            registeredUI = new();

        private ChessPiece hiddenPiece;

        private double nextEditorRefreshTime;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();

            /*
             * Play Mode에서는 Spawner 또는
             * 런타임 등록 시스템에서 RegisterPiece를
             * 호출하는 구조를 유지한다.
             */
            if (Application.isPlaying)
                return;

            RefreshEditorPreview();
        }

        private void Update()
        {
            /*
             * 런타임에서는 여기서
             * 전체 ChessPiece를 검색하지 않는다.
             */
            if (Application.isPlaying)
                return;

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.timeSinceStartup <
                nextEditorRefreshTime)
            {
                return;
            }
#endif

            RefreshEditorPreview();
        }

        private void ResolveReferences()
        {
            if (targetCamera == null)
            {
                targetCamera =
                    Camera.main;
            }

            if (uiRoot == null)
            {
                uiRoot =
                    transform as RectTransform;
            }
        }

        /*
         * =========================================
         * EDIT MODE PREVIEW
         * =========================================
         */

        public void RefreshEditorPreview()
        {
            if (Application.isPlaying)
                return;


            ResolveReferences();

            if (pieceStatusUIPrefab == null)
                return;

            if (uiRoot == null)
                return;

            ChessPiece[] pieces =
                FindObjectsByType<ChessPiece>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            /*
             * 새롭게 씬에 추가된 Piece 등록.
             */
            for (int i = 0;
                 i < pieces.Length;
                 i++)
            {
                ChessPiece piece =
                    pieces[i];

                if (piece == null)
                    continue;

                if (registeredUI.ContainsKey(
                        piece))
                {
                    continue;
                }

                CreateEditorPreviewUI(
                    piece
                );
            }

            /*
             * 씬에서 삭제된 Piece의
             * Preview UI 제거.
             */
            RemoveInvalidEditorPreview();
        }

        private void CreateEditorPreviewUI(
            ChessPiece piece)
        {
            if (piece == null ||
                pieceStatusUIPrefab == null ||
                uiRoot == null)
            {
                return;
            }

            PieceWorldStatusUI newUI =
                Instantiate(
                    pieceStatusUIPrefab,
                    uiRoot
                );

            if (newUI == null)
                return;

            newUI.name =
                $"PieceWorldStatusUI_{piece.name}";

            /*
             * Edit Mode Preview임을 표시.
             *
             * 씬 저장 시 일반 게임 오브젝트처럼
             * 저장되는 것을 방지한다.
             */
            newUI.gameObject.hideFlags =
                HideFlags.DontSaveInEditor;

            newUI.InitializePreview(
                piece,
                targetCamera
            );

            registeredUI.Add(
                piece,
                newUI
            );
        }

        private void RemoveInvalidEditorPreview()
        {
            List<ChessPiece> removeTargets =
                new();

            foreach (
                KeyValuePair<
                    ChessPiece,
                    PieceWorldStatusUI> pair
                in registeredUI)
            {
                ChessPiece piece =
                    pair.Key;

                PieceWorldStatusUI ui =
                    pair.Value;

                if (piece != null &&
                    ui != null)
                {
                    continue;
                }

                if (ui != null)
                {
                    DestroyImmediate(
                        ui.gameObject
                    );
                }

                removeTargets.Add(
                    piece
                );
            }

            for (int i = 0;
                 i < removeTargets.Count;
                 i++)
            {
                registeredUI.Remove(
                    removeTargets[i]
                );
            }
        }

        private void ClearEditorPreview()
        {
            if (Application.isPlaying)
                return;

            foreach (
                PieceWorldStatusUI ui
                in registeredUI.Values)
            {
                if (ui == null)
                    continue;

                DestroyImmediate(
                    ui.gameObject
                );
            }

            registeredUI.Clear();
        }

        /*
         * =========================================
         * PLAY MODE
         * =========================================
         */

        public PieceWorldStatusUI RegisterPiece(
            ChessPiece piece)
        {
            if (piece == null)
                return null;

            ResolveReferences();

            if (registeredUI.TryGetValue(
                    piece,
                    out PieceWorldStatusUI existingUI))
            {
                if (existingUI != null)
                {
                    return existingUI;
                }

                registeredUI.Remove(
                    piece
                );
            }

            if (pieceStatusUIPrefab == null)
            {
                Debug.LogError(
                    $"[PieceWorldUIManager] " +
                    $"PieceStatusUIPrefab 없음 | " +
                    $"Piece={piece.name}"
                );

                return null;
            }

            if (uiRoot == null)
            {
                Debug.LogError(
                    "[PieceWorldUIManager] " +
                    "UIRoot가 없습니다."
                );

                return null;
            }

            PieceWorldStatusUI newUI =
                Instantiate(
                    pieceStatusUIPrefab,
                    uiRoot
                );

            if (newUI == null)
                return null;

            newUI.name =
                $"PieceWorldStatusUI_{piece.name}";

            newUI.Initialize(
                piece,
                targetCamera
            );

            registeredUI.Add(
                piece,
                newUI
            );

            return newUI;
        }

        /*
         * 이미 씬에 존재하는 Piece를
         * 런타임에서 등록할 때 사용 가능.
         *
         * 앞으로 씬 직접 배치 방식을 도입하면
         * 게임 초기화 단계에서 이 함수를
         * 사용할 수도 있다.
         */
        public void RegisterExistingPieces()
        {
            if (!Application.isPlaying)
                return;

            ChessPiece[] pieces =
                FindObjectsByType<ChessPiece>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            for (int i = 0;
                 i < pieces.Length;
                 i++)
            {
                RegisterPiece(
                    pieces[i]
                );
            }
        }

        public void UnregisterPiece(
            ChessPiece piece)
        {
            if (piece == null)
                return;

            if (!registeredUI.TryGetValue(
                    piece,
                    out PieceWorldStatusUI ui))
            {
                return;
            }

            registeredUI.Remove(
                piece
            );

            if (ui == null)
                return;

            if (Application.isPlaying)
            {
                ui.Release();

                Destroy(
                    ui.gameObject
                );
            }
            else
            {
                DestroyImmediate(
                    ui.gameObject
                );
            }
        }

        public bool TryGetUI(
            ChessPiece piece,
            out PieceWorldStatusUI ui)
        {
            if (piece == null)
            {
                ui = null;
                return false;
            }

            return registeredUI.TryGetValue(
                piece,
                out ui
            );
        }

        public void HidePieceUI(
            ChessPiece piece)
        {
            if (piece == null)
                return;

            if (hiddenPiece != null &&
                hiddenPiece != piece)
            {
                SetPieceUIVisible(
                    hiddenPiece,
                    true
                );
            }

            hiddenPiece = piece;

            SetPieceUIVisible(
                piece,
                false
            );
        }

        public void RestoreHiddenPieceUI()
        {
            if (hiddenPiece == null)
                return;

            SetPieceUIVisible(
                hiddenPiece,
                true
            );

            hiddenPiece = null;
        }

        public void SetPieceUIVisible(
            ChessPiece piece,
            bool visible)
        {
            if (!TryGetUI(
                    piece,
                    out PieceWorldStatusUI ui) ||
                ui == null)
            {
                return;
            }

            ui.gameObject.SetActive(
                visible
            );
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                return;

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication
                .isPlayingOrWillChangePlaymode)
            {
                registeredUI.Clear();
                return;
            }
#endif

            ClearEditorPreview();
        }
    }
}