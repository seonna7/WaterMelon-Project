using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    public enum EnemyPatrolLoopMode
    {
        Loop,
        PingPong,
        Once
    }

    /*
     * 적이 순찰할 그리드 위치 목록을 저장한다.
     *
     * 프리팹이나 적 종류별로 서로 다른 경로를
     * Inspector에서 지정할 수 있도록 ScriptableObject로 만든다.
     */
    [CreateAssetMenu(
        fileName = "EnemyPatrolRoute",
        menuName = "Game/AI/Enemy Patrol Route"
    )]
    public sealed class EnemyPatrolRoute
        : ScriptableObject
    {
        [SerializeField]
        private List<Vector2Int> patrolPositions =
            new();

        [SerializeField]
        private EnemyPatrolLoopMode loopMode =
            EnemyPatrolLoopMode.Loop;

        [SerializeField]
        [Min(0)]
        private int waitTurnsAtPoint;

        public IReadOnlyList<Vector2Int>
            PatrolPositions =>
                patrolPositions;

        public EnemyPatrolLoopMode LoopMode =>
            loopMode;

        public int WaitTurnsAtPoint =>
            waitTurnsAtPoint;

        public int Count =>
            patrolPositions != null
                ? patrolPositions.Count
                : 0;

        public bool HasValidRoute =>
            Count > 0;

        public bool TryGetPosition(
            int index,
            out Vector2Int position)
        {
            if (patrolPositions == null ||
                index < 0 ||
                index >= patrolPositions.Count)
            {
                position = default;
                return false;
            }

            position =
                patrolPositions[index];

            return true;
        }

        private void OnValidate()
        {
            waitTurnsAtPoint =
                Mathf.Max(
                    0,
                    waitTurnsAtPoint
                );

            if (patrolPositions == null)
            {
                patrolPositions =
                    new List<Vector2Int>();
            }
        }
    }
}