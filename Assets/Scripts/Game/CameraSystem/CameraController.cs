using Game.GamePlay.Grid;
using System;
using System.Collections;
using UnityEngine;

namespace Game.CameraSystem
{
    public class CameraController : MonoBehaviour
    {
        private enum CameraPOV
        {
            Grid,
            PieceClick,
            Move,
            Moving,
            Skill,
            DirectAttack,
            SkillEngaging
        }

        [Header("Camera Target")]
        [SerializeField]
        private Transform cameraTransform;

        [Header("Grid Target")]
        [SerializeField]
        private GridManager grid;

        [Header("Transition")]
        [SerializeField]
        [Min(0.01f)]
        private float transitionDuration = 0.45f;

        [Header("Default Grid POV")]
        [SerializeField]
        private float gridHeightMultiplier = 0.9f;

        [SerializeField]
        private float gridDistanceMultiplier = 0.85f;

        [SerializeField]
        private float gridLookHeight;

        [Header("Top Down POV")]
        [SerializeField]
        private float topDownHeightMultiplier = 1.25f;

        [SerializeField]
        [Range(70f, 90f)]
        private float topDownAngle = 88f;

        [Header("Piece Click POV")]
        [SerializeField]
        private float pieceViewDistance = 5f;

        [SerializeField]
        private float pieceViewHeight = 2.5f;

        [SerializeField]
        private float pieceLookHeight = 1.2f;

        [Header("Piece Orbit")]
        [SerializeField]
        private float orbitSensitivity = 0.25f;

        [SerializeField]
        private float minimumOrbitPitch = -15f;

        [SerializeField]
        private float maximumOrbitPitch = 55f;

        private CameraPOV currentPOV =
            CameraPOV.Grid;

        private Transform currentPiece;

        private Coroutine transitionCoroutine;

        private System.Action pendingTransitionComplete;

        private float orbitYaw;

        private float orbitPitch = 15f;

        private bool isTransitioning;

        public bool IsTransitioning =>
            isTransitioning;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                cameraTransform =
                    Camera.main != null
                        ? Camera.main.transform
                        : transform;
            }

