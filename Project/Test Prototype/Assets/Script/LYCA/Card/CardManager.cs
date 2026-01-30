using Map;
using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

namespace CommandCard
{
    public partial class CardManager : MonoBehaviour
    {
        public static CardManager Instance;

        [Header("Lock")]
        public bool isLock = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (isLock) return;
            if (Input.GetKeyDown(KeyCode.E)) Enter();
        }

        /// <summary>
        /// 移动指令部分
        /// 当启动移动指令时，监听单位被选中事件，推入等待移动指令
        /// </summary>
        public void Enter()
        {
            if (isLock) return;
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                isLock = true;
                Central.Instance.UnitSelectEvent.RemoveListener(SelectUnit); // 取消监听防呆
                Central.Instance.UnitSelectEvent.AddListener(SelectUnit); // 监听单位被选中事件
                UnitCommandManager.Instance.ActionSequenceEnd.AddListener(ActionEnd);
                UnitManager.Instance.units.ForEach(unit =>
                {
                    unit.unitElement.ResetMove(); // 重置移动力
                });
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_CanMove") && unit.unitElement.currentSpeed > 0;
                }); // 启用单位选择，只能选择可移动单位
            }
        }

        private void ActionEnd()
        {
            MapManager.Instance.EnableUnitPick(unit =>
            {
                return unit.unitElement.CheckTraits("Trait_CanMove") && unit.unitElement.currentSpeed > 0;
            });
            Central.Instance.UnitSelectEvent.RemoveListener(SelectUnit); // 取消监听防呆
            Central.Instance.UnitSelectEvent.AddListener(SelectUnit); // 重新监听单位被选中事件
            CheckEnd(); // 检查是否还有单位可以移动
        }

        private void CheckEnd()
        {
            List<Units> _units = UnitManager.Instance.units;
            foreach (var unit in _units)
            {
                if (unit.unitElement.currentSpeed > 0 &&
                    unit.unitElement.CheckTraits("Trait_CanMove")) return;
            }
            Central.Instance.UnitSelectEvent.RemoveListener(SelectUnit); // 取消监听
            UnitCommandManager.Instance.ActionSequenceEnd.RemoveListener(CheckEnd);
            isLock = false;
        }

        private void SelectUnit(Units _unit)
        {
            //if (UnitCommandManager.isUnitActing) return;
            if (!_unit.unitElement.CheckTraits("Trait_CanMove")) return;
            if (_unit.unitElement.currentSpeed <= 0) return;
            UnitCommandManager.Instance.PushCommand_Front(new(
                _unit,
                ActionType.WaitForMove,
                new(0, 0),
                true
                )); // 推入等待移动指令
            Central.Instance.ActionStart?.Invoke();
            Central.Instance.UnitSelectEvent.RemoveListener(SelectUnit); // 取消监听，防止重复选择
        }
    } 

    public partial class CardManager
    {
        public void ReinforceEnter()
        {
            if (isLock) return;
            StartCoroutine(Enumerator());

            IEnumerator Enumerator()
            {
                isLock = true;
                Central.Instance.UnitSelectEvent.AddListener(ReinforceSelectUnit); // 监听单位被选中事件
                UnitCommandManager.Instance.ActionSequenceEnd.AddListener(ReinforceActionEnd);
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_Flag");
                }); // 启用单位选择，只能选择可增援单位
            }
        }

        private void ReinforceActionEnd()
        {
            isLock = false;
            Central.Instance.UnitSelectEvent.RemoveListener(ReinforceSelectUnit);
            UnitCommandManager.Instance.ActionSequenceEnd.RemoveListener(ReinforceActionEnd);
        }

        private void ReinforceSelectUnit(Units _unit)
        {
            if (!_unit.unitElement.CheckTraits("Trait_Flag")) return;
            UnitCommandManager.Instance.PushCommand_Front(new(
                _unit,
                ActionType.Reinforce,
                new(0, 0),
                true
                )); // 推入增援指令
            Debug.LogWarning("推入增援");
            Central.Instance.ActionStart?.Invoke();
        }
    } // 增援部分   
}