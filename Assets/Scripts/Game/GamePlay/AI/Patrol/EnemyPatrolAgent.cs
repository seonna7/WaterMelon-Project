using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * 적 프리팹 또는 씬의 적 오브젝트에 붙여
     * 순찰 경로를 지정한다.
     */
    public sealed class EnemyPatrolAgent
        : MonoBehaviour
    {
        [SerializeField]
        private EnemyPatrolRoute patrolRoute;

        public EnemyPatrolRoute PatrolRoute =>
            patrolRoute;
    }
}