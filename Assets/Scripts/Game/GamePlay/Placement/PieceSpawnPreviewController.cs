using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif



namespace Game.GamePlay.Placement
{
    [ExecuteAlways]
    public sealed class PieceSpawnPreviewController
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private PieceSpawnData spawnData;

        [Header("Preview Roots")]
        [SerializeField]
        private Transform previewRoot;

        [SerializeField]
        private Transform whiteRoot;

        [SerializeField]
        private Transform blackRoot;

        [Header("Synchronization")]
        [SerializeField]
        private bool rebuildOnEnable;

        [SerializeField]
        private bool autoSynchronizeHierarchy = true;

        private bool isSynchronizing;
        private bool previewIsActive;

        public GridManager GridManager =>
            gridManager;

        public PieceSpawnData SpawnData =>
            spawnData;

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (Application.isPlaying)
                return;

            ResolveReferences();

            EditorApplication.hierarchyChanged -=
                OnHierarchyChanged;

            EditorApplication.hierarchyChanged +=
                OnHierarchyChanged;

            previewIsActive =
                GetOwnedMarkers().Count > 0;

            if (rebuildOnEnable)
            {
                EditorApplication.delayCall +=
                    RebuildPreviewFromData;
            }
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -=
                OnHierarchyChanged;
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<GridManager>();
            }

            if (previewRoot == null)
            {
                previewRoot = transform;
            }
        }

        private void OnHierarchyChanged()
        {
            if (Application.isPlaying ||
                isSynchronizing ||
                !previewIsActive ||
                !autoSynchronizeHierarchy)
            {
                return;
            }

            EditorApplication.delayCall -=
                SynchronizeSceneToData;

            EditorApplication.delayCall +=
                SynchronizeSceneToData;
        }

        [ContextMenu("Rebuild Preview From Data")]
        public void RebuildPreviewFromData()
        {
            if (Application.isPlaying ||
                isSynchronizing)
            {
                return;
            }

            ResolveReferences();

            if (gridManager == null ||
                spawnData == null)
            {
                Debug.LogWarning(
                    "[PieceSpawnPreviewController] " +
                    "GridManager 또는 SpawnData가 없습니다.",
                    this
                );

                return;
            }

            isSynchronizing = true;

            try
            {
                Undo.RecordObject(
                    spawnData,
                    "Validate Piece Spawn Data");

                spawnData.EnsureEntryIds();
                ClearPreviewInternal();

                for (int i = 0;
                     i < spawnData.Pieces.Count;
                     i++)
                {
                    PieceSpawnEntry entry =
                        spawnData.Pieces[i];

                    CreatePreview(entry);
                }

                previewIsActive = true;
                EditorUtility.SetDirty(spawnData);
            }
            finally
            {
                isSynchronizing = false;
            }

            SceneView.RepaintAll();
        }

        private void CreatePreview(
            PieceSpawnEntry entry)
        {
            if (entry == null ||
                entry.Prefab == null ||
                !gridManager.IsInsideGrid(
                    entry.SpawnPosition))
            {
                return;
            }

            Transform parent =
                GetTeamRoot(entry.Color);

            GameObject previewObject =
                PrefabUtility.InstantiatePrefab(
                    entry.Prefab.gameObject,
                    parent) as GameObject;

            if (previewObject == null)
                return;

            Undo.RegisterCreatedObjectUndo(
                previewObject,
                "Create Piece Spawn Preview");

            ChessPiece piece =
                previewObject.GetComponent<ChessPiece>();

            if (piece == null)
            {
                Undo.DestroyObjectImmediate(previewObject);
                return;
            }

            piece.Initialize(entry.Color);
            piece.SetGridPosition(entry.SpawnPosition);
            piece.transform.position =
                gridManager.GridToWorld(
                    entry.SpawnPosition);

            PieceSpawnPreviewInstance marker =
                Undo.AddComponent<
                    PieceSpawnPreviewInstance>(
                        previewObject);

            marker.Bind(
                this,
                entry.EntryId);

            EditorUtility.SetDirty(marker);
            EditorUtility.SetDirty(piece);
        }

        [ContextMenu("Synchronize Scene To Data")]
        public void SynchronizeSceneToData()
        {
            if (Application.isPlaying ||
                isSynchronizing ||
                !previewIsActive ||
                spawnData == null ||
                gridManager == null)
            {
                return;
            }

            isSynchronizing = true;

            try
            {
                Undo.RecordObject(
                    spawnData,
                    "Synchronize Piece Spawn Data");

                AddUntrackedScenePieces();

                List<PieceSpawnPreviewInstance> markers =
                    GetOwnedMarkers();

                HashSet<string> existingIds =
                    new();

                for (int i = 0;
                     i < markers.Count;
                     i++)
                {
                    PieceSpawnPreviewInstance marker =
                        markers[i];

                    ChessPiece piece =
                        marker.GetComponent<ChessPiece>();

                    if (piece == null)
                        continue;

                    ChessPiece prefab =
                        PrefabUtility
                            .GetCorrespondingObjectFromSource(
                                piece);

                    if (prefab == null)
                        continue;

                    Vector2Int position =
                        gridManager.WorldToGrid(
                            piece.transform.position);

                    if (!gridManager.IsInsideGrid(position))
                        continue;

                    PieceSpawnEntry entry =
                        spawnData.FindPieceById(
                            marker.EntryId);

                    if (entry == null)
                    {
                        entry = spawnData.AddPiece(
                            prefab,
                            piece.Color,
                            position);

                        marker.Bind(
                            this,
                            entry.EntryId);
                    }

                    entry.SetPrefab(prefab);
                    entry.SetColor(piece.Color);
                    entry.SetSpawnPosition(position);
                    piece.SetGridPosition(position);

                    existingIds.Add(entry.EntryId);

                    EditorUtility.SetDirty(marker);
                    EditorUtility.SetDirty(piece);
                }

                List<string> removedIds =
                    new();

                for (int i = 0;
                     i < spawnData.Pieces.Count;
                     i++)
                {
                    PieceSpawnEntry entry =
                        spawnData.Pieces[i];

                    if (entry != null &&
                        !existingIds.Contains(entry.EntryId))
                    {
                        removedIds.Add(entry.EntryId);
                    }
                }

                for (int i = 0;
                     i < removedIds.Count;
                     i++)
                {
                    spawnData.RemovePieceById(
                        removedIds[i]);
                }

                EditorUtility.SetDirty(spawnData);
            }
            finally
            {
                isSynchronizing = false;
            }
        }

        private void AddUntrackedScenePieces()
        {
            Transform root =
                previewRoot != null
                    ? previewRoot
                    : transform;

            ChessPiece[] pieces =
                root.GetComponentsInChildren<
                    ChessPiece>(true);

            for (int i = 0;
                 i < pieces.Length;
                 i++)
            {
                ChessPiece piece = pieces[i];

                PieceSpawnPreviewInstance marker =
                    piece.GetComponent<
                        PieceSpawnPreviewInstance>();

                if (marker != null)
                    continue;

                ChessPiece prefab =
                    PrefabUtility
                        .GetCorrespondingObjectFromSource(
                            piece);

                if (prefab == null)
                    continue;

                Vector2Int position =
                    gridManager.WorldToGrid(
                        piece.transform.position);

                if (!gridManager.IsInsideGrid(position))
                    continue;

                PieceSpawnEntry entry =
                    spawnData.AddPiece(
                        prefab,
                        piece.Color,
                        position);

                marker =
                    Undo.AddComponent<
                        PieceSpawnPreviewInstance>(
                            piece.gameObject);

                marker.Bind(this, entry.EntryId);

                EditorUtility.SetDirty(marker);
            }
        }

        [ContextMenu("Clear Preview")]
        public void ClearPreview()
        {
            if (Application.isPlaying ||
                isSynchronizing)
            {
                return;
            }

            isSynchronizing = true;

            try
            {
                previewIsActive = false;
                ClearPreviewInternal();
            }
            finally
            {
                isSynchronizing = false;
            }

            SceneView.RepaintAll();
        }

        private void ClearPreviewInternal()
        {
            List<PieceSpawnPreviewInstance> markers =
                GetOwnedMarkers();

            for (int i = markers.Count - 1;
                 i >= 0;
                 i--)
            {
                PieceSpawnPreviewInstance marker =
                    markers[i];

                if (marker != null)
                {
                    Undo.DestroyObjectImmediate(
                        marker.gameObject);
                }
            }
        }

        private List<PieceSpawnPreviewInstance>
            GetOwnedMarkers()
        {
            PieceSpawnPreviewInstance[] allMarkers =
                FindObjectsByType<
                    PieceSpawnPreviewInstance>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);

            List<PieceSpawnPreviewInstance> result =
                new();

            for (int i = 0;
                 i < allMarkers.Length;
                 i++)
            {
                if (allMarkers[i] != null &&
                    allMarkers[i].Owner == this)
                {
                    result.Add(allMarkers[i]);
                }
            }

            return result;
        }

        private Transform GetTeamRoot(
            PieceColor color)
        {
            Transform teamRoot =
                color == PieceColor.White
                    ? whiteRoot
                    : blackRoot;

            if (teamRoot != null)
                return teamRoot;

            return previewRoot != null
                ? previewRoot
                : transform;
        }
#endif
    }
}