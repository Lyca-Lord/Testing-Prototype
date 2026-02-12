using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

namespace CommandCard
{
    public partial class CardColumn : MonoBehaviour
    {
        [Header("List")]
        public List<CardInfo> cardDeck = new();
        public List<CardInfo> currentCardDeck = new();
        public List<CardController> handCard = new();
        public List<CardInfo> discardPile = new();
        public GameObject cardPrefab;

        [Header("Parameter")]
        public CardController selectedCard;
        public int maxHandCount = 5;
        public Vector2 originPos;

        [Header("Component")]
        public Transform cardParent;

        public void SetUp(CardDeckInfo _deck)
        {
            cardDeck = new(_deck.cardList);
            currentCardDeck = new(cardDeck);
            ClearHand();
            ShuffleDeck();
            DrawCardToMax();
            originPos = transform.localPosition;
        }

        public void PlaySelectedCard()
        {
            selectedCard.CardEffect();
            Discard(selectedCard);
        }

        public void EndCommand()
        {
            if (UnitCommandManager.isUnitActing) return;
            Central.Instance.ActionEndEarly?.Invoke();
        }

        private void GetDown()
        {
            transform.localPosition = originPos + Vector2.down * 50;
        }

        private void ResetLocalPosition() => transform.localPosition = originPos;
    }

    public partial class CardColumn
    {
        public void ShuffleDeck()
        {
            if (currentCardDeck.Count <= 1) return;
            for (int i = 0; i < currentCardDeck.Count; i++)
            {
                int randomIndex = Random.Range(i, currentCardDeck.Count);
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
                    yield return new WaitForSeconds(0.15f);
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
        }

        public void ReconstructDeck()
        {
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
                IsLock = false;
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
}