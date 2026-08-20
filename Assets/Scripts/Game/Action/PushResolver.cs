using Game.GamePlay;
using Game.GamePlay.Grid;
using UnityEngine;

namespace Game.Action
{
    public sealed class PushResolver
    {
        public PushResult Predict(
            ChessPiece target,
            GridManager grid,
            int distance,
            Vector2Int direction)
        {
            if (!CanPush(
                    target,
                    grid,
                    distance,
                    ref direction))
            {
                return PushResult.CreateFail(
                    target,
                    target != null
                        ? target.GridPosition
                        : Vector2Int.zero,
                    distance
                );
            }

            Vector2Int start = target.GridPosition;
            Vector2Int current = start;
            int moved = 0;

            for (int step = 0;
                 step < distance;
                 step++)
            {
                Vector2Int next = current + direction;

                if (!grid.IsInsideGrid(next))
                {
                    return PushResult.CreateSuccess(
                        target,
                        start,
                        next,
                        distance,
                        moved + 1,
                        pushedOut: true,
                        blocked: false
                    );
                }

                if (!grid.IsWalkable(next) ||
                    !grid.IsEmpty(next))
                {
                    return PushResult.CreateSuccess(
                        target,
                        start,
                        current,
                        distance,
                        moved,
                        pushedOut: false,
                        blocked: true
                    );
                }

                current = next;
                moved++;
            }

            return PushResult.CreateSuccess(
                target,
                start,
                current,
                distance,
                moved,
                pushedOut: false,
                blocked: false
            );
        }

        public PushResult TryPush(
            ChessPiece target,
            GridManager grid,
            int distance)
        {
            if (target == null || grid == null)
            {
                return PushResult.CreateFail(
                    target,
                    Vector2Int.zero,
                    distance
                );
            }

            return TryPush(
                target,
                grid,
                distance,
                grid.GetKnockbackDirection(target.Color)
            );
        }

        public PushResult TryPush(
            ChessPiece target,
            GridManager grid,
            int distance,
            Vector2Int direction)
        {
            PushResult prediction = Predict(
                target,
                grid,
                distance,
                direction
            );

            if (!prediction.Success)
                return prediction;

            direction = NormalizeDirection(direction);

            Vector2Int current =
                prediction.StartPosition;

            int insideSteps = prediction.PushedOut
                ? prediction.MovedDistance - 1
                : prediction.MovedDistance;

            for (int step = 0;
                 step < insideSteps;
                 step++)
            {
                Vector2Int next = current + direction;

                if (!grid.MovePiece(current, next))
                {
                    return PushResult.CreateSuccess(
                        target,
                        prediction.StartPosition,
                        current,
                        distance,
                        step,
                        pushedOut: false,
                        blocked: true
                    );
                }

                current = next;
            }

            if (prediction.PushedOut)
            {
                bool removed = grid.RemovePiece(target);

                if (removed)
                    target.gameObject.SetActive(false);

                return removed
                    ? prediction
                    : PushResult.CreateFail(
                        target,
                        current,
                        distance
                    );
            }

            return prediction;
        }

        private static bool CanPush(
            ChessPiece target,
            GridManager grid,
            int distance,
            ref Vector2Int direction)
        {
            if (target == null ||
                grid == null ||
                target.IsDead ||
                !target.IsPlaced ||
                distance <= 0)
            {
                return false;
            }

            direction = NormalizeDirection(direction);

            return direction != Vector2Int.zero;
        }

        private static Vector2Int NormalizeDirection(
            Vector2Int direction)
        {
            return new Vector2Int(
                System.Math.Sign(direction.x),
                System.Math.Sign(direction.y)
            );
        }
    }
}