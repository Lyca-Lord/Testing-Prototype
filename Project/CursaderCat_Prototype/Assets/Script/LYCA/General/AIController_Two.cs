using CommandCard;
using Map;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unit;
using UnityEngine;

/// <summary>
/// AIController_Two
/// 基于用户提供的行为树文本实现的电脑玩家回合控制器（独立于现有 AIController）
/// - 订阅 Central.ComputerPlayerReinforceEvent 以累计每种单位的优先权重（unitPrior）
/// - 按 符合项目现有接口 的协程流程驱动出牌（增援 -> 追击/掩护 -> 攻击 -> 撤退）
/// 
/// 注意：实现尽量复用项目已有 API（CardManager / CardEffect / UnitManager / TargetManager / MapManager / Central）
/// </summary>
namespace AI
{
    public class AIController_Two : MonoBehaviour
    {
        public static AIController_Two Instance;

        [Header("Debug显示")]
        public int unitOnStage = 0;

        [Header("配置 — 可在 Inspector 调整")]
        [Tooltip("优先期望友方（电脑）非军营棋子数量（若 levelInfo 不为空则优先使用 levelInfo.armyScale）")]
        public LevelInfo levelInfo;
        public int expectedArmySize = 5;

        [Tooltip("当场上单位小于 lowerBound 时走 强增援 策略；达到 lowerBound 且小于 upperBound 时仅增援一个单位")]
        public int reinforceLowerBound = -1;
        public int reinforceUpperBound = -1;

        [Header("恒定参数")]
        private const float cardPickTime = 0.4f;
        private const float cardEffectWaitTime = 0.45f;
        private const float unitActionWaitTime = 0.5f;
        private const float rebuildWaitTime = 0.75f;
        private const float turnEndWaitTime = 0.5f;

        // unit priority weights：key = UnitInfo.index, value = 累计权重
        private Dictionary<int, float> _unitPrior = new Dictionary<int, float>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;

            if (levelInfo != null)
            {
                expectedArmySize = levelInfo.armyScale;
            }

            if (reinforceLowerBound <= 0) reinforceLowerBound = expectedArmySize;
            if (reinforceUpperBound <= 0) reinforceUpperBound = expectedArmySize + 2;
        }

        private void Start()
        {
            if (Central.Instance != null)
            {
                Central.Instance.TurnBeginEvent.AddListener(OnTurnBegin);
            }
        }

        private void OnDestroy()
        {
            if (Central.Instance != null)
            {
                Central.Instance.TurnBeginEvent.RemoveListener(OnTurnBegin);
            }
        }

        private void OnComputerPlayerReinforce(int unitIndex)
        {
            if (!_unitPrior.ContainsKey(unitIndex)) _unitPrior[unitIndex] = 0f;
            _unitPrior[unitIndex] += 1f; // 每次增援累积权重
        }

        private void OnTurnBegin()
        {
            if (Central.isPlayerTurn) return;
            StartCoroutine(RunBehaviorTree());
        }

        private IEnumerator RunBehaviorTree()
        {
            yield return new WaitForEndOfFrame();

            // 1. 增援行为（费用增加卡相关）
            yield return StartCoroutine(ReinforcePhase());

            // 判断去向：如果在增援环节前场上单位达到了下限，并且手牌数量 >= 出牌数上限 -> 追击阶段
            CardColumn enemyCol = CardManager.Instance.enemyColumn;
            int handCount = enemyCol.handCard.Count;
            int playMax = enemyCol.playMax;

            bool reachedLower = GetEnemyNonBarrackCount() >= reinforceLowerBound;
            if ((reachedLower && handCount >= playMax) || GetEnemyBarrackCount() <= 0) 
            {
                // 2. 追击阶段
                yield return StartCoroutine(PursuitPhase());
                // 3. 攻击阶段
                yield return StartCoroutine(AttackPhase());
            }
            else
            {
                // 4. 掩护阶段（优先选择能使更多单位接敌的攻击方式）
                yield return StartCoroutine(CoverPhase());
            }

            // 5. 撤退阶段
            yield return StartCoroutine(RetreatPhase());

            yield return new WaitForEndOfFrame();
            if (enemyCol.currentCardDeck.Count <= 0 && enemyCol.handCard.Count < enemyCol.playMax)
            {
                Debug.LogWarning("尝试重构");
                enemyCol.ReconstructDeck();
                yield return new WaitForSeconds(rebuildWaitTime);
            }

            // 最后结束回合
            yield return new WaitForSeconds(turnEndWaitTime);
            CardManager.Instance.NextTurn();
        }

