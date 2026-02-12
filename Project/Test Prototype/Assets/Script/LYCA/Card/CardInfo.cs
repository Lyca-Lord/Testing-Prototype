using System.Collections.Generic;
using UnityEngine;

// 右键Create -> Card -> CardInfo 创建卡片配置文件
[CreateAssetMenu(fileName = "NewCardInfo", menuName = "Card/CardInfo", order = 1)]
public class CardInfo : ScriptableObject
{
    [Header("卡片基础信息")]
    public int cardId;          // 卡片唯一编号
    public Sprite cardSprite;   // 卡片显示的精灵图
}