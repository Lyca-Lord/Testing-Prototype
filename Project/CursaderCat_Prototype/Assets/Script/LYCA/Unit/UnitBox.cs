using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unit
{
    [Serializable]
    [CreateAssetMenu(fileName = "NewUnitBox", menuName = "Unit/UnitBox", order = 2)]
    public class UnitBox : ScriptableObject
    {
        [Header("B ¡¤ O ¡¤ X")]
        public string boxName;
        public List<UnitInfo> unitInfos;
    }
}
