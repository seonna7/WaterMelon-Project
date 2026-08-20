using Game.GamePlay.Skill;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    public abstract class SkillStrategy
    {
        public string SkillName { get; }

        public SkillType SkillType { get; }

        public int ActionPointCost { get; }

        protected SkillStrategy(
            string skillName,
            SkillType skillType,
            int actionPointCost)
        {
            SkillName = skillName;
            SkillType = skillType;
            ActionPointCost =
                Mathf.Max(0, actionPointCost);
        }

        public virtual bool CanUse(
            SkillContext context)
        {
            return context.Caster != null &&
                   !context.Caster.IsDead &&
                   context.Caster.IsPlaced &&
                   context.GridManager != null;
        }

        public abstract List<Vector2Int> GetTargetablePositions(
            SkillContext context);

        public abstract bool CanApply(
            SkillContext context,
            Vector2Int targetPosition);

        public abstract SkillResult Execute(
            SkillContext context);

    }
}