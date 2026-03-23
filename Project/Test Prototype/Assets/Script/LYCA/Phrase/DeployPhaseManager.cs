using CommandCard;
using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

public class DeployPhaseManager : MonoBehaviour, IInitialiazer
{
    public static DeployPhaseManager Instance;

    [Header("Config")]
    public UnitBox playerUnitBox;  // 玩家初始棋子来源
    public UnitBox enemyUnitBox;   // 敌人初始棋子来源
    public int enemyDeployCount = 3; // 敌人最多部署数量

    private Queue<UnitInfo> _playerQueue = new();
    private Queue<UnitInfo> _enemyQueue = new();
    private bool _isPlayerDeploying; // 当前是否在处理玩家部署

    public void Initialize()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Awake()
    {
        playerUnitBox = CardManager.Instance.playerUnitBox;
        enemyUnitBox = CardManager.Instance.enemyUnitBox;
    }

    public void StartDeployPhase()
    {
        // 锁定正常卡牌列，防止玩家误操作
        CardManager.Instance.playerColumn.SetLockTrue();
        CardManager.Instance.enemyColumn.SetLockTrue();

        // 构建玩家队列：Box 中每个 UnitInfo 各一个
        _playerQueue.Clear();
        foreach (var info in playerUnitBox.unitInfos)
            _playerQueue.Enqueue(info);

        // 构建敌人队列：随机打乱后取前 N 个
        _enemyQueue.Clear();
        //List<UnitInfo> enemyPool = new(enemyUnitBox.unitInfos);
        //Shuffle(enemyPool);
        //int count = Mathf.Min(enemyDeployCount, enemyPool.Count);
        //for (int i = 0; i < count; i++)
        //    _enemyQueue.Enqueue(enemyPool[i]);
        foreach(var info in enemyUnitBox.unitInfos)
            _enemyQueue.Enqueue(info);

        BeginPlayerDeploy();
    }
    private void BeginPlayerDeploy()
    {
        if (_playerQueue.Count == 0)
        {
            // 玩家部署完毕，切换到敌人部署
            BeginEnemyDeploy();
            return;
        }

        _isPlayerDeploying = true;
        UnitInfo next = _playerQueue.Dequeue();

        UnitManager.Instance.SetUnit(
            next,
            true,
            false,
            true,
            OnOneUnitDeployed
            );

        CardEffect.Instance.DeployEnter(true);
        //CardEffect.Instance.DeployEnter(true, OnOneUnitDeployed);
    }

    private void BeginEnemyDeploy()
    {
        if (_enemyQueue.Count == 0)
        {
            EndDeployPhase();
            return;
        }

        _isPlayerDeploying = false;
        UnitInfo next = _enemyQueue.Dequeue();
        UnitManager.Instance.unitInfoWaitForReinforce = next;

        UnitManager.Instance.SetUnit(
            next,
            false,
            false,
            true,
            OnOneUnitDeployed
            );

        CardEffect.Instance.DeployEnter(false);
    }

    // ─────────────────────────────────────────────
    // 每放置一个单位后的回调（由 CardEffect.DeployEnter 在结束时调用）
    // ─────────────────────────────────────────────

    private void OnOneUnitDeployed()
    {
        if (_isPlayerDeploying)
            BeginPlayerDeploy(); // 继续下一个玩家棋子
        else
            BeginEnemyDeploy(); // 继续下一个敌人棋子
    }

    private void EndDeployPhase()
    {
        // 解锁正常卡牌列，允许正式游戏开始
        CardManager.Instance.playerColumn.SetLockFalse();
        CardManager.Instance.enemyColumn.SetLockFalse();
        Central.Instance.DeployingPhaseEnd?.Invoke(); // 通知外部（如 GameManager）正式游戏开始
        Debug.Log("部署阶段结束，正式游戏开始");
    }

    // ─────────────────────────────────────────────
    // 工具函数
    // ─────────────────────────────────────────────

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}
