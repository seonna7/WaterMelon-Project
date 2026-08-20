using Game.GamePlay.Grid;
using UnityEngine;

namespace Game.GamePlay.Skill
{
    public sealed class SkillResolver
    {
        public SkillResult Resolve(
            ChessPiece caster,
            SkillSlot skillSlot,
            GridManager gridManager,
            ChessPiece targetPiece = null,
            Vector2Int targetPosition = default)
        {
            if (caster == null)
            {
                return SkillResult.CreateFail(
                    null,
                    "스킬 시전자가 없습니다."
                );
            }

            if (gridManager == null)
            {
                return SkillResult.CreateFail(
                    caster,
                    "GridManager가 없습니다."
                );
            }

            SkillContext skillContext = new SkillContext(
                caster,
                gridManager,
                targetPiece,
                targetPosition
            );

            SkillResult result =
                caster.UseSkill(skillSlot, skillContext);

            if (!result.Success)
                return result;

            ChessPiece affectedTarget = result.Target;

            if (affectedTarget != null &&
                affectedTarget.IsDead &&
                affectedTarget.IsPlaced)
            {
                gridManager.RemovePiece(affectedTarget);
            }

            return result;
        }
    }
}