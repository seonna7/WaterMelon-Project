using Game.GamePlay.Grid;
using UnityEngine;

namespace Game.GamePlay.Skill
{
    public readonly struct SkillContext
    {
        public ChessPiece Caster { get; }

        public ChessPiece TargetPiece { get; }

        public Vector2Int TargetPosition { get; }

        public GridManager GridManager { get; }

        public SkillContext(
            ChessPiece caster,
            GridManager gridManager,
            ChessPiece targetPiece = null,
            Vector2Int targetPosition = default)
        {
            Caster = caster;
            GridManager = gridManager;
            TargetPiece = targetPiece;
            TargetPosition = targetPosition;
        }
    }
}
