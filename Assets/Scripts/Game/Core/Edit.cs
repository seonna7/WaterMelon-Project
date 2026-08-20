using Game.GamePlay;
using Game.GamePlay.Grid;
using Game.GamePlay.Placement;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Core
{
    [ExecuteAlways]
    public sealed class Edit : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private PieceSpawnData spawnData;

        [SerializeField]
        private PieceSpawnPreviewController previewController;

        [Header("Raycast")]
        [SerializeField]
        private LayerMask boardLayerMask;

        [SerializeField]
        private LayerMask pieceLayerMask;

        [SerializeField]
        private LayerMask obstacleLayerMask;

        [SerializeField]
        [Min(0.1f)]
        private float rayDistance = 100f;

        [Header("Prefab Placement")]
        [SerializeField]
        private float prefabHeightOffset = 0f;

        [SerializeField]
        [Tooltip(
            "배치가 끝난 체스말도 GridPosition에 해당하는 " +
            "셀의 X/Z 정중앙을 계속 유지합니다.")]
        private bool keepPlacedPiecesCentered = true;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("등록된 체스말의 셀 중앙 위치를 검사하는 간격입니다.")]
        private float placedPieceCenterCheckInterval = 0.2f;

        [SerializeField]
        [Tooltip(
            "씬에 존재하지만 IsPlaced가 false인 ChessPiece를 " +
            "현재 Transform 위치에 해당하는 셀에 자동 등록합니다.")]
        private bool autoRegisterScenePieces = true;

        [Header("Highlight")]
        [SerializeField]
        private Color validHighlightColor =
            new Color(
                0f,
                1f,
                0.3f,
                0.35f);

        [SerializeField]
        private Color blockedHighlightColor =
            new Color(
                1f,
                0.15f,
                0.1f,
                0.35f);

        [SerializeField]
        private float highlightHeightOffset = 0.03f;

        [Header("Collision Check")]
        [SerializeField]
        [Range(0.1f, 1f)]
        private float overlapCellRatio = 0.9f;

        [SerializeField]
        [Min(0.01f)]
        private float overlapHeight = 1f;

        [Header("State")]
        [SerializeField]
        private GameObject selectedObject;

        [SerializeField]
        private Vector2Int highlightedGridPosition;

        [SerializeField]
        private Vector3 highlightedWorldPosition;

        [SerializeField]
        private bool hasHighlightedPosition;

        [SerializeField]
        private bool isPlacementBlocked;

        [SerializeField]
        private bool isDragging;

        private Vector3 originalWorldPosition;
        private Vector2Int originalGridPosition;
        private float highlightedBoardLocalHeight;
        private string selectedSpawnEntryId;
        private double nextPlacedPieceCenterCheckTime;

