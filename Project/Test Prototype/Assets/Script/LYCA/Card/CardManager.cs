using CommandCard;
using UnityEngine;

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

        UnlockPlayerColumn();
    }
} // 引用部分

public partial class CardManager
{
    [Header("Buttons")]
    public GameObject playCardButton;
    public GameObject endCommandButton;
    public GameObject rebuildButton;
} // UI引用

public partial class CardManager
{
    [Header("Circumstance")]
    public bool isPlayerTurn = true;
    public int cardHasPlayed = 0;

    public void LockPlayerColumn(bool isTurnOver = false)
    {
        playerColumn.SetLockTrue();
        rebuildButton.SetActive(false);
        playCardButton.SetActive(false);
        if (isTurnOver) endCommandButton.SetActive(false);
        else endCommandButton.SetActive(true);
    }

    public void UnlockPlayerColumn()
    {
        playerColumn.SetLockFalse();
        rebuildButton.SetActive(true);
        playCardButton.SetActive(true);
        endCommandButton.SetActive(false);
    }

    public void LockColumn()
    {
        if (isPlayerTurn) LockPlayerColumn();
    }

    public void UnLockColumn()
    {
        if (isPlayerTurn) UnlockPlayerColumn();
    }
} // 行动部分