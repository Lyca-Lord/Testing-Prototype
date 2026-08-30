using CommandCard;
using Unit;
using UnityEngine;
using UnityEngine.Events;

public partial class CardManager : MonoBehaviour, IInitialiazer
{
    public static CardManager Instance { get; private set; }

    [Header("Deck")]
    public CardDeckInfo playerDeck;
    public CardColumn playerColumn;
    public CardDeckInfo enemyDeck;
    public CardColumn enemyColumn;

    [Header("ReinforceDeck")]
    public ReinforceCardColumn playerReinforceColumn;
    public ReinforceCardColumn enemyReinforceColumn;
    public UnitBox playerUnitBox;
    public UnitBox enemyUnitBox;

    private void RegisterInstance()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    public void Initialize()
    {
        RegisterInstance();
        playerColumn.SetUp();
        if (enemyColumn != null) enemyColumn.SetUp();
    }

    public void SetUpCardAndBox()
    {
        playerUnitBox = Central.Instance.levelInfo.playerBox;
        enemyUnitBox = Central.Instance.levelInfo.enemyBox;

        playerColumn.RegisterDeck(playerDeck);
        if (enemyColumn != null) enemyColumn.RegisterDeck(enemyDeck);

        playerReinforceColumn.SetUp(playerUnitBox);
        if (enemyReinforceColumn != null) enemyReinforceColumn.SetUp(enemyUnitBox);
    }
} // 引用部分

public partial class CardManager
{
    [Header("Circumstance")]
    public UnityEvent<bool> LockButtonsEvent = new();
    public UnityEvent UnlockButtonsEvent = new();

    public void LockPlayerColumn(bool isTurnOver = false)
    {
        if (Central.isPlayerTurn)
            playerColumn.SetLockTrue();
        else enemyColumn.SetLockTrue();
        LockButtonsEvent.Invoke(isTurnOver);
    }

    public void UnlockPlayerColumn()
    {
        if (Central.isPlayerTurn)
            playerColumn.SetLockFalse();
        else enemyColumn.SetLockFalse();
        UnlockButtonsEvent.Invoke();
    }
} // 行动部分

public partial class CardManager
{
    public bool isOpenBox = false;
    // 将在ReinforceCardColumn.Show()时设为true，在ReinforceCardColumn.Hide()时设为false

    public void OpenReinforceBox()
    {
        if (Central.isPlayerTurn)
        {
            if (!isOpenBox) playerReinforceColumn.Show();
            else playerReinforceColumn.Hide();
        }
        else
        {
            if (!isOpenBox) enemyReinforceColumn.Show();
            else enemyReinforceColumn.Hide();
        }
    }

    public void PlaySelectedCard()
    {
        if (isOpenBox)
        {
            if (Central.isPlayerTurn)
                playerReinforceColumn.PlaySelectedCard();
            else enemyReinforceColumn.PlaySelectedCard();
        }
        else
        {
            if (Central.isPlayerTurn)
                playerColumn.PlaySelectedCard();
            else enemyColumn.PlaySelectedCard();
        }
    }

    public void NextTurn()
    {
        if (Central.isPlayerTurn)
            playerColumn.NextTurn();
        else enemyColumn.NextTurn();
    }

    public void EndCommand()
    {
        if (Central.isPlayerTurn)
            playerColumn.EndCommand();
        else enemyColumn.EndCommand();
    }

    public void SkipCommand()
    {
        if (Central.isPlayerTurn)
            playerColumn.SkipCommand();
        else enemyColumn.SkipCommand();
    }

    public void RebuildDeck()
    {
        if (Central.isPlayerTurn)
            playerColumn.ReconstructDeck();
        else enemyColumn.ReconstructDeck();
    }

    public void IntoTactic()
    {
        CardEffect.Instance.TacticEnter(Central.isPlayerTurn);
    }
} // 卡牌效果部分

public partial class CardManager
{
    public void AddCost(bool _isPlayer, int _cost)
    {
        if (_isPlayer) playerColumn.AddCost(_cost);
        else enemyColumn.AddCost(_cost);
    }

    public int GetCost(bool _isPlayer)
        => _isPlayer ? playerColumn.reinforceCost : enemyColumn.reinforceCost;

    public void ReduceCost(int _tmp)
    {
        if (Central.isPlayerTurn) playerColumn.ReduceCost(_tmp);
        else enemyColumn.ReduceCost(_tmp);
    }

    public void AddPlayNum(int _tmp)
    {
        if (Central.isPlayerTurn) playerColumn.AddPlayCount(_tmp);
        else enemyColumn.AddPlayCount(_tmp);
    }

    public int GetPlayNum() 
        => Central.isPlayerTurn ? playerColumn.playCount : enemyColumn.playCount;

    public int GetPlayMax() 
        => Central.isPlayerTurn ? playerColumn.playMax : enemyColumn.playMax;
} // 增加增援费用
