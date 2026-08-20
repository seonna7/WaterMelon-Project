using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Skill
{
    public sealed class AdjacentAttackSkill
        : SkillStrategy
    {
        private readonly int damage;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        public AdjacentAttackSkill(
            int damage = 5,
            int actionPointCost = 1)
            : base(
                "Adjacent Attack",
                SkillType.Attack,
                actionPointCost)
        {
            this.damage =
                Mathf.Max(0, damage);
        }

        public override bool CanUse(
            SkillContext context)
        {
            if (!base.CanUse(context))
                return false;

            return CanApply(
                context,
                context.TargetPosition
            );
        }

        public override List<Vector2Int>
            GetTargetablePositions(
                SkillContext context)
        {
            List<Vector2Int> positions =
                new();

            /*
             * 아직 대상을 선택하기 전이므로
             * 시전자와 GridManager만 검사한다.
             */
            if (!base.CanUse(context))
                return positions;

            foreach (Vector2Int direction
                     in Directions)
            {
                Vector2Int position =
                    context.Caster.GridPosition +
                    direction;

                /*
                 * 보드 밖은 스킬 범위에 포함하지 않는다.
                 */
                if (!context.GridManager
                        .IsInsideGrid(position))
                {
                    continue;
                }

                /*
                 * 적이 존재하는지와 관계없이
                 * 인접한 모든 칸을 범위로 반환한다.
                 *
                 * 실제 적용 가능 여부는
                 * CanApply()에서 판단한다.
                 */
                positions.Add(position);
            }

            return positions;
        }

        public override bool CanApply(
            SkillContext context,
            Vector2Int targetPosition)
        {
            if (!base.CanUse(context))
                return false;

            if (!context.GridManager
                    .IsInsideGrid(targetPosition))
            {
                return false;
            }

            Vector2Int difference =
                targetPosition -
                context.Caster.GridPosition;

            int distance =
                Mathf.Abs(difference.x) +
                Mathf.Abs(difference.y);

            /*
             * 상하좌우로 한 칸 떨어진 위치만 허용한다.
             */
            if (distance != 1)
                return false;

            ChessPiece target =
                context.GridManager.GetPieceAt(
                    targetPosition
                );

            /*
             * 빈칸에는 공격 스킬을 적용할 수 없다.
             */
            if (target == null)
                return false;

            if (target.IsDead ||
                !target.IsPlaced)
            {
                return false;
            }

            /*
             * 아군은 공격할 수 없다.
             */
            if (target.Color ==
                context.Caster.Color)
            {
                return false;
            }

            return true;
        }

        public override SkillResult Execute(
            SkillContext context)
        {
            if (!CanUse(context))
            {
                return SkillResult.CreateFail(
                    context.Caster,
                    "인접한 적만 공격할 수 있습니다."
                );
            }

            ChessPiece target =
                context.GridManager.GetPieceAt(
                    context.TargetPosition
                );

            /*
             * CanUse()에서 이미 검사했지만
             * 실행 단계의 안정성을 위해 다시 확인한다.
             */
            if (target == null)
            {
                return SkillResult.CreateFail(
                    context.Caster,
                    "공격할 대상이 없습니다."
                );
            }

            int hpBefore =
                target.CurrentHP;

            target.TakeDamage(damage);

            SkillResult result =
                SkillResult.CreateSuccess(
                    context.Caster,
                    target,
                    context.TargetPosition
                );

            result.AppliedDamage =
                hpBefore -
                target.CurrentHP;

            result.TargetKilled =
                target.IsDead;

            return result;
        }
    }
}