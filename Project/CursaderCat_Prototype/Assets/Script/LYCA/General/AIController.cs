//using CommandCard;
//using Map;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using Unit;
//using UnityEngine;

//namespace AI
//{
//    /// <summary>
//    /// AI 回合控制器，根据行为树逻辑驱动敌方自动出牌与行动。
//    /// 挂载在与 CardManager、TargetManager 同级的 GameObject 上即可。
//    /// </summary>
//    public class AIController : MonoBehaviour
//    {
//        public static AIController Instance;

//        // ─────────────────────────────────────────────
//        // 配置参数
//        // ─────────────────────────────────────────────

//        [Header("AI Config")]
//        [Tooltip("当前关卡信息，权重表与单位类型均从此读取")]
//        public LevelInfo levelInfo;

//        [Tooltip("AI 期望维持的友方非军营棋子数量（若 levelInfo 不为 null 则优先使用 levelInfo.armyScale）")]
//        public int expectedArmySize = 5;

//        // ─────────────────────────────────────────────
//        // 内部状态
//        // ─────────────────────────────────────────────

//        /// <summary>权重增援法中每种单位累积的 value 值，索引与 levelInfo.enemyBox.unitInfos 一一对应</summary>
//        private List<float> _reinforceValues = new List<float>();

//        /// <summary>本回合是否已执行过战术调整（免费一格移动）</summary>
//        private bool _tacticUsed = false;

//        private void Awake()
//        {
//            if (Instance != null && Instance != this) { Destroy(this); return; }
//            Instance = this;

//            if (levelInfo != null)
//            {
//                expectedArmySize = levelInfo.armyScale;
//            }
//        }

//        private void Start()
//        {
//            Central.Instance.TurnBeginEvent.AddListener(OnTurnBegin);
//        }

//        private void OnDestroy()
//        {
//            if (Central.Instance != null)
//                Central.Instance.TurnBeginEvent.RemoveListener(OnTurnBegin);
//        }

//        // ─────────────────────────────────────────────
//        // 入口
//        // ─────────────────────────────────────────────

//        private void OnTurnBegin()
//        {
//            if (Central.isPlayerTurn) return;
//            _tacticUsed = false;
//            StartCoroutine(RunBehaviorTree());
//        }

//        // ─────────────────────────────────────────────
//        // 行为树主协程
//        // ─────────────────────────────────────────────

//        private IEnumerator RunBehaviorTree()
//        {
//            yield return new WaitForEndOfFrame();

//            // ── 节点：是否有费用增援 ──────────────────
//            if (HasCostForReinforce())
//            {
//                yield return StartCoroutine(DoReinforce());
//            }

//            // ── 节点：出牌决策 ───────────────────────
//            yield return StartCoroutine(CardDecision());

//            // ── 免费战术调整（若还未使用）──────────────
//            if (!_tacticUsed)
//            {
//                yield return StartCoroutine(DoTactic());
//            }

//            // ── 结束回合 ─────────────────────────────
//            yield return new WaitForSeconds(0.5f);
//            CardManager.Instance.NextTurn();
//        }

//        // ─────────────────────────────────────────────
//        // 辅助：选中卡牌并打出
//        // ─────────────────────────────────────────────

//        /// <summary>
//        /// 标准出牌流程：先 PickCard 选中，再 PlaySelectedCard 打出并触发效果。
//        /// </summary>
//        private IEnumerator PlayCard(CardController card)
//        {
//            card.PickCard();                              // 1. 选中（写入 selectedCard）
//            yield return new WaitForSeconds(0.1f);
//            //card.column.PlaySelectedCard();               // 2. 执行效果 + 弃牌
//            CardManager.Instance.PlaySelectedCard();       // 2. 执行效果 + 弃牌
//            yield return new WaitForEndOfFrame();
//        }

//        // ─────────────────────────────────────────────
//        // 增援逻辑
//        // ─────────────────────────────────────────────

//        private bool HasCostForReinforce()
//        {
//            if (CardManager.Instance.GetCost(false) <= 0) return false;

