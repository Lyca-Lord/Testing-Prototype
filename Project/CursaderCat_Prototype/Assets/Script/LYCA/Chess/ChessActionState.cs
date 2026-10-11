using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 棋子指令状态基类，对应某一类指令（移动/近战/远程等）的执行流程。
/// 状态机只负责推进"指令执行到哪一步"，具体动画表现统一委托给 Chess 持有的 IChessView，
/// 不在状态类里直接操作 Transform 或等待固定秒数。
/// </summary>
public abstract class ChessActionState
{
    [Header("基础信息")]
    public string stateName; // 状态名称，便于调试打印
    public Chess chess; // 本状态归属的棋子
    public ChessStateMachine stateMachine; // 所属状态机引用

    [Header("交互标记")]
    protected bool isCancel; // 本次是否收到取消输入
    protected bool isSkip; // 本次是否收到跳过输入
    protected Vector2 clickPosition; // 最近一次点击坐标

    public ChessActionState(string _stateName, Chess _chess, ChessStateMachine _stateMachine)
    {
        stateName = _stateName;
        chess = _chess;
        stateMachine = _stateMachine;
    } // 构造函数，注入所属棋子与状态机

    /// <summary>
    /// 进入该状态时调用，_targetPosition 为指令携带的目标坐标。
    /// </summary>
    public virtual void Enter(Vector2 _targetPosition)
    {
        isCancel = false;
        isSkip = false;
    } // 进入状态时重置交互标记，子类可覆写追加自己的初始化

    /// <summary>
    /// 离开该状态时调用，子类通常在这里广播"本类型指令已结算"的领域事件。
    /// </summary>
    public virtual void Exit()
    {
    } // 子类按需覆写

    /// <summary>
    /// 由 Chess.Update 每帧调用一次，用于检查退出条件是否满足。
    /// </summary>
    public virtual void Tick()
    {
    } // 子类按需覆写

    public virtual void ReceiveClick(Vector2 _position)
    {
        clickPosition = _position;
    } // 接收点击输入，等待型状态会用它判断去哪

    public virtual void ReceiveCancel() => isCancel = true; // 接收取消输入

    public virtual void ReceiveSkip() => isSkip = true; // 接收跳过输入

    public override string ToString() => stateName;
} // 指令状态基类

/// <summary>
/// 棋子状态机，负责在不同 ChessActionState 之间切换。
/// </summary>
public class ChessStateMachine
{
    public ChessActionState CurrentState { get; private set; } // 当前状态，只读

    /// <summary>
    /// 切换到新状态，会先调用旧状态的 Exit，再调用新状态的 Enter。
    /// </summary>
    /// <param name="_newState">目标状态</param>
    /// <param name="_targetPosition">指令目标坐标，透传给新状态的 Enter</param>
    public void ChangeState(ChessActionState _newState, Vector2 _targetPosition)
    {
        if (_newState == null)
        {
            Debug.LogWarning("尝试切换到空状态"); // 防呆提示

            return;
        }

        CurrentState?.Exit();
        CurrentState = _newState;
        CurrentState.Enter(_targetPosition);
    } // 切换状态
} // 棋子状态机

/// <summary>
/// 待机状态，指令执行完毕后的默认落点。
/// </summary>
public class ChessIdleState : ChessActionState
{
    public ChessIdleState(Chess _chess, ChessStateMachine _stateMachine)
        : base("Idle", _chess, _stateMachine)
    {
    }
} // 待机状态

/// <summary>
/// 移动状态，演示"状态机只管推进，不直接操作 Transform"的写法：
/// 真正的位移插值全部交给 Chess.view 去播放，状态机只在收到"播放完成"回调后再切回待机。
/// </summary>
public class ChessMoveState : ChessActionState
{
    private List<Vector2> path; // 本次移动途经的路径点列表
    private bool isPresentationFinished; // 表现层是否已播放完移动动画

    public ChessMoveState(Chess _chess, ChessStateMachine _stateMachine)
        : base("Move", _chess, _stateMachine)
    {
    }

    public override void Enter(Vector2 _targetPosition)
    {
        base.Enter(_targetPosition);

        isPresentationFinished = false;
        path = new List<Vector2> { chess.Location, _targetPosition }; // 简化示例：正式接入后应改为由 BoardManager 的寻路结果提供完整路径点

        chess.view.PlayMove(path, OnPresentationFinished); // 把表现细节完全委托给表现层，状态机本身不关心怎么插值
    } // 进入移动状态，发起表现层播放请求

    public override void Tick()
    {
        base.Tick();

        if (isPresentationFinished)
        {
            chess.UpdateLocation(path[path.Count - 1]); // 表现播完之后再同步逻辑坐标，避免中途被其他逻辑提前读到新位置
            stateMachine.ChangeState(chess.GetIdleState(), Vector2.zero);
        }
    } // 每帧检查表现是否播放完毕

    private void OnPresentationFinished()
    {
        isPresentationFinished = true;
        chess.NotifyCommandFinished(); // 通知棋子本体，指令已执行完毕
    } // 表现层回调：移动动画播放完成
} // 移动状态
