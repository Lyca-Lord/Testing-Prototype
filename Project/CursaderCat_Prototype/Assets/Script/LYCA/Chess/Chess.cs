using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子逻辑门面类，只负责身份、状态、指令分发以及数值变更的转发，
/// 具体的动画表现全部委托给同物体上的 IChessView，本类不直接操作任何 SpriteRenderer。
/// 这是架构重构方案中"逻辑层"的示例实现，暂不依赖旧的 Units/MapCell，便于独立挂载测试。
/// </summary>
public partial class Chess : MonoBehaviour
{
    [Header("身份信息")]
    public bool isPlayer = true; // 是否为玩家阵营棋子，敌方请设为 false
    public string chessName; // 棋子标识名，供技能表/数值表查找使用

    [Header("组件引用")]
    public IChessView view; // 表现层引用，Chess 只调用其接口，不关心具体动画怎么做

    private ChessStateMachine stateMachine; // 状态机实例，驱动指令执行到哪一步
    private ChessIdleState idleState; // 待机状态实例，所有指令执行完毕后都会回到这里
    private Dictionary<ChessActionKind, ChessActionState> stateTable; // 指令类型到状态实例的查表

    private void Awake()
    {
        view = GetComponent<IChessView>(); // 自动获取挂在同一物体上的表现组件

        if (view == null)
        {
            Debug.LogWarning(chessName + " 没有找到 IChessView 组件，请确认同物体上挂载了 ChessView。"); // 防呆提示
        }
    } // 缓存组件引用

    private void Start()
    {
        stateMachine = new ChessStateMachine();
        idleState = new ChessIdleState(this, stateMachine);

        stateTable = new Dictionary<ChessActionKind, ChessActionState>
        {
            { ChessActionKind.Move, new ChessMoveState(this, stateMachine) },
            // 新增指令类型时只需在此追加一行注册，不需要改动 ReceiveCommand 本身
        };

        stateMachine.ChangeState(idleState, Vector2.zero);
        ResetTurnValues(); // 初始数值重置一次，避免未经历回合开始就是 0
    } // 运行时初始化状态机与查表
} // 基础信息与生命周期

public partial class Chess
{
    public event Action<Chess> OnCommandFinished; // 指令执行完毕事件，供 ActionSequencer/ActionBatchRunner 推进下一条指令
    public event Action<Chess> OnDied; // 死亡事件，供 ChessRoster 等外部系统订阅移除

    public bool IsActing { get; private set; } = false; // 当前是否正在执行某个指令，供外部做互斥判断
    private ChessCommand currentCommand; // 当前正在执行的指令数据

    /// <summary>
    /// 接收一条指令并驱动状态机切换，替代原方案中按 if 链判断的 GetCommand。
    /// </summary>
    /// <param name="_command">待执行的指令数据</param>
    public void ReceiveCommand(ChessCommand _command)
    {
        if (IsLocked)
        {
            Debug.LogWarning(chessName + " 已被锁定，拒绝接收指令：" + _command.actionKind); // 锁定中的棋子不应该收到指令

            return;
        }

        if (!stateTable.TryGetValue(_command.actionKind, out ChessActionState targetState))
        {
            Debug.LogWarning("未注册的指令类型：" + _command.actionKind); // 查表失败时给出警告，便于排查遗漏注册

            return;
        }

        currentCommand = _command;
        IsActing = true;
        stateMachine.ChangeState(targetState, _command.targetPosition);
    } // 指令分发入口

    /// <summary>
    /// 供状态机的 Exit 阶段调用，通知外部本次指令已经执行完毕。
    /// </summary>
    public void NotifyCommandFinished()
    {
        IsActing = false;
        OnCommandFinished?.Invoke(this);
    } // 指令完成通知

    public ChessActionState GetIdleState() => idleState; // 供状态机内部查询待机状态

    public ChessCommand GetCurrentCommand() => currentCommand; // 供状态机查询当前指令的详细参数
} // 指令接收与分发

public partial class Chess
{
    [Header("技能列表")]
    public List<Trait> traits; // 棋子拥有的技能配置列表（沿用现有 Trait 资源，后续可平滑替换为 SkillDefinition）

    /// <summary>
    /// 按技能标识查询本棋子是否拥有该技能，供 SkillSystem 遍历触发使用。
    /// </summary>
    /// <param name="_skillId">技能的资源名（即 SkillId）</param>
    public bool HasSkill(string _skillId)
        => traits != null && traits.Find(t => t != null && t.name == _skillId) != null;
} // 技能查询

