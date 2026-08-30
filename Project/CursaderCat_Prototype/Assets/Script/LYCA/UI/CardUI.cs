using CommandCard;
using System;
using TMPro;
using Unit;
using UnityEngine;

public partial class CardUI : MonoBehaviour, IInitialiazer
{
    [Header("Handcard Infomation")]
    public CardColumn playerColumn;
    public CardColumn enemyColumn;

    [Header("Component")]
    public TextMeshProUGUI playerDeckNum1;
    public TextMeshProUGUI playerDeckNum2;
    public TextMeshProUGUI enemyDeckNum1;
    public TextMeshProUGUI enemyDeckNum2;
    public TextMeshProUGUI playerCost;
    public TextMeshProUGUI enemyCost;
    public TextMeshProUGUI playerPlayNum;
    public TextMeshProUGUI enemyPlayNum;

    private void Awake()
    {
        playerColumn.DeckDrawEvent.AddListener(UpdatePlayerDeckNumber);
        playerColumn.DeckRebuildEvent.AddListener(UpdatePlayerDeckNumber);
        enemyColumn.DeckDrawEvent.AddListener(UpdateEnemyDeckNumber);
        enemyColumn.DeckRebuildEvent.AddListener(UpdateEnemyDeckNumber);

        CardManager.Instance.LockButtonsEvent.AddListener(LockButtons);
        CardManager.Instance.UnlockButtonsEvent.AddListener(UnlockButtons);

        UnitCommandManager.Instance.ActionSequenceStart.AddListener(SequenceStart);
        UnitCommandManager.Instance.ActionSequenceEnd.AddListener(SequenceEnd);

        //Central.Instance.ActionEnd.AddListener(CloseSkipCommandButton);
        Central.Instance.CostUpdateEvent.AddListener(UpdatePlayerCost);
        Central.Instance.PlayNumUpdateEvent.AddListener(UpdatePlayNum);
        OpenSkipButtonAction += OpenSkipCommandButton;
    }

    private void Start()
    {
        CloseSkipCommandButton();
        UpdatePlayerCost(false);
        UpdatePlayerCost(true);
    }

    private void UpdatePlayerDeckNumber()
    {
        playerDeckNum1.text = playerColumn.currentCardDeck.Count.ToString();
        playerDeckNum2.text = playerColumn.cardDeck.Count.ToString();
    }

    private void UpdateEnemyDeckNumber()
    {
        enemyDeckNum1.text = enemyColumn.currentCardDeck.Count.ToString();
        enemyDeckNum2.text = enemyColumn.cardDeck.Count.ToString();
    }

    private void UpdatePlayerCost(bool _isPlayer)
    {
        if (_isPlayer)
            playerCost.text = "C" + playerColumn.reinforceCost.ToString();
        else enemyCost.text = "C" + enemyColumn.reinforceCost.ToString();
    }

    private void UpdatePlayNum(bool _isPlayer)
    {
        if (_isPlayer)
            playerPlayNum.text =
                "(" + playerColumn.playCount.ToString() + "/" + playerColumn.playMax.ToString() + ")";
        else enemyPlayNum.text =
                "(" + enemyColumn.playCount.ToString() + "/" + enemyColumn.playMax.ToString() + ")";
    }

    private void ChangeEnemyDeckNumber() { } // 暂时不需要

    public void Initialize()
    {
        UnlockButtons();
    }
} // 卡片部分UI

public partial class CardUI
{
    [Header("Buttons")]
    public GameObject rebuildButton;
    //public GameObject playCardButton;
    public GameObject nextTurnButton;
    public GameObject endCommandButton;
    public GameObject skipCommandButton;

    [Header("Other")]
    public static Action<bool> OpenSkipButtonAction;
    public bool needShowEndCommandButton = true;

    public void SequenceStart()
    {
        endCommandButton.SetActive(false);
    }

    public void SequenceEnd()
    {
        endCommandButton.SetActive(needShowEndCommandButton);
    } // 行动序列结束时判断是否需要显示提前结束指令的按钮（判断是否所有单位执行完毕）

    public void CloseSkipCommandButton() => skipCommandButton.SetActive(false);

    public void OpenSkipCommandButton(bool _tmp) => skipCommandButton.SetActive(_tmp);

    public void LockButtons(bool _isTurnOver = false)
    {
        rebuildButton.SetActive(false);
        //playCardButton.SetActive(false);
        nextTurnButton.SetActive(false);
        if (_isTurnOver)
        {
            endCommandButton.SetActive(false);
            needShowEndCommandButton = false;
        }
        else
        {
            endCommandButton.SetActive(true);
            needShowEndCommandButton = true;
        }
    }

    public void UnlockButtons()
    {
        rebuildButton.SetActive(true);
        //playCardButton.SetActive(true);
        nextTurnButton.SetActive(true);
        endCommandButton.SetActive(false);
    }
} //按钮部分
