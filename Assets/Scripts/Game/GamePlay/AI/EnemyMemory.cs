using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * AI가 한 번 발견한 상대에 관한
     * 마지막 정보를 저장한다.
     *
     * EnemyPerception이 시야 판정 결과를 이용해
     * 이 클래스를 갱신한다.
     *
     * EnemyDecisionMaker는 현재 보이는 적이 없을 때
     * 마지막으로 확인된 위치를 추적할 수 있다.
     */
    public sealed class EnemyMemory
    {
        public sealed class TargetMemory
        {
            public ChessPiece Target
            {
                get;
                private set;
            }

            public Vector2Int LastKnownPosition
            {
                get;
                private set;
            }

            public int LastSeenTurn
            {
                get;
                private set;
            }

            public bool IsCurrentlyVisible
            {
                get;
                private set;
            }

            public bool IsValid =>
                Target != null &&
                !Target.IsDead;

            public TargetMemory(
                ChessPiece target,
                Vector2Int lastKnownPosition,
                int lastSeenTurn,
                bool isCurrentlyVisible)
            {
                Target = target;
                LastKnownPosition =
                    lastKnownPosition;

                LastSeenTurn =
                    Mathf.Max(0, lastSeenTurn);

                IsCurrentlyVisible =
                    isCurrentlyVisible;
            }

            public void UpdateVisible(
                Vector2Int position,
                int currentTurn)
            {
                LastKnownPosition = position;

                LastSeenTurn =
                    Mathf.Max(0, currentTurn);

                IsCurrentlyVisible = true;
            }

            public void MarkNotVisible()
            {
                IsCurrentlyVisible = false;
            }
        }

        private readonly Dictionary<
            ChessPiece,
            TargetMemory> targetMemories =
                new();

        private readonly List<ChessPiece>
            cleanupBuffer = new();

        public int MemoryDurationTurns
        {
            get;
            private set;
        }

        public int Count =>
            targetMemories.Count;

        public EnemyMemory(
            int memoryDurationTurns = 3)
        {
            MemoryDurationTurns =
                Mathf.Max(
                    0,
                    memoryDurationTurns
                );
        }

        /*
         * 현재 시야에서 확인한 상대 정보를 기록한다.
         *
         * 이미 기억 중인 대상이면
         * 마지막 위치와 마지막 확인 턴을 갱신한다.
         */
        public void RememberVisibleTarget(
            ChessPiece target,
            int currentTurn)
        {
            if (target == null ||
                target.IsDead ||
                !target.IsPlaced)
            {
                return;
            }

            if (targetMemories.TryGetValue(
                    target,
                    out TargetMemory memory))
            {
                memory.UpdateVisible(
                    target.GridPosition,
                    currentTurn
                );

                return;
            }

            targetMemories.Add(
                target,
                new TargetMemory(
                    target,
                    target.GridPosition,
                    currentTurn,
                    true
                )
            );
        }

        /*
         * 모든 기억을 우선 보이지 않는 상태로 바꾼다.
         *
         * EnemyPerception은 시야 계산을 시작할 때
         * 이 메서드를 호출하고,
         * 실제로 발견한 대상만 다시 Visible 상태로 만든다.
         */
        public void BeginPerceptionUpdate()
        {
            foreach (TargetMemory memory
                     in targetMemories.Values)
            {
                memory.MarkNotVisible();
            }
        }

        /*
         * 죽은 대상, 파괴된 대상,
         * 기억 유지 시간이 지난 대상을 제거한다.
         */
        public void RemoveExpiredMemories(
            int currentTurn)
        {
            cleanupBuffer.Clear();

            foreach (KeyValuePair<
                         ChessPiece,
                         TargetMemory> pair
                     in targetMemories)
            {
                ChessPiece target =
                    pair.Key;

                TargetMemory memory =
                    pair.Value;

                if (target == null ||
                    memory == null ||
                    !memory.IsValid)
                {
                    cleanupBuffer.Add(target);
                    continue;
                }

                if (memory.IsCurrentlyVisible)
                    continue;

                int elapsedTurns =
                    Mathf.Max(
                        0,
                        currentTurn -
                        memory.LastSeenTurn
                    );

                if (elapsedTurns >
                    MemoryDurationTurns)
                {
                    cleanupBuffer.Add(target);
                }
            }

            for (int i = 0;
                 i < cleanupBuffer.Count;
                 i++)
            {
                targetMemories.Remove(
                    cleanupBuffer[i]
                );
            }

            cleanupBuffer.Clear();
        }

        public bool TryGetMemory(
            ChessPiece target,
            out TargetMemory memory)
        {
            if (target == null)
            {
                memory = null;
                return false;
            }

            return targetMemories.TryGetValue(
                target,
                out memory
            );
        }

        /*
         * 현재 보이지 않더라도 기억 중인 대상 가운데
         * 관찰자와 가장 가까운 마지막 위치를 반환한다.
         */
        public bool TryGetNearestRememberedTarget(
            Vector2Int observerPosition,
            out TargetMemory nearestMemory)
        {
            nearestMemory = null;

            int nearestDistance =
                int.MaxValue;

            foreach (TargetMemory memory
                     in targetMemories.Values)
            {
                if (memory == null ||
                    !memory.IsValid)
                {
                    continue;
                }

                int distance =
                    ManhattanDistance(
                        observerPosition,
                        memory.LastKnownPosition
                    );

                if (distance >=
                    nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestMemory = memory;
            }

            return nearestMemory != null;
        }

        /*
         * 현재 실제 시야에 들어온 대상 중
         * 가장 가까운 대상을 반환한다.
         */
        public bool TryGetNearestVisibleTarget(
            Vector2Int observerPosition,
            out TargetMemory nearestMemory)
        {
            nearestMemory = null;

            int nearestDistance =
                int.MaxValue;

            foreach (TargetMemory memory
                     in targetMemories.Values)
            {
                if (memory == null ||
                    !memory.IsValid ||
                    !memory.IsCurrentlyVisible)
                {
                    continue;
                }

                int distance =
                    ManhattanDistance(
                        observerPosition,
                        memory.LastKnownPosition
                    );

                if (distance >=
                    nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearestMemory = memory;
            }

            return nearestMemory != null;
        }

        public IReadOnlyCollection<TargetMemory>
            GetAllMemories()
        {
            return targetMemories.Values;
        }

        public void ForgetTarget(
            ChessPiece target)
        {
            if (target == null)
                return;

            targetMemories.Remove(target);
        }

        public void Clear()
        {
            targetMemories.Clear();
            cleanupBuffer.Clear();
        }

        public void SetMemoryDuration(
            int durationTurns)
        {
            MemoryDurationTurns =
                Mathf.Max(
                    0,
                    durationTurns
                );
        }

        private static int ManhattanDistance(
            Vector2Int first,
            Vector2Int second)
        {
            return Mathf.Abs(
                       first.x - second.x
                   ) +
                   Mathf.Abs(
                       first.y - second.y
                   );
        }
    }
}