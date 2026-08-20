using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Placement
{
    public sealed class PlacementHighlightManager
        : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GridManager gridManager;

        [Header("Highlight Prefabs")]
        [Tooltip("배치 가능한 칸에 표시할 초록색 하이라이트")]
        [SerializeField]
        private GameObject validHighlightPrefab;

        [Tooltip("배치 영역이지만 현재 배치할 수 없는 칸")]
        [SerializeField]
        private GameObject invalidHighlightPrefab;

        [Header("Visual")]
        [SerializeField]
        private float yOffset = 0.05f;

        private readonly List<GameObject>
            spawnedHighlights = new();

        private readonly HashSet<Vector2Int>
            validPositions = new();

        private readonly HashSet<Vector2Int>
            invalidPositions = new();

        public IReadOnlyCollection<Vector2Int>
            ValidPositions =>
                validPositions;

        public IReadOnlyCollection<Vector2Int>
            InvalidPositions =>
                invalidPositions;

        private void Awake()
        {
            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<
                        GridManager>();
            }
        }

        public void ShowHighlights(
            IReadOnlyList<Vector2Int>
                valid,
            IReadOnlyList<Vector2Int>
                invalid)
        {
            ClearHighlights();

            if (gridManager == null)
                return;

            if (valid != null)
            {
                for (int i = 0;
                     i < valid.Count;
                     i++)
                {
                    Vector2Int position =
                        valid[i];

                    if (!gridManager.IsInsideGrid(
                            position))
                    {
                        continue;
                    }

                    validPositions.Add(
                        position
                    );

                    CreateHighlight(
                        position,
                        validHighlightPrefab
                    );
                }
            }

            if (invalid != null)
            {
                for (int i = 0;
                     i < invalid.Count;
                     i++)
                {
                    Vector2Int position =
                        invalid[i];

                    if (!gridManager.IsInsideGrid(
                            position))
                    {
                        continue;
                    }

                    /*
                     * 혹시 양쪽 목록에 동시에 들어왔다면
                     * Valid를 우선한다.
                     */
                    if (validPositions.Contains(
                            position))
                    {
                        continue;
                    }

                    invalidPositions.Add(
                        position
                    );

                    CreateHighlight(
                        position,
                        invalidHighlightPrefab
                    );
                }
            }
        }

        private void CreateHighlight(
            Vector2Int position,
            GameObject prefab)
        {
            if (prefab == null)
                return;

            Vector3 worldPosition =
                gridManager.GridToWorld(
                    position
                );

            worldPosition.y +=
                yOffset;

            GameObject highlight =
                Instantiate(
                    prefab,
                    worldPosition,
                    Quaternion.identity,
                    transform
                );

            spawnedHighlights.Add(
                highlight
            );
        }

        public bool IsValidPosition(
            Vector2Int position)
        {
            return validPositions.Contains(
                position
            );
        }

        public bool IsInvalidPosition(
            Vector2Int position)
        {
            return invalidPositions.Contains(
                position
            );
        }

        public bool IsHighlightedPosition(
            Vector2Int position)
        {
            return
                validPositions.Contains(position) ||
                invalidPositions.Contains(position);
        }

        public void ClearHighlights()
        {
            validPositions.Clear();
            invalidPositions.Clear();

            for (int i = 0;
                 i < spawnedHighlights.Count;
                 i++)
            {
                if (spawnedHighlights[i] != null)
                {
                    Destroy(
                        spawnedHighlights[i]
                    );
                }
            }

            spawnedHighlights.Clear();
        }

        private void OnDisable()
        {
            ClearHighlights();
        }
    }
}