using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Map
{
    public partial class MapManager
    {
        [Header("Reference")]
        [SerializeField] private GameObject cellPrefab;

        [Header("Parameter")]
        [SerializeField][Tooltip("请用函数访问")] private int mapWidth = 10;
        [SerializeField][Tooltip("请用函数访问")] private int mapHeight = 10;

        public void SetMapManager(int n, int m)
        {
            (mapWidth, mapHeight) = (n, m);
        }

    } // 引用和参数部分

    public partial class MapManager : MonoBehaviour
    {
        [Header("Cell List")]
        [SerializeField] private List<MapCell> cellList;

        public void CreateMap()
        {
            if (cellList.Count > 0)
            {
                Debug.LogWarning("地图已存在，正在清除旧地图");
                ClearMap();
            }
            for (int i = 0; i < mapHeight; i++)
            {
                for (int j = 0; j < mapWidth; j++)
                {
                    int index = i * mapWidth + j;
                    Vector2 position = new Vector2(
                        -(mapWidth * 1f / 2) + j - 0.5f,
                        (mapHeight * 1f / 2) - i - 0.5f
                        );
                    GameObject cellObj = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                    MapCell cell = cellObj.GetComponent<MapCell>();
                    cell.Initialize(index, position * cell.CellSize);
                    cellList.Add(cell);
                }
            }
        }

        public void ClearMap()
        {
            foreach (var cell in cellList) cell.DestoryCell();
            cellList.Clear();
        }
    } // 地图生成部分

    public partial class MapManager
    {
        private void Start()
        {
            TestingCode();
        }

        private void TestingCode()
        {
            CreateMap();
        }
    }  // 生命周期部分
}