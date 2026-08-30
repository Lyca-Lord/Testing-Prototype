using System.Collections.Generic;
using TMPro;
using Unit;
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
        public AudioSource clickAudio;
        public CardColumn column;
        public Button useButton;
        public Transform parent;
        public Image image;

        [Header("Text Component")]
        public TextMeshProUGUI title;
        public TextMeshProUGUI description;

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

            useButton.onClick.AddListener(
                CardManager.Instance.PlaySelectedCard
                );

            title.text = _info.title;
            description.text = _info.description;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PickCard();
        }

        public void UnSelectCard()
        {
            useButton.gameObject.SetActive(false);
            isSelected = false;
            transform.localPosition = new(0, 0, 0);

            CloseAllCardSelectIndicator();
        }

        public void DestroyCard()
        {
            Destroy(parent.gameObject);
        }

        private void CloseAllCardSelectIndicator()
        {
            List<Units> units = UnitManager.Instance.units;
            foreach (Units unit in units)
            {
                unit.CloseCardSelectIndicator();
            }
        }

        public void ShowCardSelectIndicator()
        {
            List<Units> units = UnitManager.Instance.units;
            Debug.LogWarning("ShowCardSelectIndicator: " + cardInfo.cardId);

            switch (cardInfo.cardId)
            {
                case 0:
                    foreach (Units unit in units)
                    {
                        if (unit.isPlayer == column.isPlayer && unit.unitElement.CheckTraits("Trait_CanMove"))
                            unit.OpenCardSelectIndicator();
                    }
                    break;
                case 1:
                    foreach (Units unit in units)
                    {
                        if (unit.isPlayer == column.isPlayer && unit.unitElement.CheckTraits("Trait_Flag"))
                            unit.OpenCardSelectIndicator();
                    }
                    break;
                case 2:
                    foreach (Units unit in units)
                    {
                        if (unit.isPlayer == column.isPlayer && unit.unitElement.CheckTraits("Trait_CanMelee"))
                            unit.OpenCardSelectIndicator();
                    }
                    break;
                case 3:
                    foreach (Units unit in units)
                    {
                        if (unit.isPlayer == column.isPlayer && unit.unitElement.CheckTraits("Trait_CanRanged"))
                            unit.OpenCardSelectIndicator();
                    }
                    break;
            }
        }

        public void CardEffect()
        {
            CloseAllCardSelectIndicator();
            switch (cardInfo.cardId)
            {
                case 0:
                    CommandText.Instance.UpdateCommandText("移动");
                    CommandCard.CardEffect.Instance.MoveEnter(column.isPlayer);
                    break;
                case 1:
                    CommandText.Instance.UpdateCommandText("征召");
                    CardManager.Instance.AddCost(column.isPlayer, 2);
                    break;
                case 2:
                    CommandText.Instance.UpdateCommandText("近战");
                    CommandCard.CardEffect.Instance.MeleeActionEnter(column.isPlayer);
                    break;
                case 3:
                    CommandText.Instance.UpdateCommandText("远程");
                    CommandCard.CardEffect.Instance.RangedActionEnter(column.isPlayer);
                    break;
            }
            CardManager.Instance.AddPlayNum(1);
        }

        public void PickCard()
        {
            if (column.IsLock) return;
            if (column.isPlayer != Central.isPlayerTurn) return;

            clickAudio.Play();
            isSelected = !isSelected;
            SelectEvent.Invoke(this, isSelected);

            if (isSelected)
            {
                useButton.gameObject.SetActive(true);
                transform.localPosition = new(0, offsetY, 0);
                ShowCardSelectIndicator();
            }
            else
            {
                useButton.gameObject.SetActive(false);
                transform.localPosition = new(0, 0, 0);
                CloseAllCardSelectIndicator();
            }
        }
    }
}
