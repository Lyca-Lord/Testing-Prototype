using System;
using System.Collections.Generic;
using Unit;
using UnityEngine;

namespace Map
{
    public partial class MapManager
    {
        public static MapManager Instance;

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
                    cell.Initialize(index, position * cell.CellSize, new(i, j));
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
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            TestingCode();
        }

        private void TestingCode()
        {
            CreateMap();
        }
    }  // 生命周期部分

    public partial class MapManager
    {
        public int GetMapWidth() { return mapWidth; }

        public int GetMapHeight() { return mapHeight; }

        public List<MapCell> GetCellList() { return cellList; }

        public MapCell FindCellByLocation(Vector2 _location)
        {
            foreach (var cell in cellList)
            {
                if (cell.location == _location) return cell;
            }
            Debug.LogWarning("未找到对应位置的格子，返回null");
            return null;
        }

        public void EnableCellByRange(Vector2 _pivot, int _radius, string _mode)
        {
            DisableAllCell(); // 先禁用所有格子
            foreach (var cell in cellList)
            {
                float distance = Distance(_pivot, cell.location, _mode);
                if (distance <= _radius) // 在允许范围内
                {
                    cell.EnableClick();
                }
            }
        } // 允许范围内的格子被点击

        public void EnableUnitPick(Func<Units, bool> Check = null)
        {
            DisableAllCell(); // 先禁用所有格子
            if (Check == null) Check = (unit) => true;
            foreach (var cell in cellList)
            {
                if (cell.unit != null && Check(cell.unit)) // 包含单位且通过检查
                {
                    cell.EnableClick();
                }
            }
        } // 允许所有包含单位的格子被点击

        public void DisableAllCell()
        {
            foreach(var cell in cellList)
            {
                cell.DisableClick();
            }
        }
    } // 访问器部分

    public partial class MapManager
    {
        public int Distance(Vector2 _a, Vector2 _b, string _mode)
        {
            if (_mode == "Manhattan")
            {
                return (int)(Mathf.Abs(_a.x - _b.x) + Mathf.Abs(_a.y - _b.y));
            }
            else if (_mode == "Chebyshev")
            {
                return (int)(Mathf.Max(Mathf.Abs(_a.x - _b.x), Mathf.Abs(_a.y - _b.y)));
            }
            else
            {
                Debug.LogWarning("未知的距离计算模式，返回-1");
                return -1;
            }
        } // 根据mode判断采用曼哈顿距离还是切比雪夫距离
    } // 其他功能部分
}