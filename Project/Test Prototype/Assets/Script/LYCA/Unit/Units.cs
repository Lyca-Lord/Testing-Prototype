using DG.Tweening;
using Map;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Unit
{
    public partial class Units : MonoBehaviour
    {
        [Header("Situation")]
        public bool isPlayer = true;
        public bool isLocked = false;
        public UnitElement unitElement;
        public Vector2 location;
        public MapCell cell;

        [Header("Trigger")]
        [HideInInspector] public bool isSelected = false;
        [HideInInspector] public bool isTrigger = false;

        [Header("StateMachine")]
        public StateMachine stateMachine = new StateMachine();
        public IdleState idleState;
        public MoveState moveState;
        public MeleeState meleeState;
        public RangedState rangedState;
        public TacticState tacticState;
        public ReinforceState reinforceState;
        public WaitForMoveState waitForMoveState;
        public WaitForMeleeState waitForMeleeState;
        public WaitForRangedState waitForRangedState;
        public UnitCommand currentCommand;

        [Header("Component")]
        [HideInInspector] public SpriteRenderer sr;
        private Animator ani;

        private void OnValidate()
        {
            sr = GetComponent<SpriteRenderer>();
            ani = GetComponent<Animator>();
            unitElement = GetComponent<UnitElement>();
        }

        private void Start()
        {
            currentCommand = null;
            idleState = new IdleState(this, stateMachine);
            tacticState = new TacticState(this, stateMachine);
            moveState = new MoveState(1.0f, this, stateMachine);
            meleeState = new MeleeState(1.0f, this, stateMachine);
            reinforceState = new ReinforceState(this, stateMachine);
            rangedState = new RangedState(1.0f, this, stateMachine);
            waitForMoveState = new WaitForMoveState(this, stateMachine);
            waitForMeleeState = new WaitForMeleeState(this, stateMachine);
            waitForRangedState = new WaitForRangedState(this, stateMachine);
            stateMachine.ChangeState(idleState, Vector2.zero);
        } // 创建状态机节点

        void Update()
        {
            stateMachine.currentState.Update();
            //Debug.LogWarning(stateMachine.currentState.ToString());
        }

        public void GetCommand(UnitCommand _unitAction)
        {
            currentCommand = _unitAction;
            if (currentCommand.actionType == ActionType.Move)
            {
                stateMachine.ChangeState(moveState, currentCommand.position);
            }
            if (currentCommand.actionType == ActionType.WaitForMove)
            {
                stateMachine.ChangeState(waitForMoveState, currentCommand.position);
            }
            if (currentCommand.actionType == ActionType.Reinforce)
            {
                stateMachine.ChangeState(new ReinforceState(this, stateMachine), Vector2.zero);
            }
            if (currentCommand.actionType == ActionType.Melee)
            {
                stateMachine.ChangeState(meleeState, currentCommand.position);
            }
            if (currentCommand.actionType == ActionType.WaitForMelee)
            {
                stateMachine.ChangeState(waitForMeleeState, currentCommand.position);
            }
            if (currentCommand.actionType == ActionType.Ranged)
            {
                stateMachine.ChangeState(rangedState, currentCommand.position);
            }
            if (currentCommand.actionType == ActionType.WaitForRanged)
            {
                stateMachine.ChangeState(waitForRangedState, currentCommand.position);
            }
            if (currentCommand.actionType == ActionType.Tactic)
            {
                stateMachine.ChangeState(tacticState, currentCommand.position);
            }
        }

        public void SetUp(MapCell _cell, bool _isPlayer = false, bool _isLocked = false)
        {
            if (_cell == null) Debug.LogWarning("噢哟，格子不存在唷");

            isPlayer = _isPlayer;
            isLocked = _isLocked;
            location = _cell.location;
            _cell.CellRegister(this);
            unitElement.SetUp();

            square.color = (isPlayer ? Central.Instance.playerColor : Central.Instance.enemyColor);
        }

        public void SetUp(MapCell _cell)
        {
            if (_cell == null) Debug.LogWarning("噢哟，格子不存在唷");

            location = _cell.location;
            _cell.CellRegister(this);
            unitElement.SetUp();

            square.color = (isPlayer ? Central.Instance.playerColor : Central.Instance.enemyColor);
        }
    } // 单位类

    public partial class Units
    {
        public void ActionEnd()
        {
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                yield return new WaitForEndOfFrame();
                Central.Instance.ActionEnd?.Invoke();
            }
        } // 行动结束处理

        public void GetDamaged(int _tmp)
        {
            unitElement.DecreaseHealth(_tmp);
            StartCoroutine(HitEnumerator());
            IEnumerator HitEnumerator()
            {
                Time.timeScale = 0;
                sr.material = UnitManager.Instance.hitMaterial;
                yield return new WaitForSecondsRealtime(0.025f);
                sr.material = UnitManager.Instance.normalMaterial;
                Time.timeScale = 1;
            }
        }
    } // Trivia
}

