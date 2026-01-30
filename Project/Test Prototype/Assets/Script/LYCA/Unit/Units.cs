using Map;
using System.Collections;
using UnityEngine;

namespace Unit
{
    public partial class Units : MonoBehaviour
    {
        [Header("Situation")]
        public bool isPlayerUnit = true;
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

        public void SetUp(MapCell _cell)
        {
            if (_cell == null) Debug.LogWarning("噢哟，格子不存在唷");
            location = _cell.location;
            _cell.CellRegister(this);
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
                yield return new WaitForSecondsRealtime(0.05f);
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
        public bool isClick = false;
        public Vector2 clickPosition;

        [Header("Unit")]
        public Units unit;
        public StateMachine stateMachine;

        public UnitState(string name, float duration, Units unit, StateMachine stateMachine)
        {
            this.name = name;
            this.duration = duration;
            this.unit = unit;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter(Vector2 _position)
        {
            // 到时候补齐animator
            isClick = false;
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
    } // 单位状态基类

    public class StateMachine
    {
        public UnitState currentState;

        public void ChangeState(UnitState newState, Vector2 _position)
        {
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

        public MoveState(float duration, Units unit, StateMachine stateMachine) : base("Move", duration, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
            mapCellToward = MapManager.Instance.FindCellByLocation(_position);
            unit.cell.CellRelease();
        }

        public override void Exit()
        {
            mapCellToward.CellRegister(unit);
            Central.Instance.MoveEnd?.Invoke(unit.currentCommand);
            unit.ActionEnd(); // 延迟一帧唤起行动结束事件，方便做行动插入
            base.Exit();
        }

        public override void Update()
        {
            unit.transform.position = Vector2.MoveTowards(
                unit.transform.position,
                mapCellToward.Position,
                5 * Time.deltaTime
                );
            base.Update();
            if (Vector2.Distance(unit.transform.position, mapCellToward.Position) < 0.01f)
            {
                unit.transform.position = mapCellToward.Position;
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
            Central.Instance.MeleeEnd?.Invoke(unit.currentCommand);
            unit.ActionEnd(); // 延迟一帧唤起行动结束事件，方便做行动插入
            base.Exit();
        }
        public override void Update()
        {
            base.Update();
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
            Central.Instance.RangeEnd?.Invoke(unit.currentCommand);
            unit.ActionEnd(); // 延迟一帧唤起行动结束事件，方便做行动插入
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
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
            MapManager.Instance.EnableCellByRange(
                unit.location,
                unit.unitElement.currentSpeed,
                "Manhattan"
                );
        }

        public override void Exit()
        {
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            UnitCommandManager.Instance.PushCommand_Front(new(
                unit,
                ActionType.Move,
                clickPosition,
                true
                ));
            //Central.ActionStart?.Invoke();
            unit.ActionEnd(); // 延迟唤起行动结束事件，方便做行动插入
            MapManager.Instance.DisableAllCell();
            unit.unitElement.DecreaseCurrentSpeed(
                MapManager.Instance.Distance(unit.location, clickPosition, "Manhattan")
                );
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (ExitCondition())
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
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
            base.Enter(_position);
            Central.Instance.ClickEvent.AddListener(GetClick);
        }

        public override void Exit()
        {
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            UnitCommandManager.Instance.PushCommand_Front(new(
                unit,
                ActionType.Melee,
                clickPosition,
                true
                ));
            Central.Instance.ActionStart?.Invoke();
            MapManager.Instance.DisableAllCell();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (isClick) stateMachine.ChangeState(unit.idleState, Vector2.zero);
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
        }

        public override void Exit()
        {
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            UnitCommandManager.Instance.PushCommand_Front(new(
                unit,
                ActionType.Ranged,
                clickPosition,
                true
                ));
            Central.Instance.ActionStart?.Invoke();
            MapManager.Instance.DisableAllCell();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (isClick) stateMachine.ChangeState(unit.idleState, Vector2.zero);
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
            MapManager.Instance.EnableCellByRange(
                unit.location,
                1,
                "Chebyshev"
                );
        }

        public override void Exit()
        {
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            UnitManager.Instance.CreateUnit(clickPosition);

            unit.ActionEnd(); // 延迟唤起行动结束事件，方便做行动插入
            MapManager.Instance.DisableAllCell();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (ExitCondition())
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
    /// 如果是是奖励移动，不消耗战术调整
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
            Central.Instance.ClickEvent.AddListener(GetClick);
            isBonusMove = _position.x > 0.5f; // 约定大于0.5为true，小于等于0.5为false
            movePoint = isBonusMove ? unit.unitElement.tacticSpeed : unit.unitElement.currentTacticSpeed;

            MapManager.Instance.EnableCellByRange(
                unit.location,
                movePoint,
                "Manhattan"
                );
        }

        public override void Exit()
        {
            Central.Instance.ClickEvent.RemoveListener(GetClick);
            UnitCommandManager.Instance.PushCommand_Front(new(
                unit,
                ActionType.Move,
                clickPosition,
                true
                ));
            //Central.ActionStart?.Invoke();
            unit.ActionEnd(); // 延迟唤起行动结束事件，方便做行动插入
            MapManager.Instance.DisableAllCell();

            if (!isBonusMove)
            {
                unit.unitElement.DecreaseCurrentTacticSpeed(
                    MapManager.Instance.Distance(unit.location, clickPosition, "Manhattan")
                    );
            } // 非奖励移动才消耗战术调整点

            base.Exit();
        }

        public override void Update()
        {
            base.Update();
            if (ExitCondition())
                stateMachine.ChangeState(unit.idleState, Vector2.zero);
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