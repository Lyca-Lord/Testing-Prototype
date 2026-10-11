using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 表现层对外契约，Chess 只认这个接口，不关心具体是用插值、DOTween 还是动画状态机实现。
/// 所有方法都是"播放某个表现，完成后回调"的形式，方便状态机等待动画播完再继续推进。
/// </summary>
public interface IChessView
{
    void PlayMove(List<Vector2> _path, Action _onComplete); // 播放移动位移表现

    void PlayKnockback(List<Vector2> _path, Action _onComplete); // 播放击退位移表现

    void PlayHit(int _actualDamage, Action _onComplete); // 播放受击反馈表现

    void PlayDeath(Action _onComplete); // 播放死亡表现

    void SetPickIndicator(bool _visible); // 显示/隐藏"可被选中"提示

    void SetCardSelectIndicator(bool _visible); // 显示/隐藏"卡牌选中后高亮"提示

    void SetBodyVisible(bool _visible); // 显示/隐藏棋子本体

    void SyncSortingOrder(int _order); // 同步排序层级
} // 表现层契约

/// <summary>
/// 棋子表现层默认实现，承担一切"看得见"的东西：
/// 位移插值、受击反馈、死亡表现、指示器开关与排序层级，
/// 不做任何规则判断，只负责"被告知要演什么、演多久"。
/// </summary>
public partial class ChessView : MonoBehaviour, IChessView
{
    [Header("组件引用")]
    public SpriteRenderer body; // 棋子本体精灵
    public SpriteRenderer pickIndicator; // 可被选中提示框
    public SpriteRenderer cardSelectIndicator; // 卡牌选中后的高亮提示

    [Header("表现参数")]
    public float moveSpeed = 5f; // 移动插值速度，与原型手感基准保持一致
    public float hitFlashDuration = 0.1f; // 受击闪白持续时间
    public float deathFadeDuration = 0.3f; // 死亡淡出持续时间

    private Color normalColor; // 本体原始颜色，受击闪白后用于恢复

    private void Awake()
    {
        if (body != null)
        {
            normalColor = body.color; // 缓存原始颜色，供受击闪白恢复使用
        }

        SetPickIndicator(false);
        SetCardSelectIndicator(false);
    } // 初始化默认表现状态
} // 组件引用与基础设置

public partial class ChessView
{
    /// <summary>
    /// 播放一段沿路径点连续移动的位移表现，全部走完后调用 _onComplete。
    /// </summary>
    /// <param name="_path">途经的世界坐标点列表（含起点）</param>
    /// <param name="_onComplete">播放完成回调</param>
    public void PlayMove(List<Vector2> _path, Action _onComplete)
        => StartCoroutine(MoveAlongPath(_path, _onComplete));

    /// <summary>
    /// 击退表现复用与主动移动相同的插值机制，日后可以在这里换一条不同的缓动曲线以示区分。
    /// </summary>
    public void PlayKnockback(List<Vector2> _path, Action _onComplete)
        => StartCoroutine(MoveAlongPath(_path, _onComplete));

    private IEnumerator MoveAlongPath(List<Vector2> _path, Action _onComplete)
    {
        if (_path == null || _path.Count == 0)
        {
            _onComplete?.Invoke(); // 空路径直接判定播放完成，避免调用方卡死等待

            yield break;
        }

        for (int i = 0; i < _path.Count; i++)
        {
            Vector2 target = _path[i]; // 当前途经点

            while (Vector2.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    target,
                    moveSpeed * Time.deltaTime
                    );

                yield return null;
            }

            transform.position = target;
        }

        _onComplete?.Invoke();
    } // 沿路径逐点插值移动
} // 位移表现（移动与击退）

public partial class ChessView
{
    /// <summary>
    /// 播放受击反馈：本体短暂变红后恢复，完成后调用 _onComplete。
    /// </summary>
    /// <param name="_actualDamage">实际造成的扣血量，可用于后续按伤害大小调整反馈强度</param>
    /// <param name="_onComplete">播放完成回调</param>
    public void PlayHit(int _actualDamage, Action _onComplete)
        => StartCoroutine(HitEnumerator(_onComplete));

    private IEnumerator HitEnumerator(Action _onComplete)
    {
        if (body != null)
        {
            body.color = Color.red;

            yield return new WaitForSeconds(hitFlashDuration);

            body.color = normalColor;
        }

        _onComplete?.Invoke();
    } // 受击闪白表现

    /// <summary>
    /// 播放死亡表现：本体淡出后调用 _onComplete，由调用方（Chess）决定什么时候真正销毁物体。
    /// </summary>
    public void PlayDeath(Action _onComplete)
        => StartCoroutine(DeathEnumerator(_onComplete));

    private IEnumerator DeathEnumerator(Action _onComplete)
    {
        if (body != null)
        {
            float timer = 0f; // 淡出计时器
            Color startColor = body.color;

            while (timer < deathFadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, timer / deathFadeDuration);
                body.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

                yield return null;
            }
        }

        _onComplete?.Invoke();
    } // 死亡淡出表现
} // 受击与死亡表现

public partial class ChessView
{
    public void SetPickIndicator(bool _visible)
    {
        if (pickIndicator != null)
        {
            pickIndicator.enabled = _visible;
        }
    } // 可被选中提示开关

    public void SetCardSelectIndicator(bool _visible)
    {
        if (cardSelectIndicator != null)
        {
            cardSelectIndicator.enabled = _visible;
        }
    } // 卡牌选中高亮提示开关

    public void SetBodyVisible(bool _visible)
    {
        if (body != null)
        {
            body.enabled = _visible;
        }
    } // 本体可见性开关

    /// <summary>
    /// 同步棋子相关精灵的排序层级，保证站在前排格子的棋子画面不被后排的遮挡。
    /// </summary>
    /// <param name="_order">目标排序层级</param>
    public void SyncSortingOrder(int _order)
    {
        if (body != null)
        {
            body.sortingOrder = _order;
        }

        if (pickIndicator != null)
        {
            pickIndicator.sortingOrder = _order + 1;
        }

        if (cardSelectIndicator != null)
        {
            cardSelectIndicator.sortingOrder = _order - 1;
        }
    } // 排序层级同步
} // 指示器与排序层级
