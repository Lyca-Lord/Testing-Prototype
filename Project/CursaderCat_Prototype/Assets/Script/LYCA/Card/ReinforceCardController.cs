using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CommandCard
{
    public class ReinforceCardController : MonoBehaviour, IPointerClickHandler
    {
        [Header("Play Info")]
        public UnitInfo unitInfo;
        public float offsetY = 20f;

        [Header("Component")]
        public Button useButton;
        public ReinforceCardColumn column;
        public Transform parent;
        public Image image;

        [Header("Text Component")]
        public TextMeshProUGUI title;
        public TextMeshProUGUI description;
        public TextMeshProUGUI costText;
        public TextMeshProUGUI maxiText;

        [Header("Action")]
        public UnityEvent<ReinforceCardController, bool> SelectEvent = new();
        public UnityEvent<ReinforceCardController> UseCardEvent = new();

        [Header("Boolean")]
        public bool isSelected = false;

        // 与 CardController 相似的 SetUp：接收卡片信息、要召唤的 UnitInfo、选择回调和所属列
        public void SetUp(
            UnitInfo _unitInfo,
            UnityAction<ReinforceCardController, bool> SelectCard,
            ReinforceCardColumn _column
            )
        {
            unitInfo = _unitInfo;
            column = _column;
            parent = transform.parent;
            costText.text = "C" + _unitInfo.cost.ToString();
            maxiText.text = "M" + _unitInfo.maxi.ToString();

            if (image != null) image.sprite = _unitInfo.unitSprite;
            if (SelectCard != null) SelectEvent.AddListener(SelectCard);

            useButton.onClick.AddListener(
                CardManager.Instance.PlaySelectedCard
                );

            title.text = _unitInfo.titile;
            description.text = _unitInfo.description;
        }

        public void PickThisCard()
        {
            if (column.IsLock) return;
            if (column.isPlayer != Central.isPlayerTurn) return;

            isSelected = !isSelected;
            SelectEvent.Invoke(this, isSelected);
            if (isSelected)
            {
                useButton.gameObject.SetActive(true);
                transform.localPosition = new(0, offsetY, 0);
            }
            else
            {
                useButton.gameObject.SetActive(false);
                transform.localPosition = new(0, 0, 0);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            PickThisCard();
        }

        public void UnSelectCard()
        {
            useButton.gameObject.SetActive(false);
            isSelected = false;
            transform.localPosition = new(0, 0, 0);
        }

        public void DestroyCard()
        {
            Destroy(parent.gameObject);
        }

        // 使用卡牌：先把要增援的 UnitInfo 登记到 UnitManager，再进入增援流程
        public void CardEffect()
        {
            if (!CheckCost()) return;
            CommandText.Instance.UpdateCommandText("增援");
            if (unitInfo != null && UnitManager.Instance != null)
            {
                UnitManager.Instance.SetUnit(
                    unitInfo,
                    column.isPlayer,
                    !column.isPlayer,
                    false
                    );
            }

            CommandCard.CardEffect.Instance.ReinforceEnter(column.isPlayer);
            UseCardEvent?.Invoke(this);
        }

        private bool CheckCost()
        {
            int total = CardManager.Instance.GetCost(column.isPlayer);
            return total >= unitInfo.cost;
        }
    }
}