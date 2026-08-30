using Map;
using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

namespace CommandCard
{
    public partial class CardEffect
    {
        [Header("Infor")]
        public bool isPlayer;
    }

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
        }

        /// <summary>
        /// 移动指令部分
        /// 当启动移动指令时，监听单位被选中事件，推入等待移动指令
        /// 防呆指令去掉了，需要测试是否会有问题
        /// </summary>
        public void MoveEnter(bool _isPlayer)
        {
            if (isLock) return;
            isPlayer = _isPlayer;
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
                    if (unit.isPlayer != isPlayer) return;
                    unit.unitElement.ResetMove(); // 重置移动力
                });
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_CanMove") && unit.isPlayer == isPlayer && !unit.isLocked;
                }); // 启用单位选择，只能选择可移动单位
            }
        }

        private void MoveActionEnd()
        {
            MapManager.Instance.EnableUnitPick(unit =>
            {
                return unit.unitElement.CheckTraits("Trait_CanMove")
                && unit.unitElement.currentSpeed > 0 && unit.isPlayer == isPlayer && !unit.isLocked;
            });
            //Central.Instance.UnitSelectEvent.RemoveListener(MoveSelectUnit); // 取消监听，防呆
            Central.Instance.UnitSelectEvent.AddListener(MoveSelectUnit); // 重新监听单位被选中事件
            MoveCheckEnd(); // 检查是否还有单位可以移动
        }

        private void MoveCheckEnd()
        {
            List<Units> _units = UnitManager.Instance.units;
            foreach (var unit in _units)
            {
                if (unit.unitElement.currentSpeed > 0 &&
                    unit.unitElement.CheckTraits("Trait_CanMove") && unit.isPlayer == isPlayer) return;
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
                true,
                false
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
        public void ReinforceEnter(bool _isPlayer)
        {
            if (isLock) return;
            StartCoroutine(Enumerator());

            IEnumerator Enumerator()
            {
                isLock = true;

                CardManager.Instance.LockPlayerColumn(); // 锁定牌列，防止在移动过程中打出其他卡牌
                Central.Instance.UnitSelectEvent.AddListener(ReinforceSelectUnit); // 监听单位被选中事件
                Central.Instance.ActionEndEarly.AddListener(ReinforceActionEndEarly); // 监听行动提前结束事件
                UnitCommandManager.Instance.ActionSequenceEnd.AddListener(ReinforceActionEnd);
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_Flag") && unit.isPlayer == _isPlayer;
                }); // 启用单位选择，只能选择可增援单位
            }
        }

        private void ReinforceActionEnd()
        {
            isLock = false;
            Central.Instance.UnitSelectEvent.RemoveListener(ReinforceSelectUnit);
            Central.Instance.ActionEndEarly.RemoveListener(ReinforceActionEndEarly); // 取消监听行动提前结束事件
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
                true,
                true
                )); // 推入增援指令
            Central.Instance.ActionStart?.Invoke();
            Central.Instance.UnitSelectEvent.RemoveListener(ReinforceSelectUnit);
        }

        private void ReinforceActionEndEarly()
        {
            ReinforceCommandEnd();
        }

        private void ReinforceCommandEnd()
        {
            MapManager.Instance.DisableAllCell();
            CardManager.Instance.UnlockPlayerColumn(); // 解锁牌列
            Central.Instance.UnitSelectEvent.RemoveListener(ReinforceSelectUnit); // 取消监听
            Central.Instance.ActionEndEarly.RemoveListener(ReinforceActionEndEarly); // 取消监听行动提前结束事件
            UnitCommandManager.Instance.ActionSequenceEnd.RemoveListener(ReinforceActionEnd);
            isLock = false;
        }
    } // 增援部分   

    public partial class CardEffect
    {
        public void MeleeActionEnter(bool _isPlayer)
        {
            if (isLock) return;
            isPlayer = _isPlayer;
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
                    if (unit.isPlayer != isPlayer) return;
                    unit.unitElement.ResetAttack(); // 重置攻击次数
                });
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_CanMelee") && unit.isPlayer == isPlayer && !unit.isLocked;
                }); // 启用单位选择，只能选择可近战单位
            }
        }

        private void MeleeActionEnd()
        {
            MapManager.Instance.EnableUnitPick(unit =>
            {
                return unit.unitElement.CheckTraits("Trait_CanMelee") && unit.unitElement.currentAttackTime > 0 && unit.isPlayer == isPlayer && !unit.isLocked;
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
                    unit.unitElement.CheckTraits("Trait_CanMelee") && unit.isPlayer == isPlayer) return;
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
                true,
                false
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
        public void RangedActionEnter(bool _isPlayer)
        {
            if (isLock) return;
            isPlayer = _isPlayer;
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
                    if (unit.isPlayer != isPlayer) return;
                    unit.unitElement.ResetAttack(); // 重置攻击次数
                });
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_CanRanged") && unit.isPlayer == isPlayer && !unit.isLocked;
                }); // 启用单位选择，只能选择可远程单位
            }
        }

        private void RangedActionEnd()
        {
            MapManager.Instance.EnableUnitPick(unit =>
            {
                return unit.unitElement.CheckTraits("Trait_CanRanged") && unit.unitElement.currentAttackTime > 0 && unit.isPlayer == isPlayer && !unit.isLocked;
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
                    unit.unitElement.CheckTraits("Trait_CanRanged") && unit.isPlayer == isPlayer) return;
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
                true,
                false
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

    public partial class CardEffect
    {
        public void TacticEnter(bool _isPlayer)
        {
            if (isLock) return;
            isPlayer = _isPlayer;
            StartCoroutine(Enumerator());
            IEnumerator Enumerator()
            {
                isLock = true;

                CardManager.Instance.LockPlayerColumn(); // 锁定牌列，防止在战术调整过程中打出其他卡牌
                Central.Instance.UnitSelectEvent.AddListener(TacticSelectUnit); // 监听单位被选中事件
                Central.Instance.ActionEndEarly.AddListener(TacticActionEndEarly); // 监听行动提前结束事件
                Central.Instance.CancelEvent.AddListener(TacticActionEndEarly);
                UnitCommandManager.Instance.ActionSequenceEnd.AddListener(TacticActionEnd);

                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableUnitPick(unit =>
                {
                    return unit.unitElement.CheckTraits("Trait_CanMove") && unit.unitElement.currentTacticSpeed > 0 && unit.isPlayer == isPlayer;
                }); // 启用单位选择，只能选择可战术调整单位
            }
        }

        private void TacticActionEnd()
        {
            MapManager.Instance.EnableUnitPick(unit =>
            {
                return unit.unitElement.CheckTraits("Trait_CanMove") && unit.unitElement.currentTacticSpeed > 0 && unit.isPlayer == isPlayer;
            });
            Central.Instance.UnitSelectEvent.AddListener(TacticSelectUnit); // 重新监听单位被选中事件
            Central.Instance.CancelEvent.AddListener(TacticActionEndEarly);
            TacticCheckEnd(); // 检查是否还有单位可以战术调整
        }

        private void TacticCheckEnd()
        {
            List<Units> _units = UnitManager.Instance.units;
            foreach (var unit in _units)
            {
                if (unit.unitElement.currentTacticSpeed > 0 &&
                    unit.unitElement.CheckTraits("Trait_CanMove") && unit.isPlayer == isPlayer) return;
            }
            TacticCommandEnd();
        }

        private void TacticSelectUnit(Units _unit)
        {
            if (!_unit.unitElement.CheckTraits("Trait_CanMove")) return;
            if (_unit.unitElement.currentTacticSpeed <= 0) return;
            UnitCommandManager.Instance.PushCommand_Front(new(
                _unit,
                ActionType.Tactic,
                new(0, 0),
                true,
                false
                )); // 推入等待战术调整指令
            Central.Instance.ActionStart?.Invoke();
            Central.Instance.UnitSelectEvent.RemoveListener(TacticSelectUnit); // 取消监听，防止重复选择
            Central.Instance.CancelEvent.RemoveListener(TacticActionEndEarly);
        }

        private void TacticActionEndEarly()
        {
            TacticCommandEnd();
        }

        private void TacticCommandEnd()
        {
            MapManager.Instance.DisableAllCell();
            CardManager.Instance.UnlockPlayerColumn(); // 解锁牌列
            Central.Instance.UnitSelectEvent.RemoveListener(TacticSelectUnit); // 取消监听
            Central.Instance.CancelEvent.RemoveListener(TacticActionEndEarly);
            UnitCommandManager.Instance.ActionSequenceEnd.RemoveListener(TacticActionEnd);
            isLock = false;
        }
    } // 战术调整指令部分

    public partial class CardEffect
    {
        public void DeployEnter(bool _isPlayer)
        {
            if (isLock) return;
            StartCoroutine(Enumerator());

            IEnumerator Enumerator()
            {
                isLock = true;

                Central.Instance.ClickEvent.AddListener(DeployOnClick); // 监听玩家点格事件
                Central.Instance.ActionEndEarly.AddListener(DeployActionEndEarly); // 监听跳过当前部署事件
                yield return new WaitForEndOfFrame();
                MapManager.Instance.EnableForDeploying(_isPlayer); // 部署阶段特供
            }
        }

        private void DeployOnClick(Vector2 _pos)
        {
            MapCell cell = MapManager.Instance.FindCellByLocation(_pos);
            if (cell == null || cell.unit != null) return; // 格子有人则不放

            DeployCommandEnd();
            UnitManager.Instance.CreateUnit(_pos); // 在点击的格子创建单位，并触发 AfterReinforceAction 回调
        }

        private void DeployActionEndEarly()
        {
            // 玩家跳过本次部署：直接结束，不放置单位，仍然触发 AfterReinforceAction 通知 DeployPhaseManager 继续
            DeployCommandEnd();
            UnitManager.Instance.AfterReinforceAction?.Invoke();
        }

        private void DeployCommandEnd()
        {
            MapManager.Instance.DisableAllCell();
            Central.Instance.ClickEvent.RemoveListener(DeployOnClick); // 取消监听点格
            Central.Instance.ActionEndEarly.RemoveListener(DeployActionEndEarly); // 取消监听跳过事件
            isLock = false;
        }
    } // 初始部署指令部分
}