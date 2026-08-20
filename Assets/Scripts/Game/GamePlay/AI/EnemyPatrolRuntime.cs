using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * 적 유닛 하나의 순찰 진행 상태다.
     *
     * EnemyPatrolRoute는 공유 데이터고,
     * 이 클래스는 적 개체마다 별도로 생성한다.
     */
    public sealed class EnemyPatrolRuntime
    {
        public EnemyPatrolRoute Route
        {
            get;
            private set;
        }

        public int CurrentIndex
        {
            get;
            private set;
        }

        public int Direction
        {
            get;
            private set;
        } = 1;

        public int RemainingWaitTurns
        {
            get;
            private set;
        }

        public bool IsCompleted
        {
            get;
            private set;
        }

        public bool HasRoute =>
            Route != null &&
            Route.HasValidRoute;

        public bool IsWaiting =>
            RemainingWaitTurns > 0;

        public EnemyPatrolRuntime(
            EnemyPatrolRoute route)
        {
            SetRoute(route);
        }

        public void SetRoute(
            EnemyPatrolRoute route)
        {
            Route = route;

            CurrentIndex = 0;
            Direction = 1;
            RemainingWaitTurns = 0;
            IsCompleted = false;
        }

        public bool TryGetCurrentPosition(
            out Vector2Int position)
        {
            if (!HasRoute ||
                IsCompleted)
            {
                position = default;
                return false;
            }

            return Route.TryGetPosition(
                CurrentIndex,
                out position
            );
        }

        /*
         * 현재 순찰 지점에 도착했을 때 호출한다.
         */
        public void HandleArrival()
        {
            if (!HasRoute ||
                IsCompleted)
            {
                return;
            }

            RemainingWaitTurns =
                Route.WaitTurnsAtPoint;

            AdvanceIndex();
        }

        /*
         * 순찰 지점에서 한 턴 대기한다.
         */
        public void ConsumeWaitTurn()
        {
            if (RemainingWaitTurns <= 0)
                return;

            RemainingWaitTurns =
                Mathf.Max(
                    0,
                    RemainingWaitTurns - 1
                );
        }

        public void Reset()
        {
            CurrentIndex = 0;
            Direction = 1;
            RemainingWaitTurns = 0;
            IsCompleted = false;
        }

        private void AdvanceIndex()
        {
            if (!HasRoute)
                return;

            int count =
                Route.Count;

            if (count <= 1)
            {
                if (Route.LoopMode ==
                    EnemyPatrolLoopMode.Once)
                {
                    IsCompleted = true;
                }

                return;
            }

            switch (Route.LoopMode)
            {
                case EnemyPatrolLoopMode.Loop:
                    CurrentIndex =
                        (CurrentIndex + 1) %
                        count;
                    break;

                case EnemyPatrolLoopMode.PingPong:
                    CurrentIndex += Direction;

                    if (CurrentIndex >= count)
                    {
                        Direction = -1;
                        CurrentIndex =
                            count - 2;
                    }
                    else if (CurrentIndex < 0)
                    {
                        Direction = 1;
                        CurrentIndex = 1;
                    }
                    break;

                case EnemyPatrolLoopMode.Once:
                    if (CurrentIndex >=
                        count - 1)
                    {
                        IsCompleted = true;
                        return;
                    }

                    CurrentIndex++;
                    break;
            }
        }
    }
}