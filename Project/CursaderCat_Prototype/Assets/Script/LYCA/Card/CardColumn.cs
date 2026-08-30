using System;
using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;
using UnityEngine.Events;

namespace CommandCard
{
    public partial class CardColumn : MonoBehaviour
    {
        [Header("List")]
        public List<CardInfo> cardDeck = new(); // 可以随意插入新牌，原本牌组信息储存在SO里
        public List<CardInfo> currentCardDeck = new();
        public List<CardController> handCard = new();
        public List<CardInfo> discardPile = new();
        public GameObject cardPrefab;

        [Header("Parameter")]
        public CardController selectedCard;
        public int maxHandCount = 5;
        public Vector2 originPos;
        public bool isPlayer = true; // 是否为玩家牌列

        [Header("Component")]
        public Transform cardParent;

        [Header("Unity Event")]
        public UnityEvent DeckDrawEvent;
        public UnityEvent DeckRebuildEvent;

        public void SetUp()
        {
            originPos = transform.localPosition;
        }

        public void RegisterDeck(CardDeckInfo _deck)
        {
            cardDeck = new(_deck.cardList);
            currentCardDeck = new(cardDeck);
            ClearHand();
            ShuffleDeck();
            DrawCardToMax();

            TurnBegin();
            SetCost(_deck.originCost);
            Central.Instance.TurnBeginEvent.AddListener(TurnBegin);
        } // 初始化牌列，设置牌组信息，清空手牌，洗牌并抽取至最大手牌数

        public void TurnBegin()
        {
            if (Central.isPlayerTurn != isPlayer)
            {
                IsLock = true; // 如果不是玩家回合，则锁定牌列，避免进行其他操作(象征性写一下)
                GetDown(); // 如果不是玩家回合，则将牌列位置下移
            }
            else
            {
                IsLock = false; // 如果是玩家回合，则解锁牌列，允许进行操作
                ResetLocalPosition(); // 如果是玩家回合，则重置牌列位置
            }
        }

        public void PlaySelectedCard()
        {
            if (selectedCard == null) return;
            if (CardManager.Instance.GetPlayNum() >= CardManager.Instance.GetPlayMax()) return;
            selectedCard.CardEffect();
            Discard(selectedCard);
        } // 玩家选中卡牌后，执行卡牌效果并将其弃置

        public void EndCommand()
        {
            if (UnitCommandManager.isUnitActing) return;
            Central.Instance.ActionEndEarly?.Invoke();
        } // 结束指令，若单位正在行动则不执行

        public void SkipCommand()
        {
            //if (UnitCommandManager.isUnitActing) return;
            Central.Instance.SkipEvent?.Invoke();
        }

        public void NextTurn()
        {
            if (UnitCommandManager.isUnitActing) return;
            Central.Instance.NextTurnStart?.Invoke();
            StartCoroutine(Enumerator());

            AddCost(costAddition); // 每回合增加费用
            SetPlayCount(0); // 重置已打出卡牌数
            UnitManager.Instance.units.ForEach(unit =>
            {
                if (unit.isPlayer != isPlayer) return;
                unit.unitElement.ResetTactic(); // 重置战术调整移动力
            });
            UnitManager.Instance.UnlockUnit(isPlayer); // 解锁单位，允许行动

            IEnumerator Enumerator()
            {
                IsLock = true; // 锁定牌列，避免在下一回合开始时进行其他操作
                bool isActing = false;
                //yield return new WaitForSeconds(0.5f); // 等待一段时间以显示当前回合结束
                NextTurnAction(() => isActing = true, () => isActing = false);
                yield return new WaitUntil(() => !isActing); // 等待直到当前回合结束
                Central.Instance.TurnBeginEvent?.Invoke();
            }
        } // 进入下一回合，若单位正在行动则不执行

        private void NextTurnAction(Action _Begin, Action _End)
        {
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                _Begin?.Invoke();
                ShuffleHandBackIntoDeck();
                DrawCardToMax();
                yield return new WaitUntil(() => !IsLock);
                IsLock = true; // 锁定牌列，避免在下一回合开始时进行其他操作
                _End?.Invoke();
            }
        }

        private void GetDown()
        {
            transform.localPosition =
                originPos + Vector2.down * (transform.rotation.z == 0 ? 50 : -50);
        } // 将牌列位置下移，表示处于锁定状态（如敌方回合时）

