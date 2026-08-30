using Map;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unit
{
    public partial class UnitManager : MonoBehaviour, IInitialiazer
    {
        public static UnitManager Instance;

        [Header("Units")]
        public List<Units> units;
        public GameObject unitPrefab;

        [Header("Closure")]
        public UnitInfo unitInfoWaitForReinforce;
        public bool isPlayerWaitForReinforce;
        public bool isLockedWaitForReinforce;
        public bool isCostFree;
        public Action AfterReinforceAction;

        public void Initialize()
        {
            Instance = this;
        }

        private void Awake()
        {
        }

        public void Register()
        {
        }

        public void CreateUnit(Vector2 _location)
        {
            if (!isCostFree) CardManager.Instance.ReduceCost(unitInfoWaitForReinforce.cost);

            MapCell cell = MapManager.Instance.FindCellByLocation(_location);
            GameObject gameObject = Instantiate(unitInfoWaitForReinforce.unitPrefab);
            Units unit = gameObject.GetComponent<Units>();
            unit.SetUp(
                cell, 
                unitInfoWaitForReinforce.unitName, 
                isPlayerWaitForReinforce, 
                isLockedWaitForReinforce
                );
            units.Add(unit);

            AfterReinforceAction?.Invoke();
            //if (!isPlayerWaitForReinforce)
            //    Central.Instance.ComputerPlayerReinforceEvent?.Invoke(
            //        unitInfoWaitForReinforce.index
            //        );
        }

        public void SetUnit(UnitInfo _info, bool _isPlayer, bool _isLocked, bool _isCostFree, Action _OnDone = null) 
        {
            isCostFree = _isCostFree;
            unitInfoWaitForReinforce = _info;
            isPlayerWaitForReinforce = _isPlayer;
            isLockedWaitForReinforce = _isLocked;
            AfterReinforceAction = _OnDone;
        }

        public void UnlockUnit(bool _isPlayer)
        {
            foreach (var i in units)
            {
                if (i.isPlayer == _isPlayer) i.isLocked = false;
            }
        }
    }

    public partial class UnitManager
    {
        [Header("Material")]
        public Material normalMaterial = null;
        public Material hitMaterial;
    } // 巧思部分，把材质素材等放在这里
}