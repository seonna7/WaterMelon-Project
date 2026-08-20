using UnityEngine;

namespace Game.CameraSystem
{
    public enum CameraCommandType
    {
        ReturnToGrid,
        PieceClick,
        PieceMove,
        PieceMoving,
        PieceDirectAttack,
        PieceSkill,
        PieceSkillEngaging
    }

    public readonly struct CameraCommand
    {
        public CameraCommandType Type { get; }

        public Transform Target { get; }

        public System.Action OnComplete { get; }

        public CameraCommand(
            CameraCommandType type,
            Transform target = null,
            System.Action onComplete = null)
        {
            Type = type;
            Target = target;
            OnComplete = onComplete;
        }
    }
}