//            var (chosenUnit, _) = ChooseReinforcementUnit();
//            if (chosenUnit == null) return false;

//            return CardManager.Instance.GetCost(false) >= chosenUnit.cost;
//        }

//        /// <summary>
//        /// 根据权重选择要增援的单位。
//        /// </summary>
//        /// <returns>返回选中的单位信息 (UnitInfo) 和其在增援卡组中的索引 (int)。如果无法选择，则返回 (null, -1)。</returns>
//        private (UnitInfo unitInfo, int cardIndex) ChooseReinforcementUnit()
//        {
//            if (levelInfo == null)
//            {
//                Debug.LogWarning("[AIController] levelInfo 未赋值，无法选择增援单位。");
//                return (null, -1);
//            }

//            UnitBox box = levelInfo.enemyBox;
//            if (box == null || box.unitInfos.Count == 0) return (null, -1);

//            List<float> weights = new();
//            int unitCount = box.unitInfos.Count;

//            while (weights.Count < unitCount)
//                weights.Add(1f);

//            if (_reinforceValues.Count != unitCount)
//                _reinforceValues = new List<float>(new float[unitCount]);

//            for (int i = 0; i < unitCount; i++)
//                _reinforceValues[i] += weights[i];

//            int chosenIndex = -1;
//            float minVal = float.PositiveInfinity;
//            for (int i = 0; i < unitCount; i++)
//            {
//                if (_reinforceValues[i] < minVal)
//                {
//                    minVal = _reinforceValues[i];
//                    chosenIndex = i;
//                }
//            }

//            if (chosenIndex == -1) return (null, -1);

//            UnitInfo chosenInfo = box.unitInfos[chosenIndex];
//            return (chosenInfo, chosenIndex);
//        }

//        /// <summary>
//        /// 执行增援流程：选择单位、地点并打出增援卡。
//        /// </summary>
//        private IEnumerator DoReinforce()
//        {
//            var (chosenUnit, cardIndex) = ChooseReinforcementUnit();
//            if (chosenUnit == null) yield break;

//            // 检查费用是否足够
//            if (CardManager.Instance.GetCost(false) < chosenUnit.cost) yield break;

//            // 更新权重值
//            _reinforceValues[cardIndex] = 0f;

//            Units targetBarrack = PickReinforceBarrack();
//            if (targetBarrack == null) yield break;

//            MapCell spawnCell = PickSpawnCellNearBarrack(targetBarrack, chosenUnit);
//            if (spawnCell == null) yield break;

//            yield return StartCoroutine(PlayReinforceCard(targetBarrack, spawnCell, cardIndex));
//        }


//        /// <summary>
//        /// 正确增援流程：
//        /// 1. OpenReinforceBox 打开增援面板
//        /// 2. 从 enemyReinforceColumn.cardDeck 选中对应的 ReinforceCardController
//        /// 3. PlaySelectedCard 打出（触发 ReinforceCardController.CardEffect → ReinforceEnter，自动关闭 box）
//        /// 4. PickThisCell 选军营 → PickThisCell 选生成格
//        /// </summary>
//        private IEnumerator PlayReinforceCard(Units barrack, MapCell spawnCell, int cardIndex)
//        {
//            // 1. 打开增援面板
//            CardManager.Instance.OpenReinforceBox();
//            yield return new WaitForEndOfFrame();
//            yield return new WaitForSeconds(0.2f);

//            // 2. 从 enemyReinforceColumn 中找到对应索引的增援卡并选中
//            ReinforceCardColumn reinforceCol = CardManager.Instance.enemyReinforceColumn;
//            if (reinforceCol == null || cardIndex >= reinforceCol.cardDeck.Count) yield break;

//            ReinforceCardController reinforceCard = reinforceCol.cardDeck[cardIndex];
//            if (reinforceCard == null) yield break;

//            // 模拟点击选中（触发 SelectEvent → column.SelectCard）
//            reinforceCard.PickThisCard();
//            //reinforceCol.SelectCard(reinforceCard, true);
//            //reinforceCard.isSelected = true;
//            yield return new WaitForSeconds(0.2f);

