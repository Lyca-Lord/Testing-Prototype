using Map;
using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

namespace CommandCard
{
    public partial class CardEffect : MonoBehaviour
    {
        public static CardEffect Instance;

        [Header("Lock")]
        public bool isLock = false;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (isLock) return;
            if (Input.GetKeyDown(KeyCode.E)) MoveEnter();
        }

        /// <summary>
        /// 移动指令部分
        /// 当启动移动指令时，监听单位被选中事件，推入等待移动指令
        /// 防呆指令去掉了，需要测试是否会有问题
        /// </summary>
        public void MoveEnter()
        {
            if (isLock) return;
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                isLock = true;
                //Central.Instance.UnitSelectEvent.RemoveListener(MoveSelectUnit); // 取消监听防呆

                CardManager.Instance.LockPlayerColumn(); // 锁定牌列，防止在移动过程中打出其他卡牌
                Central.Instance.UnitSelectEvent.AddListener(MoveSelectUnit); // 监听单位被选中事件
                Central.Instance.ActionEndEarly.AddListener(MoveActionEndEarly); // 监听行动提前结束事件
                UnitCommandManager.Instance.ActionSequenceEnd.AddListener(MoveActionEnd);

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

        private void MoveActionEnd()
        {
            MapManager.Instance.EnableUnitPick(unit =>
            {
                return unit.unitElement.CheckTraits("Trait_CanMove") && unit.unitElement.currentSpeed > 0;
            });
            //Central.Instance.UnitSelectEvent.RemoveListener(MoveSelectUnit); // 取消监听防呆
            Central.Instance.UnitSelectEvent.AddListener(MoveSelectUnit); // 重新监听单位被选中事件
            MoveCheckEnd(); // 检查是否还有单位可以移动
        }

        private void MoveCheckEnd()
        {
            List<Units> _units = UnitManager.Instance.units;
            foreach (var unit in _units)
            {
                if (unit.unitElement.currentSpeed > 0 &&
                    unit.unitElement.CheckTraits("Trait_CanMove")) return;
            }
            MoveCommandEnd();
        }

        private void MoveSelectUnit(Units _unit)
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
            Central.Instance.UnitSelectEvent.RemoveListener(MoveSelectUnit); // 取消监听，防止重复选择
        }

        private void MoveActionEndEarly()
        {
            MoveCommandEnd();
        }