namespace Unit
{
    public class UnitState
    {
        [Header("Parameter")]
        public string name;
        public float duration;
        public float unscaleDuration;
        public bool isCancel = false;
        public bool isClick = false;
        public bool isSkip = false;
        public Vector2 clickPosition;

        [Header("Unit")]
        public Units unit;
        public StateMachine stateMachine;

        public UnitState(string name, float duration, Units unit, StateMachine stateMachine)
        {
            this.unit = unit;
            this.name = name;
            this.duration = duration;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter(Vector2 _position)
        {
            // 到时候补齐animator
            isClick = false;
            isCancel = false;
            isSkip = false;
            CardUI.OpenSkipButtonAction?.Invoke(false);
        }

        public virtual void Exit()
        {
        }

        public virtual void Update()
        {
            duration -= Time.deltaTime;
            unscaleDuration -= Time.unscaledDeltaTime;
        }

        public virtual void GetClick(Vector2 _position)
        {
            clickPosition = _position;
            isClick = true;
        }

        public virtual void Cancel()
        {
            isCancel = true;
        }

        public virtual void Skip() => isSkip = true;
    } // 单位状态基类

    public class StateMachine
    {
        public UnitState currentState;

        public void ChangeState(UnitState newState, Vector2 _position)
        {
            Debug.Log(newState.ToString());
            if (currentState != null) currentState.Exit();
            currentState = newState;
            currentState.Enter(_position);
        }
    } // 状态机类

    public enum ActionType
    {
        None = 0,
        Move = 1,
        Melee = 2,
        Ranged = 3,
        WaitForMove = 4,
        WaitForMelee = 5,
        WaitForRanged = 6,
        Reinforce = 7,
        Tactic = 8
    } // 行动类型枚举
} // 状态机设定和行动类型

// 需要记得补一下攻击效果
namespace Unit
{
    public class IdleState : UnitState
    {
        public IdleState(Units unit, StateMachine stateMachine) : base("Idle", Mathf.Infinity, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
        }

        public override void Exit()
        {
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
        }
    }

    public class MoveState : UnitState
    {
        private MapCell mapCellToward;
        private List<Vector2> locations;

        public MoveState(float duration, Units unit, StateMachine stateMachine) : base("Move", duration, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
            mapCellToward = MapManager.Instance.FindCellByLocation(_position);
            // 获取路径列表

            locations = new List<Vector2>(mapCellToward.movePath);
            MapManager.Instance.ClaerAllCellPath();
            unit.cell.CellRelease();

            unit.CloseSquare();
        }

        public override void Exit()
        {
            mapCellToward.CellRegister(unit);
            Central.Instance.MoveEnd?.Invoke(unit.currentCommand);
            MapManager.Instance.ClaerAllCellPath();

            unit.OpenSquare();
            unit.ActionEnd(); // 延迟一帧唤起行动结束事件，方便做行动插入
            base.Exit();
        }

        public override void Update()
        {
            base.Update();

            // 如果路径中还有未到达的点
            if (locations.Count > 0)
            {
                Vector2 targetLocation = locations[0];
                MapCell targetCell = MapManager.Instance.FindCellByLocation(targetLocation);

                unit.transform.position = Vector2.MoveTowards(
                    unit.transform.position,
                    targetCell.Position,
                    5 * Time.deltaTime
                );

                // 判断是否到达当前路径节点
                if (Vector2.Distance(unit.transform.position, targetCell.Position) < 0.01f)
                {
                    unit.transform.position = targetCell.Position;
                    locations.RemoveAt(0); // 到达后移除该节点，以便向下一个节点移动
                }
            }
            else
            {
                // 所有路径点均已到达，切换回Idle状态
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
        }
    } // 移动状态

    public class MeleeState : UnitState
    {
        public MeleeState(float duration, Units unit, StateMachine stateMachine) : base("Melee", duration, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
        }