            if (grid == null)
            {
                grid =
                    FindFirstObjectByType<GridManager>();
            }
        }

        private void Start()
        {
            ReturnToGridPOV();
        }

        private void LateUpdate()
        {
            if (currentPOV !=
                CameraPOV.PieceClick)
            {
                return;
            }

            if (currentPiece == null ||
                isTransitioning)
            {
                return;
            }

            UpdatePieceOrbitInput();
            UpdatePieceOrbitPosition();
        }

        public void ExecuteCommand(
            CameraCommand command,
            System.Action onComplete = null)
        {
            switch (command.Type)
            {
                case CameraCommandType.ReturnToGrid:
                    ReturnToGridPOV(onComplete);
                    break;

                case CameraCommandType.PieceClick:
                    ChessPieceClickPOV(
                        command.Target,
                        onComplete
                    );
                    break;

                case CameraCommandType.PieceMove:
                    ChessPieceMovePOV(
                        command.Target,
                        onComplete
                    );
                    break;

                case CameraCommandType.PieceMoving:
                    ChessPieceMovingPOV(
                        command.Target,
                        onComplete
                    );
                    break;

                case CameraCommandType.PieceDirectAttack:
                    ChessPieceDirectAttackPOV(
                        command.Target,
                        onComplete
                    );
                    break;

                case CameraCommandType.PieceSkill:
                    ChessPieceSkillPOV(
                        command.Target,
                        onComplete
                    );
                    break;

                case CameraCommandType.PieceSkillEngaging:
                    ChessPieceSkillEngagingPOV(
                        command.Target,
                        onComplete
                    );
                    break;

                default:
                    onComplete?.Invoke();
                    break;
            }
        }

        public void ChessPieceClickPOV(
            Transform chessPieceTransform,
            System.Action onComplete = null)
        {
            if (chessPieceTransform == null ||
                cameraTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            currentPOV =
                CameraPOV.PieceClick;

            currentPiece =
                chessPieceTransform;

            Vector3 directionFromPiece =
                cameraTransform.position -
                currentPiece.position;

            directionFromPiece.y = 0f;

            if (directionFromPiece.sqrMagnitude <
                0.001f)
            {
                directionFromPiece =
                    -currentPiece.forward;
            }

            orbitYaw =
                Mathf.Atan2(
                    directionFromPiece.x,
                    directionFromPiece.z
                ) * Mathf.Rad2Deg;

            orbitPitch = 15f;

            Vector3 targetPosition =
                CalculatePieceOrbitPosition();

            Quaternion targetRotation =
                CalculateLookRotation(
                    targetPosition,
                    GetPieceLookPosition()
                );

            StartCameraTransition(
                targetPosition,
                targetRotation,
                onComplete
            );
        }

        public void ChessPieceMovePOV(
            Transform chessPieceTransform,
            System.Action onComplete = null)
        {
            currentPiece =
                chessPieceTransform;

            currentPOV =
                CameraPOV.Move;

            MoveToTopDownPOV(onComplete);
        }

        public void ChessPieceMovingPOV(
            Transform chessPieceTransform,
            System.Action onComplete = null)
        {
            currentPiece =
                chessPieceTransform;

            currentPOV =
                CameraPOV.Moving;

            MoveToTopDownPOV(onComplete);
        }

        public void ChessPieceDirectAttackPOV(
            Transform chessPieceTransform,
            System.Action onComplete = null)
        {
            ChessPieceClickPOV(
                chessPieceTransform,
                onComplete
            );

            currentPOV =
                CameraPOV.DirectAttack;
        }

        public void ChessPieceSkillPOV(
            Transform chessPieceTransform,
            System.Action onComplete = null)
        {
            currentPiece =
                chessPieceTransform;

            currentPOV =
                CameraPOV.Skill;

            MoveToTopDownPOV(onComplete);
        }

        public void ChessPieceSkillEngagingPOV(
            Transform chessPieceTransform,
            System.Action onComplete = null)
        {
            currentPiece =
                chessPieceTransform;

            currentPOV =
                CameraPOV.SkillEngaging;

            MoveToTopDownPOV(onComplete);
        }

        public void ReturnToGridPOV(
            System.Action onComplete = null)
        {
            if (grid == null ||
                cameraTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            currentPOV =
                CameraPOV.Grid;

            currentPiece = null;

            Vector3 gridCenter =
                GetGridCenter();

            float gridSize =
                GetGridSize();

            float height =
                gridSize *
                gridHeightMultiplier;

            float distance =
                gridSize *
                gridDistanceMultiplier;

            Vector3 targetPosition =
                gridCenter +
                new Vector3(
                    0f,
                    height,
                    -distance
                );

            Vector3 lookPosition =
                gridCenter +
                Vector3.up *
                gridLookHeight;

            Quaternion targetRotation =
                CalculateLookRotation(
                    targetPosition,
                    lookPosition
                );

            StartCameraTransition(
                targetPosition,
                targetRotation,
                onComplete
            );
        }

        private void MoveToTopDownPOV(
            System.Action onComplete = null)
        {
            if (grid == null ||
                cameraTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 gridCenter =
                GetGridCenter();

            float gridSize =
                GetGridSize();

            float cameraHeight =
                gridSize *
                topDownHeightMultiplier;

            Vector3 targetPosition =
                gridCenter +
                Vector3.up *
                cameraHeight;

            Quaternion targetRotation =
                Quaternion.Euler(
                    topDownAngle,
                    0f,
                    0f
                );

            StartCameraTransition(
                targetPosition,
                targetRotation,
                onComplete
            );
        }

        private void UpdatePieceOrbitInput()
        {
            if (!Input.GetMouseButton(0))
                return;

            float mouseX =
                Input.GetAxis("Mouse X");

            float mouseY =
                Input.GetAxis("Mouse Y");

            orbitYaw +=
                mouseX *
                orbitSensitivity *
                100f *
                Time.deltaTime;

            orbitPitch -=
                mouseY *
                orbitSensitivity *
                100f *
                Time.deltaTime;

            orbitPitch =
                Mathf.Clamp(
                    orbitPitch,
                    minimumOrbitPitch,
                    maximumOrbitPitch
                );
        }

        private void UpdatePieceOrbitPosition()
        {
            Vector3 targetPosition =
                CalculatePieceOrbitPosition();

            Vector3 lookPosition =
                GetPieceLookPosition();

            cameraTransform.position =
                targetPosition;

            cameraTransform.rotation =
                CalculateLookRotation(
                    targetPosition,
                    lookPosition
                );
        }

        private Vector3 CalculatePieceOrbitPosition()
        {
            if (currentPiece == null)
            {
                return cameraTransform.position;
            }

            Quaternion orbitRotation =
                Quaternion.Euler(
                    orbitPitch,
                    orbitYaw,
                    0f
                );

            Vector3 offset =
                orbitRotation *
                new Vector3(
                    0f,
                    0f,
                    pieceViewDistance
                );

            return currentPiece.position +
                   Vector3.up *
                   pieceViewHeight +
                   offset;
        }

        private Vector3 GetPieceLookPosition()
        {
            if (currentPiece == null)
            {
                return Vector3.zero;
            }

            return currentPiece.position +
                   Vector3.up *
                   pieceLookHeight;
        }

        private Vector3 GetGridCenter()
        {
            if (grid == null)
                return Vector3.zero;

            float width =
                grid.GridWidth *
                grid.CellSize;

            float height =
                grid.GridHeight *
                grid.CellSize;

            return grid.WorldOrigin +
                   new Vector3(
                       width * 0.5f,
                       0f,
                       height * 0.5f
                   );
        }

        private float GetGridSize()
        {
            if (grid == null)
                return 1f;

            float width =
                grid.GridWidth *
                grid.CellSize;

            float height =
                grid.GridHeight *
                grid.CellSize;

            return Mathf.Max(
                width,
                height
            );
        }

        private static Quaternion
            CalculateLookRotation(
                Vector3 cameraPosition,
                Vector3 lookPosition)
        {
            Vector3 direction =
                lookPosition -
                cameraPosition;

            if (direction.sqrMagnitude <
                0.001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );
        }

        private void StartCameraTransition(
            Vector3 targetPosition,
            Quaternion targetRotation,
            System.Action onComplete = null)
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(
                    transitionCoroutine
                );

                transitionCoroutine = null;
                isTransitioning = false;

                System.Action interruptedCallback =
                    pendingTransitionComplete;

                pendingTransitionComplete = null;

                interruptedCallback?.Invoke();
            }

            pendingTransitionComplete =
                onComplete;

            transitionCoroutine =
                StartCoroutine(
                    CameraTransitionRoutine(
                        targetPosition,
                        targetRotation
                    )
                );
        }

        private IEnumerator CameraTransitionRoutine(
            Vector3 targetPosition,
            Quaternion targetRotation)
        {
            isTransitioning = true;

            Vector3 startPosition =
                cameraTransform.position;

            Quaternion startRotation =
                cameraTransform.rotation;

            float elapsed = 0f;

            while (elapsed <
                   transitionDuration)
            {
                elapsed +=
                    Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        transitionDuration
                    );

                float smoothT =
                    EaseInOutCubic(t);

                cameraTransform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        smoothT
                    );

                cameraTransform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        smoothT
                    );

                yield return null;
            }

            cameraTransform.position =
                targetPosition;

            cameraTransform.rotation =
                targetRotation;

            isTransitioning = false;
            transitionCoroutine = null;

            System.Action completeCallback =
                pendingTransitionComplete;

            pendingTransitionComplete = null;

            completeCallback?.Invoke();
        }

        private static float EaseInOutCubic(
            float value)
        {
            if (value < 0.5f)
            {
                return 4f *
                       value *
                       value *
                       value;
            }

            float inverse =
                -2f * value + 2f;

            return 1f -
                   inverse *
                   inverse *
                   inverse /
                   2f;
        }

        private void OnDisable()
        {
            if (transitionCoroutine != null)
            {
                StopCoroutine(
                    transitionCoroutine
                );

                transitionCoroutine = null;
            }

            isTransitioning = false;

            System.Action callback =
                pendingTransitionComplete;

            pendingTransitionComplete = null;

            callback?.Invoke();
        }
    }
}