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

    public partial class MapManager : MonoBehaviour, IInitialiazer
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

        public void Initialize()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
    } // 地图生成部分

    public partial class MapManager
    {
    }  // 生命周期部分

    public partial class MapManager
    {
        public int GetMapWidth() { return mapWidth; }

        public int GetMapHeight() { return mapHeight; }

        public List<MapCell> GetCellList() { return cellList; }

        public MapCell FindCellByLocation(Vector2 _location)
        {
            if (CheckPositionLegal(_location) == false) return null;
            foreach (var cell in cellList)
            {
                if (cell.location == _location) return cell;
            }
            Debug.LogWarning("未找到对应位置的格子，返回null" + _location);
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

        public void EnableForDeploying(bool _isPlayer)
        {
            int[] dx = new int[8] { 0, 1, 0, -1, 1, 1, -1, -1 };
            int[] dy = new int[8] { 1, 0, -1, 0, 1, -1, 1, -1 };

            DisableAllCell();
            foreach(var cell in cellList)
            {
                for(int i = 0; i < 8; i++)
                {
                    Vector2 target = cell.location + new Vector2(dx[i], dy[i]);
                    Units _unit = FindCellByLocation(target)?.unit;
                    if (_unit != null)
                    {
                        if (_unit.unitElement.CheckTraits("Trait_Flag") && _isPlayer == _unit.isPlayer)  
                            cell.EnableClick();
                    }
                }
            }
        }

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
            foreach (var cell in cellList)
            {
                cell.DisableClick();
            }
        }

        private bool CheckPositionLegal(Vector2 _position)
        {
            if (_position.x < 0 || _position.y < 0 || _position.x >= mapHeight || _position.y >= mapWidth)
                return false;
            return true;
        } // 检查位置是否合法
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

    public partial class MapManager
    {
        public struct CellNode
        {
            public Vector2 location;
            public Vector2 parent;
            public int distance;
            public List<Vector2> path;

            public CellNode(Vector2 _location, int _distance, Vector2 _parent)
            {
                location = _location;
                distance = _distance;
                parent = _parent;
                path = new();
            }
        }

        public void ClaerAllCellPath()
        {
            foreach (var i in cellList)
            {
                i.movePath.Clear();
                i.distance = 10001;
            }
            HideAllPath();
        } // 在移动之后再调用

        /// <summary>
        /// 用SPFA寻找从startPoint到endPoint的路径，只将路径粗存在endPoint的movePath中
        /// step为移动步数限制，isPlayer表示是否为玩家单位（可能影响路径选择）
        /// 注意cell的type为2的格子需要消耗2行动力
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="step"></param>
        /// <param name="isPlayer"></param>
        public void FindMovePath(Vector2 startPoint, Vector2 endPoint, int step, bool isPlayer)
        {
            Queue<CellNode> queue = new();
            //Debug.LogWarning("Find Move Path");
            //List<Vector2> locations = new();

            int[] dx, dy;
            int signx = startPoint.x < endPoint.x ? 1 : -1;
            int signy = startPoint.y < endPoint.y ? 1 : -1;
            if (Mathf.Abs(startPoint.x - endPoint.x) > Mathf.Abs(startPoint.y - endPoint.y))
            {
                (dx, dy) = (
                    new int[4] { signx, 0, 0, -signx },
                    new int[4] { 0, signy, -signy, 0 }
                    );
            }
            else
            {
                (dx, dy) = (
                    new int[4] { 0, signx, -signx, 0 },
                    new int[4] { signy, 0, 0, -signy }
                    );
            }

            CellNode _node = new(startPoint, 0, startPoint);
            _node.path.Add(startPoint);
            queue.Enqueue(_node);

            while (queue.Count > 0)
            {
                CellNode node = queue.Dequeue();
                if (node.location == endPoint)
                {
                    MapCell cell = FindCellByLocation(endPoint);
                    cell.distance = node.distance;
                    cell.movePath = node.path;
                    return;
                }
                for (int i = 0; i < 4; i++)
                {
                    Vector2 nextLocation = node.location + new Vector2(dx[i], dy[i]);
                    if (nextLocation.x < 0 || nextLocation.y < 0 || nextLocation.x > mapHeight || nextLocation.y > mapWidth) continue;

                    MapCell nextCell = FindCellByLocation(nextLocation);
                    if (nextCell == null) continue; // 越界
                    int cost = nextCell.type == 2 ? 2 : 1; // 不同地形的行动力消耗

                    if (node.distance + Distance(node.location, endPoint, "Manhattan") > step) continue; // 超出步数限制
                    if (!nextCell.IsWalkable(isPlayer)) continue; // 有单位阻挡

                    CellNode nextNode = new(nextLocation, node.distance + cost, node.location);
                    nextNode.path.AddRange(node.path);
                    nextNode.path.Add(nextLocation);
                    queue.Enqueue(nextNode);
                }
            }
        }

        public void HighLightMovePath(Vector2 startPoint, int step, bool isPlayer)
        {
            DisableAllCell(); // 先禁用所有格子
            foreach (var i in cellList)
            {
                if (Distance(i.location, startPoint, "Manhattan") > step) continue;
                FindMovePath(startPoint, i.location, step, isPlayer); // 每次只找一个点的最短路径

                if (i.movePath.Count > 0) i.EnableClick();
            }
        }

        public void HideAllPath()
        {
            foreach (var i in cellList) i.pathIndicator.gameObject.SetActive(false);
        }

        public void ShowPath(List<Vector2> path)
        {
            foreach (var i in cellList) i.pathIndicator.gameObject.SetActive(false);
            foreach (var location in path)
            {
                MapCell cell = FindCellByLocation(location);
                if (cell != null) cell.pathIndicator.gameObject.SetActive(true);
            }
        }
    } // 寻路部分
}