using Game.GamePlay.Grid;
using UnityEngine;

namespace Game.GamePlay.Attack
{
    public static class DirectAttackPositionResolver
    {
        public static Vector2Int ResolveAttackerPosition(
            DirectAttackRule rule,
            Vector2Int attackerStart,
            Vector2Int targetStart,
            Vector2Int targetEnd,
            bool targetStartVacated,
            GridManager grid)
        {
            if (grid == null)
                return attackerStart;

            switch (rule.AdvanceMode)
            {
                case DirectAttackAdvanceMode.TargetStart:
                    if (CanOccupy(
                            targetStart,
                            targetStart,
                            targetStartVacated,
                            grid))
                    {
                        return targetStart;
                    }

                    /*
                     * 밀치기를 시도했지만 적이 한 칸도 움직이지 않았다면
                     * 적과 공격자 사이에서 적에게 가장 가까운 빈칸으로 간다.
                     */
                    return FindClosestCellBeforeTarget(
                        attackerStart,
                        targetStart,
                        targetEnd,
                        targetStartVacated,
                        grid
                    );

                case DirectAttackAdvanceMode.BeforeTarget:
                    return FindClosestCellBeforeTarget(
                        attackerStart,
                        targetStart,
                        targetEnd,
                        targetStartVacated,
                        grid
                    );

                default:
                    return attackerStart;
            }
        }

        private static Vector2Int FindClosestCellBeforeTarget(
            Vector2Int attackerStart,
            Vector2Int targetStart,
            Vector2Int targetEnd,
            bool targetStartVacated,
            GridManager grid)
        {
            Vector2Int direction = NormalizeDirection(
                targetStart - attackerStart
            );

            if (direction == Vector2Int.zero)
                return attackerStart;

            Vector2Int difference =
                targetEnd - attackerStart;

            int distance = Mathf.Max(
                Mathf.Abs(difference.x),
                Mathf.Abs(difference.y)
            );

            for (int step = 1;
                 step <= distance;
                 step++)
            {
                Vector2Int candidate =
                    targetEnd - direction * step;

                if (candidate == attackerStart)
                    return attackerStart;

                if (CanOccupy(
                        candidate,
                        targetStart,
                        targetStartVacated,
                        grid))
                {
                    return candidate;
                }
            }

            return attackerStart;
        }

        private static bool CanOccupy(
            Vector2Int position,
            Vector2Int targetStart,
            bool targetStartVacated,
            GridManager grid)
        {
            if (!grid.IsInsideGrid(position) ||
                !grid.IsWalkable(position))
            {
                return false;
            }

            if (targetStartVacated &&
                position == targetStart)
            {
                return true;
            }

            return grid.IsEmpty(position);
        }

        public static Vector2Int NormalizeDirection(
            Vector2Int direction)
        {
            return new Vector2Int(
                System.Math.Sign(direction.x),
                System.Math.Sign(direction.y)
            );
        }
    }
}