using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CommandCard
{
    public class CardController : MonoBehaviour, IPointerClickHandler
    {
        [Header("Play Info")]
        public CardInfo cardInfo;
        public float offsetY = 20f;

        [Header("Component")]
        public CardColumn column;
        public Transform parent;
        public Image image;

        [Header("Action")]
        public UnityEvent<CardController, bool> SelectEvent = new();

        [Header("Boolean")]
        public bool isSelected = false;

        public void SetUp(
            CardInfo _info, UnityAction<CardController, bool> SelectCard,
            CardColumn _column
            )
        {
            cardInfo = _info;
            column = _column;
            parent = transform.parent;
            image.sprite = _info.cardSprite;
            SelectEvent.AddListener(SelectCard);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (column.IsLock) return;
            isSelected = !isSelected;
            SelectEvent.Invoke(this, isSelected);
            if (isSelected) transform.localPosition = new(0, offsetY, 0);
            else transform.localPosition = new(0, 0, 0);
        }

        public void UnSelectCard()
        {
            isSelected = false;
            transform.localPosition = new(0, 0, 0);
        }

        public void DestroyCard()
        {
            Destroy(parent.gameObject);
        }

        public void CardEffect()
        {
            switch (cardInfo.cardId)
            {
                case 0:
                    CommandCard.CardEffect.Instance.MoveEnter();
                    break;
                case 1:
                    CommandCard.CardEffect.Instance.ReinforceEnter();
                    break;
                case 2:
                    CommandCard.CardEffect.Instance.MeleeActionEnter();
                    break;
                case 3:
                    CommandCard.CardEffect.Instance.RangedActionEnter();
                    break;
            }
        }
    }
}
