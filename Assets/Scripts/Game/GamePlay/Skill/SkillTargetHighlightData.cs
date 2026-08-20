using UnityEngine;

public readonly struct SkillTargetHighlightData
{
    public Vector2Int GridPosition { get; }

    public bool CanApply { get; }

    public SkillTargetHighlightData(
        Vector2Int gridPosition,
        bool canApply)
    {
        GridPosition = gridPosition;
        CanApply = canApply;
    }
}