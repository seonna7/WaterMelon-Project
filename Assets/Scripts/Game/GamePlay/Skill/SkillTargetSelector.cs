using Game.Action;
using Game.CameraSystem;
using Game.GamePlay.Grid;
using Game.GamePlay.Skill;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Selection
{
    public class SkillTargetSelector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private GridRenderer gridRenderer;

        [SerializeField]
        private PieceActionController actionController;

        [SerializeField]
        private CameraCommandManager cameraCommandManager;

        [Header("Raycast")]
        [SerializeField]
        private LayerMask targetLayerMask;

        [SerializeField]
        private float rayDistance = 100f;

        private readonly HashSet<Vector2Int>
            targetablePositions = new();

        private readonly HashSet<Vector2Int>
            applicablePositions = new();

        private readonly List<GridHighlightData>
            highlightData = new();

        private ChessPiece currentCaster;

        private SkillSlot currentSkillSlot;

        public bool IsSkillMode =>
            currentCaster != null;

        private void Awake()
        {
            if (cameraCommandManager == null)
            {
                cameraCommandManager =
                    FindFirstObjectByType<
                        CameraCommandManager>();
            }
        }

        private void Update()
        {
            if (!IsSkillMode)
                return;

            if (Input.GetMouseButtonDown(1))
            {
                ExitSkillMode();
                return;
            }

            if (!Input.GetMouseButtonDown(0))
                return;

            //if (EventSystem.current != null &&
            //    EventSystem.current.IsPointerOverGameObject())
            //{
            //    return;
            //}

            TrySelectTarget();
        }

        public void EnterSkillMode(
            ChessPiece caster,
            SkillSlot skillSlot)
        {
            ExitSkillMode();

            if (caster == null ||
                caster.IsDead ||
                !caster.IsPlaced ||
                gridManager == null ||
                gridRenderer == null)
            {
                return;
            }

            SkillStrategy skill =
                caster.GetSkill(skillSlot);

            if (skill == null)
            {
                Debug.LogWarning(
                    "[SkillTargetSelector] " +
                    "선택한 슬롯에 스킬이 없습니다."
                );

                return;
            }

            currentCaster = caster;
            currentSkillSlot = skillSlot;

            SkillContext context =
                new SkillContext(
                    currentCaster,
                    gridManager
                );

            List<Vector2Int> positions =
                currentCaster.GetSkillTargetablePositions(
                    currentSkillSlot,
                    context
                );

            BuildHighlights(
                skill,
                context,
                positions
            );

            Debug.Log(
                $"[SkillTargetSelector] " +
                $"SkillMode 진입 | " +
                $"Caster={currentCaster.name} | " +
                $"Slot={currentSkillSlot}"
            );
        }

        private void BuildHighlights(
            SkillStrategy skill,
            SkillContext context,
            IReadOnlyList<Vector2Int> positions)
        {
            targetablePositions.Clear();
            applicablePositions.Clear();
            highlightData.Clear();

            if (positions == null)
            {
                gridRenderer.ClearHighlights();
                return;
            }

            for (int i = 0;
                 i < positions.Count;
                 i++)
            {
                Vector2Int position =
                    positions[i];

                if (!gridManager.IsInsideGrid(position))
                    continue;

                if (!targetablePositions.Add(position))
                    continue;

                bool canApply =
                    skill.CanApply(
                        context,
                        position
                    );

                if (canApply)
                {
                    applicablePositions.Add(position);
                }

                highlightData.Add(
                    new GridHighlightData(
                        position,
                        canApply
                            ? GridHighlightType.ValidSkill
                            : GridHighlightType.InvalidSkill
                    )
                );
            }

            gridRenderer.SetHighlights(
                highlightData
            );
        }

        private void TrySelectTarget()
        {
            if (targetCamera == null ||
                gridManager == null)
            {
                ExitSkillMode();
                return;
            }

            Ray ray =
                targetCamera.ScreenPointToRay(
                    Input.mousePosition
                );

            bool hitTarget =
                Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    rayDistance,
                    targetLayerMask,
                    QueryTriggerInteraction.Collide
                );

            if (!hitTarget)
            {
                ExitSkillMode();
                return;
            }

            Vector2Int gridPosition =
                WorldToGrid(hit.point);

            if (!gridManager.IsInsideGrid(
                    gridPosition) ||
                !targetablePositions.Contains(
                    gridPosition))
            {
                ExitSkillMode();
                return;
            }

            if (!applicablePositions.Contains(
                    gridPosition))
            {
                Debug.Log(
                    "적용할 수 없습니다."
                );

                return;
            }

            ExecuteSkill(gridPosition);
        }

        private Vector2Int WorldToGrid(
            Vector3 worldPosition)
        {
            Vector3 localPosition =
                worldPosition -
                gridManager.WorldOrigin;

            int x = Mathf.FloorToInt(
                localPosition.x /
                gridManager.CellSize
            );

            int y = Mathf.FloorToInt(
                localPosition.z /
                gridManager.CellSize
            );

            return new Vector2Int(x, y);
        }

        private void ExecuteSkill(
            Vector2Int targetPosition)
        {
            if (currentCaster == null ||
                actionController == null ||
                gridManager == null)
            {
                ExitSkillMode();
                return;
            }

            ChessPiece targetPiece =
                gridManager.GetPieceAt(
                    targetPosition
                );

            ActionResult result =
                actionController.TryUseSkill(
                    currentCaster,
                    currentSkillSlot,
                    targetPiece,
                    targetPosition
                );

            if (!result.Success)
            {
                Debug.LogWarning(
                    "[SkillTargetSelector] " +
                    "스킬 사용에 실패했습니다."
                );

                return;
            }

            Debug.Log("스킬 적용");

            ExitSkillMode();
        }

        public void ExitSkillMode()
        {
            gridRenderer?.ClearHighlights();

            targetablePositions.Clear();
            applicablePositions.Clear();
            highlightData.Clear();

            currentCaster = null;

            cameraCommandManager?.
                ReturnToGrid();
        }
    }
}

