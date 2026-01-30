using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit
{
    public partial class UnitElement : MonoBehaviour
    {
        [Header("Parameter")]
        public int tacticSpeed;
        public int health;
        public int attack;
        public int defend;
        public int speed;

        [Header("Temp Addition")] // 回合结束后清空的增益
        public int tempHealth;
        public int tempAttack;
        public int tempDefend;
        public int tempSpeed;

        [Header("Traits")]
        public List<Trait> traits;

        [Header("Current Parameter")]
        public int currentSpeed = 0;
        public int currentHealth = 0;
        public int currentAttackTime = 0;
        public int currentTacticSpeed = 0;

        public bool CheckTraits(string _traitName)
            => traits.Find(t => t.name == _traitName) != null;

        public void ResetMove()
        {
            currentSpeed = speed + tempSpeed;
            //Debug.Log("重置移动力 " + this.ToString());
        } 

        /// <summary>
        /// 重置战术调整
        /// 只应该在回合开始时调用
        /// </summary>
        public void ResetTactic() => currentTacticSpeed = tacticSpeed; 

        public void ResetAttack() => currentAttackTime = 1;
    }

    public partial class UnitElement
    {
        public void DecreaseCurrentSpeed(int _tmp) => currentSpeed -= _tmp;

        public void DecreaseCurrentTacticSpeed(int _tmp) => currentTacticSpeed -= _tmp;

        public void DecreaseHealth(int _tmp) => currentHealth -= _tmp;
    } // 实时计算部分
}