using Game.GamePlay;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.PieceStatus
{
    [ExecuteAlways]
    public sealed class PieceTypeIconUI
        : MonoBehaviour
    {
        [Serializable]
        private struct ActionTypeSpriteEntry
        {
            public ActionType type;
            public Sprite sprite;
        }

        [Serializable]
        private struct MovementTypeSpriteEntry
        {
            public MovementType type;
            public Sprite sprite;
        }

        [Header("References")]
        [SerializeField]
        private Image movementFrameImage;

        [SerializeField]
        private Image actionTypeImage;

        [Header("Action Type Sprites")]
        [SerializeField]
        private List<ActionTypeSpriteEntry>
            actionTypeSprites = new();

        [Header("Movement Type Sprites")]
        [SerializeField]
        private List<MovementTypeSpriteEntry>
            movementTypeSprites = new();

        private ChessPiece targetPiece;

        /*
         * Edit Mode 변경 감지용.
         */
        private ActionType cachedActionType;

        private MovementType cachedMovementType;

        private bool hasCachedValue;

        public ChessPiece TargetPiece =>
            targetPiece;

        private void OnEnable()
        {
            ResolveTargetPiece();

            if (targetPiece != null)
            {
                Refresh(
                    true
                );
            }
        }

        private void Update()
        {
            ResolveTargetPiece();

            if (targetPiece == null)
                return;

            ActionType currentActionType =
                targetPiece.ActionType;

            MovementType currentMovementType =
                targetPiece.movementType;

            /*
             * ActionType 또는 MovementType이
             * 변경된 경우에만 갱신.
             */
            if (!hasCachedValue ||
                cachedActionType !=
                    currentActionType ||
                cachedMovementType !=
                    currentMovementType)
            {
                Refresh(
                    true
                );
            }
        }

        private void OnValidate()
        {
            ResolveTargetPiece();

            if (targetPiece != null)
            {
                Refresh(
                    true
                );
            }
        }

        private void ResolveTargetPiece()
        {
            if (targetPiece != null)
                return;

            targetPiece =
                GetComponentInParent<
                    ChessPiece>();
        }

        public void Initialize(
            ChessPiece piece)
        {
            targetPiece =
                piece;

            hasCachedValue =
                false;

            Refresh(
                true
            );
        }

        public void SetTarget(
            ChessPiece piece)
        {
            Initialize(
                piece
            );
        }

        public void Refresh(
            bool force = false)
        {
            if (targetPiece == null)
            {
                ClearImages();
                return;
            }

            ActionType currentActionType =
                targetPiece.ActionType;

            MovementType currentMovementType =
                targetPiece.movementType;

            if (!force &&
                hasCachedValue &&
                cachedActionType ==
                    currentActionType &&
                cachedMovementType ==
                    currentMovementType)
            {
                return;
            }

            cachedActionType =
                currentActionType;

            cachedMovementType =
                currentMovementType;

            hasCachedValue =
                true;

            RefreshActionType(
                currentActionType
            );

            RefreshMovementType(
                currentMovementType
            );
        }

        private void RefreshActionType(
            ActionType actionType)
        {
            if (actionTypeImage == null)
                return;

            Sprite sprite =
                FindActionTypeSprite(
                    actionType
                );

            actionTypeImage.sprite =
                sprite;

            actionTypeImage.enabled =
                sprite != null;
        }

        private void RefreshMovementType(
            MovementType movementType)
        {
            if (movementFrameImage == null)
                return;

            Sprite sprite =
                FindMovementTypeSprite(
                    movementType
                );

            movementFrameImage.sprite =
                sprite;

            movementFrameImage.enabled =
                sprite != null;
        }

        private Sprite FindActionTypeSprite(
            ActionType type)
        {
            for (int i = 0;
                 i < actionTypeSprites.Count;
                 i++)
            {
                if (actionTypeSprites[i].type ==
                    type)
                {
                    return actionTypeSprites[i]
                        .sprite;
                }
            }

            if (Application.isPlaying)
            {
                Debug.LogWarning(
                    $"[PieceTypeIconUI] " +
                    $"ActionType Sprite 없음 | " +
                    $"Type={type}"
                );
            }

            return null;
        }

        private Sprite FindMovementTypeSprite(MovementType type)
        {
            for (int i = 0; i < movementTypeSprites.Count; i++)
            {
                if (movementTypeSprites[i].type == type)
                {
                    return movementTypeSprites[i]
                        .sprite;
                }
            }

            if (Application.isPlaying)

            {
                Debug.LogWarning(
                    $"[PieceTypeIconUI] " +
                    $"MovementType Sprite 없음 | " +
                    $"Type={type}"
                );
            }

            return null;
        }

        public void Clear()
        {
            targetPiece =
                null;

            hasCachedValue =
                false;

            ClearImages();
        }

        private void ClearImages()
        {
            if (movementFrameImage != null)
            {
                movementFrameImage.sprite =
                    null;

                movementFrameImage.enabled =
                    false;
            }

            if (actionTypeImage != null)
            {
                actionTypeImage.sprite =
                    null;

                actionTypeImage.enabled =
                    false;
            }
        }
    }
}