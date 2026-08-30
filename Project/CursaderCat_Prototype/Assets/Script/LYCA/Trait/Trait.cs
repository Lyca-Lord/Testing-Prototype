using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[CreateAssetMenu(fileName = "NewTrait", menuName = "Trait", order = 1)]
public class Trait : ScriptableObject
{
    public string traitName;
    public string description;
    public Sprite icon;
}
