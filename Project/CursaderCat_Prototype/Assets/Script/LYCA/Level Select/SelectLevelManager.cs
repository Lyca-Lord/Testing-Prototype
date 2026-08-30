using System.Collections.Generic;
using UnityEngine;

public class SelectLevelManager : MonoBehaviour
{
    [Header("Select Level Settings")]
    public List<SelectLevelCell> levelCells;

    [Header("Reference")]
    public SelectLevelCell currentSelectedCell;
    public GameObject boardCanvas;

    private void Awake()
    {
        foreach (var cell in levelCells)
        {
            cell.onLevelPick.AddListener(OnLevelPick);
        }
    }

    private void OnLevelPick(SelectLevelCell _cell)
    {
        if (currentSelectedCell != null)
            currentSelectedCell.outline.enabled = false;
        currentSelectedCell = _cell;
        currentSelectedCell.outline.enabled = true;
    }

    public void StartGame()
    {
        if (currentSelectedCell == null)
        {
            Debug.LogWarning("No level selected! Please select a level before starting the game.");
            return;
        }
        Central.Instance.StartGame(currentSelectedCell.levelInfo);
        boardCanvas.SetActive(false);
    }
}
