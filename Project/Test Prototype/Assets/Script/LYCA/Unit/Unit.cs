using System.Collections;
using UnityEngine;

namespace Unit
{
    public partial class Unit : MonoBehaviour
    {
        [Header("Element")]
        public UnitElement unitElement;

        [Header("Trigger")]
        [HideInInspector] public bool isSelected = false;
        [HideInInspector] public bool isTrigger = false;

        [Header("StateMachine")]
        public StateMachine stateMachine = new StateMachine();
        public UnitCommand currentCommand;

        void Start()
        {

        }

        void Update()
        {

        }

        public void GetCommand(UnitCommand _unitAction)
        {
            currentCommand = _unitAction;
        }
    } // 单位类

    public partial class Unit
    {
        public void ActionEnd()
        {
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                yield return new WaitForEndOfFrame();
                // 切换回待机状态
                Central.ActionEnd?.Invoke();
            }
        }
    }
}

namespace Unit
{
    public class UnitState
    {
        [Header("Parameter")]
        public string name;
        public float duration;
        public float unscaleDuration;

        [Header("Unit")]
        public Unit unit;
        public StateMachine stateMachine;

        public UnitState(string name, float duration, Unit unit, StateMachine stateMachine)
        {
            this.name = name;
            this.duration = duration;
            this.unit = unit;
            this.stateMachine = stateMachine;
        }

        public virtual void Enter(Vector2 _position)
        {
            // 到时候补齐animator
        }

        public virtual void Exit()
        {
        }

        public virtual void Update()
        {
            duration -= Time.deltaTime;
            unscaleDuration -= Time.unscaledDeltaTime;
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
        WaitForRanged = 6
    } // 行动类型枚举
} // 状态机设定和行动类型

namespace Unit
{
    public class IdleState : UnitState
    {
        public IdleState(Unit unit, StateMachine stateMachine) : base("Idle", Mathf.Infinity, unit, stateMachine)
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
        public MoveState(float duration, Unit unit, StateMachine stateMachine) : base("Move", duration, unit, stateMachine)
        {
        }

        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
        }

        public override void Exit()
        {
            Central.MoveEnd?.Invoke(unit.currentCommand);
            unit.ActionEnd();
            base.Exit();
        }

        public override void Update()
        {
            base.Update();
        }
    } // 移动状态

    public class MeleeState : UnitState
    {
        public MeleeState(float duration, Unit unit, StateMachine stateMachine) : base("Melee", duration, unit, stateMachine)
        {
        }
        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
        }
        public override void Exit()
        {
            Central.MeleeEnd?.Invoke(unit.currentCommand);
            unit.ActionEnd();
            base.Exit();
        }
        public override void Update()
        {
            base.Update();
        }
    } // 近战状态

    public class RangedState : UnitState
    {
        public RangedState(float duration, Unit unit, StateMachine stateMachine) : base("Ranged", duration, unit, stateMachine)
        {
        }
        public override void Enter(Vector2 _position)
        {
            base.Enter(_position);
        }
        public override void Exit()
        {
            Central.RangeEnd?.Invoke(unit.currentCommand);
            unit.ActionEnd();
            base.Exit();
        }
        public override void Update()
        {
            base.Update();
        }
    } // 远程状态
} // 不同状态设定（和ActionType类型相同）