        // ----------------------------
        // 1. 增援实现
        // ----------------------------
        private IEnumerator ReinforcePhase()
        {
            if (GetEnemyBarrackCount() <= 0) yield break;

            CardColumn col = CardManager.Instance.enemyColumn;
            // 计算当前非军营单位数量
            int currentCount = GetEnemyNonBarrackCount();

            // 找到手牌中的所有费用增加卡（cardId == 1）
            List<CardController> costCards = col.handCard.Where(c => c.cardInfo.cardId == 1).ToList();

            // 如果小于下限 -> 打出所有费用增加卡并尽可能增援
            if (currentCount < reinforceLowerBound)
            {
                // 一直使用费用卡的版本
                //foreach (var costCard in costCards.ToList())
                //{
                //    if (col.playCount >= col.playMax) break; // 已用完出牌次数
                //    // 打出费用卡
                //    yield return StartCoroutine(PlayCard(costCard));
                //    yield return new WaitForSeconds(0.25f);

                //    // 持续增援直到费用不足
                //    yield return StartCoroutine(ReinforceUntilCostIsInsufficient());

                //    if (GetEnemyNonBarrackCount() >= reinforceUpperBound) yield break;
                //}
                CardController costCard = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == 1);
                if (costCard != null && col.playCount < col.playMax)
                {
                    yield return StartCoroutine(PlayCard(costCard));
                    yield return new WaitForSeconds(cardPickTime);
                }
                yield return StartCoroutine(ReinforceUntilCostIsInsufficient());
                yield return new WaitForSeconds(cardEffectWaitTime);
            }
            else if (currentCount >= reinforceLowerBound && currentCount < reinforceUpperBound)
            {
                // 达到下限但未到上限 -> 只增援一个单位（若有费用卡）
                CardController costCard = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == 1);
                if (costCard != null && col.playCount < col.playMax)
                {
                    yield return StartCoroutine(PlayCard(costCard));
                    yield return new WaitForSeconds(cardPickTime);
                    yield return StartCoroutine(DoOneReinforceByPrior());
                    yield return new WaitForSeconds(cardEffectWaitTime);
                }
            }
            else
            {
                // 到达上限 -> 跳过增援阶段
            }
        }

        /// <summary>
        /// 循环增援，直到费用不足以召唤下一个偏好单位。
        /// </summary>
        private IEnumerator ReinforceUntilCostIsInsufficient()
        {
            while (true)
            {
                // 预判下一个要增援的单位及其花费
                var (chosenCard, _) = ChooseReinforcementUnit();
                if (chosenCard == null)
                {
                    // 没有可增援的单位
                    yield break;
                }

                // 检查当前费用是否足够
                if (CardManager.Instance.GetCost(false) < chosenCard.unitInfo.cost)
                {
                    // 费用不足，停止增援循环
                    yield break;
                }

                // 费用充足，执行一次增援
                yield return StartCoroutine(DoOneReinforceByPrior());
                yield return new WaitForSeconds(unitActionWaitTime);
            }
        }

        /// <summary>
        /// 动态计算权重选择要增援的单位卡。
        /// 遍历场上敌方活着的单位，查表计算同类累积权重。
        /// </summary>
        /// <returns>返回选中的增援卡 (ReinforceCardController) 和其在卡组中的索引 (int)。如果无法选择，则返回 (null, -1)。</returns>
        private (ReinforceCardController card, int index) ChooseReinforcementUnit()
        {
            ReinforceCardColumn reinforceCol = CardManager.Instance.enemyReinforceColumn;
            if (reinforceCol == null || reinforceCol.cardDeck == null || reinforceCol.cardDeck.Count == 0)
                return (null, -1);

            // 获取场上存活的所有非玩家单位
            var enemyUnitsOnField = UnitManager.Instance.units.Where(u => u != null && !u.isPlayer).ToList();

            int chosenIdx = -1;
            float minVal = float.PositiveInfinity;

            for (int i = 0; i < reinforceCol.cardDeck.Count; i++)
            {
                var rc = reinforceCol.cardDeck[i];
                if (rc == null || rc.unitInfo == null) continue;
                
                string candidateName = rc.unitInfo.unitName;
                
                // 默认权重倍率，如果没有在 LevelInfo 中单独配置配重的话视作 1
                float singleWeight = 1f;
                if (TargetManager.Instance != null && TargetManager.Instance.unitPriority.TryGetValue(candidateName, out float w))
                {
                    singleWeight = w;
                }

                // 计算场上叫这个名字的单位带给它的累积权重
                float accumulatedVal = 0f;
                foreach(var unit in enemyUnitsOnField)
                {
                    if (unit.unitName == candidateName)
                    {
                        accumulatedVal += singleWeight;
                    }
                }

                // 添加小额费用偏移以处理当两者权重平局（如都是0只时），更乐于下重型棋或是进行区分
                accumulatedVal += rc.unitInfo.cost * 0.001f;

                if (accumulatedVal < minVal)
                {
                    minVal = accumulatedVal;
                    chosenIdx = i;
                }
            }

            if (chosenIdx == -1) return (null, -1);

            return (reinforceCol.cardDeck[chosenIdx], chosenIdx);
        }


        /// <summary>
        /// 按 unitPrior 权重选择一项增援并完成增援流程（调用 ReinforceCardColumn 中的对应卡）
        /// </summary>
        private IEnumerator DoOneReinforceByPrior()
        {
            var (chosenCard, chosenIdx) = ChooseReinforcementUnit();
            if (chosenCard == null) yield break;

            // 检查费用是否够
            if (CardManager.Instance.GetCost(false) < chosenCard.unitInfo.cost)
                yield break;

            // 打开增援面板并选中该增援卡（PlaySelectedCard 流程会触发 ReinforceEnter）
            CardManager.Instance.OpenReinforceBox();
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(cardPickTime);

            chosenCard.PickThisCard();
            yield return new WaitForSeconds(cardEffectWaitTime);

            CardManager.Instance.PlaySelectedCard();
            yield return new WaitForEndOfFrame();

            // 等待 CardEffect 注册并锁定
            yield return new WaitUntil(() => CardEffect.Instance.isLock);
            yield return new WaitForSeconds(unitActionWaitTime);

            // 选军营并选生成格
            Units barrack = PickReinforceBarrack();
            Debug.LogWarning(barrack);
            if (barrack == null)
            {
                // 若没有军营则结束该增援
                Central.Instance.ActionEndEarly?.Invoke();
                yield break;
            }

            MapCell barrackCell = MapManager.Instance.FindCellByLocation(barrack.location);
            if (barrackCell != null)
                barrackCell.PickThisCell();

            yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
            yield return new WaitForSeconds(unitActionWaitTime);

            MapCell spawn = PickSpawnCellNearBarrack(barrack, chosenCard.unitInfo);
            if (spawn != null)
            {
                spawn.PickThisCell();
                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                yield return new WaitForSeconds(unitActionWaitTime);
            }
            else
            {
                Central.Instance.ActionEndEarly?.Invoke();
            }

            // 等待增援流程结束
            yield return new WaitUntil(() => !CardEffect.Instance.isLock);
        }

        private Units PickReinforceBarrack()
        {
            var barrackPrior = TargetManager.Instance.barrackPrior;
            var barrackThreaten = TargetManager.Instance.barrackThreaten;
            Debug.LogWarning(TargetManager.Instance.barrackPrior.Count);
            Debug.LogWarning(TargetManager.Instance.barrackThreaten.Count);
            if (barrackPrior == null || barrackPrior.Count == 0) return null;
            if (barrackPrior.Count != barrackThreaten.Count) Application.Quit();
 
            bool anyThreatened = barrackThreaten.Any(kv => kv.Value);
            return anyThreatened
                ? barrackPrior.OrderByDescending(kv => kv.Value).First().Key
                : barrackPrior.OrderBy(kv => kv.Value).First().Key;
        } // 优先选择威胁较小的军营；如果有军营被威胁，则优先选择威胁较大的军营

        private MapCell PickSpawnCellNearBarrack(Units barrack, UnitInfo info)
        {
            if (barrack == null || info == null) return null;
            Vector2 barrackLoc = barrack.location;
            List<MapCell> candidates = new List<MapCell>();

            int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 };
            int[] dy = { 1, 0, -1, 0, 1, -1, 1, -1 };

            for (int i = 0; i < 8; i++)
            {
                Vector2 neighbor = barrackLoc + new Vector2(dx[i], dy[i]);
                MapCell cell = MapManager.Instance.FindCellByLocation(neighbor);
                if (cell != null && cell.unit == null && cell.IsWalkable(false))
                    candidates.Add(cell);
            }
            if (candidates.Count == 0) return null;

            Vector2 mapCenter = new Vector2(MapManager.Instance.GetMapHeight() / 2f, MapManager.Instance.GetMapWidth() / 2f);

            if (info.isMeleeInclination)
                return candidates.OrderBy(c => MapManager.Instance.Distance(c.location, mapCenter, "Manhattan")).First();
            else if (info.isRangeInclination)
                return candidates.OrderByDescending(c => MapManager.Instance.Distance(c.location, mapCenter, "Manhattan")).First();
            else
                return candidates[Random.Range(0, candidates.Count)];
        }

        // ----------------------------
        // 2. 追击阶段（移动）
        // ----------------------------
        private IEnumerator PursuitPhase()
        {
            CardColumn col = CardManager.Instance.enemyColumn;

            int remainingPlays = col.playMax - col.playCount;
            if (remainingPlays < 1) yield break; // 必须保证有出牌次数

            // 1. 积极且无条件的第一次移动：只要出牌次数足够（这里无论如何都先尝试移动一次）
            // 注意这里只要手牌里有移动卡才会真移动，所以内部也做了处理
            yield return StartCoroutine(PlayMoveCardTowardTargets());

            // 2. 更新剩余出牌次数
            remainingPlays = col.playMax - col.playCount;

            // 如果此时还剩下至少两次出牌机会（通常这里意味着你还能打一张移动卡和一张攻击卡）
            if (remainingPlays >= 2)
            {
                // 检查是否所有可远程的单位的目标都进入了攻击范围
                bool allRangeIn = AllUnitsTargetsInRange(isMelee: false);
                // 检查是否所有可近战的单位的目标都进入了攻击范围
                bool allMeleeIn = AllUnitsTargetsInRange(isMelee: true);

                // 只要有一种兵种的接敌需求没有被“全部满足”，（即：不是所有的远程都接敌，且不是所有的近战都接敌），说明队伍还没站好位置，继续推进
                if (!allRangeIn && !allMeleeIn)
                {
                    yield return StartCoroutine(PlayMoveCardTowardTargets());
                }
            }
        }

        /// <summary>
        /// 检查某一种类型单位是否“全部”目标都进入了攻击范围。
        /// 若不存在该类型单位，则视同需求已达成，返回 true。
        /// </summary>
        private bool AllUnitsTargetsInRange(bool isMelee)
        {
            string trait = isMelee ? "Trait_CanMelee" : "Trait_CanRanged";

            var relevantUnits = UnitManager.Instance.units
                .Where(u => u != null && !u.isPlayer && !u.isLocked && u.unitElement.CheckTraits(trait))
                .ToList();

            // 如果场上压根没有这个兵种，那它们“并没有没接敌的情况”，直接返回 True
            if (relevantUnits.Count == 0) return true;

            foreach (var attacker in relevantUnits)
            {
                Units target = attacker.actionTarget;
                
                // 如果有人没目标，代表它也算不在范围
                if (target == null) return false;

                bool inRange = false;
                if (isMelee)
                {
                    inRange = MapManager.Instance.Distance(attacker.location, target.location, "Chebyshev") <= 1;
                }
                else
                {
                    inRange = MapManager.Instance.Distance(attacker.location, target.location, "Manhattan") <= (int)attacker.unitElement.rangedRadius;
                }

                // 只要有一个人不达标，就不满足"全员达标"的条件
                if (!inRange)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator PlayMoveCardTowardTargets()
        {
            CardColumn col = CardManager.Instance.enemyColumn;
            CardController moveCard = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == 0);
            if (moveCard == null || col.playCount >= col.playMax) yield break;

            // 使用现有 Card 流程
            yield return StartCoroutine(PlayCard(moveCard));
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(cardEffectWaitTime);

            yield return new WaitUntil(() => CardEffect.Instance.isLock);
            yield return new WaitForSeconds(0.2f);

            List<Units> aiUnits = UnitManager.Instance.units
                .Where(u => u != null && !u.isPlayer && !u.isLocked && !u.unitElement.CheckTraits("Trait_Flag") && u.unitElement.currentSpeed > 0)
                .ToList();

            foreach (var unit in aiUnits)
            {
                // 使用 targetUnit 而不是 actionTarget
                Units target = unit.actionTarget;
                if (target == null) continue;

                Vector2 moveTarget = GetMoveTargetTowardAttackRange(unit, target);
                if (moveTarget == unit.location) continue;

                MapCell unitCell = MapManager.Instance.FindCellByLocation(unit.location);
                MapCell targetCell = MapManager.Instance.FindCellByLocation(moveTarget);
                if (unitCell == null || targetCell == null) continue;

                unitCell.PickThisCell();
                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                yield return new WaitForSeconds(unitActionWaitTime);

                targetCell.PickThisCell();
                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                yield return new WaitForSeconds(unitActionWaitTime);
            }

            Central.Instance.ActionEndEarly?.Invoke();
            yield return new WaitForSeconds(unitActionWaitTime);
            yield return new WaitUntil(() => !CardEffect.Instance.isLock);
        }

        // ----------------------------
        // 3. 攻击阶段（含战术调整）
        // ----------------------------
        private IEnumerator AttackPhase()
        {
            CardColumn col = CardManager.Instance.enemyColumn;

            // 先执行战术调整：对于所有未接敌且有战术调整次数的单位，执行战术调整使其靠近目标（一次性打开 Tactic 模式并依次选择单位）
            yield return StartCoroutine(PerformMassTacticTowardsTargets());

            // 选择近战或远程中，能成功接敌数量最多的一种并执行（需要对应攻击卡）
            int meleePossible = CountAttackPairsInRange(true);
            int rangedPossible = CountAttackPairsInRange(false);

            if (meleePossible == 0 && rangedPossible == 0)
            {
                // 没有可攻击目标或无卡则跳过攻击阶段
                yield break;
            }

            if (meleePossible >= rangedPossible)
            {
                if (col.handCard.Any(c => c.cardInfo.cardId == 2))
                    yield return StartCoroutine(PlayAttackCardOfType(2, true));
                if (col.handCard.Any(c => c.cardInfo.cardId == 3))
                    yield return StartCoroutine(PlayAttackCardOfType(3, false)); 
                // 去掉了else使得可能混用，看看是否更好
            }
            else
            {
                if (col.handCard.Any(c => c.cardInfo.cardId == 3))
                    yield return StartCoroutine(PlayAttackCardOfType(3, false));
                if (col.handCard.Any(c => c.cardInfo.cardId == 2))
                    yield return StartCoroutine(PlayAttackCardOfType(2, true));
            }
        }

        private IEnumerator PerformMassTacticTowardsTargets()
        {
            // 找到所有可战术调整且当前未接敌的单位
            List<Units> aiUnits = UnitManager.Instance.units
                .Where(u => u != null && !u.isPlayer && !u.isLocked && !u.unitElement.CheckTraits("Trait_Flag") && u.unitElement.currentTacticSpeed > 0)
                .ToList();

            aiUnits = aiUnits.Where(u =>
            {
                Units t = u.actionTarget;
                if (t == null) return false;
                
                // 若近战且切比雪夫 <=1 则视为已接敌；若远程且曼哈顿 <= range 则已接敌
                bool meleeOk = u.unitElement.CheckTraits("Trait_CanMelee") && MapManager.Instance.Distance(u.location, t.location, "Chebyshev") <= 1;
                bool rangedOk = u.unitElement.CheckTraits("Trait_CanRanged") && MapManager.Instance.Distance(u.location, t.location, "Manhattan") <= (int)u.unitElement.rangedRadius;
                return !(meleeOk || rangedOk);
            }).ToList();

            if (aiUnits.Count == 0) yield break;

            // 进入 Tactic 模式（一次打开，依次选择）
            CardManager.Instance.IntoTactic();
            yield return new WaitForEndOfFrame();
            yield return new WaitUntil(() => CardEffect.Instance.isLock);
            yield return new WaitForSeconds(0.2f);

            foreach (var unit in aiUnits)
            {
                Units target = unit.actionTarget;
                if (target == null) continue;
                
                MapCell unitCell = MapManager.Instance.FindCellByLocation(unit.location);
                Vector2 step = GetOneStepToward(unit.location, target.location);
                MapCell stepCell = MapManager.Instance.FindCellByLocation(step);

                if (stepCell != null && stepCell.unit == null && stepCell.IsWalkable(false))
                {
                    if (unitCell != null)
                        unitCell.PickThisCell();

                    yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                    yield return new WaitForSeconds(unitActionWaitTime);

                    stepCell.PickThisCell();
                    yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                    yield return new WaitForSeconds(unitActionWaitTime);
                }
            }

            // 结束 Tactic 模式
            Central.Instance.ActionEndEarly?.Invoke();
            yield return new WaitForSeconds(0.2f);
            yield return new WaitUntil(() => !CardEffect.Instance.isLock);
        }

        // 将原先的 GetAttackPairsInRange 改为动态获取符合条件的攻击者列表
        private List<Units> GetAttackersInRange(bool isMelee)
        {
            string trait = isMelee ? "Trait_CanMelee" : "Trait_CanRanged";

            return UnitManager.Instance.units
                .Where(u => u != null && !u.isPlayer && !u.isLocked && u.unitElement.CheckTraits(trait))
                .Where(attacker =>
                {
                    Units target = attacker.actionTarget; // 使用新逻辑：读取 targetUnit
                    if (target == null) return false;

                    // 检查距离
                    if (isMelee)
                    {
                        return MapManager.Instance.Distance(attacker.location, target.location, "Chebyshev") <= 1;
                    }
                    else
                    {
                        return MapManager.Instance.Distance(attacker.location, target.location, "Manhattan") <= (int)attacker.unitElement.rangedRadius;
                    }
                })
                .OrderByDescending(attacker =>
                {
                    // 排序时确保目标有效，根据破防收益预估优先级
                    Units target = attacker.actionTarget ;
                    if (target == null) return -999;
                    int atk = attacker.unitElement.attack;
                    int def = target.unitElement.defend;
                    return Mathf.Max(0, atk - def);
                })
                .ToList();
        }

        // 修改原先的统计方法以适应新的接口
        private int CountAttackPairsInRange(bool isMelee)
        {
            return GetAttackersInRange(isMelee).Count;
        }

        // 修改打出攻击卡流程
        private IEnumerator PlayAttackCardOfType(int cardId, bool isMelee) // 打出攻击卡
        {
            CardColumn col = CardManager.Instance.enemyColumn;

            while (col.playCount < col.playMax)
            {
                CardController card = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == cardId);
                if (card == null) yield break;

                // 依据动态目标获取当前的有效攻击者
                List<Units> attackers = GetAttackersInRange(isMelee);
                if (attackers.Count == 0) yield break;

                // 1. 打出攻击卡
                yield return StartCoroutine(PlayCard(card));
                yield return new WaitForSeconds(cardEffectWaitTime);

                // 等待 CardEffect 注册
                yield return new WaitUntil(() => CardEffect.Instance.isLock);
                yield return new WaitForSeconds(0.2f);

                // 2. 依次取出攻击单位并实施攻击
                foreach (var attacker in attackers)
                {
                    // 再次实时检查：可能因为上一名攻击者的行动导致自身状态被改变
                    if (attacker == null || attacker.isLocked || attacker.unitElement.currentAttackTime <= 0) continue;

                    // 从单位实体上取出属于它的实时目标
                    Units target = attacker.actionTarget;
                    if (target == null) continue; 

                    // 实时检查距离：在本次循环中的其他单位行动，可能引起目标位移情况（如击退掩护等）
                    bool stillInRange;
                    if (isMelee)
                    {
                        int dist = MapManager.Instance.Distance(attacker.location, target.location, "Chebyshev");
                        stillInRange = dist <= 1;
                    }
                    else
                    {
                        int dist = MapManager.Instance.Distance(attacker.location, target.location, "Manhattan");
                        stillInRange = dist <= (int)attacker.unitElement.rangedRadius;
                    }
                    if (!stillInRange) continue;

                    MapCell attackerCell = MapManager.Instance.FindCellByLocation(attacker.location);
                    if (attackerCell == null) continue;
                    attackerCell.PickThisCell();

                    yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                    yield return new WaitForSeconds(unitActionWaitTime);

                    // 行动前再看一眼目标是否已因连发等被提前干掉
                    if (target == null || !UnitManager.Instance.units.Contains(target)) continue;

                    MapCell targetCell = MapManager.Instance.FindCellByLocation(target.location);
                    if (targetCell == null) continue;
                    targetCell.PickThisCell();

                    yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                    yield return new WaitForSeconds(unitActionWaitTime);
                }

                // 3. 结束本次攻击卡效果
                yield return new WaitForSeconds(0.2f);
                Central.Instance.ActionEndEarly?.Invoke();
                yield return new WaitForSeconds(0.2f);
                yield return new WaitUntil(() => !CardEffect.Instance.isLock);
            }
        }

        // ----------------------------
        // 4. 掩护阶段（保守攻击）
        // ----------------------------
        private IEnumerator CoverPhase()
        {
            CardColumn col = CardManager.Instance.enemyColumn;

            int meleePossible = CountAttackPairsInRange(true);
            int rangedPossible = CountAttackPairsInRange(false);

            if (meleePossible == 0 && rangedPossible == 0) yield break;

            if (meleePossible >= rangedPossible)
            {
                if (col.handCard.Any(c => c.cardInfo.cardId == 2))
                    yield return StartCoroutine(PlayAttackCardOfType(2, true));
            }
            else
            {
                if (col.handCard.Any(c => c.cardInfo.cardId == 3))
                    yield return StartCoroutine(PlayAttackCardOfType(3, false));
            }
        }

        // ----------------------------
        // 5. 撤退阶段（后撤并结束回合）
        // ----------------------------
        private IEnumerator RetreatPhase()
        {
            Units backline = GetBacklineBarrack();
            if (backline == null)
            {
                // 如果没有军营，直接结束
                yield break;
            }

            CardColumn col = CardManager.Instance.enemyColumn;
            if (col.playMax <= col.playCount) yield break;

            CardController moveCard = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == 0);
            if (moveCard == null) yield break;

            // 打出移动卡并让单位向后线军营靠近
            yield return StartCoroutine(PlayCard(moveCard));
            yield return new WaitForSeconds(cardEffectWaitTime);

            yield return new WaitUntil(() => CardEffect.Instance.isLock);
            yield return new WaitForSeconds(0.2f);

            List<Units> aiUnits = UnitManager.Instance.units
                .Where(u => u != null && !u.isPlayer && !u.isLocked && !u.unitElement.CheckTraits("Trait_Flag") && u.unitElement.currentSpeed > 0)
                .ToList();

            foreach (var unit in aiUnits)
            {
                Vector2 retreatTarget = GetRetreatTarget(unit, backline);
                if (retreatTarget == unit.location) continue;

                MapCell unitCell = MapManager.Instance.FindCellByLocation(unit.location);
                MapCell targetCell = MapManager.Instance.FindCellByLocation(retreatTarget);
                if (unitCell == null || targetCell == null) continue;

                unitCell.PickThisCell();
                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                yield return new WaitForSeconds(unitActionWaitTime);

                targetCell.PickThisCell();
                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
                yield return new WaitForSeconds(unitActionWaitTime);
            }

            Central.Instance.ActionEndEarly?.Invoke();
            yield return new WaitForSeconds(0.2f);
            yield return new WaitUntil(() => !CardEffect.Instance.isLock);

            // 如果进入撤退阶段时因手牌数量小于出牌数上限（例如牌库打空），重组牌库（ReconstructDeck）
            //if (col.currentCardDeck.Count <= 0 && col.handCard.Count < col.playMax)
            //{
            //    Debug.Log("尝试重构");
            //    isConstruct = true;
            //    col.ReconstructDeck();
            //    yield return new WaitForSeconds(0.8f);
            //}
        }

        // ----------------------------
        // 辅助工具方法（复用/实现必要逻辑）
        // ----------------------------
        private IEnumerator PlayCard(CardController card)
        {
            card.PickCard();
            yield return new WaitForSeconds(cardPickTime);
            CardManager.Instance.PlaySelectedCard();
            yield return new WaitForEndOfFrame();
        }



        private int GetEnemyNonBarrackCount()
            => UnitManager.Instance.units.Count(u => u != null && !u.isPlayer && !u.unitElement.CheckTraits("Trait_Flag"));

        private int GetEnemyBarrackCount()
            => UnitManager.Instance.units.Count(u => u != null && !u.isPlayer && u.unitElement.CheckTraits("Trait_Flag"));

        private Units GetBacklineBarrack()
        {
            var barrackPrior = TargetManager.Instance.barrackPrior;
            if (barrackPrior == null || barrackPrior.Count == 0) return null;
            return barrackPrior.OrderBy(kv => kv.Value).First().Key;
        }

        private Vector2 GetMoveTargetTowardAttackRange(Units unit, Units target)
        {
            int speed = unit.unitElement.currentSpeed;
            float attackRange = unit.unitElement.rangedRadius;
            bool isMelee = unit.unitElement.CheckTraits("Trait_CanMelee");
            if (speed <= 0) return unit.location;

            // 初始化最佳位置为当前位置
            Vector2 bestLocation = unit.location;
            
            // 封装一个计算分数的局部方法，方便给初始位置和各种临近点打分
            int CalculateScore(Vector2 loc)
            {
                int distToTargetManhattan = MapManager.Instance.Distance(loc, target.location, "Manhattan");
                int distToTargetChebyshev = MapManager.Instance.Distance(loc, target.location, "Chebyshev");

                bool inRange = isMelee
                    ? distToTargetChebyshev <= 1
                    : distToTargetManhattan <= (int)attackRange;

                // 基础分：如果能打到，先加个大分
                int score = inRange ? 1000 : 0;
                
                if (isMelee)
                {
                    // 近战：距离越小越好
                    score -= distToTargetManhattan;
                }
                else
                {
                    // 远程：要求不在同一格（避免贴脸，切比雪夫距离为0意味着完全重合，但在基于格子的游戏里，距离为1可能就被视为“贴脸被拦截”了）
                    // 假设“贴脸”指的是在它的身旁 1 格内（切比雪夫 <= 1），我们对这种距离给予惩罚
                    if (distToTargetChebyshev <= 1)
                    {
                        score -= 500; // 给一个大惩罚，让它不爱去这里
                    }
                    else
                    {
                        // 只要没贴脸，还是尽量靠近目标更好，但不能扣太多以免不如没进入射程的点
                        score -= distToTargetManhattan;
                    }
                }

                // 如果目标格子是水格（type == 2），略微降低其评分，稍微倾向于在平地停下
                MapCell locCell = MapManager.Instance.FindCellByLocation(loc);
                if (locCell != null && locCell.type == 2)
                {
                    score -= 5;
                }

                return score;
            }

            int bestScore = CalculateScore(unit.location);

            var openSet = new List<Vector2> { unit.location };
            var costSoFar = new Dictionary<Vector2, int> { { unit.location, 0 } };

            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };

            while (openSet.Count > 0)
            {
                Vector2 current = openSet[0];
                openSet.RemoveAt(0);

                int currentCost = costSoFar[current];
                if (currentCost >= speed) continue;

                for (int i = 0; i < 4; i++)
                {
                    Vector2 next = current + new Vector2(dx[i], dy[i]);
                    MapCell nextCell = MapManager.Instance.FindCellByLocation(next);

                    if (nextCell == null || !nextCell.IsWalkable(false) || (nextCell.unit != null && nextCell.unit != unit))
                        continue;

                    // 判断格型计算不同行动消耗
                    int moveCost = nextCell.type == 2 ? 2 : 1;
                    int newCost = currentCost + moveCost;
                    if (newCost > speed) continue;

                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        openSet.Add(next);

                        int score = CalculateScore(next);

                        // 更新最佳位置（如果分数更高）
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestLocation = next;
                        }
                    }
                }
            }
            return bestLocation;
        }

        private Vector2 GetRetreatTarget(Units unit, Units backlineBarrack)
        {
            int speed = unit.unitElement.currentSpeed;
            if (speed <= 0) return unit.location;

            List<Units> playerUnits = UnitManager.Instance.units.Where(u => u != null && u.isPlayer).ToList();

            // 计算当前位置的分数
            float currentScore = 0;
            if (playerUnits.Count > 0)
            {
                currentScore += playerUnits.Sum(p => MapManager.Instance.Distance(unit.location, p.location, "Manhattan"));
            }
            currentScore -= MapManager.Instance.Distance(unit.location, backlineBarrack.location, "Manhattan") * 2f;

            Vector2 bestLocation = unit.location;
            float bestScore = currentScore;

            var openSet = new List<Vector2> { unit.location };
            var costSoFar = new Dictionary<Vector2, int> { { unit.location, 0 } };

            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };

            while (openSet.Count > 0)
            {
                Vector2 current = openSet[0];
                openSet.RemoveAt(0);

                int currentCost = costSoFar[current];
                if (currentCost >= speed) continue;

                for (int i = 0; i < 4; i++)
                {
                    Vector2 next = current + new Vector2(dx[i], dy[i]);
                    MapCell nextCell = MapManager.Instance.FindCellByLocation(next);

                    if (nextCell == null || !nextCell.IsWalkable(false) || (nextCell.unit != null && nextCell.unit != unit))
                        continue;

                    // 判断格型计算不同行动消耗
                    int moveCost = nextCell.type == 2 ? 2 : 1;
                    int newCost = currentCost + moveCost;
                    if (newCost > speed) continue;

                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        openSet.Add(next);

                        float awayFromPlayer = playerUnits.Count > 0
                            ? playerUnits.Sum(p => MapManager.Instance.Distance(next, p.location, "Manhattan"))
                            : 0f;
                        float toBarrack = -MapManager.Instance.Distance(next, backlineBarrack.location, "Manhattan");

                        float score = awayFromPlayer + toBarrack * 2f;
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestLocation = next;
                        }
                    }
                }
            }
            return bestLocation;
        }

        private Vector2 GetOneStepToward(Vector2 from, Vector2 to)
        {
            float dx = to.x - from.x;
            float dy = to.y - from.y;

            // 优先混合横向与纵向移动：如果需要横纵均大于 0 且步数不足，尽量分配（简化实现：当两边均需要移动时，按绝对大小决定先横或先纵）
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                return from + new Vector2(Mathf.Sign(dx), 0);
            else
                return from + new Vector2(0, Mathf.Sign(dy));
        }

        private void Update()
        {
            unitOnStage = GetEnemyNonBarrackCount();
        }
    }
}