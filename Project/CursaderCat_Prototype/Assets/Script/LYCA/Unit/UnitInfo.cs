using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewUnit", menuName = "Unit/UnitInfo", order = 1)]
    public class UnitInfo : ScriptableObject
    {
        [Header("Information")]
        public string unitName;
        public string titile; // 区别在于，unitName是程序索引，title是显示用的
        public string description;
        public Sprite unitSprite;

        [Header("Element")]
        public int maxHP;
        public int attack;
        public int defense;
        public float speed;
        public float attackRange;

        [Header("Cost")]
        public int cost;
        public int maxi;

        [Header("Traits")]
        public List<Trait> traits;

        [Header("Prefab")]
        public GameObject unitPrefab;

        [Header("AI Tag")]
        public bool isMeleeInclination;
        public bool isRangeInclination;

        private void OnValidate()
        {
            unitName = this.name;
        }
    }
}