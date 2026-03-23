using Map;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unit
{
    public partial class TargetManager : MonoBehaviour, IInitialiazer
    {
        public static TargetManager Instance;

        [Header("PreferredTarget")]
        public Dictionary<Units, Units> preferredTarget = new();

        public void Initialize()
        {
            Central.Instance.UnitNumChangeEvent.AddListener(PickTarget);
            Central.Instance.UnitNumChangeEvent.AddListener(CheckBarrackPrior);
            Instance = this;
        }

        private void Start()
        {
            UnitCommandManager.Instance.ActionSequenceEnd.AddListener(PickTarget);
        }

        public void PickTarget()
        {
            preferredTarget.Clear();

            List<Units> enemyUnits = UnitManager.Instance.units.Where(u => u != null && !u.isPlayer).ToList();
            List<Units> playerUnits = UnitManager.Instance.units.Where(u => u != null && u.isPlayer).ToList();

            foreach (var e in enemyUnits)
            {
                Units best = null;
                float bestScore = float.PositiveInfinity;

                foreach (var p in playerUnits)
                {
                    if (p == null) continue;
                    int dis = MapManager.Instance.Distance(e.location, p.location, "Manhattan");

                    // 你给的 pi：棋子=1，军营=1.1
                    float pi = p.unitElement != null && p.unitElement.CheckTraits("Trait_Flag") ? 1.1f : 1f;

                    float score = dis * (5f - pi);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = p;
                    }
                }

                preferredTarget[e] = best;
            }
        }
    } // 敌人优先目标选择

    public partial class TargetManager
    {
        [Header("Barrack Prior")]
        public Dictionary<Units, float> barrackPrior = new();

        private void CheckBarrackPrior()
        {
            barrackPrior.Clear();
            List<Units> barracks = UnitManager.Instance.units.Where(
                u => u != null && !u.isPlayer && u.unitElement.CheckTraits("Trait_Flag")
                ).ToList();

            foreach (var barrack in barracks)
            {
                int x = MapManager.Instance.GetMapWidth() - (int)barrack.location.x;
                int y = MapManager.Instance.GetMapHeight() - (int)barrack.location.y;
                float dis = Mathf.Sqrt(1f * (x * x + y * y));
                barrackPrior[barrack] = dis;
            }
        }
    } // 标记军营优先度
}