//            // 3. 打出选中的增援卡（因为 isOpenBox == true，调用 enemyReinforceColumn.PlaySelectedCard）
//            //    CardEffect 内部会调用 ReinforceEnter 并触发 UseCardEvent（自动关闭 box）
//            CardManager.Instance.PlaySelectedCard();
//            yield return new WaitForEndOfFrame();
//            yield return new WaitForSeconds(0.3f);

//            // 4. 等待 ReinforceEnter 协程完成注册监听
//            yield return new WaitUntil(() => CardEffect.Instance.isLock);
//            yield return new WaitForSeconds(0.2f);

//            // 5. 选中军营（触发 UnitSelectEvent → ReinforceSelectUnit）
//            MapCell barrackCell = MapManager.Instance.FindCellByLocation(barrack.location);
//            if (barrackCell != null)
//                barrackCell.PickThisCell();

//            yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//            yield return new WaitForSeconds(0.2f);

//            // 6. 选中生成格（触发 ClickEvent）
//            spawnCell.PickThisCell();

//            yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//            yield return new WaitForSeconds(0.3f);
//            yield return new WaitUntil(() => !CardEffect.Instance.isLock);
//        }

//        private Units PickReinforceBarrack()
//        {
//            var barrackPrior = TargetManager.Instance.barrackPrior;
//            var barrackThreaten = TargetManager.Instance.barrackThreaten;

//            if (barrackPrior.Count == 0) return null;

//            // 边界情况：如果只有一个军营，直接返回它
//            if (barrackPrior.Count == 1)
//            {
//                return barrackPrior.First().Key;
//            }

//            bool anyThreatened = barrackThreaten.Any(kv => kv.Value);

//            // 受威胁时优先向前线军营（权重最大）增援以保护；
//            // 安全时向后线军营（权重最小）增援积蓄力量。
//            return anyThreatened
//                ? barrackPrior.OrderByDescending(kv => kv.Value).First().Key
//                : barrackPrior.OrderBy(kv => kv.Value).First().Key;
//        }

//        private MapCell PickSpawnCellNearBarrack(Units barrack, UnitInfo info)
//        {
//            Vector2 barrackLoc = barrack.location;
//            List<MapCell> candidates = new List<MapCell>();

//            int[] dx = { 0, 1, 0, -1, 1, 1, -1, -1 };
//            int[] dy = { 1, 0, -1, 0, 1, -1, 1, -1 };

//            for (int i = 0; i < 8; i++)
//            {
//                Vector2 neighbor = barrackLoc + new Vector2(dx[i], dy[i]);
//                MapCell cell = MapManager.Instance.FindCellByLocation(neighbor);
//                if (cell != null && cell.unit == null && cell.IsWalkable(false))
//                    candidates.Add(cell);
//            }

//            if (candidates.Count == 0) return null;

//            Vector2 mapCenter = new Vector2(
//                MapManager.Instance.GetMapHeight() / 2f,
//                MapManager.Instance.GetMapWidth() / 2f
//            );

//            if (info.isMeleeInclination)
//                return candidates.OrderBy(c => MapManager.Instance.Distance(c.location, mapCenter, "Manhattan")).First();
//            else if (info.isRangeInclination)
//                return candidates.OrderByDescending(c => MapManager.Instance.Distance(c.location, mapCenter, "Manhattan")).First();
//            else
//                return candidates[Random.Range(0, candidates.Count)];
//        }

//        /// <summary>
//        /// 正确出牌流程：
//        /// 1. PickCard → PlaySelectedCard 打出增援卡并触发 ReinforceEnter
//        /// 2. PickThisCell 选军营（触发 UnitSelectEvent）
//        /// 3. PickThisCell 选生成格（触发 ClickEvent）
//        /// </summary>
//        private IEnumerator PlayReinforceCard(Units barrack, MapCell spawnCell)
//        {
//            CardColumn col = CardManager.Instance.enemyColumn;
//            CardController costCard = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == 1);
//            if (costCard == null) yield break;