        private void MoveCommandEnd()
        {
            MapManager.Instance.DisableAllCell();
            CardManager.Instance.UnlockPlayerColumn(); // 解锁牌列
            Central.Instance.UnitSelectEvent.RemoveListener(MoveSelectUnit); // 取消监听
            UnitCommandManager.Instance.ActionSequenceEnd.RemoveListener(MoveActionEnd);
            isLock = false;
        }
    }  // 移动指令部分

    public partial class CardEffect
    {
        public void ReinforceEnter()
        {
            if (isLock) return;
            StartCoroutine(Enumerator());

            IEnumerator Enumerator()
            {
                isLock = true;

                CardManager.Instance.LockPlayerColumn(); // 锁定牌列，防止在移动过程中打出其他卡牌
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
            CardManager.Instance.UnlockPlayerColumn(); // 解锁牌列
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

    public partial class CardEffect
    {
        public void MeleeActionEnter()
        {
            if (isLock) return;
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                isLock = true;
                CardManager.Instance.LockPlayerColumn(); // 锁定牌列，防止在移动过程中打出其他卡牌
                //Central.Instance.UnitSelectEvent.RemoveListener(MeleeSelectUnit); // 取消监听防呆

                Central.Instance.UnitSelectEvent.AddListener(MeleeSelectUnit); // 监听单位被选中事件
                Central.Instance.ActionEndEarly.AddListener(MeleeActionEndEarly); // 监听行动提前结束事件
                UnitCommandManager.Instance.ActionSequenceEnd.AddListener(MeleeActionEnd);

                UnitManager.Instance.units.ForEach(unit =>
                {
                    unit.unitElement.ResetAttack(); // 重置攻击次数
                });
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_CanMelee");
                }); // 启用单位选择，只能选择可近战单位
            }
        }

        private void MeleeActionEnd()
        {
            MapManager.Instance.EnableUnitPick(unit =>
            {
                return unit.unitElement.CheckTraits("Trait_CanMelee") && unit.unitElement.currentAttackTime > 0;
            });
            //Central.Instance.UnitSelectEvent.RemoveListener(MeleeSelectUnit); // 取消监听，防呆
            Central.Instance.UnitSelectEvent.AddListener(MeleeSelectUnit); // 重新监听单位被选中事件
            MeleeCheckEnd(); // 检查是否还有单位可以近战
        }

        private void MeleeCheckEnd()
        {
            List<Units> _units = UnitManager.Instance.units;
            foreach (var unit in _units)
            {
                if (unit.unitElement.currentAttackTime > 0 &&
                    unit.unitElement.CheckTraits("Trait_CanMelee")) return;
            }
            MeleeCommandEnd();
        }

        private void MeleeSelectUnit(Units _unit)
        {
            if (!_unit.unitElement.CheckTraits("Trait_CanMelee")) return;
            if (_unit.unitElement.currentAttackTime <= 0) return;
            UnitCommandManager.Instance.PushCommand_Front(new(
                _unit,
                ActionType.WaitForMelee,
                new(0, 0),
                true
                )); // 推入等待近战指令
            Central.Instance.ActionStart?.Invoke();
            Central.Instance.UnitSelectEvent.RemoveListener(MeleeSelectUnit); // 取消监听，防止重复选择
            //Central.Instance.UnitSelectEvent.RemoveListener(MeleeSelectUnit); // 取消监听，防止重复选择
        }

        private void MeleeActionEndEarly()
        {
            MeleeCommandEnd();
        }

        private void MeleeCommandEnd()
        {
            MapManager.Instance.DisableAllCell();
            CardManager.Instance.UnlockPlayerColumn(); // 解锁牌列
            Central.Instance.UnitSelectEvent.RemoveListener(MeleeSelectUnit); // 取消监听
            UnitCommandManager.Instance.ActionSequenceEnd.RemoveListener(MeleeActionEnd);
            isLock = false;
        }
    } // 近战指令部分

    public partial class CardEffect
    {
        public void RangedActionEnter()
        {
            if (isLock) return;
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                isLock = true;
                CardManager.Instance.LockPlayerColumn(); // 锁定牌列，防止在移动过程中打出其他卡牌
                Central.Instance.UnitSelectEvent.AddListener(RangedSelectUnit); // 监听单位被选中事件
                Central.Instance.ActionEndEarly.AddListener(RangedActionEndEarly); // 监听行动提前结束事件
                UnitCommandManager.Instance.ActionSequenceEnd.AddListener(RangedActionEnd);
                UnitManager.Instance.units.ForEach(unit =>
                {
                    unit.unitElement.ResetAttack(); // 重置攻击次数
                });
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_CanRanged");
                }); // 启用单位选择，只能选择可远程单位
            }
        }

        private void RangedActionEnd()
        {
            MapManager.Instance.EnableUnitPick(unit =>
            {
                return unit.unitElement.CheckTraits("Trait_CanRanged") && unit.unitElement.currentAttackTime > 0;
            });
            Central.Instance.UnitSelectEvent.AddListener(RangedSelectUnit); // 重新监听单位被选中事件
            RangedCheckEnd(); // 检查是否还有单位可以远程
        }

        private void RangedCheckEnd()
        {
            List<Units> _units = UnitManager.Instance.units;
            foreach (var unit in _units)
            {
                if (unit.unitElement.currentAttackTime > 0 &&
                    unit.unitElement.CheckTraits("Trait_CanRanged")) return;
            }
            RangedCommandEnd();
        }

        private void RangedSelectUnit(Units _unit)
        {
            if (!_unit.unitElement.CheckTraits("Trait_CanRanged")) return;
            if (_unit.unitElement.currentAttackTime <= 0) return;
            UnitCommandManager.Instance.PushCommand_Front(new(
                _unit,
                ActionType.WaitForRanged,
                new(0, 0),
                true
                )); // 推入等待远程指令
            Central.Instance.ActionStart?.Invoke();
            Central.Instance.UnitSelectEvent.RemoveListener(RangedSelectUnit); // 取消监听，防止重复选择
        }

        private void RangedActionEndEarly()
        {
            RangedCommandEnd();
        }

        private void RangedCommandEnd()
        {
            MapManager.Instance.DisableAllCell();
            CardManager.Instance.UnlockPlayerColumn(); // 解锁牌列
            Central.Instance.UnitSelectEvent.RemoveListener(RangedSelectUnit); // 取消监听
            UnitCommandManager.Instance.ActionSequenceEnd.RemoveListener(RangedActionEnd);
            isLock = false;
        }
    } // 远程指令部分
}