public partial class Chess
{
    public bool IsLocked { get; private set; } = false; // 是否被锁定，锁定中的棋子不可被选为指令对象

    public void Lock() => IsLocked = true; // 锁定本棋子

    public void Unlock() => IsLocked = false; // 解锁本棋子

    public Vector2 Location { get; private set; } // 当前所在格子坐标，只读，变更须通过 UpdateLocation

    /// <summary>
    /// 变更棋子的位置记录，应在移动/击退等表现播放完成之后调用，避免逻辑坐标与表现位置不同步。
    /// </summary>
    /// <param name="_newLocation">新的格子坐标</param>
    public void UpdateLocation(Vector2 _newLocation) => Location = _newLocation;
} // 锁定与位置管理

public partial class Chess
{
    [Header("基础数值")]
    public int maxHealth = 10; // 最大生命值
    public int attack = 1; // 攻击力
    public int defend = 0; // 防御力
    public int speed = 3; // 基础移速
    public int tacticSpeed = 1; // 基础战术调整力

    [Header("当前数值")]
    public int currentHealth; // 当前生命值
    public int currentShield; // 当前护盾值
    public int currentSpeed; // 本回合剩余移速
    public int currentTacticSpeed; // 本回合剩余战术调整力
    public int currentAttackTime; // 本回合剩余攻击次数

    /// <summary>
    /// 承受伤害，内部先扣减护盾再扣减生命，返回实际造成的扣血量，供表现层决定反馈强度。
    /// </summary>
    /// <param name="_damage">原始伤害数值</param>
    public int DecreaseHealth(int _damage)
    {
        int remain = _damage - defend; // 先扣除防御力

        if (remain < 0)
        {
            remain = 0;
        }

        if (currentShield > 0)
        {
            if (currentShield < remain)
            {
                remain -= currentShield;
                currentShield = 0;
            }
            else
            {
                currentShield -= remain;
                remain = 0;
            }
        }

        currentHealth -= remain;
        view?.PlayHit(remain, CheckDeath); // 播放受击表现，完成后检查是否需要进入死亡流程

        return remain;
    } // 扣血结算

    public void AddShield(int _value) => currentShield += _value; // 增加护盾

    /// <summary>
    /// 回合开始时统一重置本回合的移速/战术调整力/攻击次数，应由 TurnManager 广播事件触发调用。
    /// </summary>
    public void ResetTurnValues()
    {
        currentHealth = currentHealth <= 0 ? maxHealth : currentHealth; // 首次调用时补齐满血，避免生成时为 0
        currentSpeed = speed;
        currentTacticSpeed = tacticSpeed;
        currentAttackTime = 1;
    } // 回合数值重置

    private void CheckDeath()
    {
        if (currentHealth > 0)
        {
            return;
        }

        view?.PlayDeath(OnDeathAnimationFinished); // 表现层播完死亡动画后再真正销毁，避免画面突兀
    } // 受击结算后检查是否死亡

    private void OnDeathAnimationFinished()
    {
        OnDied?.Invoke(this);
        Destroy(gameObject);
    } // 死亡表现播放完毕的回调
} // 数值与战斗结算

/// <summary>
/// 棋子指令数据，描述一次"要谁、做什么、去哪里"的执行请求。
/// </summary>
[Serializable]
public class ChessCommand
{
    public Chess executor; // 执行该指令的棋子
    public ChessActionKind actionKind; // 指令类型
    public Vector2 targetPosition; // 指令目标坐标（移动终点/攻击目标格等）
    public bool canCancel; // 是否允许被玩家取消
    public bool canSkip; // 是否允许被跳过

    public ChessCommand(Chess _executor, ChessActionKind _actionKind, Vector2 _targetPosition, bool _canCancel, bool _canSkip)
    {
        executor = _executor;
        actionKind = _actionKind;
        targetPosition = _targetPosition;
        canCancel = _canCancel;
        canSkip = _canSkip;
    } // 构造指令数据

    public override string ToString() => actionKind + " -> " + targetPosition;
} // 指令数据

/// <summary>
/// 指令类型枚举，新增一种全体指令时在此追加一项即可。
/// </summary>
public enum ChessActionKind
{
    None = 0,
    Move = 1,
    Melee = 2,
    Ranged = 3,
    Tactic = 4,
    Reinforce = 5,
} // 指令类型枚举