//            // 1. 选中并打出增援费用卡（触发 ReinforceEnter）
//            yield return StartCoroutine(PlayCard(costCard));
//            yield return new WaitForEndOfFrame();
//            yield return new WaitForSeconds(0.3f);

//            // 2. 等待 ReinforceEnter 协程完成注册监听
//            yield return new WaitUntil(() => CardEffect.Instance.isLock);
//            yield return new WaitForSeconds(0.1f);

//            // 3. 选中军营（触发 UnitSelectEvent → ReinforceSelectUnit）
//            MapCell barrackCell = MapManager.Instance.FindCellByLocation(barrack.location);
//            if (barrackCell != null)
//                barrackCell.PickThisCell();

//            yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//            yield return new WaitForSeconds(0.2f);

//            // 4. 选中生成格（触发 ClickEvent）
//            spawnCell.PickThisCell();

//            yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//            yield return new WaitForSeconds(0.3f);
//            yield return new WaitUntil(() => !CardEffect.Instance.isLock);
//        }

//        // ─────────────────────────────────────────────
//        // 出牌决策
//        // ─────────────────────────────────────────────

//        private IEnumerator CardDecision()
//        {
//            CardColumn col = CardManager.Instance.enemyColumn;

//            bool deckEmpty = col.currentCardDeck.Count == 0;
//            if (deckEmpty)
//            {
//                if (col.playCount < col.playMax)
//                {
//                    col.ReconstructDeck();
//                    yield return new WaitForSeconds(1.0f);
//                }
//                yield break;
//            }

//            int targetArmySize = (levelInfo != null) ? levelInfo.armyScale : expectedArmySize;
//            bool reachedExpectedSize = GetEnemyNonBarrackCount() >= targetArmySize;
//            bool noBarracks = GetEnemyBarrackCount() == 0;

//            if (reachedExpectedSize || noBarracks)
//            {
//                Debug.LogWarning("尝试进攻");
//                yield return StartCoroutine(TryAttackWithOptionalMove());
//            }
//            else
//            {
//                CardController reinforceCard = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == 1);
//                if (reinforceCard != null)
//                {
//                    // 打出增援费用卡并执行增援
//                    yield return StartCoroutine(PlayCard(reinforceCard));
//                    yield return new WaitForSeconds(0.3f);
//                    yield return StartCoroutine(DoReinforce());
//                    //yield return StartCoroutine(CardDecision());
//                    //yield break;
//                }
//                else
//                {
//                    Debug.LogWarning("尝试撤退");
//                    yield return StartCoroutine(PlayMoveCardAndRetreat());
//                }
//            }
//        }

//        // ─────────────────────────────────────────────
//        // 进攻逻辑：先评估直接攻击，若无单位在射程内则先移动
//        // ─────────────────────────────────────────────

//        private IEnumerator TryAttackWithOptionalMove()
//        {
//            CardColumn col = CardManager.Instance.enemyColumn;

//            bool canAttackNow = CanAnyUnitAttackInRange(true) || CanAnyUnitAttackInRange(false);

//            if (canAttackNow)
//            {
//                Debug.LogWarning("可以直接攻击");
//                yield return StartCoroutine(PlayAttackCards());
//            }
//            else
//            {
//                bool hasMoveCard = col.handCard.Any(c => c.cardInfo.cardId == 0);
//                if (hasMoveCard && col.playCount < col.playMax)
//                {
//                    Debug.LogWarning("尝试前压");
//                    yield return StartCoroutine(PlayMoveCardTowardTargets());
//                    if (CanAnyUnitAttackInRange(true) || CanAnyUnitAttackInRange(false))
//                        yield return StartCoroutine(PlayAttackCards());
//                }
//                else
//                {
//                    yield return StartCoroutine(PlayAttackCards());
//                }
//            }

//            //yield return StartCoroutine(RetreatAll());
//        }

//        private bool CanAnyUnitAttackInRange(bool isMelee)
//        {
//            string trait = isMelee ? "Trait_CanMelee" : "Trait_CanRanged";