        private void ResetLocalPosition() => transform.localPosition = originPos;
    }

    public partial class CardColumn
    {
        public void ShuffleDeck()
        {
            if (currentCardDeck.Count <= 1) return;
            for (int i = 0; i < currentCardDeck.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, currentCardDeck.Count);
                (currentCardDeck[i], currentCardDeck[randomIndex]) =
                    (currentCardDeck[randomIndex], currentCardDeck[i]);
            }
        }

        public void ClearHand()
        {
            foreach (var card in handCard) Destroy(card.gameObject);
            handCard.Clear();
        }

        public void Discard(CardController _card)
        {
            selectedCard = null;
            discardPile.Add(_card.cardInfo);
            handCard.Remove(_card);
            _card.DestroyCard();
        }

        public void DrawCardToMax()
        {
            StartCoroutine(Enumerator());

            IEnumerator Enumerator()
            {
                IsLock = true;
                while (handCard.Count < maxHandCount && currentCardDeck.Count > 0)
                {
                    DrawCard();
                    yield return new WaitForSeconds(0.1f);
                }
                IsLock = false;
            }
        }

        public void DrawCard()
        {
            if (currentCardDeck.Count == 0) return;
            if (handCard.Count >= maxHandCount) return;
            CardInfo cardInfo = currentCardDeck[0];
            currentCardDeck.RemoveAt(0);
            GameObject cardObj = Instantiate(cardPrefab, cardParent);
            CardController cardController = cardObj.transform.GetChild(0).GetComponent<CardController>();
            cardController.SetUp(cardInfo, SelectCard, this);
            handCard.Add(cardController);
            DeckDrawEvent?.Invoke();
        }

        public void ReconstructDeck()
        {
            if (CardManager.Instance.GetPlayNum() >= CardManager.Instance.GetPlayMax()) return;
            StartCoroutine(Enumerator());

            IEnumerator Enumerator()
            {
                IsLock = true;
                List<CardController> tmpHandCard = new(handCard);
                for (int i = 0; i < tmpHandCard.Count; i++)
                {
                    Discard(tmpHandCard[i]);
                    yield return new WaitForSeconds(0.15f);
                }
                tmpHandCard.Clear();
                handCard.Clear();

                currentCardDeck.AddRange(discardPile);
                discardPile.Clear();
                ShuffleDeck();
                DrawCardToMax();
                DeckRebuildEvent?.Invoke();
                IsLock = false;

                AddPlayCount(playMax);
            }
        }// 将手牌和弃牌堆的卡片重新放回牌堆并洗牌（先将手牌逐一弃牌）

        public void SelectCard(CardController _card, bool _isSelected)
        {
            if (_isSelected)
            {
                if (selectedCard != null)
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

        public void ShuffleHandBackIntoDeck()
        {
            if (handCard.Count == 0) return;
            foreach (var card in handCard)
            {
                currentCardDeck.Add(card.cardInfo);
                card.DestroyCard();
            }
            handCard.Clear();
            ShuffleDeck();
            DrawCardToMax();
        }
    } // 功能性函数

    public partial class CardColumn
    {
        public bool IsLock { get; private set; } = true; // 锁定状态：当牌列处于锁定状态时，无法选择卡牌或进行其他操作（如敌方回合时）

        public void SetLockTrue()
        {
            IsLock = true;
            GetDown();
        }

        public void SetLockFalse()
        {
            IsLock = false;
            ResetLocalPosition();
        }
    }

    public partial class CardColumn
    {
        [Header("Cost to Reinforce")]
        public int reinforceCost = 3; // 强化所需费用
        public int maxCost = 10; // 最大费用
        public int costAddition = 1; // 每回合增加的费用，玩家标准值为1，敌人标准值为2

        [Header("Card Play Maximum")]
        public int playMax = 3; // 每回合最大可打出卡牌数
        public int playCount = 0; // 已打出卡牌数

        private void SetCost(int _tmp)
        {
            reinforceCost = Mathf.Clamp(_tmp, 0, maxCost);
            Central.Instance.CostUpdateEvent?.Invoke(isPlayer);
        }

        public void AddCost(int _tmp)
        {
            reinforceCost = Mathf.Min(reinforceCost + _tmp, maxCost);
            Central.Instance.CostUpdateEvent?.Invoke(isPlayer);
        }

        public void ReduceCost(int _tmp)
        {
            reinforceCost = Mathf.Max(reinforceCost - _tmp, 0);
            Central.Instance.CostUpdateEvent?.Invoke(isPlayer);
        }

        private void SetPlayCount(int _tmp)
        {
            playCount = Mathf.Clamp(_tmp, 0, playMax);
            Central.Instance.PlayNumUpdateEvent?.Invoke(isPlayer);
        }

        public void AddPlayCount(int _tmp)
        {
            playCount = Mathf.Min(playCount + _tmp, playMax);
            Central.Instance.PlayNumUpdateEvent?.Invoke(isPlayer);
        }
    }
}