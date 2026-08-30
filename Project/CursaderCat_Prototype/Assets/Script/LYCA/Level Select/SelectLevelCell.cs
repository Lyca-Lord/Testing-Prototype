using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SelectLevelCell : MonoBehaviour, IPointerClickHandler
{
    [Header("Reference")]
    public LevelInfo levelInfo;
    public Outline outline;

    [Header("Unity Event")]
    public UnityEvent<SelectLevelCell> onLevelPick = new();

    private void Awake()
    {
        outline.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onLevelPick.Invoke(this);
    }
}
