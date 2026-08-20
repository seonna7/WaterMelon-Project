using Game.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.StatusEffects
{
    public sealed class StatusEffectManager
        : MonoBehaviour
    {
        private readonly Dictionary<
            ChessPiece,
            List<StatusEffect>>
            effectsByTarget = new();

        private readonly List<ChessPiece>
            targetBuffer = new();

        private GameManager gameManager;

        private TurnManager turnManager;

        /*
         * 상태 추가 / 제거 / 지속시간 변경 /
         * Shield 수치 변경 시 발생.
         *
         * PieceStatusEffectUI가 이 이벤트를 구독한다.
         */
        public event Action<ChessPiece>
            StatusEffectsChanged;

        public void Initialize(
            GameManager manager)
        {
            Unsubscribe();

            gameManager =
                manager;

            turnManager =
                manager != null
                    ? manager.TurnManager
                    : null;

            Subscribe();
        }

        /*
         * =========================================
         * 상태 추가
         * =========================================
         */
        public bool AddEffect(
            ChessPiece target,
            StatusEffect effect)
        {
            if (target == null ||
                effect == null ||
                target.IsDead)
            {
                return false;
            }

            if (!effectsByTarget.TryGetValue(
                    target,
                    out List<StatusEffect> effects))
            {
                effects =
                    new List<StatusEffect>();

                effectsByTarget.Add(
                    target,
                    effects
                );
            }

            /*
             * 같은 EffectId가 이미 있는 경우.
             */
            for (int i = 0;
                 i < effects.Count;
                 i++)
            {
                StatusEffect existing =
                    effects[i];

                if (existing.EffectId !=
                    effect.EffectId)
                {
                    continue;
                }

                /*
                 * 중첩 불가능한 효과라면
                 * 기존 효과 지속시간만 갱신.
                 */
                if (!existing.CanStackWith(
                        effect))
                {
                    existing.RefreshDuration(
                        effect.RemainingTurns
                    );

                    NotifyChanged(
                        target
                    );

                    Debug.Log(
                        $"[StatusEffect] Refreshed | " +
                        $"Target={target.name} | " +
                        $"Effect={existing.EffectId} | " +
                        $"Duration={existing.RemainingTurns}"
                    );

                    return true;
                }
            }

            effects.Add(
                effect
            );

            effect.OnApplied(
                target,
                CreateContext(
                    turnManager != null
                        ? turnManager
                            .CurrentTurnNumber
                        : 0,

                    turnManager != null
                        ? turnManager
                            .CurrentTurnColor
                        : target.Color
                )
            );

            NotifyChanged(
                target
            );

            Debug.Log(
                $"[StatusEffect] Added | " +
                $"Target={target.name} | " +
                $"Effect={effect.EffectId} | " +
                $"Category={effect.Category} | " +
                $"Duration={effect.RemainingTurns}"
            );

            return true;
        }

        /*
         * =========================================
         * 상태 조회
         * =========================================
         */
        public IReadOnlyList<StatusEffect>
            GetEffects(
                ChessPiece target)
        {
            if (target == null)
            {
                return Array.Empty<
                    StatusEffect>();
            }

            if (!effectsByTarget
                    .TryGetValue(
                        target,
                        out List<StatusEffect> effects))
            {
                return Array.Empty<
                    StatusEffect>();
            }

            return effects;
        }

        public bool HasEffect(
            ChessPiece target,
            string effectId)
        {
            if (target == null ||
                string.IsNullOrEmpty(
                    effectId))
            {
                return false;
            }

            if (!effectsByTarget
                    .TryGetValue(
                        target,
                        out List<StatusEffect> effects))
            {
                return false;
            }

            for (int i = 0;
                 i < effects.Count;
                 i++)
            {
                if (effects[i].EffectId ==
                    effectId)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsStunned(
            ChessPiece target)
        {
            return HasEffect(
                target,
                StunStatusEffect.Id
            );
        }

        /*
         * =========================================
         * Shield
         * =========================================
         *
         * 들어오는 피해에 상태효과의 방어 효과를
         * 적용한 뒤 실제 HP에 들어갈 피해를 반환한다.
         *
         * 예:
         *
         * incomingDamage = 8
         * Shield = 5
         *
         * 반환값 = 3
         */
        public int ApplyDamageModifiers(
            ChessPiece target,
            int incomingDamage)
        {
            if (target == null)
                return incomingDamage;

            if (incomingDamage <= 0)
                return 0;

            if (!effectsByTarget
                    .TryGetValue(
                        target,
                        out List<StatusEffect> effects))
            {
                return incomingDamage;
            }

            int remainingDamage =
                incomingDamage;

            bool changed =
                false;

            /*
             * 현재는 Shield만 피해 수정 효과를 가진다.
             *
             * 이후 DamageReduction,
             * Invincible,
             * Vulnerable 등도 이 위치에
             * 확장 가능하다.
             */
            for (int i = 0;
                 i < effects.Count;
                 i++)
            {
                StatusEffect effect =
                    effects[i];

                if (effect is not
                    ShieldStatusEffect shield)
                {
                    continue;
                }

                int damageBefore =
                    remainingDamage;

                remainingDamage =
                    shield.AbsorbDamage(
                        remainingDamage
                    );

                if (damageBefore !=
                    remainingDamage)
                {
                    changed =
                        true;
                }

                if (remainingDamage <= 0)
                    break;
            }

            if (changed)
            {
                NotifyChanged(
                    target
                );
            }

            return Mathf.Max(
                0,
                remainingDamage
            );
        }

        /*
         * 현재 Shield 총량 조회.
         *
         * 나중에 체력바 옆 Shield 숫자나
         * 별도 게이지 표시에도 사용할 수 있다.
         */
        public int GetTotalShield(
            ChessPiece target)
        {
            if (target == null)
                return 0;

            if (!effectsByTarget
                    .TryGetValue(
                        target,
                        out List<StatusEffect> effects))
            {
                return 0;
            }

            int totalShield =
                0;

            for (int i = 0;
                 i < effects.Count;
                 i++)
            {
                if (effects[i] is
                    ShieldStatusEffect shield)
                {
                    totalShield +=
                        shield.ShieldAmount;
                }
            }

            return totalShield;
        }

        /*
         * =========================================
         * 상태 전체 제거
         * =========================================
         */
        public void RemoveAllEffects(
            ChessPiece target)
        {
            if (target == null)
                return;

            if (!effectsByTarget
                    .TryGetValue(
                        target,
                        out List<StatusEffect> effects))
            {
                return;
            }

            StatusEffectContext context =
                CreateContext(
                    turnManager != null
                        ? turnManager
                            .CurrentTurnNumber
                        : 0,

                    turnManager != null
                        ? turnManager
                            .CurrentTurnColor
                        : target.Color
                );

            for (int i = 0;
                 i < effects.Count;
                 i++)
            {
                effects[i].OnRemoved(
                    target,
                    context
                );
            }

            effectsByTarget.Remove(
                target
            );

            NotifyChanged(
                target
            );
        }

        /*
         * =========================================
         * Turn Event
         * =========================================
         */
        private void HandleTurnStarted(
            PlayerRuntimeData player,
            int turnNumber,
            PieceColor turnColor)
        {
            ProcessEffects(
                StatusEffectTickTiming
                    .TurnStart,
                turnNumber,
                turnColor
            );
        }

        private void HandleTurnEnding(
            PlayerRuntimeData player,
            int turnNumber,
            PieceColor turnColor)
        {
            ProcessEffects(
                StatusEffectTickTiming
                    .TurnEnd,
                turnNumber,
                turnColor
            );
        }

        /*
         * =========================================
         * 상태 Tick
         * =========================================
         */
        private void ProcessEffects(
            StatusEffectTickTiming timing,
            int turnNumber,
            PieceColor turnColor)
        {
            targetBuffer.Clear();

            /*
             * Dictionary 순회 중 제거될 수 있으므로
             * Key를 임시 Buffer에 복사한다.
             */
            foreach (
                ChessPiece target
                in effectsByTarget.Keys)
            {
                targetBuffer.Add(
                    target
                );
            }

            StatusEffectContext context =
                CreateContext(
                    turnNumber,
                    turnColor
                );

            for (int targetIndex = 0;
                 targetIndex <
                 targetBuffer.Count;
                 targetIndex++)
            {
                ChessPiece target =
                    targetBuffer[
                        targetIndex
                    ];

                if (target == null)
                    continue;

                /*
                 * 죽은 말은 상태 제거.
                 */
                if (target.IsDead)
                {
                    RemoveAllEffects(
                        target
                    );

                    continue;
                }

                /*
                 * 해당 팀의 턴에서만
                 * 상태 지속시간을 처리한다.
                 */
                if (target.Color !=
                    turnColor)
                {
                    continue;
                }

                if (!effectsByTarget
                        .TryGetValue(
                            target,
                            out List<StatusEffect> effects))
                {
                    continue;
                }

                bool changed =
                    false;

                for (int effectIndex =
                         effects.Count - 1;
                     effectIndex >= 0;
                     effectIndex--)
                {
                    StatusEffect effect =
                        effects[
                            effectIndex
                        ];

                    if (effect.TickTiming !=
                        timing)
                    {
                        continue;
                    }

                    /*
                     * 실제 상태 효과 실행.
                     */
                    effect.OnTick(
                        target,
                        context
                    );

                    /*
                     * 남은 턴 감소.
                     */
                    effect.ConsumeTurn();

                    changed =
                        true;

                    /*
                     * 만료 안 됐으면 유지.
                     */
                    if (!effect.IsExpired)
                        continue;

                    effect.OnRemoved(
                        target,
                        context
                    );

                    effects.RemoveAt(
                        effectIndex
                    );
                }

                if (effects.Count == 0)
                {
                    effectsByTarget.Remove(
                        target
                    );
                }

                if (changed)
                {
                    NotifyChanged(
                        target
                    );
                }
            }
        }

        /*
         * =========================================
         * UI / 외부 시스템 알림
         * =========================================
         */
        private void NotifyChanged(
            ChessPiece target)
        {
            if (target == null)
                return;

            StatusEffectsChanged?
                .Invoke(
                    target
                );
        }

        private StatusEffectContext
            CreateContext(
                int turnNumber,
                PieceColor turnColor)
        {
            return new StatusEffectContext(
                gameManager,
                turnManager,
                turnNumber,
                turnColor
            );
        }

        /*
         * =========================================
         * TurnManager Event
         * =========================================
         */
        private void Subscribe()
        {
            if (turnManager == null)
                return;

            turnManager.TurnStarted +=
                HandleTurnStarted;

            turnManager.TurnEnding +=
                HandleTurnEnding;
        }

        private void Unsubscribe()
        {
            if (turnManager == null)
                return;

            turnManager.TurnStarted -=
                HandleTurnStarted;

            turnManager.TurnEnding -=
                HandleTurnEnding;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }
    }
}