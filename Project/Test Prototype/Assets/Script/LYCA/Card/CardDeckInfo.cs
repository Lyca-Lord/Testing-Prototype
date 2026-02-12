using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewCardInfo", menuName = "Card/CardDeckInfo", order = 2)]
public class CardDeckInfo : ScriptableObject
{
    [Header("卡组信息")]
    public string deckName;             // 卡组名称
    public List<CardInfo> cardList;     // 卡组中的卡片列表
}