//            foreach (var attacker in UnitManager.Instance.units)
//            {
//                if (attacker == null || attacker.isPlayer) continue;
//                if (attacker.isLocked) continue;
//                if (!attacker.unitElement.CheckTraits(trait)) continue;

//                if (!TargetManager.Instance.preferredTarget.TryGetValue(attacker, out Units target)) continue;
//                if (target == null) continue;

//                if (isMelee)
//                {
//                    // 近战：切比雪夫距离 ≤ 1（上下左右斜均算相邻）
//                    int dist = MapManager.Instance.Distance(attacker.location, target.location, "Chebyshev");
//                    if (dist <= 1)
//                        return true;
//                }
//                else
//                {
//                    // 远程：曼哈顿距离 ≤ 攻击范围
//                    int dist = MapManager.Instance.Distance(attacker.location, target.location, "Manhattan");
//                    if (dist <= (int)attacker.unitElement.rangedRadius)
//                        return true;
//                }
//            }
//            return false;
//        }

//        private IEnumerator PlayAttackCards()
//        {
//            Debug.LogWarning("进入攻击卡流程");
//            yield return StartCoroutine(PlayAttackCardOfType(2, true));  // 近战
//            yield return StartCoroutine(PlayAttackCardOfType(3, false)); // 远程
//        }

//        /// <summary>
//        /// 打出指定类型攻击卡的完整流程：
//        /// 1. PickCard → PlaySelectedCard 打出（触发 MeleeActionEnter / RangedActionEnter）
//        /// 2. 逐对 PickThisCell 选攻击单位 → PickThisCell 选目标格
//        /// 3. ActionEndEarly 结束本次攻击卡效果
//        /// </summary>
//        private IEnumerator PlayAttackCardOfType(int cardId, bool isMelee)
//        {
//            CardColumn col = CardManager.Instance.enemyColumn;

//            while (col.playCount < col.playMax)
//            {
//                CardController card = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == cardId);
//                if (card == null) yield break;

//                List<(Units attacker, Units target)> pairs = GetAttackPairsInRange(isMelee);
//                Debug.LogWarning(pairs.Count + " 数量");
//                if (pairs.Count == 0) yield break;

//                // 1. 选中并打出攻击卡
//                Debug.LogWarning("打出攻击卡");
//                yield return StartCoroutine(PlayCard(card));
//                yield return new WaitForEndOfFrame();
//                yield return new WaitForSeconds(0.3f);

//                // 等待 CardEffect 完成注册监听
//                yield return new WaitUntil(() => CardEffect.Instance.isLock);
//                yield return new WaitForSeconds(0.1f);

//                // 2. 依次处理每个攻击者-目标对
//                foreach (var (attacker, target) in pairs)
//                {
//                    if (attacker == null || target == null) continue;
//                    if (attacker.unitElement.currentAttackTime <= 0) continue;
//                    if (attacker.isLocked) continue;
//                    Debug.Log(attacker.location + " : " + target.location);

//                    // 实时距离校验（与 GetAttackPairsInRange 保持一致）
//                    bool stillInRange;
//                    if (isMelee)
//                    {
//                        int dist = MapManager.Instance.Distance(attacker.location, target.location, "Chebyshev");
//                        stillInRange = dist <= 1;
//                    }
//                    else
//                    {
//                        int dist = MapManager.Instance.Distance(attacker.location, target.location, "Manhattan");
//                        stillInRange = dist <= (int)attacker.unitElement.rangedRadius;
//                    }
//                    if (!stillInRange) continue;

//                    // 2a. 选中攻击单位（触发 UnitSelectEvent）
//                    MapCell attackerCell = MapManager.Instance.FindCellByLocation(attacker.location);
//                    if (attackerCell == null) continue;
//                    attackerCell.PickThisCell();

//                    yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//                    yield return new WaitForSeconds(0.2f);

//                    // 2b. 选中目标格（触发 ClickEvent）
//                    MapCell targetCell = MapManager.Instance.FindCellByLocation(target.location);
//                    if (targetCell == null) continue;
//                    targetCell.PickThisCell();

//                    yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//                    yield return new WaitForSeconds(0.3f);
//                }

