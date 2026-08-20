using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.GamePlay.Grid
{
    [ExecuteAlways]
    public sealed class GridRenderer
        : MonoBehaviour
    {
        private const string GridContainerName =
            "Generated Grid Lines";

        private const string HighlightContainerName =
            "Generated Grid Highlights";

        [Header("Reference")]
        [SerializeField]
        private GridManager gridManager;

        [Header("Display")]
        [SerializeField]
        private bool drawGrid = true;

        [SerializeField]
        private Color gridColor =
            Color.white;

        [SerializeField]
        [Min(0.001f)]
        private float lineWidth =
            0.03f;

        [SerializeField]
        private float heightOffset =
            0.7f;

        [Header("Material")]
        [SerializeField]
        private Material lineMaterial;

        [Header("Highlight Material")]
        [SerializeField]
        private Material highlightBorderMaterial;

        [SerializeField]
        private Material highlightFillMaterial;

        [Header("Highlight Display")]
        [SerializeField]
        [Min(0.001f)]
        private float highlightBorderWidth =
            0.06f;

        [SerializeField]
        private float highlightHeightOffset =
            0.72f;

        [SerializeField]
        private float highlightFillHeightOffset =
            0.715f;

        [SerializeField]
        [Min(0f)]
        private float highlightFillInset =
            0.05f;

        [Header("Highlight Color")]
        [SerializeField]
        private Color validSkillColor =
            new Color32(
                40,
                255,
                120,
                255
            );

        [SerializeField]
        private Color invalidSkillColor =
            new Color32(
                255,
                70,
                70,
                255
            );

        [SerializeField]
        private Color moveColor =
            new Color32(
                70,
                160,
                255,
                255
            );

        [SerializeField]
        private Color attackColor =
            new Color32(
                255,
                140,
                40,
                255
            );

        [SerializeField]
        private Color selectedColor =
            new Color32(
                255,
                230,
                60,
                255
            );

        [Header("Highlight Glass Effect")]
        [SerializeField]
        [Range(0f, 1f)]
        private float minimumFillAlpha =
            0.18f;

        [SerializeField]
        [Range(0f, 1f)]
        private float maximumFillAlpha =
            0.38f;

        [SerializeField]
        [Min(0f)]
        private float pulseSpeed =
            2.5f;

        [SerializeField]
        [Min(0f)]
        private float emissionIntensity =
            1.5f;

        [Header("Highlight Appear Animation")]
        [SerializeField]
        [Min(0.01f)]
        private float highlightAppearDuration =
            0.2f;

        [SerializeField]
        [Min(0f)]
        private float highlightRiseDistance =
            0.15f;

        [SerializeField]
        [Range(0.5f, 1f)]
        private float highlightStartScale =
            0.92f;

        private readonly List<LineRenderer>
            lineRenderers =
                new();

        private readonly List<GridHighlightData>
            highlights =
                new();

        private readonly Dictionary<
            GridHighlightType,
            HighlightLayer>
            highlightLayers =
                new();

        private Transform lineContainer;

        private Transform highlightContainer;

        private int previousWidth;
        private int previousHeight;

        private float previousCellSize;

        private Vector3 previousOrigin;

        private Vector3 previousGridPosition;

        private Quaternion previousGridRotation;

        private Vector3 previousGridScale;

        private Color previousColor;

        private float previousLineWidth;

        private float previousHeightOffset;

        private bool previousDrawGrid;

        private float highlightAppearStartTime;

        private bool isHighlightAppearing;

        private sealed class HighlightLayer
        {
            public Transform RootTransform;

            public Mesh BorderMesh;

            public Mesh FillMesh;

            public MeshRenderer BorderRenderer;

            public MeshRenderer FillRenderer;

            public readonly MaterialPropertyBlock
                BorderPropertyBlock =
                    new();

            public readonly MaterialPropertyBlock
                FillPropertyBlock =
                    new();
        }

        private void OnEnable()
        {
            ResolveReferences();

            RebuildGrid();

            RebuildHighlightMeshes();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<
                        GridManager>();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying &&
                UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif
            if (gridManager == null)
            {
                ResolveReferences();
            }

            if (gridManager == null)
            {
                SetGridVisible(
                    false
                );

                SetHighlightsVisible(
                    false
                );

                return;
            }

            if (HasGridChanged())
            {
                RebuildGrid();

                RebuildHighlightMeshes();
            }

            SetGridVisible(
                drawGrid
            );

            UpdateHighlightAnimation();
        }

        public void SetHighlights(
            IReadOnlyList<GridHighlightData>
                newHighlights)
        {
            highlights.Clear();

            if (newHighlights != null)
            {
                for (int i = 0;
                     i < newHighlights.Count;
                     i++)
                {
                    GridHighlightData highlight =
                        newHighlights[i];

                    if (gridManager != null &&
                        !gridManager.IsInsideGrid(
                            highlight.GridPosition))
                    {
                        continue;
                    }

                    highlights.Add(
                        highlight
                    );
                }
            }

            RebuildHighlightMeshes();

            highlightAppearStartTime =
                Time.realtimeSinceStartup;

            isHighlightAppearing =
                highlights.Count > 0;
        }

        public void ClearHighlights()
        {
            highlights.Clear();

            isHighlightAppearing =
                false;

            ClearHighlightObjects();
        }

        private bool HasGridChanged()
        {
            if (gridManager == null)
                return false;

            Transform gridTransform =
                gridManager.transform;

            return
                previousWidth !=
                    gridManager.GridWidth ||

                previousHeight !=
                    gridManager.GridHeight ||

                !Mathf.Approximately(
                    previousCellSize,
                    gridManager.CellSize
                ) ||

                previousOrigin !=
                    gridManager.WorldOrigin ||

                previousGridPosition !=
                    gridTransform.position ||

                previousGridRotation !=
                    gridTransform.rotation ||

                previousGridScale !=
                    gridTransform.lossyScale ||

                previousColor !=
                    gridColor ||

                !Mathf.Approximately(
                    previousLineWidth,
                    lineWidth
                ) ||

                !Mathf.Approximately(
                    previousHeightOffset,
                    heightOffset
                ) ||

                previousDrawGrid !=
                    drawGrid;
        }

        [ContextMenu("Rebuild Grid")]
        public void RebuildGrid()
        {
            ClearGrid();

            if (gridManager == null)
                return;

            if (drawGrid)
            {
                CreateLineContainer();

                float cellSize =
                    gridManager.CellSize;

                int width =
                    gridManager.GridWidth;

                int height =
                    gridManager.GridHeight;

                /*
                 * 세로선
                 */
                for (int x = 0;
                     x <= width;
                     x++)
                {
                    Vector3 start =
                        gridManager
                            .GridLocalToWorld(
                                new Vector3(
                                    x * cellSize,
                                    heightOffset,
                                    0f
                                )
                            );

                    Vector3 end =
                        gridManager
                            .GridLocalToWorld(
                                new Vector3(
                                    x * cellSize,
                                    heightOffset,
                                    height *
                                    cellSize
                                )
                            );

                    CreateLine(
                        $"Vertical_{x}",
                        start,
                        end
                    );
                }

                /*
                 * 가로선
                 */
                for (int y = 0;
                     y <= height;
                     y++)
                {
                    Vector3 start =
                        gridManager
                            .GridLocalToWorld(
                                new Vector3(
                                    0f,
                                    heightOffset,
                                    y * cellSize
                                )
                            );

                    Vector3 end =
                        gridManager
                            .GridLocalToWorld(
                                new Vector3(
                                    width *
                                    cellSize,
                                    heightOffset,
                                    y * cellSize
                                )
                            );

                    CreateLine(
                        $"Horizontal_{y}",
                        start,
                        end
                    );
                }
            }

            CacheCurrentValues();
        }

        private void CreateLineContainer()
        {
            GameObject containerObject =
                new GameObject(
                    GridContainerName
                );

            lineContainer =
                containerObject.transform;

            lineContainer.SetParent(
                transform,
                false
            );
        }

        private void CreateLine(
            string lineName,
            Vector3 start,
            Vector3 end)
        {
            GameObject lineObject =
                new GameObject(
                    lineName
                );

            lineObject.transform.SetParent(
                lineContainer,
                false
            );

            LineRenderer lineRenderer =
                lineObject.AddComponent<
                    LineRenderer>();

            /*
             * GridManager가 이미 World 좌표를
             * 계산해주므로 WorldSpace 사용.
             */
            lineRenderer.useWorldSpace =
                true;

            lineRenderer.positionCount =
                2;

            lineRenderer.SetPosition(
                0,
                start
            );

            lineRenderer.SetPosition(
                1,
                end
            );

            lineRenderer.startWidth =
                lineWidth;

            lineRenderer.endWidth =
                lineWidth;

            lineRenderer.startColor =
                gridColor;

            lineRenderer.endColor =
                gridColor;

            lineRenderer.numCapVertices =
                0;

            lineRenderer.numCornerVertices =
                0;

            lineRenderer.textureMode =
                LineTextureMode.Stretch;

            lineRenderer.alignment =
                LineAlignment.View;

            lineRenderer.shadowCastingMode =
                ShadowCastingMode.Off;

            lineRenderer.receiveShadows =
                false;

            lineRenderer.lightProbeUsage =
                LightProbeUsage.Off;

            lineRenderer.reflectionProbeUsage =
                ReflectionProbeUsage.Off;

            lineRenderer.sharedMaterial =
                lineMaterial != null
                    ? lineMaterial
                    : CreateDefaultGridMaterial();

            lineRenderers.Add(
                lineRenderer
            );
        }

        private Material
            CreateDefaultGridMaterial()
        {
            Shader shader =
                Shader.Find(
                    "Sprites/Default"
                );

            if (shader == null)
            {
                Debug.LogError(
                    "GridRenderer: " +
                    "Sprites/Default 셰이더를 " +
                    "찾지 못했습니다.",
                    this
                );

                return null;
            }

            return new Material(
                shader
            )
            {
                name =
                    "Runtime Grid Material",

                color =
                    gridColor,

                hideFlags =
                    HideFlags.HideAndDontSave
            };
        }

        private void RebuildHighlightMeshes()
        {
            ClearHighlightObjects();

            if (gridManager == null ||
                highlights.Count == 0)
            {
                return;
            }

            CreateHighlightContainer();

            foreach (
                GridHighlightType type
                in Enum.GetValues(
                    typeof(
                        GridHighlightType)))
            {
                List<GridHighlightData>
                    typeHighlights =
                        new();

                foreach (
                    GridHighlightData highlight
                    in highlights)
                {
                    if (highlight.Type ==
                        type)
                    {
                        typeHighlights.Add(
                            highlight
                        );
                    }
                }

                if (typeHighlights.Count ==
                    0)
                {
                    continue;
                }

                HighlightLayer layer =
                    CreateHighlightLayer(
                        type
                    );

                BuildHighlightLayerMeshes(
                    typeHighlights,
                    layer
                );

                highlightLayers[type] =
                    layer;
            }

            SetHighlightsVisible(
                true
            );

            UpdateHighlightAnimation();
        }

        private void
            CreateHighlightContainer()
        {
            GameObject containerObject =
                new GameObject(
                    HighlightContainerName
                );

            highlightContainer =
                containerObject.transform;

            highlightContainer.SetParent(
                transform,
                false
            );
        }

        private HighlightLayer
            CreateHighlightLayer(
                GridHighlightType type)
        {
            GameObject layerObject =
                new GameObject(
                    $"Highlight_{type}"
                );

            layerObject.transform.SetParent(
                highlightContainer,
                false
            );

            GameObject borderObject =
                new GameObject(
                    "Border"
                );

            borderObject.transform.SetParent(
                layerObject.transform,
                false
            );

            MeshFilter borderFilter =
                borderObject.AddComponent<
                    MeshFilter>();

            MeshRenderer borderRenderer =
                borderObject.AddComponent<
                    MeshRenderer>();

            GameObject fillObject =
                new GameObject(
                    "Glass"
                );

            fillObject.transform.SetParent(
                layerObject.transform,
                false
            );

            MeshFilter fillFilter =
                fillObject.AddComponent<
                    MeshFilter>();

            MeshRenderer fillRenderer =
                fillObject.AddComponent<
                    MeshRenderer>();

            Mesh borderMesh =
                new Mesh
                {
                    name =
                        $"Highlight Border {type}"
                };

            Mesh fillMesh =
                new Mesh
                {
                    name =
                        $"Highlight Fill {type}"
                };

            borderMesh.MarkDynamic();

            fillMesh.MarkDynamic();

            borderFilter.sharedMesh =
                borderMesh;

            fillFilter.sharedMesh =
                fillMesh;

            borderRenderer.sharedMaterial =
                highlightBorderMaterial != null
                    ? highlightBorderMaterial
                    : CreateDefaultHighlightMaterial(
                        $"Runtime Border {type}"
                    );

            fillRenderer.sharedMaterial =
                highlightFillMaterial != null
                    ? highlightFillMaterial
                    : CreateDefaultHighlightMaterial(
                        $"Runtime Fill {type}"
                    );

            ConfigureHighlightRenderer(
                borderRenderer
            );

            ConfigureHighlightRenderer(
                fillRenderer
            );

            return new HighlightLayer
            {
                RootTransform =
                    layerObject.transform,

                BorderMesh =
                    borderMesh,

                FillMesh =
                    fillMesh,

                BorderRenderer =
                    borderRenderer,

                FillRenderer =
                    fillRenderer
            };
        }

        private static void
            ConfigureHighlightRenderer(
                MeshRenderer meshRenderer)
        {
            meshRenderer.shadowCastingMode =
                ShadowCastingMode.Off;

            meshRenderer.receiveShadows =
                false;

            meshRenderer.lightProbeUsage =
                LightProbeUsage.Off;

            meshRenderer.reflectionProbeUsage =
                ReflectionProbeUsage.Off;
        }

        private void
            BuildHighlightLayerMeshes(
                IReadOnlyList<
                    GridHighlightData>
                    layerHighlights,
                HighlightLayer layer)
        {
            List<Vector3> borderVertices =
                new();

            List<int> borderTriangles =
                new();

            List<Vector3> fillVertices =
                new();

            List<int> fillTriangles =
                new();

            float cellSize =
                gridManager.CellSize;

            foreach (
                GridHighlightData highlight
                in layerHighlights)
            {
                Vector2Int position =
                    highlight.GridPosition;

                AddBorderQuads(
                    borderVertices,
                    borderTriangles,
                    position,
                    cellSize
                );

                AddFillQuad(
                    fillVertices,
                    fillTriangles,
                    position,
                    cellSize
                );
            }

            ApplyMeshData(
                layer.BorderMesh,
                borderVertices,
                borderTriangles
            );

            ApplyMeshData(
                layer.FillMesh,
                fillVertices,
                fillTriangles
            );
        }

        /*
         * Grid Local 좌표를
         * GridManager World 좌표로 변환한 다음,
         * GridRenderer Local 좌표로 변환한다.
         *
         * 이 과정 때문에 GridManager나
         * 부모 Transform을 움직여도
         * 하이라이트가 어긋나지 않는다.
         */
        private Vector3
            GridPointToRendererLocal(
                Vector3 gridLocalPosition)
        {
            Vector3 worldPosition =
                gridManager
                    .GridLocalToWorld(
                        gridLocalPosition
                    );

            return transform
                .InverseTransformPoint(
                    worldPosition
                );
        }

        private void AddBorderQuads(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2Int gridPosition,
            float cellSize)
        {
            float width =
                Mathf.Min(
                    highlightBorderWidth,
                    cellSize * 0.5f
                );

            float y =
                highlightHeightOffset;

            float x0 =
                gridPosition.x *
                cellSize;

            float x1 =
                x0 +
                cellSize;

            float z0 =
                gridPosition.y *
                cellSize;

            float z1 =
                z0 +
                cellSize;

            /*
             * 아래
             */
            AddGridQuad(
                vertices,
                triangles,

                new Vector3(
                    x0,
                    y,
                    z0
                ),

                new Vector3(
                    x1,
                    y,
                    z0
                ),

                new Vector3(
                    x1,
                    y,
                    z0 + width
                ),

                new Vector3(
                    x0,
                    y,
                    z0 + width
                )
            );

            /*
             * 위
             */
            AddGridQuad(
                vertices,
                triangles,

                new Vector3(
                    x0,
                    y,
                    z1 - width
                ),

                new Vector3(
                    x1,
                    y,
                    z1 - width
                ),

                new Vector3(
                    x1,
                    y,
                    z1
                ),

                new Vector3(
                    x0,
                    y,
                    z1
                )
            );

            /*
             * 왼쪽
             */
            AddGridQuad(
                vertices,
                triangles,

                new Vector3(
                    x0,
                    y,
                    z0 + width
                ),

                new Vector3(
                    x0 + width,
                    y,
                    z0 + width
                ),

                new Vector3(
                    x0 + width,
                    y,
                    z1 - width
                ),

                new Vector3(
                    x0,
                    y,
                    z1 - width
                )
            );

            /*
             * 오른쪽
             */
            AddGridQuad(
                vertices,
                triangles,

                new Vector3(
                    x1 - width,
                    y,
                    z0 + width
                ),

                new Vector3(
                    x1,
                    y,
                    z0 + width
                ),

                new Vector3(
                    x1,
                    y,
                    z1 - width
                ),

                new Vector3(
                    x1 - width,
                    y,
                    z1 - width
                )
            );
        }

        private void AddFillQuad(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2Int gridPosition,
            float cellSize)
        {
            float inset =
                Mathf.Clamp(
                    highlightFillInset,
                    0f,
                    cellSize * 0.45f
                );

            float x0 =
                gridPosition.x *
                cellSize +
                inset;

            float x1 =
                (gridPosition.x + 1) *
                cellSize -
                inset;

            float z0 =
                gridPosition.y *
                cellSize +
                inset;

            float z1 =
                (gridPosition.y + 1) *
                cellSize -
                inset;

            float y =
                highlightFillHeightOffset;

            AddGridQuad(
                vertices,
                triangles,

                new Vector3(
                    x0,
                    y,
                    z0
                ),

                new Vector3(
                    x1,
                    y,
                    z0
                ),

                new Vector3(
                    x1,
                    y,
                    z1
                ),

                new Vector3(
                    x0,
                    y,
                    z1
                )
            );
        }

        private void AddGridQuad(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            int startIndex =
                vertices.Count;

            vertices.Add(
                GridPointToRendererLocal(
                    a
                )
            );

            vertices.Add(
                GridPointToRendererLocal(
                    b
                )
            );

            vertices.Add(
                GridPointToRendererLocal(
                    c
                )
            );

            vertices.Add(
                GridPointToRendererLocal(
                    d
                )
            );

            triangles.Add(
                startIndex
            );

            triangles.Add(
                startIndex + 2
            );

            triangles.Add(
                startIndex + 1
            );

            triangles.Add(
                startIndex
            );

            triangles.Add(
                startIndex + 3
            );

            triangles.Add(
                startIndex + 2
            );
        }

        private static void ApplyMeshData(
            Mesh mesh,
            List<Vector3> vertices,
            List<int> triangles)
        {
            mesh.Clear();

            mesh.SetVertices(
                vertices
            );

            mesh.SetTriangles(
                triangles,
                0
            );

            mesh.RecalculateNormals();

            mesh.RecalculateBounds();
        }

        private void
            UpdateHighlightAnimation()
        {
            if (highlightLayers.Count ==
                0)
            {
                return;
            }

            float appearProgress =
                1f;

            if (isHighlightAppearing)
            {
                float elapsed =
                    Time.realtimeSinceStartup -
                    highlightAppearStartTime;

                appearProgress =
                    Mathf.Clamp01(
                        elapsed /
                        highlightAppearDuration
                    );

                appearProgress =
                    EaseOutCubic(
                        appearProgress
                    );

                if (appearProgress >=
                    1f)
                {
                    appearProgress =
                        1f;

                    isHighlightAppearing =
                        false;
                }
            }

            float pulse =
                Mathf.Sin(
                    Time.realtimeSinceStartup *
                    pulseSpeed
                ) * 0.5f + 0.5f;

            float fillAlpha =
                Mathf.Lerp(
                    minimumFillAlpha,
                    maximumFillAlpha,
                    pulse
                ) *
                appearProgress;

            foreach (
                KeyValuePair<
                    GridHighlightType,
                    HighlightLayer>
                    pair
                in highlightLayers)
            {
                HighlightLayer layer =
                    pair.Value;

                UpdateHighlightTransform(
                    layer,
                    appearProgress
                );

                Color baseColor =
                    GetHighlightColor(
                        pair.Key
                    );

                Color borderColor =
                    baseColor;

                borderColor.a =
                    appearProgress;

                Color fillColor =
                    baseColor;

                fillColor.a =
                    fillAlpha;

                ApplyRendererColor(
                    layer.BorderRenderer,
                    layer.BorderPropertyBlock,
                    borderColor,
                    false
                );

                ApplyRendererColor(
                    layer.FillRenderer,
                    layer.FillPropertyBlock,
                    fillColor,
                    true
                );
            }
        }

        private void
            UpdateHighlightTransform(
                HighlightLayer layer,
                float progress)
        {
            if (layer.RootTransform ==
                null)
            {
                return;
            }

            float rise =
                Mathf.Lerp(
                    -highlightRiseDistance,
                    0f,
                    progress
                );

            /*
             * Grid의 Up 방향을
             * GridRenderer Local 방향으로 변환.
             */
            Vector3 worldOffset =
                gridManager.transform.up *
                rise;

            Vector3 localOffset =
                transform
                    .InverseTransformVector(
                        worldOffset
                    );

            layer.RootTransform
                .localPosition =
                    localOffset;

            float scale =
                Mathf.Lerp(
                    highlightStartScale,
                    1f,
                    progress
                );

            /*
             * 기존 애니메이션 의도 유지.
             */
            layer.RootTransform
                .localScale =
                    new Vector3(
                        scale,
                        scale,
                        scale
                    );
        }

        private static float EaseOutCubic(
            float value)
        {
            float inverse =
                1f - value;

            return
                1f -
                inverse *
                inverse *
                inverse;
        }

        private void ApplyRendererColor(
            Renderer targetRenderer,
            MaterialPropertyBlock propertyBlock,
            Color color,
            bool applyEmission)
        {
            if (targetRenderer == null)
                return;

            targetRenderer
                .GetPropertyBlock(
                    propertyBlock
                );

            propertyBlock.SetColor(
                "_Color",
                color
            );

            propertyBlock.SetColor(
                "_BaseColor",
                color
            );

            if (applyEmission)
            {
                Color emissionColor =
                    new Color(
                        color.r,
                        color.g,
                        color.b,
                        1f
                    ) *
                    emissionIntensity;

                propertyBlock.SetColor(
                    "_EmissionColor",
                    emissionColor
                );
            }

            targetRenderer
                .SetPropertyBlock(
                    propertyBlock
                );
        }

        private Color GetHighlightColor(
            GridHighlightType type)
        {
            return type switch
            {
                GridHighlightType.Move =>
                    moveColor,

                GridHighlightType.ValidSkill =>
                    validSkillColor,

                GridHighlightType.InvalidSkill =>
                    invalidSkillColor,

                GridHighlightType.Attack =>
                    attackColor,

                GridHighlightType.Selected =>
                    selectedColor,

                _ =>
                    Color.white
            };
        }

        private Material
            CreateDefaultHighlightMaterial(
                string materialName)
        {
            Shader shader =
                Shader.Find(
                    "Sprites/Default"
                );

            if (shader == null)
            {
                Debug.LogError(
                    "GridRenderer: " +
                    "하이라이트용 Sprites/Default " +
                    "셰이더를 찾지 못했습니다.",
                    this
                );

                return null;
            }

            return new Material(
                shader
            )
            {
                name =
                    materialName,

                color =
                    Color.white,

                hideFlags =
                    HideFlags.HideAndDontSave
            };
        }

        private void SetGridVisible(
            bool visible)
        {
            foreach (
                LineRenderer lineRenderer
                in lineRenderers)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.enabled =
                        visible;
                }
            }
        }

        private void SetHighlightsVisible(
            bool visible)
        {
            foreach (
                HighlightLayer layer
                in highlightLayers.Values)
            {
                if (layer.BorderRenderer !=
                    null)
                {
                    layer.BorderRenderer
                        .enabled =
                            visible;
                }

                if (layer.FillRenderer !=
                    null)
                {
                    layer.FillRenderer
                        .enabled =
                            visible;
                }
            }
        }

        [ContextMenu("Clear Grid")]
        public void ClearGrid()
        {
            lineRenderers.Clear();

            Transform existingContainer =
                lineContainer != null
                    ? lineContainer
                    : transform.Find(
                        GridContainerName
                    );

            lineContainer =
                null;

            DestroyGeneratedObject(
                existingContainer
            );
        }

        private void ClearHighlightObjects()
        {
            foreach (
                HighlightLayer layer
                in highlightLayers.Values)
            {
                DestroyMesh(
                    layer.BorderMesh
                );

                DestroyMesh(
                    layer.FillMesh
                );
            }

            highlightLayers.Clear();

            Transform existingContainer =
                highlightContainer != null
                    ? highlightContainer
                    : transform.Find(
                        HighlightContainerName
                    );

            highlightContainer =
                null;

            DestroyGeneratedObject(
                existingContainer
            );
        }

        private static void DestroyMesh(
            Mesh mesh)
        {
            if (mesh == null)
                return;

            if (Application.isPlaying)
            {
                Destroy(
                    mesh
                );
            }
            else
            {
                DestroyImmediate(
                    mesh
                );
            }
        }

        private static void
            DestroyGeneratedObject(
                Transform target)
        {
            if (target == null)
                return;

            target.gameObject.SetActive(
                false
            );

            if (Application.isPlaying)
            {
                Destroy(
                    target.gameObject
                );
            }
            else
            {
                DestroyImmediate(
                    target.gameObject
                );
            }
        }

        private void CacheCurrentValues()
        {
            if (gridManager == null)
                return;

            previousWidth =
                gridManager.GridWidth;

            previousHeight =
                gridManager.GridHeight;

            previousCellSize =
                gridManager.CellSize;

            previousOrigin =
                gridManager.WorldOrigin;

            previousGridPosition =
                gridManager
                    .transform
                    .position;

            previousGridRotation =
                gridManager
                    .transform
                    .rotation;

            previousGridScale =
                gridManager
                    .transform
                    .lossyScale;

            previousColor =
                gridColor;

            previousLineWidth =
                lineWidth;

            previousHeightOffset =
                heightOffset;

            previousDrawGrid =
                drawGrid;
        }

        [ContextMenu("Test Skill Highlights")]
        private void TestSkillHighlights()
        {
            List<GridHighlightData>
                testData =
                    new()
                    {
                        new GridHighlightData(
                            new Vector2Int(
                                1,
                                1
                            ),
                            GridHighlightType
                                .ValidSkill
                        ),

                        new GridHighlightData(
                            new Vector2Int(
                                2,
                                1
                            ),
                            GridHighlightType
                                .InvalidSkill
                        ),

                        new GridHighlightData(
                            new Vector2Int(
                                1,
                                2
                            ),
                            GridHighlightType
                                .ValidSkill
                        ),

                        new GridHighlightData(
                            new Vector2Int(
                                2,
                                2
                            ),
                            GridHighlightType
                                .InvalidSkill
                        )
                    };

            SetHighlights(
                testData
            );
        }

        private void OnDisable()
        {
            SetGridVisible(
                false
            );

            SetHighlightsVisible(
                false
            );
        }

        private void OnDestroy()
        {
            ClearHighlights();

            ClearGrid();
        }
    }
}