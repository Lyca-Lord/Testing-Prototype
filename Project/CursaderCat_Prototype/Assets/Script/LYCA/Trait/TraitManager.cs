using Map;
using UnityEngine;

namespace Unit
{
    public partial class TraitManager : MonoBehaviour
    {
        public void Start()
        {
            Central.Instance.MeleeEnd.AddListener(TraitMachine_Trait_FightwithShield);
            Central.Instance.WaitForMeleeAction.AddListener(TraitMachine_Trait_CavalryCharge);
            Central.Instance.MeleeEnd.AddListener(TraitMachine_Trait_Pull);
        }
    }

    public partial class TraitManager
    {
        /// <summary>
        /// 防御式进攻
        /// 进攻后获得一点永久护盾，0级时上限为2点
        /// 这个函数应该订阅MeleeEnd的事件，在攻击结束时触发
        /// </summary>
        private void TraitMachine_Trait_FightwithShield(UnitCommand _command)
        {
            Units unit = _command.selectedUnit;
            if (unit == null)
            {
                Debug.Log("技能相应单位不存在" + _command);
                return;
            }
            if (!unit.unitElement.CheckTraits("Trait_FightwithShield")) return;

            unit.unitElement.AddCurrentShield(1);
        }

        /// <summary>
        /// 骑兵冲锋
        /// 近战前可以且必须移动一格
        /// 这个函数应该订阅MeleeAction的事件，在行动序列的首部插入一次战术调整移动
        /// </summary>
        /// <param name="_command"></param>
        private void TraitMachine_Trait_CavalryCharge(UnitCommand _command)
        {
            Units unit = _command.selectedUnit;
            if (unit == null)
            {
                Debug.Log("技能相应单位不存在" + _command);
                return;
            }
            if (!unit.unitElement.CheckTraits("Trait_CavalryCharge")) return;
            //UnitCommand _firstCommand = UnitCommandManager.Instance.actionSequence.First.Value;

            if (_command.canCancel == true) _command.canCancel = false;
            else Debug.LogWarning("加入的该指令原本应该可以取消，但检测发现无法取消");

            if(_command.canSkip == false) _command.canSkip = true;
            else Debug.LogWarning("加入的该指令原本应该无法跳过，但检测发现可以跳过");

            UnitCommandManager.Instance.PushCommand_Front(
                new UnitCommand(unit, ActionType.Tactic, new(1, 1), true, false)
            ); // 这里的位置应该传入isBounsMove参数
        }

        private void TraitMachine_Trait_Pull(UnitCommand _command)
        {
            Units unit = _command.selectedUnit;
            if (unit == null)
            {
                Debug.Log("技能相应单位不存在" + _command);
                return;
            }
            if (!unit.unitElement.CheckTraits("Trait_Pull")) return;

            Units _unit = MapManager.Instance.FindCellByLocation(_command.position).unit;
            _unit.ApplyKnockback(
                unit.location, 1
                );
            UnitCommandManager.Instance.PushCommand_Front(
                new UnitCommand(unit, ActionType.None, unit.location, true, false)
            );
        }
    }
}