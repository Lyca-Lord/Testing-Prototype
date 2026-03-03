using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CommandCard;
using Unit;
using TMPro;

public partial class CardUI : MonoBehaviour, IInitialiazer
{
    [Header("Handcard Infomation")]
    public CardColumn playerColumn;
    public CardColumn enemyColumn;

    [Header("Component")]
    public TextMeshProUGUI playerDeckNum1;
    public TextMeshProUGUI playerDeckNum2;

    private void Awake()
    {
        playerColumn.DeckDrawEvent.AddListener(ChangePlayerDeckNumber);
        playerColumn.DeckRebuildEvent.AddListener(ChangePlayerDeckNumber);
    }

    private void Start()
    {
        CardManager.Instance.LockButtonsEvent.AddListener(LockButtons);
        CardManager.Instance.UnlockButtonsEvent.AddListener(UnlockButtons);
    }

    private void ChangePlayerDeckNumber()
    {
        playerDeckNum1.text = playerColumn.currentCardDeck.Count.ToString();
        playerDeckNum2.text = playerColumn.cardDeck.Count.ToString();
    }

    private void changeEnemyDeckNumber() { } // 暂时不需要

    public void Initialize()
    {
        UnlockButtons();
    }
} // 卡片部分UI

public partial class CardUI {
    [Header("Buttons")]
    public GameObject rebuildButton;
    public GameObject playCardButton;
    public GameObject nextTurnButton;
    public GameObject endCommandButton;

    public void LockButtons(bool _isTurnOver = false)
    {
        rebuildButton.SetActive(false);
        playCardButton.SetActive(false);
        nextTurnButton.SetActive(false);
        if (_isTurnOver) endCommandButton.SetActive(false);
        else endCommandButton.SetActive(true);
    }

    public void UnlockButtons()
    {
        rebuildButton.SetActive(true);
        playCardButton.SetActive(true);
        nextTurnButton.SetActive(true);
        endCommandButton.SetActive(false);
    }
} //按钮部分
