using Map;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unit
{
    public partial class TargetManager : MonoBehaviour, IInitialiazer
    {
        public static TargetManager Instance;

        //[Header("PreferredTarget")]
        //public Dictionary<Units, Units> preferredTarget = new(); 
        // 字典存在问题，由于ai引用的时候复制了一份，并不会跟随picktarget实时刷新
        [Header("Reinforce Priority")]
        public Dictionary<string, float> unitPriority = new();

        public void Initialize()
        {
            Central.Instance.UnitNumChangeEvent.AddListener(PickTarget);
            Central.Instance.UnitNumChangeEvent.AddListener(CheckBarrackPrior);
            Central.Instance.UnitNumChangeEvent.AddListener(CheckBarrackThreaten);
            Instance = this;

            InitializeDictionary();

            LevelInfo levelInfo = Central.Instance.levelInfo;
            foreach (var i in levelInfo.weightList)
            {
                unitPriority[i.unitName] = i.weight;
            }
        }

        private void Start()
        {
            UnitCommandManager.Instance.ActionSequenceEnd.AddListener(PickTarget);
            UnitCommandManager.Instance.ActionSequenceEnd.AddListener(CheckBarrackThreaten);
        }

        public void PickTarget()
        {
            //preferredTarget.Clear();

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

                //preferredTarget[e] = best;
                e.actionTarget = best;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            //if (preferredTarget == null) return;
            if (UnitManager.Instance == null) return;
            List<Units> enemyUnits = UnitManager.Instance.units.Where(u => u != null && !u.isPlayer).ToList();

            Gizmos.color = Color.red;
            foreach (var kv in enemyUnits)
            {
                Units attacker = kv;
                Units target = kv.actionTarget;
                if (attacker == null || target == null) continue;

                Vector3 from = attacker.transform.position;
                Vector3 to = target.transform.position;
                Gizmos.DrawLine(from, to);

                // 在攻击者位置画一个小球作为起点标记
                Gizmos.DrawSphere(from, 0.1f);
            }
        }
#endif
    } // 敌人优先目标选择

    public partial class TargetManager
    {
        [Header("Barrack Prior")]
        public Dictionary<Units, float> barrackPrior;
        public Dictionary<Units, bool> barrackThreaten;

        private void InitializeDictionary()
        {
            barrackPrior = new();
            barrackThreaten = new();
        }

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

        private void CheckBarrackThreaten()
        {
            barrackThreaten.Clear();

            List<Units> barracks = UnitManager.Instance.units.Where(
                u => u != null && !u.isPlayer && u.unitElement.CheckTraits("Trait_Flag")
            ).ToList();

            // 取所有敌对单位
            List<Units> units = UnitManager.Instance.units.Where(
                u => u != null && u.isPlayer
            ).ToList();

            foreach (var barrack in barracks)
            {
                // 统计切比雪夫距离为 1 的相邻格内的敌对单位数量
                int adjacentEnemyCount = units.Count(
                    e => MapManager.Instance.Distance(barrack.location, e.location, "Chebyshev") == 1
                );

                barrackThreaten[barrack] = adjacentEnemyCount >= 2;
            }
        }
    } // 标记军营优先度
}

