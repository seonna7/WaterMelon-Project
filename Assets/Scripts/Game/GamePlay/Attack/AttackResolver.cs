using Game.Action;
using Game.GamePlay.Grid;
using Game.GamePlay.StatusEffects;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Attack
{
    public sealed class AttackResolver
    {
        private readonly PushResolver pushResolver =
            new PushResolver();

        private readonly DirectAttackRuleResolver ruleResolver =
            new DirectAttackRuleResolver();

        private readonly StatusEffectManager statusEffectManager;

        public AttackResolver()
        {
            statusEffectManager =
                Object.FindFirstObjectByType<StatusEffectManager>();
        }

        public AttackResult Resolve(
            ChessPiece attacker,
            ChessPiece target,
            GridManager grid)
        {
            AttackFailReason failReason = Validate(
                attacker,
                target,
                grid
            );

            if (failReason != AttackFailReason.None)
            {
                return AttackResult.CreateFail(
                    failReason,
                    attacker,
                    target
                );
            }

            DirectAttackRule rule =
                ruleResolver.GetRule(attacker);

            Vector2Int attackerStart = attacker.GridPosition;
            Vector2Int targetStart = target.GridPosition;
            Vector2Int attackDirection =
                DirectAttackPositionResolver.NormalizeDirection(
                    targetStart - attackerStart
                );

            int targetHPBefore = target.CurrentHP;
            int damage = ApplyDamage(
                target,
                attacker.AttackPower
            );

            bool targetKilled = target.IsDead;
            bool targetRemoved = false;
            bool targetStartVacated = false;
            Vector2Int targetEnd = targetStart;

            if (targetKilled)
            {
                targetRemoved = RemovePiece(
                    target,
                    grid
                );

                if (!targetRemoved)
                {
                    return AttackResult.CreateFail(
                        AttackFailReason.TargetRemovalFailed,
                        attacker,
                        target
                    );
                }

                targetStartVacated = true;
            }
            else if (rule.PushDistance > 0)
            {
                PushResult pushResult = pushResolver.TryPush(
                    target,
                    grid,
                    rule.PushDistance,
                    attackDirection
                );

                targetStartVacated =
                    pushResult.Success &&
                    (pushResult.MovedDistance > 0 ||
                     pushResult.PushedOut);

                if (target.IsPlaced)
                    targetEnd = target.GridPosition;
            }

            Vector2Int attackerEnd =
                DirectAttackPositionResolver.ResolveAttackerPosition(
                    rule,
                    attackerStart,
                    targetStart,
                    targetEnd,
                    targetStartVacated,
                    grid
                );

            MoveAttacker(
                attacker,
                attackerEnd,
                grid
            );

            if (rule.SelfHealAmount > 0 &&
                !attacker.IsDead)
            {
                attacker.Heal(rule.SelfHealAmount);
            }

            if (rule.HasAreaEffect)
            {
                Vector2Int center =
                    rule.AreaCenteredOnAttacker
                        ? attacker.GridPosition
                        : targetStart;

                ApplyAreaEffect(
                    attacker,
                    target,
                    center,
                    rule,
                    grid
                );
            }

            return AttackResult.CreateSuccess(
                attacker,
                target,
                damage,
                targetHPBefore,
                target.CurrentHP,
                targetKilled,
                targetRemoved
            );
        }

        private void ApplyAreaEffect(
            ChessPiece attacker,
            ChessPiece primaryTarget,
            Vector2Int center,
            DirectAttackRule rule,
            GridManager grid)
        {
            List<ChessPiece> targets =
                CollectAdjacentEnemies(
                    attacker,
                    primaryTarget,
                    center,
                    grid
                );

            for (int i = 0; i < targets.Count; i++)
            {
                ChessPiece target = targets[i];

                if (target == null ||
                    target.IsDead ||
                    !target.IsPlaced)
                {
                    continue;
                }

                if (rule.AreaDamage > 0)
                {
                    ApplyDamage(target, rule.AreaDamage);

                    if (target.IsDead)
                    {
                        RemovePiece(target, grid);
                        continue;
                    }
                }

                if (rule.AreaPushDistance <= 0)
                    continue;

                Vector2Int direction =
                    DirectAttackPositionResolver.NormalizeDirection(
                        target.GridPosition - center
                    );

                pushResolver.TryPush(
                    target,
                    grid,
                    rule.AreaPushDistance,
                    direction
                );
            }
        }

        private static List<ChessPiece> CollectAdjacentEnemies(
            ChessPiece attacker,
            ChessPiece primaryTarget,
            Vector2Int center,
            GridManager grid)
        {
            List<ChessPiece> result = new List<ChessPiece>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector2Int position =
                        center + new Vector2Int(x, y);

                    if (!grid.IsInsideGrid(position))
                        continue;

                    ChessPiece candidate =
                        grid.GetPieceAt(position);

                    if (candidate == null ||
                        candidate == attacker ||
                        candidate == primaryTarget ||
                        candidate.IsDead ||
                        candidate.Color == attacker.Color)
                    {
                        continue;
                    }

                    result.Add(candidate);
                }
            }

            return result;
        }

        private int ApplyDamage(
            ChessPiece target,
            int incomingDamage)
        {
            if (target == null || incomingDamage <= 0)
                return 0;

            int hpBefore = target.CurrentHP;
            int finalDamage = statusEffectManager != null
                ? statusEffectManager.ApplyDamageModifiers(
                    target,
                    incomingDamage)
                : incomingDamage;

            if (finalDamage > 0)
                target.TakeDamage(finalDamage);

            return Mathf.Max(
                hpBefore - target.CurrentHP,
                0
            );
        }

        private static bool MoveAttacker(
            ChessPiece attacker,
            Vector2Int destination,
            GridManager grid)
        {
            if (attacker == null ||
                !attacker.IsPlaced ||
                attacker.IsDead ||
                destination == attacker.GridPosition)
            {
                return false;
            }

            return grid.MovePiece(attacker, destination);
        }

        private static bool RemovePiece(
            ChessPiece target,
            GridManager grid)
        {
            if (target == null || grid == null)
                return false;

            bool removed = grid.RemovePiece(target);

            if (removed)
                target.gameObject.SetActive(false);

            return removed;
        }

        private static AttackFailReason Validate(
            ChessPiece attacker,
            ChessPiece target,
            GridManager grid)
        {
            if (grid == null)
                return AttackFailReason.GridManagerIsNull;
            if (attacker == null)
                return AttackFailReason.AttackerIsNull;
            if (target == null)
                return AttackFailReason.TargetIsNull;
            if (attacker == target)
                return AttackFailReason.SamePiece;
            if (attacker.IsDead)
                return AttackFailReason.AttackerIsDead;
            if (target.IsDead)
                return AttackFailReason.TargetIsDead;
            if (!attacker.IsPlaced)
                return AttackFailReason.AttackerIsNotPlaced;
            if (!target.IsPlaced)
                return AttackFailReason.TargetIsNotPlaced;
            if (attacker.IsMoving)
                return AttackFailReason.AttackerIsMoving;
            if (attacker.Color == target.Color)
                return AttackFailReason.SameTeam;
            if (grid.GetPieceAt(target.GridPosition) != target)
                return AttackFailReason.TargetPositionMismatch;

            List<Vector2Int> attackPositions =
                attacker.GetDirectAttackPositions(grid);

            return attackPositions.Contains(target.GridPosition)
                ? AttackFailReason.None
                : AttackFailReason.TargetOutOfAttackRange;
        }
    }
}