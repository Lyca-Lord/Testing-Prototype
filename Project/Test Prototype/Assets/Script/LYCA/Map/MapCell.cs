using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace Map 
{
    [RequireComponent(typeof(BoxCollider2D))]
    public partial class MapCell : MonoBehaviour
    {
        [Header("Index")]
        public int index;

        public Vector2 Position => transform.position;

        public float CellSize => sr.bounds.size.x; // 假设格子为正方形，返回格子大小

        public void CellRelease() { } // 当单位离开格子时调用

        public void CellRegister() { } // 当单位进入格子时调用
    } // 地图单元储存信息

    public partial class MapCell
    {
        [Header("Component")]
        public SpriteRenderer sr;
        public SpriteRenderer highlight;
        public SpriteRenderer indicator;
        public BoxCollider2D cd;

        private void OnValidate()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        [Header("Parameter")]
        public float clickOffsetY = -0.1f;
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
        public void Initialize(int _index, Vector2 _position)
        {
            transform.position = _position;
            index = _index;

            indicator.gameObject.SetActive(false);
            highlight.gameObject.SetActive(false);

            sr.sortingOrder = indicator.sortingOrder = highlight.sortingOrder
                = _index;
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
        }

        public void OnMouseExit()
        {
            isMouseEnter = false;
            indicator.gameObject.SetActive(false);
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
        } // 核心要素，鼠标点击检测
    } // 鼠标检测
}