//                // 3. 结束本次攻击卡效果
//                yield return new WaitForSeconds(0.2f);
//                Central.Instance.ActionEndEarly?.Invoke();
//                yield return new WaitForSeconds(0.2f);
//                yield return new WaitUntil(() => !CardEffect.Instance.isLock);
//            }
//        }

//        private List<(Units attacker, Units target)> GetAttackPairsInRange(bool isMelee)
//        {
//            string trait = isMelee ? "Trait_CanMelee" : "Trait_CanRanged";

//            return UnitManager.Instance.units
//                .Where(u => u != null && !u.isPlayer
//                    && !u.isLocked
//                    && u.unitElement.CheckTraits(trait))
//                .Select(attacker =>
//                {
//                    if (!TargetManager.Instance.preferredTarget.TryGetValue(attacker, out Units target))
//                        return (attacker: attacker, target: (Units)null);

//                    bool inRange;
//                    if (isMelee)
//                    {
//                        // 近战：切比雪夫距离 ≤ 1
//                        int dist = MapManager.Instance.Distance(attacker.location, target.location, "Chebyshev");
//                        inRange = dist <= 1;
//                    }
//                    else
//                    {
//                        // 远程：曼哈顿距离 ≤ 攻击范围
//                        int dist = MapManager.Instance.Distance(attacker.location, target.location, "Manhattan");
//                        inRange = dist <= (int)attacker.unitElement.rangedRadius;
//                    }

//                    return inRange
//                        ? (attacker: attacker, target: target)
//                        : (attacker: attacker, target: (Units)null);
//                })
//                .Where(pair => pair.target != null)
//                .OrderByDescending(pair =>
//                {
//                    int atk = pair.attacker.unitElement.attack;
//                    int def = pair.target.unitElement.defend;
//                    return Mathf.Max(0, atk - def);
//                })
//                .ToList();
//        }

//        // ─────────────────────────────────────────────
//        // 移动逻辑
//        // ─────────────────────────────────────────────

//        /// <summary>
//        /// 打出移动卡并让所有 AI 单位向优先目标靠近（移动进攻用）。
//        /// 流程：PickCard → PlaySelectedCard → PickThisCell 选单位 → PickThisCell 选格 → ActionEndEarly
//        /// </summary>
//        private IEnumerator PlayMoveCardTowardTargets()
//        {
//            CardColumn col = CardManager.Instance.enemyColumn;
//            CardController moveCard = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == 0);
//            if (moveCard == null) yield break;

//            // 1. 选中并打出移动卡（触发 MoveEnter）
//            yield return StartCoroutine(PlayCard(moveCard));
//            yield return new WaitForEndOfFrame();
//            yield return new WaitForSeconds(0.2f);

//            yield return new WaitUntil(() => CardEffect.Instance.isLock);
//            yield return new WaitForSeconds(0.1f);

//            List<Units> aiUnits = UnitManager.Instance.units
//                .Where(u => u != null && !u.isPlayer
//                    && !u.isLocked                                        // ← 跳过被锁定单位
//                    && !u.unitElement.CheckTraits("Trait_Flag")
//                    && u.unitElement.currentSpeed > 0)
//                .ToList();

//            foreach (var unit in aiUnits)
//            {
//                if (!TargetManager.Instance.preferredTarget.TryGetValue(unit, out Units target)) continue;
//                if (target == null) continue;

//                Vector2 moveTarget = GetMoveTargetTowardAttackRange(unit, target);
//                if (moveTarget == unit.location) continue;

//                MapCell unitCell = MapManager.Instance.FindCellByLocation(unit.location);
//                MapCell targetCell = MapManager.Instance.FindCellByLocation(moveTarget);
//                if (unitCell == null || targetCell == null) continue;

//                unitCell.PickThisCell();
//                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//                yield return new WaitForSeconds(0.2f);

//                targetCell.PickThisCell();
//                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//                yield return new WaitForSeconds(0.2f);
//            }

