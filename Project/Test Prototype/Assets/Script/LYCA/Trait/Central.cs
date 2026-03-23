using Map;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unit;
using UnityEngine;
using UnityEngine.Events;

public class Central : MonoBehaviour
{
    public static Central Instance { get; private set; }
    public static bool isPlayerTurn = true;
    public static GamePhase currentBattlePhrase = GamePhase.Deploying;

    [Header("Color Pick")]
    public Color playerColor;
    public Color enemyColor;
    public bool isPlayer;

    [Header("Next Turn Event")]
    [HideInInspector] public UnityEvent NextTurnStart; // 在CardColumn中调用，AI直接调用，玩家需要点击按钮
    [HideInInspector] public UnityEvent TurnBeginEvent; // 也是在CardColumn中调用

    [Header("For Action Sequence")]
    [HideInInspector] public UnityEvent ActionStart; // 在UnitCommandManager中调用
    [HideInInspector] public UnityEvent ActionEnd; // 在UnitCommandManager中调用
    [HideInInspector] public UnityEvent ActionEndEarly;

    [Header("For Type Action")]
    [HideInInspector] public UnityEvent<UnitCommand> MoveAction;
    [HideInInspector] public UnityEvent<UnitCommand> MeleeAction;
    [HideInInspector] public UnityEvent<UnitCommand> RangeAction;
    [HideInInspector] public UnityEvent<UnitCommand> SkillAction;
    [HideInInspector] public UnityEvent<UnitCommand> MagicAction;
    [HideInInspector] public UnityEvent<UnitCommand> WaitForMoveAction;
    [HideInInspector] public UnityEvent<UnitCommand> WaitForMeleeAction;
    [HideInInspector] public UnityEvent<UnitCommand> WaitForRangeAction;
    [HideInInspector] public UnityEvent<UnitCommand> WaitForSkillAction;
    [HideInInspector] public UnityEvent<UnitCommand> WaitForMagicAction;

    [Header("For Action End")]
    [HideInInspector] public UnityEvent<UnitCommand> MoveEnd;
    [HideInInspector] public UnityEvent<UnitCommand> MeleeEnd;
    [HideInInspector] public UnityEvent<UnitCommand> RangeEnd;
    [HideInInspector] public UnityEvent<UnitCommand> SkillEnd;
    [HideInInspector] public UnityEvent<UnitCommand> MagicEnd;

    [Header("Mouse Event")]
    [HideInInspector] public UnityEvent<Vector2> ClickEvent;
    [HideInInspector] public UnityEvent<Units> UnitSelectEvent;
    [HideInInspector] public UnityEvent ReleaseSelectEvent;
    [HideInInspector] public UnityEvent CancelEvent;
    [HideInInspector] public UnityEvent SkipEvent;

    [Header("Card Action")]
    [HideInInspector] public UnityEvent CardPlayEvent;
    [HideInInspector] public UnityEvent CardEndEvent;
    [HideInInspector] public UnityEvent<bool> CostUpdateEvent;
    [HideInInspector] public UnityEvent<bool> PlayNumUpdateEvent;

    [Header("Phase Event")]
    [HideInInspector] public UnityEvent DeployingPhaseEnd;

    [Header("Unit Event")]
    [HideInInspector] public UnityEvent<Units> UnitDieEvent;
    [HideInInspector] public UnityEvent UnitNumChangeEvent;

