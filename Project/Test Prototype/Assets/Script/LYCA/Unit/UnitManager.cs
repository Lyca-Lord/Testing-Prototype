using Map;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unit
{
    public partial class UnitManager : MonoBehaviour
    {
        public static UnitManager Instance;

        [Header("Units")]
        public List<Units> units;
        public GameObject unitPrefab;

        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame();
            //Units unit = FindFirstObjectByType<Units>();
            //if (unit != null)
            //{
            //    unit.SetUp(MapManager.Instance.FindCellByLocation(new(0, 0)));
            //    UnitCommandManager.Instance.PushCommand_Front(new(
            //        unit,
            //        ActionType.WaitForMove,
            //        new(1, 1),
            //        false
            //        ));
            //    Central.ActionStart?.Invoke();
            //}
            foreach(var i in units)
            {
                i.SetUp(MapManager.Instance.FindCellByLocation(i.location));
            }
        }

        public void CreateUnit(Vector2 _location, UnitInfo _info = null)
        {
            MapCell cell = MapManager.Instance.FindCellByLocation(_location);
            GameObject gameObject = Instantiate(unitPrefab);
            Units unit = gameObject.GetComponent<Units>();
            unit.SetUp(cell);
            units.Add(unit);
        }
    }

    public partial class UnitManager
    {
        [Header("Material")]
        public Material normalMaterial = null;
        public Material hitMaterial;
    } // 巧思部分，把材质素材等放在这里
}