//            Central.Instance.ActionEndEarly?.Invoke();
//            yield return new WaitForSeconds(0.2f);
//            yield return new WaitUntil(() => !CardEffect.Instance.isLock);
//        }

//        /// <summary>
//        /// 打出移动卡并让所有 AI 单位向后线军营方向后撤。
//        /// </summary>
//        private IEnumerator RetreatAll()
//        {
//            Units backlineBarrack = GetBacklineBarrack();
//            if (backlineBarrack == null) yield break;

//            CardColumn col = CardManager.Instance.enemyColumn;
//            CardController moveCard = col.handCard.FirstOrDefault(c => c.cardInfo.cardId == 0);
//            if (moveCard == null) yield break;

//            // 1. 选中并打出移动卡（触发 MoveEnter）
//            yield return StartCoroutine(PlayCard(moveCard));
//            yield return new WaitForEndOfFrame();
//            yield return new WaitForSeconds(0.2f);

//            yield return new WaitUntil(() => CardEffect.Instance.isLock);
//            yield return new WaitForSeconds(0.1f);

//            List<Units> aiUnits = UnitManager.Instance.units
//                .Where(u => u != null && !u.isPlayer
//                    && !u.isLocked                                        // ← 跳过被锁定单位
//                    && !u.unitElement.CheckTraits("Trait_Flag")
//                    && u.unitElement.currentSpeed > 0)
//                .ToList();

//            foreach (var unit in aiUnits)
//            {
//                Vector2 retreatTarget = GetRetreatTarget(unit, backlineBarrack);
//                if (retreatTarget == unit.location) continue;

//                MapCell unitCell = MapManager.Instance.FindCellByLocation(unit.location);
//                MapCell targetCell = MapManager.Instance.FindCellByLocation(retreatTarget);
//                if (unitCell == null || targetCell == null) continue;

//                unitCell.PickThisCell();
//                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//                yield return new WaitForSeconds(0.15f);

//                targetCell.PickThisCell();
//                yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//                yield return new WaitForSeconds(0.2f);
//            }

//            Central.Instance.ActionEndEarly?.Invoke();
//            yield return new WaitForSeconds(0.2f);
//            yield return new WaitUntil(() => !CardEffect.Instance.isLock);
//        }

//        private IEnumerator PlayMoveCardAndRetreat()
//        {
//            yield return StartCoroutine(RetreatAll());
//        }

//        // ─────────────────────────────────────────────
//        // 战术调整（免费一格移动）
//        // ─────────────────────────────────────────────

//        private IEnumerator DoTactic()
//        {
//            _tacticUsed = true;

//            List<Units> aiUnits = UnitManager.Instance.units
//                .Where(u => u != null && !u.isPlayer
//                    && !u.isLocked                                        // ← 跳过被锁定单位
//                    && !u.unitElement.CheckTraits("Trait_Flag")
//                    && u.unitElement.currentTacticSpeed > 0)
//                .ToList();

//            if (aiUnits.Count == 0) yield break;

//            //CardEffect.Instance.TacticEnter(false);
//            CardManager.Instance.IntoTactic();
//            yield return new WaitForEndOfFrame();
//            yield return new WaitUntil(() => CardEffect.Instance.isLock);
//            yield return new WaitForSeconds(0.1f);

//            Units first = aiUnits.First();
//            if (TargetManager.Instance.preferredTarget.TryGetValue(first, out Units target) && target != null)
//            {
//                MapCell unitCell = MapManager.Instance.FindCellByLocation(first.location);

//                Vector2 step = GetOneStepToward(first.location, target.location);
//                MapCell stepCell = MapManager.Instance.FindCellByLocation(step);

//                if (stepCell != null && stepCell.unit == null && stepCell.IsWalkable(false))
//                {
//                    if (unitCell != null)
//                        unitCell.PickThisCell();

//                    yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//                    yield return new WaitForSeconds(0.2f);

//                    stepCell.PickThisCell();
//                }
//                else
//                    Central.Instance.ActionEndEarly?.Invoke();
//            }
//            else
//            {
//                Central.Instance.ActionEndEarly?.Invoke();
//            }

