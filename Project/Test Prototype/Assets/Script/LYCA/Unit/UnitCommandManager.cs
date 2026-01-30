using System;
using System.Collections;
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

        [Header("Lock Action")]
        public UnityEvent ActionSequenceStart;
        public UnityEvent ActionSequenceEnd; // 后续方便给UI交互上锁
        public static bool isUnitActing = true;

        public void Update()
        {
            //if (isUnitActing) return;
            //if(actionSequence.Count > 0)
            //{
            //    ActionSequenceStart?.Invoke();
            //    UnitCommand command = actionSequence.First.Value; // 获取当前指令
            //    command.selectedUnit.GetCommand(command); // 让单位执行指令
            //    Debug.Log(command);
            //    actionSequence.RemoveFirst();
            //    isUnitActing = true;
            //}
        }

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
                isUnitActing = true;
                ActionSequenceEnd?.Invoke();
                Debug.Log("行动序列执行完毕");
            }
            else
            {
                isUnitActing = false;
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
            isUnitActing = false;
            Central.Instance.ActionEnd.AddListener(CommandEnd);
            Central.Instance.ActionStart.AddListener(CommandStart);
        }
    } // 主体函数

    public partial class UnitCommandManager
    {
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(this.gameObject);
        }

        public void PushCommand_Back(UnitCommand _command)
        {
            actionSequence.AddLast(_command);
        } // 在行动序列末尾添加指令

        public void PushCommand_Front(UnitCommand _command)
        {
            actionSequence.AddFirst(_command);
        }
    } // 在行动序列首位添加指令

    [Serializable]
    public class UnitCommand
    {
        public Units selectedUnit;
        public List<Trait> traits;
        public ActionType actionType;
        public Vector2 position;
        public bool canCancel;

        public UnitCommand(Units selectedUnit, ActionType actionType, Vector2 position, bool canCancel)
        {
            this.selectedUnit = selectedUnit;
            this.traits = selectedUnit.unitElement?.traits;
            this.actionType = actionType;
            this.position = position;
            this.canCancel = canCancel;
        }

        public override string ToString()
        {
            return actionType + " to " + position;
        }
    }
}