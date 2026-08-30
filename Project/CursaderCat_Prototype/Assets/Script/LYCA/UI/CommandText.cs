using System.Collections;
using TMPro;
using Unit;
using UnityEngine;

public class CommandText : MonoBehaviour, IInitialiazer
{
    public static CommandText Instance { get; private set; }

    [Header("Component")]
    public TextMeshProUGUI commandText;

    public void Initialize()
    {
        Instance = this;
    }

    private void Awake()
    {
        //UnitCommandManager.Instance.ActionSequenceEnd.AddListener(ClearText);
    }

    public void UpdateCommandText(string text)
    {
        commandText.text = "";
        StopCoroutine(Enumerator());
        StartCoroutine(Enumerator());
        IEnumerator Enumerator()
        {
            commandText.text = text;
            yield return new WaitForSeconds(2f);
            commandText.text = "";
        }
    }
}