        public override void Exit()
        {
            Units _unit = MapManager.Instance.FindCellByLocation(unit.currentCommand.position).unit;
            _unit.unitElement.GetHit(unit.unitElement.attack);

            Central.Instance.MeleeEnd?.Invoke(unit.currentCommand);
            unit.ActionEnd(); // 延迟一帧唤起行动结束事件，方便做行动插入
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            stateMachine.ChangeState(unit.idleState, Vector2.zero);
        }
    } // 近战状态

    public class RangedState : UnitState
    {
        public RangedState(float duration, Units unit, StateMachine stateMachine) : base("Ranged", duration, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
        }

        public override void Exit()
        {
            Units _unit = MapManager.Instance.FindCellByLocation(unit.currentCommand.position).unit;
            _unit.unitElement.GetHit(unit.unitElement.attack);

            Central.Instance.RangeEnd?.Invoke(unit.currentCommand);
            unit.ActionEnd(); // 延迟一帧唤起行动结束事件，方便做行动插入
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            stateMachine.ChangeState(unit.idleState, Vector2.zero);
        }
    } // 远程状态

    public class WaitForMoveState : UnitState
    {
        public WaitForMoveState(Units unit, StateMachine stateMachine) : base("WaitForMove", Mathf.Infinity, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
            Central.Instance.ClickEvent.AddListener(GetClick);
            Central.Instance.CancelEvent.AddListener(Cancel);
            Central.Instance.SkipEvent.AddListener(Skip);
            MapManager.Instance.HighLightMovePath(
                unit.cell.location,
                unit.unitElement.currentSpeed,
                unit.isPlayer
                );
        }