//            yield return new WaitUntil(() => !UnitCommandManager.isUnitActing);
//            yield return new WaitForSeconds(0.2f);

//            Central.Instance.ActionEndEarly?.Invoke();
//            yield return new WaitForSeconds(0.1f);
//            yield return new WaitUntil(() => !CardEffect.Instance.isLock);
//        }

//        // ─────────────────────────────────────────────
//        // 辅助工具方法
//        // ─────────────────────────────────────────────

//        private int GetEnemyNonBarrackCount()
//            => UnitManager.Instance.units.Count(
//                u => u != null && !u.isPlayer && !u.unitElement.CheckTraits("Trait_Flag"));

//        private int GetEnemyBarrackCount()
//            => UnitManager.Instance.units.Count(
//                u => u != null && !u.isPlayer && u.unitElement.CheckTraits("Trait_Flag"));

//        private Units GetBacklineBarrack()
//        {
//            var barrackPrior = TargetManager.Instance.barrackPrior;
//            if (barrackPrior.Count == 0) return null;
//            return barrackPrior.OrderBy(kv => kv.Value).First().Key;
//        }

//        private Vector2 GetMoveTargetTowardAttackRange(Units unit, Units target)
//        {
//            int speed = unit.unitElement.currentSpeed;
//            float attackRange = unit.unitElement.rangedRadius;
//            if (speed <= 0) return unit.location;

//            Vector2 best = unit.location;
//            int bestDistToTarget = int.MaxValue;
//            bool foundInRange = false;

//            foreach (var cell in MapManager.Instance.GetCellList())
//            {
//                int moveDist = MapManager.Instance.Distance(unit.location, cell.location, "Manhattan");
//                if (moveDist > speed || moveDist == 0) continue;
//                if (!cell.IsWalkable(false) || cell.unit != null) continue;

//                int distToTarget = MapManager.Instance.Distance(cell.location, target.location, "Manhattan");
//                bool inRange = distToTarget <= (int)attackRange;

//                if (inRange && !foundInRange)
//                {
//                    foundInRange = true;
//                    bestDistToTarget = distToTarget;
//                    best = cell.location;
//                }
//                else if (inRange && distToTarget < bestDistToTarget)
//                {
//                    bestDistToTarget = distToTarget;
//                    best = cell.location;
//                }
//                else if (!foundInRange && distToTarget < bestDistToTarget)
//                {
//                    bestDistToTarget = distToTarget;
//                    best = cell.location;
//                }
//            }
//            return best;
//        }

//        private Vector2 GetRetreatTarget(Units unit, Units backlineBarrack)
//        {
//            int speed = unit.unitElement.currentSpeed;
//            if (speed <= 0) return unit.location;

//            List<Units> playerUnits = UnitManager.Instance.units
//                .Where(u => u != null && u.isPlayer).ToList();

//            Vector2 best = unit.location;
//            float bestScore = float.NegativeInfinity;

//            foreach (var cell in MapManager.Instance.GetCellList())
//            {
//                int dist = MapManager.Instance.Distance(unit.location, cell.location, "Manhattan");
//                if (dist > speed || dist == 0) continue;
//                if (!cell.IsWalkable(false) || cell.unit != null) continue;

//                float toBarrack = -MapManager.Instance.Distance(
//                    cell.location, backlineBarrack.location, "Manhattan");

//                float awayFromPlayer = playerUnits.Count > 0
//                    ? playerUnits.Sum(p =>
//                        MapManager.Instance.Distance(cell.location, p.location, "Manhattan"))
//                    : 0f;

//                float score = toBarrack * 2f + awayFromPlayer;
//                if (score > bestScore)
//                {
//                    bestScore = score;
//                    best = cell.location;
//                }
//            }
//            return best;
//        }

//        private Vector2 GetOneStepToward(Vector2 from, Vector2 to)
//        {
//            float dx = to.x - from.x;
//            float dy = to.y - from.y;

//            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
//                return from + new Vector2(Mathf.Sign(dx), 0);
//            else
//                return from + new Vector2(0, Mathf.Sign(dy));
//        }
//    }
//}