    private void Awake()
    {
        // 单例唯一化：避免重复创建实例导致UnityEvent订阅累积
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // 跨场景持久化，避免场景切换时销毁

        ClearAllUnityEvents();
        InitUnityEvents();
        List<IInitialiazer> ini = FindObjectsOfType<MonoBehaviour>()
            .OfType<IInitialiazer>()
            .ToList();
        foreach (var i in ini) i.Initialize();

        isPlayerTurn = true; // 初始化玩家回合状态
        NextTurnStart.AddListener(() =>
        {
            isPlayerTurn = !isPlayerTurn;
            Debug.Log($"Turn changed. Is player turn: {isPlayerTurn}");
        });

        StartCoroutine(Enumerator());

        IEnumerator Enumerator()
        {
            yield return new WaitForEndOfFrame();
            MapManager.Instance.CreateMap();
            yield return new WaitForEndOfFrame();
            UnitManager.Instance.Register();
            yield return new WaitForEndOfFrame();
            DeployPhaseManager.Instance.StartDeployPhase();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1)) CancelEvent?.Invoke();
        isPlayer = isPlayerTurn;
    }

    private void InitUnityEvents()
    {
        NextTurnStart ??= new UnityEvent();
        TurnBeginEvent ??= new UnityEvent();

        ActionStart ??= new UnityEvent();
        ActionEnd ??= new UnityEvent();
        ActionEndEarly ??= new UnityEvent();

        MoveAction ??= new UnityEvent<UnitCommand>();
        MeleeAction ??= new UnityEvent<UnitCommand>();
        RangeAction ??= new UnityEvent<UnitCommand>();
        SkillAction ??= new UnityEvent<UnitCommand>();
        MagicAction ??= new UnityEvent<UnitCommand>();
        WaitForMoveAction ??= new UnityEvent<UnitCommand>();
        WaitForMeleeAction ??= new UnityEvent<UnitCommand>();
        WaitForRangeAction ??= new UnityEvent<UnitCommand>();
        WaitForSkillAction ??= new UnityEvent<UnitCommand>();
        WaitForMagicAction ??= new UnityEvent<UnitCommand>();

        MoveEnd ??= new UnityEvent<UnitCommand>();
        MeleeEnd ??= new UnityEvent<UnitCommand>();
        RangeEnd ??= new UnityEvent<UnitCommand>();
        SkillEnd ??= new UnityEvent<UnitCommand>();
        MagicEnd ??= new UnityEvent<UnitCommand>();

        ClickEvent ??= new UnityEvent<Vector2>();
        UnitSelectEvent ??= new UnityEvent<Units>();
        ReleaseSelectEvent ??= new UnityEvent();
        CancelEvent ??= new UnityEvent();
        SkipEvent ??= new UnityEvent();

        CardPlayEvent ??= new UnityEvent();
        CardEndEvent ??= new UnityEvent();
        CostUpdateEvent ??= new UnityEvent<bool>();
        PlayNumUpdateEvent ??= new UnityEvent<bool>();

        DeployingPhaseEnd ??= new UnityEvent();

        UnitDieEvent ??= new UnityEvent<Units>();
        UnitNumChangeEvent ??= new UnityEvent();
    }

    public void ClearAllUnityEvents()
    {
        NextTurnStart?.RemoveAllListeners();
        TurnBeginEvent?.RemoveAllListeners();

        ActionStart?.RemoveAllListeners();
        ActionEnd?.RemoveAllListeners();

        MoveAction?.RemoveAllListeners();
        MeleeAction?.RemoveAllListeners();
        RangeAction?.RemoveAllListeners();
        SkillAction?.RemoveAllListeners();
        MagicAction?.RemoveAllListeners();
        WaitForMoveAction?.RemoveAllListeners();
        WaitForMeleeAction?.RemoveAllListeners();
        WaitForRangeAction?.RemoveAllListeners();
        WaitForSkillAction?.RemoveAllListeners();
        WaitForMagicAction?.RemoveAllListeners();

        MoveEnd?.RemoveAllListeners();
        MeleeEnd?.RemoveAllListeners();
        RangeEnd?.RemoveAllListeners();
        SkillEnd?.RemoveAllListeners();
        MagicEnd?.RemoveAllListeners();

        ClickEvent?.RemoveAllListeners();
        UnitSelectEvent?.RemoveAllListeners();
        ReleaseSelectEvent?.RemoveAllListeners();
        CancelEvent?.RemoveAllListeners();
        SkipEvent?.RemoveAllListeners();

        CardPlayEvent?.RemoveAllListeners();
        CardEndEvent?.RemoveAllListeners();
        CostUpdateEvent?.RemoveAllListeners();
        PlayNumUpdateEvent?.RemoveAllListeners();

        DeployingPhaseEnd?.RemoveAllListeners();
        
        UnitDieEvent?.RemoveAllListeners();
        UnitNumChangeEvent?.RemoveAllListeners();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            ClearAllUnityEvents();
            Instance = null;
        }
    }
}

public enum GamePhase 
{
    Deploying,
    FirstTactic,
    Battle,
    Annihilate
}