        public override void Exit()
        {
            //Central.ActionStart?.Invoke();
            unit.ActionEnd(); // 延迟唤起行动结束事件，方便做行动插入
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            Central.Instance.CancelEvent.RemoveListener(Cancel);
            Central.Instance.SkipEvent.RemoveListener(Skip);
            MapManager.Instance.DisableAllCell();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (ExitCondition())
            {
                UnitCommandManager.Instance.PushCommand_Front(new(
                    unit,
                    ActionType.Move,
                    clickPosition,
                    true,
                    false
                ));
                unit.unitElement.DecreaseCurrentSpeed(
                    MapManager.Instance.FindCellByLocation(clickPosition).distance
                    );
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
            if (isCancel && unit.currentCommand != null && unit.currentCommand.canCancel)
            {
                MapManager.Instance.ClaerAllCellPath();
                UnitCommandManager.Instance.ClearSequence();
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
        }

        private bool ExitCondition()
        {
            if (!isClick) return false;
            MapCell cell = MapManager.Instance.FindCellByLocation(clickPosition);
            if (cell.unit != null) return false;
            return true;
        }
    } // 等待移动状态

    public class WaitForMeleeState : UnitState
    {
        public WaitForMeleeState(Units unit, StateMachine stateMachine) : base("WaitForMelee", Mathf.Infinity, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            Debug.Log("进入近战瞄准阶段");
            base.Enter(_position);
            Central.Instance.ClickEvent.AddListener(GetClick);
            Central.Instance.CancelEvent.AddListener(Cancel);
            Central.Instance.SkipEvent.AddListener(Skip);
            MapManager.Instance.EnableCellByRange(
                unit.location,
                1,
                "Chebyshev"
                );

            if (unit.currentCommand != null)
                CardUI.OpenSkipButtonAction?.Invoke(unit.currentCommand.canSkip);
        }

        /// <summary>
        /// 此处发生问题
        /// Update中一直在反复执行ChangeState
        /// 导致PushCommand_Front被执行多次，行动序列中出现重复指令
        /// 发现错误 两个ChangeState在同一帧执行，导致状态在WaitForMelee和Idle之间交替切换
        /// </summary>
        public override void Exit()
        {
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            Central.Instance.CancelEvent.RemoveListener(Cancel);
            Central.Instance.SkipEvent.RemoveListener(Skip);

            unit.ActionEnd(); // 延迟唤起行动结束事件，方便做行动插入
            //Central.Instance.ActionStart?.Invoke(); 
            // 警告，这个代码非常错误，会导致ChangeState(idle)和ChangeState(melee)在同一帧内交替执行，形成死循环
            MapManager.Instance.DisableAllCell();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (ExitCondition())
            {
                UnitCommandManager.Instance.PushCommand_Front(new(
                    unit,
                    ActionType.Melee,
                    clickPosition,
                    true,
                    false
                ));
                unit.unitElement.DecreaseAttackTime(1);
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
            if (isSkip && unit.currentCommand != null && unit.currentCommand.canSkip)
            {
                unit.unitElement.DecreaseAttackTime(1);
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
            if (isCancel && unit.currentCommand != null && unit.currentCommand.canCancel)
            {
                UnitCommandManager.Instance.ClearSequence();
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
        }

        private bool ExitCondition()
        {
            if (!isClick) return false;
            MapCell cell = MapManager.Instance.FindCellByLocation(clickPosition);
            if (cell.unit == null) return false;
            if (cell.unit == unit) return false;
            return true;
        }
    } // 等待近战状态

    public class WaitForRangedState : UnitState
    {
        public WaitForRangedState(Units unit, StateMachine stateMachine) : base("WaitForRanged", Mathf.Infinity, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
            Central.Instance.ClickEvent.AddListener(GetClick);
            Central.Instance.CancelEvent.AddListener(Cancel);
            Central.Instance.SkipEvent.AddListener(Skip);
            MapManager.Instance.EnableCellByRange(
                unit.location,
                unit.unitElement.rangedRadius,
                "Manhattan"
                );

            if (unit.currentCommand != null)
                CardUI.OpenSkipButtonAction?.Invoke(unit.currentCommand.canSkip);
        }

        public override void Exit()
        {
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            Central.Instance.CancelEvent.RemoveListener(Cancel);
            Central.Instance.SkipEvent.RemoveListener(Skip);

            unit.ActionEnd(); // 延迟唤起行动结束事件，方便做行动插入
            //Central.Instance.ActionStart?.Invoke();
            MapManager.Instance.DisableAllCell();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (ExitCondition())
            {
                UnitCommandManager.Instance.PushCommand_Front(new(
                    unit,
                    ActionType.Ranged,
                    clickPosition,
                    true,
                    false
                ));
                unit.unitElement.DecreaseAttackTime(1);
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
            if (isSkip && unit.currentCommand != null && unit.currentCommand.canSkip)
            {
                unit.unitElement.DecreaseAttackTime(1);
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
            if (isCancel && unit.currentCommand != null && unit.currentCommand.canCancel)
            {
                UnitCommandManager.Instance.ClearSequence();
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
        }

        private bool ExitCondition()
        {
            if (!isClick) return false;
            MapCell cell = MapManager.Instance.FindCellByLocation(clickPosition);
            if (cell.unit == null) return false;
            if (cell.unit == unit) return false;
            return true;
        }
    } // 等待远程

    public class ReinforceState : UnitState
    {
        public ReinforceState(Units unit, StateMachine stateMachine) : base("Reinforce", Mathf.Infinity, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
            Central.Instance.ClickEvent.AddListener(GetClick);
            Central.Instance.CancelEvent.AddListener(Cancel);
            Central.Instance.SkipEvent.AddListener(Skip);
            MapManager.Instance.EnableCellByRange(
                unit.location,
                1,
                "Chebyshev"
                );
        }

        public override void Exit()
        {
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            Central.Instance.CancelEvent.RemoveListener(Cancel);
            Central.Instance.SkipEvent.RemoveListener(Skip);

            unit.ActionEnd(); // 延迟唤起行动结束事件，方便做行动插入
            MapManager.Instance.DisableAllCell();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (ExitCondition())
            {
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
                UnitManager.Instance.CreateUnit(clickPosition);
            }
            if (isCancel && unit.currentCommand != null && unit.currentCommand.canCancel)
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            if (isSkip && unit.currentCommand != null && unit.currentCommand.canSkip)
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
        }

        private bool ExitCondition()
        {
            if (!isClick) return false;
            MapCell cell = MapManager.Instance.FindCellByLocation(clickPosition);
            if (cell.unit != null) return false;
            return true;
        }
    }

    /// <summary>
    /// 战术调整阶段
    /// 调用规则：进入阶段的Vec2中，第一个元素应指明
    /// 本次移动是奖励移动还是普通战术调整
    /// 如果是奖励移动，不消耗战术调整
    /// 约定第一个元素大于0.5为奖励移动
    /// </summary>
    public class TacticState : UnitState
    {
        public bool isBonusMove;
        public int movePoint;

        public TacticState(Units unit, StateMachine stateMachine) : base("Tactic", Mathf.Infinity, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
            isBonusMove = _position.x > 0.5f; // 约定大于0.5为true，小于等于0.5为false
            movePoint = isBonusMove ? unit.unitElement.tacticSpeed : unit.unitElement.currentTacticSpeed;

            Central.Instance.ClickEvent.AddListener(GetClick);
            Central.Instance.CancelEvent.AddListener(Cancel);
            MapManager.Instance.HighLightMovePath(
                unit.cell.location,
                unit.unitElement.currentTacticSpeed,
                unit.isPlayer
            );
        }

        public override void Exit()
        {
            unit.ActionEnd();
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            Central.Instance.CancelEvent.RemoveListener(Cancel);
            MapManager.Instance.DisableAllCell();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (ExitCondition())
            {
                UnitCommandManager.Instance.PushCommand_Front(new(
                    unit,
                    ActionType.Move,
                    clickPosition,
                    true,
                    false
                ));
                if (!isBonusMove)
                {
                    unit.unitElement.DecreaseCurrentTacticSpeed(
                        MapManager.Instance.FindCellByLocation(clickPosition).distance
                    );
                } // 非奖励移动才消耗战术调整点
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
            if (isCancel)
            {
                MapManager.Instance.ClaerAllCellPath();
                UnitCommandManager.Instance.ClearSequence();
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
            }
        }

        private bool ExitCondition()
        {
            if (!isClick) return false;
            MapCell cell = MapManager.Instance.FindCellByLocation(clickPosition);
            if (cell.unit != null) return false;
            return true;
        }
    } // 战术调整，用于战术调整阶段和奖励移动机会
} // 不同状态设定（和ActionType类型相同）

namespace Unit
{
    public partial class Units
    {
        [Header("Attach")]
        public List<SpriteRenderer> icons;
        public List<TextMeshPro> tmps;
        public SpriteRenderer square;

        public void ChangeSortingOverlay(int _order)
        {
            sr.sortingOrder = _order;
            foreach (var icon in icons) icon.sortingOrder = _order;
            foreach (var tmp in tmps) tmp.sortingOrder = _order;
            square.sortingOrder = _order - 1;
        }

        public void CloseSquare() => square.enabled = false;

        public void OpenSquare() => square.enabled = true;
    }

    public partial class Units
    {
        public void ApplyKnockback(Vector2 sourcePosition, int distance, int additionalDamage = 0)
        {
            // 启动协程执行击退动画和格子逻辑
            StartCoroutine(KnockbackCoroutine(sourcePosition, distance, additionalDamage));
        }

        private IEnumerator KnockbackCoroutine(Vector2 sourcePosition, int distance, int additionalDamage)
        {
            Vector2 delta = location - sourcePosition;
            Units other = null;

            for (int i = 0; i < distance; i++)
            {
                Vector2 nowLocation = location;
                Vector2 nextLocation = location + delta;
                if (nextLocation.x < 0 || nextLocation.y < 0 ||
                    nextLocation.x >= MapManager.Instance.GetMapHeight() ||
                    nextLocation.y >= MapManager.Instance.GetMapWidth()) break;

                MapCell nextCell = MapManager.Instance.FindCellByLocation(nextLocation);
                MapCell nowCell = MapManager.Instance.FindCellByLocation(nowLocation);

                if (nextCell == null || !nextCell.IsWalkable(isPlayer))
                    break;

                Vector3 targetPos = nextCell.Position;

                if (nextCell.unit != null)
                {
                    other = nextCell.unit;
                    while (Vector2.Distance(transform.position, targetPos) > 0.01f)
                    {
                        transform.position = Vector2.MoveTowards(transform.position, targetPos, 5f * Time.deltaTime);
                        yield return null;
                    }

                    yield return new WaitForSeconds(0.025f);
                    other.unitElement.GetHit(1 + additionalDamage);
                    unitElement.GetHit(1 + additionalDamage);
                    transform.position = nowCell.Position;
                    break;
                }

                MapCell oldCell = cell;
                oldCell.CellRelease();

                while (Vector2.Distance(transform.position, targetPos) > 0.01f)
                {
                    transform.position = Vector2.MoveTowards(transform.position, targetPos, 5f * Time.deltaTime);
                    yield return null;
                }

                // 到达目标格，更新位置与注册
                transform.position = targetPos;
                location = nextLocation;
                cell = nextCell;
                nextCell.CellRegister(this);

                yield return null;
            }
            ActionEnd();
        }
    } // 单位击退
}