#if UNITY_EDITOR

        private void OnEnable()
        {
            if (Application.isPlaying)
                return;

            SceneView.duringSceneGui -=
                OnSceneGUI;

            SceneView.duringSceneGui +=
                OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -=
                OnSceneGUI;
        }

        private void OnValidate()
        {
            overlapCellRatio =
                Mathf.Clamp(
                    overlapCellRatio,
                    0.1f,
                    1f);

            overlapHeight =
                Mathf.Max(
                    overlapHeight,
                    0.01f);

            placedPieceCenterCheckInterval =
                Mathf.Max(
                    placedPieceCenterCheckInterval,
                    0.05f);
        }

        private void OnSceneGUI(
            SceneView sceneView)
        {
            if (Application.isPlaying)
                return;

            ResolveReferences();

            if (gridManager == null)
                return;

            Event currentEvent =
                Event.current;

            Ray mouseRay =
                HandleUtility.GUIPointToWorldRay(
                    currentEvent.mousePosition);

            UpdateHighlightedCell(
                mouseRay);

            /*
             * 마우스를 놓은 뒤에도 등록된 체스말이 셀 중앙에서
             * 벗어나지 않았는지 일정 간격으로 검사하고 보정한다.
             */
            MaintainPlacedPieceCenters();

            /*
             * MouseDrag 이벤트가 발생하지 않는 Scene GUI 갱신 중에도
             * 선택한 체스말이 현재 셀의 X/Z 정중앙을 유지하게 한다.
             *
             * 예를 들어 Inspector 변경, Scene Repaint, Layout 이벤트가
             * 발생해도 드래그 중인 말의 위치가 셀 경계로 틀어지지 않는다.
             */
            if (isDragging &&
                selectedObject != null &&
                hasHighlightedPosition &&
                !isPlacementBlocked)
            {
                MoveSelectedObjectToHighlight();
            }

            HandleLeftMouse(
                currentEvent,
                mouseRay);

            HandleRightMouse(
                currentEvent);

            DrawHighlight();

            if (currentEvent.type ==
                    EventType.MouseMove ||
                currentEvent.type ==
                    EventType.MouseDrag ||
                currentEvent.type ==
                    EventType.MouseDown ||
                currentEvent.type ==
                    EventType.MouseUp)
            {
                sceneView.Repaint();
            }
        }

        private void ResolveReferences()
        {
            if (gridManager != null)
                return;

            gridManager =
                FindFirstObjectByType<
                    GridManager>();
        }

        private void HandleLeftMouse(
            Event currentEvent,
            Ray mouseRay)
        {
            if (currentEvent.alt)
                return;

            /*
             * 좌클릭으로 씬에 있는 말을 선택하고
             * 즉시 드래그를 시작한다.
             */
            if (currentEvent.type ==
                    EventType.MouseDown &&
                currentEvent.button == 0)
            {
                if (TrySelectPiece(
                        mouseRay))
                {
                    BeginDrag();

                    MoveSelectedObjectToHighlight();

                    GUIUtility.hotControl =
                        GUIUtility.GetControlID(
                            FocusType.Passive);

                    currentEvent.Use();
                }

                return;
            }

            /*
             * 좌클릭을 누르고 마우스를 움직이면
             * 선택된 프리팹이 셀 중앙을 따라 이동한다.
             */
            if (currentEvent.type ==
                    EventType.MouseDrag &&
                currentEvent.button == 0 &&
                isDragging &&
                selectedObject != null)
            {
                MoveSelectedObjectToHighlight();

                currentEvent.Use();
                return;
            }

            /*
             * 좌클릭을 놓으면 해당 셀에 배치한다.
             */
            if (currentEvent.type ==
                    EventType.MouseUp &&
                currentEvent.button == 0 &&
                isDragging)
            {
                FinishDrag();

                GUIUtility.hotControl = 0;

                currentEvent.Use();
            }
        }

        private void HandleRightMouse(
            Event currentEvent)
        {
            if (currentEvent.alt ||
                currentEvent.type !=
                    EventType.MouseUp ||
                currentEvent.button != 1 ||
                selectedObject == null)
            {
                return;
            }

            if (hasHighlightedPosition &&
                !isPlacementBlocked)
            {
                MoveSelectedObjectToHighlight();

                if (CommitSelectedObject())
                {
                    selectedObject = null;
                }
                else
                {
                    RestoreOriginalPosition();
                }
            }

            currentEvent.Use();
        }

        private bool TrySelectPiece(
            Ray mouseRay)
        {
            if (!Physics.Raycast(
                    mouseRay,
                    out RaycastHit pieceHit,
                    rayDistance,
                    pieceLayerMask,
                    QueryTriggerInteraction.Collide))
            {
                return false;
            }

            selectedObject =
                FindPieceRoot(
                    pieceHit.collider.gameObject);

            Selection.activeGameObject =
                selectedObject;

            return selectedObject != null;
        }

        private static GameObject FindPieceRoot(
            GameObject hitObject)
        {
            ChessPiece chessPiece =
                hitObject.GetComponentInParent<
                    ChessPiece>();

            return chessPiece != null
                ? chessPiece.gameObject
                : hitObject;
        }

        private void BeginDrag()
        {
            if (selectedObject == null)
                return;

            isDragging = true;

            originalWorldPosition =
                selectedObject.transform.position;

            ChessPiece chessPiece =
                selectedObject.GetComponent<
                    ChessPiece>();

            originalGridPosition =
                chessPiece != null
                    ? chessPiece.GridPosition
                    : new Vector2Int(-1, -1);

            selectedSpawnEntryId =
                ResolveSpawnEntryId(
                    chessPiece,
                    originalGridPosition);

            Undo.RecordObject(
                selectedObject.transform,
                "Move Chess Piece");

            if (chessPiece != null)
            {
                Undo.RecordObject(
                    chessPiece,
                    "Move Chess Piece");
            }
        }

        private void FinishDrag()
        {
            isDragging = false;

            if (selectedObject == null)
                return;

            if (!hasHighlightedPosition ||
                isPlacementBlocked)
            {
                RestoreOriginalPosition();
                return;
            }

            MoveSelectedObjectToHighlight();

            if (!CommitSelectedObject())
            {
                RestoreOriginalPosition();
                return;
            }

            selectedObject = null;

            SceneView.RepaintAll();
        }

        private void RestoreOriginalPosition()
        {
            if (selectedObject == null)
                return;

            selectedObject.transform.position =
                originalWorldPosition;

            ChessPiece chessPiece =
                selectedObject.GetComponent<
                    ChessPiece>();

            if (chessPiece != null)
            {
                /*
                 * 등록 도중 실패해서 기존 셀이 해제됐을 수도 있으므로
                 * Transform뿐 아니라 GridManager 점유 상태도 복구한다.
                 */
                if (gridManager != null &&
                    originalGridPosition.x >= 0 &&
                    originalGridPosition.y >= 0)
                {
                    ChessPiece originalOccupant =
                        gridManager.GetPieceAt(originalGridPosition);

                    if (originalOccupant == null)
                    {
                        chessPiece.ClearGridPosition();
                        gridManager.PlacePiece(
                            chessPiece,
                            originalGridPosition);
                    }
                    else if (originalOccupant == chessPiece)
                    {
                        chessPiece.SetGridPosition(originalGridPosition);
                    }
                }
                else
                {
                    chessPiece.ClearGridPosition();
                }

                EditorUtility.SetDirty(
                    chessPiece);
            }

            EditorUtility.SetDirty(
                selectedObject.transform);

            selectedObject = null;

            SceneView.RepaintAll();
        }

        private void MoveSelectedObjectToHighlight()
        {
            if (selectedObject == null ||
                !hasHighlightedPosition ||
                isPlacementBlocked)
            {
                return;
            }

            Vector3 targetWorldPosition =
                GetCellCenterPreservingHeight(
                    selectedObject.transform,
                    highlightedGridPosition,
                    true);

            /*
             * GridToWorld의 center=true가 +0.5 셀을 적용하므로
             * 프리팹의 Transform은 셀 정중앙으로 이동한다.
             */
            selectedObject.transform.position =
                targetWorldPosition;

            EditorUtility.SetDirty(
                selectedObject.transform);
        }

        private bool CommitSelectedObject()
        {
            if (selectedObject == null)
                return false;

            ChessPiece chessPiece =
                selectedObject.GetComponentInParent<
                    ChessPiece>();

            if (chessPiece == null)
            {
                Debug.LogError(
                    $"[Edit] ChessPiece 없음 | Object={selectedObject.name}",
                    selectedObject);
                return false;
            }

            /*
             * 배치 확정 순간에도 셀 정중앙으로 보정한다.
             * 이 함수는 GridManager Local X/Z만 변경하고 Y는 보존한다.
             */
            chessPiece.transform.position =
                GetCellCenterPreservingHeight(
                    chessPiece.transform,
                    highlightedGridPosition,
                    true);

            if (!SynchronizeGridRegistration(
                    chessPiece,
                    highlightedGridPosition))
            {
                return false;
            }

            /*
             * GridManager 등록이 성공한 뒤 실제 Transform이
             * 등록된 셀 중앙인지 다시 검사하고 즉시 보정한다.
             */
            SnapPieceToRegisteredCellCenter(
                chessPiece,
                true);

            SynchronizeSpawnData(
                chessPiece,
                highlightedGridPosition);

            EditorUtility.SetDirty(chessPiece);
            EditorUtility.SetDirty(gridManager);
            EditorUtility.SetDirty(chessPiece.transform);

            return true;
        }

        private void MaintainPlacedPieceCenters()
        {
            if (!keepPlacedPiecesCentered ||
                gridManager == null)
            {
                return;
            }

            if (!gridManager.IsInitialized)
            {
                gridManager.InitializeGrid();
            }

            /* Scene GUI 이벤트마다 전체 검색하지 않도록 검사 주기를 제한한다. */
            if (EditorApplication.timeSinceStartup <
                nextPlacedPieceCenterCheckTime)
            {
                return;
            }

            nextPlacedPieceCenterCheckTime =
                EditorApplication.timeSinceStartup +
                placedPieceCenterCheckInterval;

            ChessPiece[] pieces =
                FindObjectsByType<ChessPiece>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            for (int i = 0; i < pieces.Length; i++)
            {
                ChessPiece piece = pieces[i];

                if (piece == null)
                {
                    continue;
                }

                /*
                 * IsPlaced가 false인 씬 체스말도 현재 Transform을 기준으로
                 * 어느 셀 위에 있는지 계산해서 GridManager에 등록한다.
                 */
                Vector2Int targetPosition;

                if (piece.IsPlaced &&
                    gridManager.IsInsideGrid(piece.GridPosition))
                {
                    targetPosition = piece.GridPosition;
                }
                else
                {
                    if (!autoRegisterScenePieces ||
                        !gridManager.TryWorldToGrid(
                            piece.transform.position,
                            out targetPosition))
                    {
                        continue;
                    }
                }

                ChessPiece occupant =
                    gridManager.GetPieceAt(targetPosition);

                if (occupant == null)
                {
                    Undo.RecordObject(
                        piece,
                        "Register Scene Chess Piece");

                    piece.ClearGridPosition();

                    if (!gridManager.PlacePiece(
                            piece,
                            targetPosition))
                    {
                        continue;
                    }

                    EditorUtility.SetDirty(piece);
                    EditorUtility.SetDirty(gridManager);

                    Debug.Log(
                        $"[Edit] 씬 체스말 자동 등록 | " +
                        $"Piece={piece.name} | Grid={targetPosition}",
                        piece);
                }
                else if (occupant != piece)
                {
                    /* 다른 말이 점유한 셀에는 자동 등록하거나 이동하지 않는다. */
                    continue;
                }

                SnapPieceToRegisteredCellCenter(
                    piece,
                    false);

                /*
                 * 자동 등록된 씬 체스말도 마우스로 직접 배치한 말과 동일하게
                 * PieceSpawnData 및 PieceSpawnPreviewInstance를 보장한다.
                 */
                PieceSpawnPreviewInstance existingMarker =
                    piece.GetComponent<PieceSpawnPreviewInstance>();

                bool markerNeedsBinding =
                    existingMarker == null ||
                    string.IsNullOrWhiteSpace(existingMarker.EntryId) ||
                    (spawnData != null &&
                     spawnData.FindPieceById(existingMarker.EntryId) == null);

                if (markerNeedsBinding)
                {
                    SynchronizeSpawnData(
                        piece,
                        piece.GridPosition);
                }
            }
        }

        private void SnapPieceToRegisteredCellCenter(
            ChessPiece piece,
            bool logCorrection)
        {
            if (piece == null ||
                gridManager == null ||
                !piece.IsPlaced ||
                !gridManager.IsInsideGrid(piece.GridPosition))
            {
                return;
            }

            Transform pieceTransform =
                piece.transform;

            /*
             * GridToWorld(..., center:true)로 정확한 셀 중심을 구한다.
             * 지속 보정에서는 기존 Local Y를 보존하고 X/Z만 중앙에 맞춘다.
             * 따라서 체스말을 보드 위로 띄운 높이는 사라지지 않는다.
             */
            Vector3 currentGridLocalPosition =
                gridManager.WorldToGridLocal(
                    pieceTransform.position);

            Vector3 centerWorldPosition =
                gridManager.GridToWorld(
                    piece.GridPosition,
                    currentGridLocalPosition.y,
                    true);

            Vector3 difference =
                pieceTransform.position -
                centerWorldPosition;

            /* 0.01mm 이하의 차이는 부동소수점 오차로 보고 무시한다. */
            if (difference.sqrMagnitude <= 0.0000000001f)
            {
                return;
            }

            Undo.RecordObject(
                pieceTransform,
                "Snap Chess Piece To Cell Center");

            pieceTransform.position =
                centerWorldPosition;

            EditorUtility.SetDirty(
                pieceTransform);

            if (logCorrection)
            {
                Debug.Log(
                    $"[Edit] 셀 중앙 보정 완료 | " +
                    $"Piece={piece.name} | " +
                    $"Grid={piece.GridPosition} | " +
                    $"World={centerWorldPosition}",
                    piece);
            }
        }

        private Vector3 GetCellCenterPreservingHeight(
            Transform target,
            Vector2Int gridPosition,
            bool center)
        {
            /*
             * 핵심 규칙:
             * - X/Z는 GridManager가 계산한 셀 중앙값 사용
             * - Y는 현재 오브젝트의 Grid Local Y를 그대로 사용
             *
             * 월드 Y를 직접 보존하지 않고 Grid Local Y를 보존하므로
             * GridManager가 회전돼 있어도 그리드 평면으로부터의 높이가 유지된다.
             */
            if (target == null)
                return Vector3.zero;

            Vector3 currentGridLocal =
                gridManager.WorldToGridLocal(
                    target.position);

            return gridManager.GridToWorld(
                gridPosition,
                currentGridLocal.y,
                center);
        }

        private bool SynchronizeGridRegistration(
            ChessPiece chessPiece,
            Vector2Int targetPosition)
        {
            if (gridManager == null ||
                chessPiece == null)
            {
                return false;
            }

            if (!gridManager.IsInitialized)
            {
                gridManager.InitializeGrid();
            }

            Vector2Int previousPosition =
                chessPiece.GridPosition;

            ChessPiece previousRegisteredPiece =
                chessPiece.IsPlaced
                    ? gridManager.GetPieceAt(previousPosition)
                    : null;

            bool wasRegistered =
                previousRegisteredPiece == chessPiece;

            /*
             * 기존 셀을 먼저 지우기 전에 목적지 점유 상태부터 확인한다.
             * 목적지에 자기 자신이 있는 경우는 같은 셀 재배치이므로 허용한다.
             */
            ChessPiece targetOccupant =
                gridManager.GetPieceAt(targetPosition);

            if (targetOccupant != null &&
                targetOccupant != chessPiece)
            {
                Debug.LogWarning(
                    $"[Edit] 이미 점유된 셀입니다. | " +
                    $"Position={targetPosition} | " +
                    $"Occupant={targetOccupant.name}",
                    targetOccupant);

                return false;
            }

            if (chessPiece.IsPlaced)
            {
                ChessPiece registeredPiece =
                    gridManager.GetPieceAt(chessPiece.GridPosition);

                if (registeredPiece == chessPiece)
                {
                    gridManager.RemovePiece(chessPiece);
                }
                else
                {
                    /* 좌표만 있고 셀 점유가 없는 불일치 상태를 정리한다. */
                    chessPiece.ClearGridPosition();
                }
            }

            if (!gridManager.PlacePiece(
                    chessPiece,
                    targetPosition))
            {
                Debug.LogError(
                    $"[Edit] Grid 등록 실패 | Piece={chessPiece.name} | " +
                    $"Position={targetPosition} | " +
                    $"Inside={gridManager.IsInsideGrid(targetPosition)} | " +
                    $"Empty={gridManager.IsEmpty(targetPosition)}",
                    chessPiece
                );

                /* 등록 실패 시 기존에 점유하던 셀로 원자적으로 복구한다. */
                if (wasRegistered)
                {
                    chessPiece.ClearGridPosition();
                    gridManager.PlacePiece(
                        chessPiece,
                        previousPosition);
                }

                return false;
            }

            Debug.Log(
                $"[Edit] Grid 등록 성공 | Piece={chessPiece.name} | " +
                $"Position={targetPosition} | Placed={chessPiece.IsPlaced}",
                chessPiece);

            return true;
        }

        private string ResolveSpawnEntryId(
            ChessPiece chessPiece,
            Vector2Int gridPosition)
        {
            if (spawnData == null ||
                chessPiece == null)
            {
                return string.Empty;
            }

            PieceSpawnPreviewInstance marker =
                chessPiece.GetComponent<
                    PieceSpawnPreviewInstance>();

            if (marker != null &&
                !string.IsNullOrWhiteSpace(
                    marker.EntryId))
            {
                return marker.EntryId;
            }

            ChessPiece prefab =
                PrefabUtility
                    .GetCorrespondingObjectFromSource(
                        chessPiece);

            if (prefab == null)
                return string.Empty;

            PieceSpawnEntry entry =
                spawnData.FindPiece(
                    prefab,
                    chessPiece.Color,
                    gridPosition);

            return entry != null
                ? entry.EntryId
                : string.Empty;
        }

        private void SynchronizeSpawnData(
            ChessPiece chessPiece,
            Vector2Int gridPosition)
        {
            if (chessPiece == null)
            {
                return;
            }

            /*
             * SpawnData/Prefab 연결 상태와 관계없이 먼저 Marker를 붙인다.
             * 기존 코드는 Entry 생성이 성공한 뒤에만 Marker를 붙였기 때문에
             * 자동 등록된 말이나 Prefab 연결이 끊긴 말에는 컴포넌트가 없었다.
             */
            PieceSpawnPreviewInstance marker =
                EnsurePreviewMarker(chessPiece);

            if (spawnData == null)
            {
                Debug.LogWarning(
                    $"[Edit] PieceSpawnData가 없어 Marker만 추가했습니다. | " +
                    $"Piece={chessPiece.name}",
                    chessPiece);
                return;
            }

            ChessPiece prefab =
                PrefabUtility
                    .GetCorrespondingObjectFromSource(
                        chessPiece);

            if (prefab == null)
            {
                Debug.LogWarning(
                    $"[Edit] {chessPiece.name}은 Prefab 인스턴스가 " +
                    "아니므로 Marker만 추가하고 SpawnData에는 기록하지 않았습니다.",
                    chessPiece
                );

                return;
            }

            Undo.RecordObject(
                spawnData,
                "Update Piece Spawn Data");

            Vector2Int lookupPosition =
                selectedObject == chessPiece.gameObject
                    ? originalGridPosition
                    : gridPosition;

            PieceSpawnEntry entry =
                spawnData.FindPieceById(
                    ResolveSpawnEntryId(
                        chessPiece,
                        lookupPosition));

            /* 현재 드래그 중인 말에 대해서만 선택 당시 EntryId를 사용한다. */
            if (entry == null &&
                selectedObject == chessPiece.gameObject)
            {
                entry = spawnData.FindPieceById(
                    selectedSpawnEntryId);
            }

            if (entry == null)
            {
                entry = spawnData.FindPiece(
                    prefab,
                    chessPiece.Color,
                    lookupPosition);
            }

            if (entry == null)
            {
                entry = spawnData.AddPiece(
                    prefab,
                    chessPiece.Color,
                    gridPosition);
            }
            else
            {
                entry.SetPrefab(prefab);
                entry.SetColor(chessPiece.Color);
                entry.SetSpawnPosition(gridPosition);
            }

            selectedSpawnEntryId =
                entry != null
                    ? entry.EntryId
                    : string.Empty;

            if (entry != null &&
                marker != null)
            {
                if (previewController == null)
                {
                    previewController =
                        FindFirstObjectByType<
                            PieceSpawnPreviewController>();
                }

                marker.Bind(
                    previewController,
                    entry.EntryId);

                EditorUtility.SetDirty(marker);
            }

            EditorUtility.SetDirty(spawnData);
        }

        private PieceSpawnPreviewInstance EnsurePreviewMarker(
            ChessPiece chessPiece)
        {
            if (chessPiece == null)
                return null;

            PieceSpawnPreviewInstance marker =
                chessPiece.GetComponent<PieceSpawnPreviewInstance>();

            if (marker != null)
                return marker;

            /* Undo를 지원하면서 ChessPiece 루트에 Marker를 추가한다. */
            marker = Undo.AddComponent<PieceSpawnPreviewInstance>(
                chessPiece.gameObject);

            if (marker != null)
            {
                EditorUtility.SetDirty(marker);

                Debug.Log(
                    $"[Edit] PieceSpawnPreviewInstance 추가 | " +
                    $"Piece={chessPiece.name}",
                    chessPiece);
            }

            return marker;
        }

        private void UpdateHighlightedCell(
            Ray mouseRay)
        {
            hasHighlightedPosition = false;
            isPlacementBlocked = false;

            if (!Physics.Raycast(
                    mouseRay,
                    out RaycastHit boardHit,
                    rayDistance,
                    boardLayerMask,
                    QueryTriggerInteraction.Collide))
            {
                return;
            }

            /* 좌표 판정도 GridManager와 동일한 변환 함수를 사용한다. */
            if (!gridManager.TryWorldToGrid(
                    boardHit.point,
                    out highlightedGridPosition))
            {
                return;
            }

            Vector3 boardLocalPoint =
                gridManager.transform
                    .InverseTransformPoint(
                        boardHit.point);

            highlightedBoardLocalHeight =
                boardLocalPoint.y;

            highlightedWorldPosition =
                gridManager.GridToWorld(
                    highlightedGridPosition,
                    highlightedBoardLocalHeight +
                    highlightHeightOffset,
                    true);

            hasHighlightedPosition = true;

            CheckPlacementBlocked();
        }

        private void CheckPlacementBlocked()
        {
            if (!hasHighlightedPosition)
                return;

            float cellSize =
                gridManager.CellSize;

            float selectedLocalHeight =
                selectedObject != null
                    ? gridManager.WorldToGridLocal(
                        selectedObject.transform.position).y
                    : highlightedBoardLocalHeight + prefabHeightOffset;

            float localCenterHeight =
                selectedLocalHeight +
                overlapHeight * 0.5f;

            Vector3 overlapCenter =
                gridManager.GridToWorld(
                    highlightedGridPosition,
                    localCenterHeight,
                    true);

            Vector3 gridScale =
                gridManager.transform.lossyScale;

            float halfCellSize =
                cellSize *
                overlapCellRatio *
                0.5f;

            Vector3 halfExtents =
                new Vector3(
                    halfCellSize *
                    Mathf.Abs(gridScale.x),

                    overlapHeight *
                    0.5f *
                    Mathf.Abs(gridScale.y),

                    halfCellSize *
                    Mathf.Abs(gridScale.z));

            int collisionMask =
                pieceLayerMask.value |
                obstacleLayerMask.value;

            Collider[] overlaps =
                Physics.OverlapBox(
                    overlapCenter,
                    halfExtents,
                    gridManager.transform.rotation,
                    collisionMask,
                    QueryTriggerInteraction.Collide);

            foreach (Collider overlap in overlaps)
            {
                if (BelongsToSelectedObject(
                        overlap))
                {
                    continue;
                }

                isPlacementBlocked = true;
                return;
            }
        }

        private bool BelongsToSelectedObject(
            Collider targetCollider)
        {
            if (selectedObject == null ||
                targetCollider == null)
            {
                return false;
            }

            Transform selectedTransform =
                selectedObject.transform;

            Transform colliderTransform =
                targetCollider.transform;

            return
                colliderTransform ==
                    selectedTransform ||
                colliderTransform.IsChildOf(
                    selectedTransform);
        }

        private void DrawHighlight()
        {
            if (!hasHighlightedPosition)
                return;

            Color color =
                isPlacementBlocked
                    ? blockedHighlightColor
                    : validHighlightColor;

            float localY =
                highlightedBoardLocalHeight +
                highlightHeightOffset;

            Vector2Int bottomLeft =
                highlightedGridPosition;

            /*
             * center=false를 사용하면 각 Vector2Int 좌표의
             * 셀 모서리 위치를 얻을 수 있다.
             */
            Vector3[] vertices =
            {
                gridManager.GridToWorld(
                    bottomLeft,
                    localY,
                    false),

                gridManager.GridToWorld(
                    bottomLeft + Vector2Int.up,
                    localY,
                    false),

                gridManager.GridToWorld(
                    bottomLeft + Vector2Int.one,
                    localY,
                    false),

                gridManager.GridToWorld(
                    bottomLeft + Vector2Int.right,
                    localY,
                    false)
            };

            Handles.DrawSolidRectangleWithOutline(
                vertices,
                color,
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    1f));
        }

#endif
    }
}