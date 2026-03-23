using System;
using System.Collections;
using System.Collections.Generic;
using Unit;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelInfo", menuName = "Level/LevelInfo", order = 3)]
public class LevelInfo : ScriptableObject
{
    [Header("Deck Reference")]
    public CardDeckInfo enemyDeck;
    public UnitBox enemyBox;

    [Header("Army Expectation")]
    public List<float> weightList;
    public int armyScale;

    private void OnValidate()
    {
        if (enemyBox != null)
        {
            while (weightList.Count < enemyBox.unitInfos.Count) weightList.Add(0);
        }
    }
}