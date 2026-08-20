using Game.GamePlay;
using Game.GamePlay.StatusEffects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI.PieceStatus
{
    public sealed class PieceStatusEffectUI
        : MonoBehaviour
    {
        private const int MaxStatusCount = 12;
        private const int ColumnCount = 4;

        private ChessPiece targetPiece;

        private StatusEffectManager
            statusEffectManager;

        [Header("References")]
        [SerializeField]
        private RectTransform statusRoot;

        [SerializeField]
        private StatusEffectIconUI
            statusIconPrefab;

        [Header("Layout")]
        [SerializeField]
        private Vector2 iconSize =
            new Vector2(
                28f,
                28f
            );

        [SerializeField]
        private Vector2 spacing =
            new Vector2(
                4f,
                4f
            );

        [Serializable]
        private struct StatusEffectSpriteEntry
        {
            public string effectId;
            public Sprite sprite;
        }

        [Header("Status Sprites")]
        [SerializeField]
        private List<StatusEffectSpriteEntry>
            statusEffectSprites = new();

        /*
         * 최대 12개의 아이콘을 미리 생성해
         * 재사용한다.
         */
        private readonly List<StatusEffectIconUI>
            iconPool = new();

        /*
         * 현재 UI에 표시할 Buff 목록.
         */
        private readonly List<StatusEffectDisplayData>
            buffs = new();

        /*
         * 현재 UI에 표시할 Debuff 목록.
         */
        private readonly List<StatusEffectDisplayData>
            debuffs = new();

        public ChessPiece TargetPiece =>
            targetPiece;

        private void Awake()
        {
            BuildIconPool();

            Clear();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /*
         * 특정 ChessPiece와
         * StatusEffectManager를 연결한다.
         */
        public void Initialize(
            ChessPiece piece,
            StatusEffectManager manager)
        {
            Unsubscribe();

            targetPiece =
                piece;

            statusEffectManager =
                manager;

            Subscribe();

            RefreshFromManager();
        }

        /*
         * 추적 대상을 변경한다.
         */
        public void SetTarget(
            ChessPiece piece)
        {
            targetPiece =
                piece;

            RefreshFromManager();
        }

        /*
         * StatusEffectManager 변경 이벤트 구독.
         */
        private void Subscribe()
        {
            if (statusEffectManager == null)
                return;

            statusEffectManager
                .StatusEffectsChanged +=
                    HandleStatusEffectsChanged;
        }

        /*
         * 기존 이벤트 구독 해제.
         */
        private void Unsubscribe()
        {
            if (statusEffectManager == null)
                return;

            statusEffectManager
                .StatusEffectsChanged -=
                    HandleStatusEffectsChanged;
        }

        /*
         * 특정 말의 상태가 변경됐을 때
         * 해당 말의 UI만 갱신한다.
         */
        private void HandleStatusEffectsChanged(
            ChessPiece piece)
        {
            if (piece != targetPiece)
                return;

            RefreshFromManager();
        }

        /*
         * StatusEffectManager에서
         * 현재 대상의 실제 상태 목록을 읽어
         * UI용 데이터로 변환한다.
         */
        public void RefreshFromManager()
        {
            if (targetPiece == null ||
                statusEffectManager == null)
            {
                ClearIconsOnly();
                return;
            }

            IReadOnlyList<StatusEffect>
                effects =
                    statusEffectManager
                        .GetEffects(
                            targetPiece
                        );

            List<StatusEffectDisplayData>
                displayData =
                    new();

            if (effects != null)
            {
                int count =
                    Mathf.Min(
                        effects.Count,
                        MaxStatusCount
                    );

                for (int i = 0;
                     i < count;
                     i++)
                {
                    StatusEffect effect =
                        effects[i];

                    if (effect == null)
                        continue;

                    Sprite icon =
                        FindStatusSprite(
                            effect.EffectId
                        );

                    /*
                     * StatusEffect 자체가
                     * Buff/Debuff 정보를 가지고 있으므로
                     * UI에서 EffectId를 이용해
                     * 분류하지 않는다.
                     */
                    bool isBuff =
                        effect.IsBuff;

                    displayData.Add(
                        new StatusEffectDisplayData(
                            icon,
                            isBuff,
                            effect.RemainingTurns,
                            1
                        )
                    );
                }
            }

            SetStatusEffects(
                displayData
            );
        }

        /*
         * EffectId에 대응하는
         * UI Sprite를 찾는다.
         */
        private Sprite FindStatusSprite(
            string effectId)
        {
            if (string.IsNullOrEmpty(
                    effectId))
            {
                return null;
            }

            for (int i = 0;
                 i < statusEffectSprites.Count;
                 i++)
            {
                if (statusEffectSprites[i]
                        .effectId ==
                    effectId)
                {
                    return statusEffectSprites[i]
                        .sprite;
                }
            }

            Debug.LogWarning(
                $"[PieceStatusEffectUI] " +
                $"Status Sprite 없음 | " +
                $"EffectId={effectId}"
            );

            return null;
        }

        /*
         * 최대 12개의 아이콘을 미리 생성한다.
         */
        private void BuildIconPool()
        {
            if (statusRoot == null ||
                statusIconPrefab == null)
            {
                Debug.LogWarning(
                    "[PieceStatusEffectUI] " +
                    "StatusRoot 또는 " +
                    "StatusIconPrefab이 없습니다."
                );

                return;
            }

            for (int i = 0;
                 i < MaxStatusCount;
                 i++)
            {
                StatusEffectIconUI icon =
                    Instantiate(
                        statusIconPrefab,
                        statusRoot
                    );

                icon.Clear();

                iconPool.Add(
                    icon
                );
            }
        }

        /*
         * 상태이상 목록을 받아
         * Buff / Debuff로 분리한다.
         *
         * 표시 순서는:
         *
         * Buff
         * ↓
         * Debuff
         */
        public void SetStatusEffects(
            IReadOnlyList<StatusEffectDisplayData>
                statusEffects)
        {
            buffs.Clear();
            debuffs.Clear();

            if (statusEffects != null)
            {
                for (int i = 0;
                     i < statusEffects.Count;
                     i++)
                {
                    StatusEffectDisplayData data =
                        statusEffects[i];

                    if (data.IsBuff)
                    {
                        buffs.Add(
                            data
                        );
                    }
                    else
                    {
                        debuffs.Add(
                            data
                        );
                    }
                }
            }

            Refresh();
        }

        /*
         * Buff와 Debuff를 이미
         * 분리해서 전달하는 경우 사용.
         */
        public void SetStatusEffects(
            IReadOnlyList<StatusEffectDisplayData>
                buffList,
            IReadOnlyList<StatusEffectDisplayData>
                debuffList)
        {
            buffs.Clear();
            debuffs.Clear();

            if (buffList != null)
            {
                for (int i = 0;
                     i < buffList.Count;
                     i++)
                {
                    buffs.Add(
                        buffList[i]
                    );
                }
            }

            if (debuffList != null)
            {
                for (int i = 0;
                     i < debuffList.Count;
                     i++)
                {
                    debuffs.Add(
                        debuffList[i]
                    );
                }
            }

            Refresh();
        }

        /*
         * 실제 아이콘 표시 갱신.
         *
         * 최대 12개.
         * Buff 먼저.
         * Debuff 다음.
         */
        private void Refresh()
        {
            ClearIconsOnly();

            int index =
                0;

            /*
             * ============================
             * Buff 먼저
             * ============================
             */
            for (int i = 0;
                 i < buffs.Count &&
                 index < MaxStatusCount;
                 i++)
            {
                if (index >=
                    iconPool.Count)
                {
                    break;
                }

                ApplyIcon(
                    iconPool[index],
                    buffs[i],
                    index
                );

                index++;
            }

            /*
             * ============================
             * Debuff 다음
             * ============================
             */
            for (int i = 0;
                 i < debuffs.Count &&
                 index < MaxStatusCount;
                 i++)
            {
                if (index >=
                    iconPool.Count)
                {
                    break;
                }

                ApplyIcon(
                    iconPool[index],
                    debuffs[i],
                    index
                );

                index++;
            }

            Debug.Log(
                $"[PieceStatusEffectUI] " +
                $"Refresh | " +
                $"Piece=" +
                $"{(targetPiece != null ? targetPiece.name : "NULL")} | " +
                $"Buff={buffs.Count} | " +
                $"Debuff={debuffs.Count} | " +
                $"Displayed={index}"
            );
        }

        /*
         * 실제 아이콘 하나에
         * 데이터 및 위치를 적용한다.
         */
        private void ApplyIcon(
            StatusEffectIconUI iconUI,
            StatusEffectDisplayData data,
            int index)
        {
            if (iconUI == null)
                return;

            iconUI.Initialize(
                data.Icon,
                data.IsBuff,
                data.RemainingTurns,
                data.StackCount
            );

            RectTransform rect =
                iconUI.transform
                    as RectTransform;

            if (rect == null)
                return;

            /*
             * 한 줄 최대 4개.
             *
             * index
             *
             * 0 1 2 3
             *
             * 4 5 6 7
             *
             * 8 9 10 11
             */

            int column =
                index %
                ColumnCount;

            int row =
                index /
                ColumnCount;

            /*
             * 왼쪽 → 오른쪽
             */
            float x =
                column *
                (
                    iconSize.x +
                    spacing.x
                );

            /*
             * 상태가 많아질수록
             * 위쪽으로 증가.
             *
             * row 0
             * → 가장 아래
             *
             * row 1
             * → 그 위
             *
             * row 2
             * → 가장 위
             */
            float y =
                row *
                (
                    iconSize.y +
                    spacing.y
                );

            rect.anchorMin =
                new Vector2(
                    0f,
                    0f
                );

            rect.anchorMax =
                new Vector2(
                    0f,
                    0f
                );

            rect.pivot =
                new Vector2(
                    0f,
                    0f
                );

            rect.sizeDelta =
                iconSize;

            rect.anchoredPosition =
                new Vector2(
                    x,
                    y
                );
        }

        /*
         * 아이콘 표시만 초기화.
         *
         * targetPiece /
         * statusEffectManager 참조는 유지한다.
         */
        private void ClearIconsOnly()
        {
            for (int i = 0;
                 i < iconPool.Count;
                 i++)
            {
                iconPool[i]?
                    .Clear();
            }
        }

        /*
         * 완전 초기화.
         */
        public void Clear()
        {
            buffs.Clear();
            debuffs.Clear();

            ClearIconsOnly();
        }

        /*
         * UI가 Pool로 반환될 때 사용.
         */
        public void Release()
        {
            Unsubscribe();

            targetPiece =
                null;

            statusEffectManager =
                null;

            buffs.Clear();
            debuffs.Clear();

            ClearIconsOnly();
        }
    }
}