using System.Collections.Generic;
using System.Linq;
using Unit;
using UnityEngine;
using UnityEngine.Events;

public class Central : MonoBehaviour
{
    public static Central Instance { get; private set; }
    public static bool isPlayerTurn = true;

    [Header("Next Turn Event")]
    public UnityEvent NextTurnStart; // 在CardColumn中调用，AI直接调用，玩家需要点击按钮
    public UnityEvent TurnBeginEvent; // 也是在CardColumn中调用

    [Header("For Action Sequence")]
    public UnityEvent ActionStart; // 在UnitCommandManager中调用
    public UnityEvent ActionEnd; // 在UnitCommandManager中调用
    public UnityEvent ActionEndEarly;

    [Header("For Type Action")]
    public UnityEvent<UnitCommand> MoveAction;
    public UnityEvent<UnitCommand> MeleeAction;
    public UnityEvent<UnitCommand> RangeAction;
    public UnityEvent<UnitCommand> SkillAction;
    public UnityEvent<UnitCommand> MagicAction;

    [Header("For Action End")]
    public UnityEvent<UnitCommand> MoveEnd;
    public UnityEvent<UnitCommand> MeleeEnd;
    public UnityEvent<UnitCommand> RangeEnd;
    public UnityEvent<UnitCommand> SkillEnd;
    public UnityEvent<UnitCommand> MagicEnd;

    [Header("Mouse Event")]
    public UnityEvent<Vector2> ClickEvent;
    public UnityEvent<Units> UnitSelectEvent;
    public UnityEvent ReleaseSelectEvent;
    public UnityEvent CancelEvent;

    [Header("Card Action")]
    public UnityEvent CardPlayEvent;
    public UnityEvent CardEndEvent;

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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1)) CancelEvent?.Invoke();
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

        MoveEnd ??= new UnityEvent<UnitCommand>();
        MeleeEnd ??= new UnityEvent<UnitCommand>();
        RangeEnd ??= new UnityEvent<UnitCommand>();
        SkillEnd ??= new UnityEvent<UnitCommand>();
        MagicEnd ??= new UnityEvent<UnitCommand>();

        ClickEvent ??= new UnityEvent<Vector2>();
        UnitSelectEvent ??= new UnityEvent<Units>();
        ReleaseSelectEvent ??= new UnityEvent();

        CardPlayEvent ??= new UnityEvent();
        CardEndEvent ??= new UnityEvent();
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

        MoveEnd?.RemoveAllListeners();
        MeleeEnd?.RemoveAllListeners();
        RangeEnd?.RemoveAllListeners();
        SkillEnd?.RemoveAllListeners();
        MagicEnd?.RemoveAllListeners();

        ClickEvent?.RemoveAllListeners();
        UnitSelectEvent?.RemoveAllListeners();
        ReleaseSelectEvent?.RemoveAllListeners();

        CardPlayEvent?.RemoveAllListeners();
        CardEndEvent?.RemoveAllListeners();
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
