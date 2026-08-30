using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unit
{
    public partial class UnitCommandManager : MonoBehaviour, IInitialiazer
    {
        public static UnitCommandManager Instance;

        [Header("Unit Info")]
        public Units currentUnit;

        [Header("Action Sequence")]
        public LinkedList<UnitCommand> actionSequence = new LinkedList<UnitCommand>(); 
        // 指令栈（尼玛我是怎么把双端链表当栈用的）

        [Header("Lock Action")]
        public UnityEvent ActionSequenceStart;
        public UnityEvent ActionSequenceEnd; // 后续方便给UI交互上锁
        public static bool isUnitActing = true;

        public void Update()
        {
        }

        /// <summary>
        /// 从行动序列的首端取出指令并执行，执行完毕后从序列中移除
        /// </summary>
        public void CommandPop()
        {
            if (actionSequence.Count > 0)
            {
                ActionSequenceStart?.Invoke();
                UnitCommand command = actionSequence.First.Value; // 获取当前指令
                command.selectedUnit.GetCommand(command); // 让单位执行指令
                Debug.Log(command + " " + actionSequence.Count);
                actionSequence.RemoveFirst();
                isUnitActing = true;
            }
        }

        public void CommandEnd()
        {
            if (actionSequence.Count <= 0)
            {
                isUnitActing = false;
                ActionSequenceEnd?.Invoke();
                Debug.Log("行动序列执行完毕");
            }
            else
            {
                //isUnitActing = true;
                CommandPop();
            }
        }

        public void CommandStart()
        {
            isUnitActing = false;
            CommandPop();
        }

        public void Initialize()
        {
            if (Instance == null) Instance = this;
            else Destroy(this.gameObject);
        }
    } // 主体函数

    public partial class UnitCommandManager
    {
        private void Awake()
        {
            isUnitActing = false;
            Central.Instance.ActionEnd.AddListener(CommandEnd);
            Central.Instance.ActionStart.AddListener(CommandStart);
        }

        /// <summary>
        /// 这个函数暂时废弃，不要使用
        /// </summary>
        /// <param name="_command"></param>
        public void PushCommand_Back(UnitCommand _command)
        {
            actionSequence.AddLast(_command);
        } // 在行动序列末尾添加指令

        public void PushCommand_Front(UnitCommand _command)
        {
            Debug.Log(_command.ToString());
            actionSequence.AddFirst(_command);

            switch (_command.actionType)
            {
                case ActionType.Move: Central.Instance.MoveAction?.Invoke(actionSequence.First.Value); break;
                case ActionType.Melee: Central.Instance.MeleeAction?.Invoke(actionSequence.First.Value); break;
                case ActionType.Ranged: Central.Instance.RangeAction?.Invoke(actionSequence.First.Value); break;
                case ActionType.WaitForMelee: 
                    Central.Instance.WaitForMeleeAction?.Invoke(actionSequence.First.Value); 
                    break;
                case ActionType.WaitForRanged:
                    Central.Instance.WaitForRangeAction?.Invoke(actionSequence.First.Value); 
                    break;
            }
        }

        public void ClearSequence()
        {
            actionSequence.Clear();
        }
    } // 在行动序列首位添加指令

    /// <summary>
    /// 卡片赋予的行动应当可以取消，不可跳过
    /// 后续插入的所有行动应当不可取消，可以跳过
    /// </summary>
    [Serializable]
    public class UnitCommand
    {
        public Units selectedUnit;
        public List<Trait> traits;
        public ActionType actionType;
        public Vector2 position;
        public bool canCancel;
        public bool canSkip;

        /// <summary>
        /// canCancal代表这个指令是否可以被右键取消进行（不消耗行动资源）
        /// canSkip代表这个指令是否可以被按下跳过键跳过（消耗行动资源）
        /// </summary>
        /// <param name="selectedUnit"></param>
        /// <param name="actionType"></param>
        /// <param name="position"></param>
        /// <param name="canCancel"></param>
        /// <param name="canSkip"></param>
        public UnitCommand(Units selectedUnit, ActionType actionType, Vector2 position, bool canCancel, bool canSkip)
        {
            this.selectedUnit = selectedUnit;
            this.traits = selectedUnit.unitElement?.traits;
            this.actionType = actionType;
            this.position = position;
            this.canCancel = canCancel;
            this.canSkip = canSkip;
        }

        public override string ToString()
        {
            return actionType + " to " + position;
        }
    }
}