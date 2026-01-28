using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unit;

public class Central : MonoBehaviour
{
    [Header("For Action Sequence")]
    public static Action ActionStart;
    public static Action ActionEnd;

    [Header("For Type Action")]
    public static Action<UnitCommand> MoveAction;
    public static Action<UnitCommand> MeleeAction;
    public static Action<UnitCommand> RangeAction;
    public static Action<UnitCommand> SkillAction;
    public static Action<UnitCommand> MagicAction;

    [Header("For Action End")]
    public static Action<UnitCommand> MoveEnd;
    public static Action<UnitCommand> MeleeEnd;
    public static Action<UnitCommand> RangeEnd;
    public static Action<UnitCommand> SkillEnd;
    public static Action<UnitCommand> MagicEnd;
}
