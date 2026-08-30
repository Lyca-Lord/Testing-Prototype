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

    public void StartDeployPhase(LevelMapInfo _levelMap)
    {
        // 锁定正常卡牌列，防止玩家误操作
        CardManager.Instance.playerColumn.SetLockTrue();
        CardManager.Instance.enemyColumn.SetLockTrue();

        // 构建玩家队列：Box 中每个 UnitInfo 各一个
        playerUnitBox = Central.Instance.levelInfo.playerBox; // 确保使用当前关卡的配置


        _playerQueue.Clear();
        foreach (var info in playerUnitBox.unitInfos)
            _playerQueue.Enqueue(info);

        BeginEnemyDeploy(_levelMap);
    }
    private void BeginPlayerDeploy()
    {
        if (_playerQueue.Count == 0)
        {
            // 玩家部署完毕，进入正式游戏阶段
            EndDeployPhase();
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

    private void BeginEnemyDeploy(LevelMapInfo _levelMap)
    {
        StartCoroutine(Enumerator());
        IEnumerator Enumerator()
        {
            // 给个短暂缓冲时间，保证地图已经彻底生成
            yield return new WaitForSeconds(0.5f);

            if (_levelMap != null && _levelMap.originEnemyInfos != null)
            {
                foreach (var enemyInfo in _levelMap.originEnemyInfos)
                {
                    // 确保预制体存在并且能够在上面找到对应单位数据
                    if (enemyInfo.enemyUnit != null)
                    {
                        // 尝试从敌人预制体上获取 Units 组件并拿到它的 UnitInfo 
                        // 因为你的 SetUnit 需要 UnitInfo 参数
                        UnitInfo unitInfo = enemyInfo.enemyUnit;
                        if (unitInfo != null)
                        {
                            // 设置预演信息: (info, isPlayer=false, isLocked=false, isCostFree=true)
                            UnitManager.Instance.SetUnit(
                                unitInfo,
                                false,
                                false,
                                true,
                                null // 敌人的生成是全自动的，不需要绑定玩家点击回调
                            );

                            // 直接调用 CreateUnit 把敌人放在指定格子
                            UnitManager.Instance.CreateUnit(enemyInfo.originPosition);

                            // 延时一会不仅让演出更好看，也能避免瞬间生成造成拥堵卡顿
                            yield return new WaitForSeconds(0.2f);
                        }
                        else
                        {
                            Debug.LogWarning($"部署阶段：无法在敌人预制体 {enemyInfo.enemyUnit.name} 上找到 Units 组件或 unitInfo。");
                        }
                    }
                }
            }

            // 敌人部署就绪，开始玩家部署回合
            BeginPlayerDeploy();
        }
    }

    // ─────────────────────────────────────────────
    // 每放置一个单位后的回调（由 CardEffect.DeployEnter 在结束时调用）
    // ─────────────────────────────────────────────

    private void OnOneUnitDeployed()
    {
        if (_isPlayerDeploying)
            BeginPlayerDeploy(); // 继续下一个玩家棋子
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
