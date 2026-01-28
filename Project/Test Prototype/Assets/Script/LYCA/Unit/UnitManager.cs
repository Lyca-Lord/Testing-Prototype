using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit
{
    public partial class UnitManager : MonoBehaviour, IInitialiazer
    {
        [Header("Unit Info")]
        public Unit currentUnit;

        [Header("Action Sequence")]
        public LinkedList<UnitCommand> actionSequence = new LinkedList<UnitCommand>();

        [Header("Lock Action")]
        public static Action ActionSequenceStart;
        public static Action ActionSequenceEnd;
        public static bool isUnitActing = true;

        public void Update()
        {
            if (isUnitActing) return;
            if(actionSequence.Count > 0)
            {
                ActionSequenceStart?.Invoke();
                UnitCommand command = actionSequence.First.Value;
                command.selectedUnit.GetCommand(command);
                actionSequence.RemoveFirst();
                isUnitActing = true;
            }
        }

        public void CommandEnd()
        {
            ActionSequenceEnd?.Invoke();
            if (actionSequence.Count <= 0)
            {
                isUnitActing = true;
                ActionSequenceEnd?.Invoke();
            }
            else isUnitActing = false;
        }

        public void CommandStart()
        {
            isUnitActing = false;
        }

        public void Initialize()
        {
            Central.ActionEnd += CommandEnd;
        }
    } // Ö÷Ìåº¯Êý

    public class UnitCommand
    {
        public Unit selectedUnit;
        public List<Trait> traits;
        public ActionType actionType;
        public Vector2 position;
        public bool canCancel;

        public UnitCommand(Unit selectedUnit, ActionType actionType, Vector2 position, bool canCancel)
        {
            this.selectedUnit = selectedUnit;
            this.traits = selectedUnit.unitElement.traits;
            this.actionType = actionType;
            this.position = position;
            this.canCancel = canCancel;
        }
    }
}