using System.Collections.Generic;
using Unit;
using UnityEngine;

namespace Map
{
    [RequireComponent(typeof(BoxCollider2D))]
    public partial class MapCell : MonoBehaviour
    {
        [Header("Sprite")]
        public Sprite oddSprite;
        public Sprite evenSprite;

        [Header("Index")]
        public int index;
        public int type = 0; // 0为普通格子，1为障碍格子，2为水格子，可根据需要扩展
        public Vector2 location; // 地图格子在二维数组中的位置

        [Header("Contain")]
        public Units unit; // 当前格子包含的单位，若无则为null
        public List<Vector2> movePath;
        public int distance;

        public Vector2 Position => transform.position;

        public float CellSize => sr.bounds.size.x + 0.05f; // 假设格子为正方形，返回格子大小

        public void CellRelease() => unit = null; // 当单位离开格子时调用

        public void CellRegister(Units _unit)
        {
            unit = _unit;
            unit.ChangeSortingOverlay(this.sr.sortingOrder);
            unit.transform.SetParent(unitParent);
            unit.transform.position = Position;
            unit.location = location;
            unit.cell = this;
        }// 当单位进入格子时调用

        public bool IsWalkable(bool _isPlayer)
        {
            if (unit != null) return unit.isPlayer == _isPlayer;
            return type != 1; // 假设type为1的格子不可行走，其他类型的格子可行走
        }
    } // 地图单元储存信息

    public partial class MapCell
    {
        [Header("Component")]
        public SpriteRenderer sr;
        public SpriteRenderer highlight;
        public SpriteRenderer indicator;
        public SpriteRenderer pathIndicator;
        public BoxCollider2D cd;
        public Transform unitParent;

        private void OnValidate()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        [Header("Parameter")]
        public float clickOffsetY = -0.1f;
        private bool isEnable = false;
        private bool isClick = false;

        public void ClickDown()
        {
            Vector3 pos = transform.position;
            pos.y += clickOffsetY;
            transform.position = pos;
            isClick = true;
        } // 点击下沉反馈

        public void ClickUp()
        {
            if (!isClick) return;
            Vector3 pos = transform.position;
            pos.y -= clickOffsetY;
            transform.position = pos;
            isClick = false;
        } // 点击抬起反馈
    } // 引用组件和参数

    public partial class MapCell
    {
        public void Initialize(int _index, Vector2 _position, Vector2 _location)
        {
            location = _location;
            transform.position = _position;
            index = _index;

            indicator.gameObject.SetActive(false);
            highlight.gameObject.SetActive(false);

            sr.sprite = ((index + _location.x) % 2 == 0) ? evenSprite : oddSprite;

            sr.sortingOrder = _index;
            highlight.sortingOrder = _index + 1;
            indicator.sortingOrder = _index + 2;
            pathIndicator.sortingOrder = _index + 3;
        }

        public void DestoryCell()
        {
            Destroy(gameObject);
        }
    } // 地图单元生命周期(含初始化)

    public partial class MapCell
    {
        [Header("Control")]
        private bool isMouseEnter = false;

        public void OnMouseEnter()
        {
            isMouseEnter = true;
            indicator.gameObject.SetActive(true);
            if (movePath.Count > 0) ShowAllPathIndicator();
        }

        public void OnMouseExit()
        {
            isMouseEnter = false;
            indicator.gameObject.SetActive(false);

            if (movePath.Count > 0) CloseAllPathIndicator();
            ClickUp();
        }

        private void OnMouseDown()
        {
            ClickDown();
        }

        private void OnMouseUp()
        {
            if (!isMouseEnter) return;
            Debug.Log("Click");
            ClickUp();
            if (isEnable)
            {
                Central.Instance.ClickEvent?.Invoke(location);
                if (unit != null) Central.Instance.UnitSelectEvent?.Invoke(unit); // 思考，这个真的要区分吗
                if (movePath.Count > 0) CloseAllPathIndicator();
            } // 核心要素，鼠标点击检测
        } // 鼠标检测

        private void ShowAllPathIndicator()
        {
            foreach (var i in movePath)
            {
                MapCell _cell = MapManager.Instance.FindCellByLocation(i);
                _cell.pathIndicator.gameObject.SetActive(true);
            }
        }

        private void CloseAllPathIndicator()
        {
            foreach (var i in movePath)
            {
                MapCell _cell = MapManager.Instance.FindCellByLocation(i);
                _cell.pathIndicator.gameObject.SetActive(false);
            }
        }
    }

    public partial class MapCell
    {
        public void EnableClick()
        {
            isEnable = true;
            highlight.gameObject.SetActive(true);
        }

        public void DisableClick()
        {
            isEnable = false;
            highlight.gameObject.SetActive(false);
        }
    } // 其他功能拓展
}
