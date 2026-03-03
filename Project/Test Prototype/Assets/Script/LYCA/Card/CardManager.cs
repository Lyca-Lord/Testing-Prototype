using CommandCard;
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

    private void Awake()
    {
        // 单例唯一化：避免重复创建实例导致UnityEvent订阅累积
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject); // 跨场景持久化，避免场景切换时销毁
    }

    public void Initialize()
    {
        playerColumn.SetUp(playerDeck);
        if (enemyColumn != null) enemyColumn.SetUp(enemyDeck);
        //UnlockPlayerColumn();
    }
} // 引用部分

public partial class CardManager
{
    [Header("Circumstance")]
    public bool isPlayerTurn = true;
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
    public void PlaySelectedCard()
    {
        if (Central.isPlayerTurn)
            playerColumn.PlaySelectedCard();
        else enemyColumn.PlaySelectedCard();
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

    public void RebuildDeck()
    {
        if (Central.isPlayerTurn)
            playerColumn.ReconstructDeck();
        else enemyColumn.ReconstructDeck();
    }
} // 卡牌效果部分