using Game.GamePlay.MoveStrategy;
using Game.GamePlay.Skill;

namespace Game
{
    namespace GamePlay
    {
        public class QueenPiece : ChessPiece
        {
            protected override void Awake()
            {
                base.Awake();

                movementType = MovementType.Queen;
                ActionType = ActionType.Vagabond;

                MaxHP = 20;
                CurrentHP = 20;
                AttackPower = 5;

                moveStrategy = new QueenMoveStrategy();

                skill1 = new AdjacentAttackSkill();

                skill2 = new AdjacentAttackSkill();
            }
        }
    }
}
