using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TurnText : MonoBehaviour
{
    [Header("Component")]
    public TextMeshProUGUI tmp;

    private void Awake()
    {
        Central.Instance.TurnBeginEvent.AddListener(ChangeText);
    }

    private void ChangeText()
    {
        if (Central.Instance.isPlayer)
        {
            tmp.text = "ÎÒ·½";
        }
        else
        {
            tmp.text = "µÐ·½";
        }
    }
}
