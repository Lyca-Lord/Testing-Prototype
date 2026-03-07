using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;
using UnityEngine.Events;

namespace CommandCard
{
    public class ReinforceCardColumn : MonoBehaviour
    {
        [Header("List")]
        public List<ReinforceCardController> cardDeck = new(); // 存放所有增援卡牌控制器（对应 CardColumn.handCard）
        public UnitBox unitBox;

        [Header("Prefab")]
        public GameObject reinforcePrefab;

        [Header("Parameter")]
        public ReinforceCardController selectedCard;
        public bool isPlayer = true; // 是否为玩家增援列（对应 CardColumn.isPlayer）

        [Header("Component")]
        public Transform cardParent;

        // 锁定状态：当处于锁定时，无法选择卡牌（对应 CardColumn.IsLock）
        public bool IsLock { get; private set; } = false;

        // ─────────────────────────────────────────────
        // 初始化
        // ─────────────────────────────────────────────

        public void SetUp(UnitBox _box)
        {
            unitBox = _box;
            ClearColumn();
            BuildColumn(); // 根据 UnitBox 中的 UnitInfo 列表生成所有增援卡
            Hide();
        }

        // ─────────────────────────────────────────────
        // 显隐开关（UseCardEvent 唤起时由外部调用关闭）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 显示增援牌列
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            CardManager.Instance.isOpenBox = true; // 打开牌列时将 isOpenBox 设为 true
        }

        /// <summary>
        /// 隐藏增援牌列（UseCardEvent 唤起时调用）
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            selectedCard = null; // 清除选中状态，避免残留引用
            CardManager.Instance.isOpenBox = false; // 关闭牌列时将 isOpenBox 设为 false
        }

        // ─────────────────────────────────────────────
        // 锁定控制（对应 CardColumn.SetLockTrue / SetLockFalse）
        // ─────────────────────────────────────────────

        public void SetLockTrue() => IsLock = true;

        public void SetLockFalse() => IsLock = false;

        // ─────────────────────────────────────────────
        // 选卡逻辑（对应 CardColumn.SelectCard）
        // ─────────────────────────────────────────────

        /// <summary>
        /// 选中/取消选中某张增援卡的回调 
        /// </summary>
        public void SelectCard(ReinforceCardController _card, bool _isSelected)
        {
            if (_isSelected)
            {
                // 若已有其他卡被选中，先取消它
                if (selectedCard != null && selectedCard != _card)
                {
                    selectedCard.UnSelectCard();
                    selectedCard = null;
                }
                selectedCard = _card;
            }
            else
            {
                if (selectedCard == _card) selectedCard = null;
            }
        }

        // ─────────────────────────────────────────────
        // 使用选中的卡牌
        // ─────────────────────────────────────────────

        /// <summary>
        /// 使用当前选中的增援卡：执行卡牌效果（内部会触发 UseCardEvent，再由 UseCardEvent 关闭牌列）
        /// </summary>
        public void PlaySelectedCard()
        {
            if (selectedCard == null) return;
            if (IsLock) return;

            ReinforceCardController card = selectedCard;
            selectedCard = null;

            // UseCardEvent 已在 BuildColumn 时绑定到 OnCardUsed，触发后自动隐藏牌列
            card.UnSelectCard();
            card.CardEffect();
        }

        // ─────────────────────────────────────────────
        // UseCardEvent 回调：隐藏牌列
        // ─────────────────────────────────────────────

        /// <summary>
        /// 当任意 ReinforceCardController 的 UseCardEvent 触发时，隐藏增援牌列
        /// </summary>
        private void OnCardUsed(ReinforceCardController _card)
        {
            Hide();
        }

        // ─────────────────────────────────────────────
        // 牌列构建与清理
        // ─────────────────────────────────────────────

        /// <summary>
        /// 根据 UnitBox 中的 UnitInfo 列表生成所有增援卡牌
        /// </summary>
        private void BuildColumn()
        {
            if (unitBox == null) return;
            foreach (UnitInfo info in unitBox.unitInfos)
            {
                SpawnCard(info);
            }
        }

        /// <summary>
        /// 实例化单张增援卡并完成初始化绑定（对应 CardColumn.DrawCard）
        /// </summary>
        private void SpawnCard(UnitInfo _info)
        {
            if (reinforcePrefab == null) return;
            GameObject cardObj = Instantiate(reinforcePrefab, cardParent);
            ReinforceCardController controller = cardObj.GetComponentInChildren<ReinforceCardController>();
            if (controller == null) return;

            controller.SetUp(_info, SelectCard, this);
            controller.UseCardEvent.AddListener(OnCardUsed); // UseCardEvent 触发时关闭牌列
            cardDeck.Add(controller);
        }

        /// <summary>
        /// 清理当前牌列中所有卡牌（对应 CardColumn.ClearHand）
        /// </summary>
        private void ClearColumn()
        {
            foreach (var card in cardDeck)
            {
                if (card != null) card.DestroyCard();
            }
            cardDeck.Clear();
            selectedCard = null;
        }
    }
}