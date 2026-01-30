using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit
{
    public class UnitInfo : ScriptableObject
    {
        [Header("Information")]
        public string unitName;
        public string description;
        public Sprite unitSprite;

        [Header("Element")]
        public int maxHP;
        public int attack;
        public int defense;
        public float speed;
        public float attackRange;

        [Header("Traits")]
        public List<Trait> traits;
    }
}