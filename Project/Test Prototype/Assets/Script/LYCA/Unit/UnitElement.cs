using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

namespace Unit
{
    public class UnitElement : MonoBehaviour
    {
        [Header("Parameter")]
        public int health;
        public int attack;
        public int defend;
        public int speed;

        [Header("Temp Addition")]
        public int tempHealth;
        public int tempAttack;
        public int tempDefend;
        public int tempSpeed;

        [Header("Traits")]
        public List<Trait> traits;
    }
}