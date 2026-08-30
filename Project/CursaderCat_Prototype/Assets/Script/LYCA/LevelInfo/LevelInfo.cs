using System;
using System.Collections.Generic;
using Unit;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelInfo", menuName = "Level/LevelInfo", order = 3)]
public class LevelInfo : ScriptableObject
{
    [Header("Battle Map")]
    public LevelMapInfo mapInfo;

    [Header("Deck Reference")]
    public CardDeckInfo enemyDeck;
    public UnitBox enemyBox;
    public UnitBox playerBox;

    [Header("Army Expectation")]
    public List<EnemyUnitPriorPair> weightList;
    public int armyScale; // 军队规模上限，超过上限不再增援
    public int retreatScale; // 军队规模下限，低于下限开始撤退

    private void OnValidate()
    {
    }

    [Serializable]
    public class EnemyUnitPriorPair
    {
        public string unitName;
        public float weight;

        public EnemyUnitPriorPair(string unitName, float weight)
        {
            this.unitName = unitName;
            this.weight = weight;
        }

        public EnemyUnitPriorPair()
        {
            this.unitName = "";
            this.weight = 0;
        }
    }
}