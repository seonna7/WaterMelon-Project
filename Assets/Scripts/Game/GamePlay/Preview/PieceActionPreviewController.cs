using Game.Action;
using Game.GamePlay.Attack;
using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.GamePlay.Preview
{
    public sealed class PieceActionPreviewController
        : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField]
        private Color allyColor =
            new Color(0.1f, 0.45f, 1f, 0.35f);

        [SerializeField]
        private Color enemyColor =
            new Color(1f, 0.1f, 0.1f, 0.35f);

        [SerializeField]
        private Transform previewRoot;

        private readonly List<GameObject> ghosts =
            new List<GameObject>();

        private readonly PushResolver pushResolver =
            new PushResolver();

        private readonly DirectAttackRuleResolver ruleResolver =
            new DirectAttackRuleResolver();

        private Material allyMaterial;
        private Material enemyMaterial;

        private void Awake()
        {
            CreateMaterials();
        }

        public void ShowMove(
            ChessPiece piece,
            Vector2Int destination,
            GridManager grid)
        {
            Clear();

            if (piece == null ||
                grid == null ||
                !grid.IsInsideGrid(destination) ||
                !grid.IsWalkable(destination) ||
                !grid.IsEmpty(destination))
            {
                return;
            }

            CreateGhost(
                piece,
                destination,
                grid,
                allyMaterial,
                "MovePreview"
            );
        }

        public void ShowDirectAttack(
            ChessPiece attacker,
            ChessPiece target,
            GridManager grid)
        {
            Clear();

            if (attacker == null ||
                target == null ||
                grid == null ||
                attacker.IsDead ||
                target.IsDead ||
                !attacker.IsPlaced ||
                !target.IsPlaced)
            {
                return;
            }

            DirectAttackRule rule =
                ruleResolver.GetRule(attacker);

            Vector2Int attackerStart = attacker.GridPosition;
            Vector2Int targetStart = target.GridPosition;
            Vector2Int direction =
                DirectAttackPositionResolver.NormalizeDirection(
                    targetStart - attackerStart
                );

            PushResult primaryPush = rule.PushDistance > 0
                ? pushResolver.Predict(
                    target,
                    grid,
                    rule.PushDistance,
                    direction)
                : PushResult.CreateFail(
                    target,
                    targetStart,
                    0);

            ShowPushPath(
                target,
                primaryPush,
                direction,
                grid
            );

            bool targetStartVacated =
                primaryPush.Success &&
                (primaryPush.MovedDistance > 0 ||
                 primaryPush.PushedOut);

            Vector2Int targetEnd =
                primaryPush.Success &&
                primaryPush.MovedDistance > 0 &&
                !primaryPush.PushedOut
                    ? primaryPush.EndPosition
                    : targetStart;

            Vector2Int attackerEnd =
                DirectAttackPositionResolver.ResolveAttackerPosition(
                    rule,
                    attackerStart,
                    targetStart,
                    targetEnd,
                    targetStartVacated,
                    grid
                );

            if (attackerEnd != attackerStart)
            {
                CreateGhost(
                    attacker,
                    attackerEnd,
                    grid,
                    allyMaterial,
                    "AttackResultPreview"
                );
            }

            if (rule.AreaPushDistance <= 0)
                return;

            Vector2Int center = rule.AreaCenteredOnAttacker
                ? attackerEnd
                : targetStart;

            ShowAreaPushes(
                attacker,
                target,
                center,
                rule.AreaPushDistance,
                grid
            );
        }

        private void ShowAreaPushes(
            ChessPiece attacker,
            ChessPiece primaryTarget,
            Vector2Int center,
            int distance,
            GridManager grid)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector2Int position =
                        center + new Vector2Int(x, y);

                    if (!grid.IsInsideGrid(position))
                        continue;

                    ChessPiece target =
                        grid.GetPieceAt(position);

                    if (target == null ||
                        target == attacker ||
                        target == primaryTarget ||
                        target.IsDead ||
                        target.Color == attacker.Color)
                    {
                        continue;
                    }

                    Vector2Int direction =
                        DirectAttackPositionResolver.NormalizeDirection(
                            target.GridPosition - center
                        );

                    PushResult push = pushResolver.Predict(
                        target,
                        grid,
                        distance,
                        direction
                    );

                    ShowPushPath(
                        target,
                        push,
                        direction,
                        grid
                    );
                }
            }
        }

        private void ShowPushPath(
            ChessPiece target,
            PushResult push,
            Vector2Int direction,
            GridManager grid)
        {
            if (!push.Success || push.MovedDistance <= 0)
                return;

            for (int step = 1;
                 step <= push.MovedDistance;
                 step++)
            {
                Vector2Int position =
                    push.StartPosition + direction * step;

                if (!grid.IsInsideGrid(position))
                    break;

                CreateGhost(
                    target,
                    position,
                    grid,
                    enemyMaterial,
                    "PushPreview"
                );
            }
        }

        public void BeginSkillPreview()
        {
            Clear();
        }

        public void ShowSkillMove(
            ChessPiece piece,
            Vector2Int destination,
            GridManager grid)
        {
            if (piece == null ||
                grid == null ||
                !grid.IsInsideGrid(destination) ||
                !grid.IsWalkable(destination))
            {
                return;
            }

            CreateGhost(
                piece,
                destination,
                grid,
                allyMaterial,
                "SkillMovePreview"
            );
        }

        public void ShowSkillPush(
            ChessPiece target,
            int distance,
            Vector2Int direction,
            GridManager grid)
        {
            if (target == null || grid == null)
                return;

            direction =
                DirectAttackPositionResolver.NormalizeDirection(
                    direction
                );

            PushResult push = pushResolver.Predict(
                target,
                grid,
                distance,
                direction
            );

            ShowPushPath(
                target,
                push,
                direction,
                grid
            );
        }

        private void CreateGhost(
            ChessPiece source,
            Vector2Int position,
            GridManager grid,
            Material material,
            string prefix)
        {
            if (source == null || material == null)
                return;

            Transform parent = previewRoot != null
                ? previewRoot
                : transform;

            GameObject ghost = Instantiate(
                source.gameObject,
                grid.GridToWorld(position),
                source.transform.rotation,
                parent
            );

            ghost.name =
                $"{prefix}_{source.name}_{position.x}_{position.y}";

            DisableComponents(ghost);
            ApplyMaterial(ghost, material);
            SetLayer(ghost, 2);

            ghosts.Add(ghost);
        }

        private static void DisableComponents(
            GameObject ghost)
        {
            Behaviour[] behaviours =
                ghost.GetComponentsInChildren<Behaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
                behaviours[i].enabled = false;

            Collider[] colliders =
                ghost.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
                colliders[i].enabled = false;

            Rigidbody[] rigidbodies =
                ghost.GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].isKinematic = true;
                rigidbodies[i].detectCollisions = false;
            }
        }

        private static void ApplyMaterial(
            GameObject ghost,
            Material material)
        {
            Renderer[] renderers =
                ghost.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                int count = renderers[i].sharedMaterials.Length;
                Material[] materials = new Material[count];

                for (int j = 0; j < count; j++)
                    materials[j] = material;

                renderers[i].sharedMaterials = materials;
            }
        }

        private static void SetLayer(
            GameObject target,
            int layer)
        {
            target.layer = layer;

            foreach (Transform child in target.transform)
                SetLayer(child.gameObject, layer);
        }

        private void CreateMaterials()
        {
            if (allyMaterial == null)
                allyMaterial = CreatePreviewMaterial(allyColor);

            if (enemyMaterial == null)
                enemyMaterial = CreatePreviewMaterial(enemyColor);
        }

        private static Material CreatePreviewMaterial(
            Color color)
        {
            Shader shader =
                Shader.Find("Universal Render Pipeline/Unlit") ??
                Shader.Find("Unlit/Color") ??
                Shader.Find("Standard");

            if (shader == null)
                return null;

            Material material = new Material(shader);

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);

            if (material.HasProperty("_Mode"))
                material.SetFloat("_Mode", 3f);

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat(
                    "_SrcBlend",
                    (float)BlendMode.SrcAlpha
                );
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat(
                    "_DstBlend",
                    (float)BlendMode.OneMinusSrcAlpha
                );
            }

            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetShaderPassEnabled("ShadowCaster", false);

            return material;
        }

        public void Clear()
        {
            for (int i = 0; i < ghosts.Count; i++)
            {
                if (ghosts[i] != null)
                    Destroy(ghosts[i]);
            }

            ghosts.Clear();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            if (allyMaterial != null)
                Destroy(allyMaterial);

            if (enemyMaterial != null)
                Destroy(enemyMaterial);
        }
    }
}