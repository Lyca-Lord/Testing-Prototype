using UnityEngine;

public class FollowMouse : MonoBehaviour
{
    [Tooltip("可在 Inspector 指定用于转换屏幕坐标到世界坐标的相机，留空则使用 Camera.main")]
    [SerializeField]
    private Camera followCamera;

    void Awake()
    {
        if (followCamera == null)
        {
            followCamera = Camera.main;
        }
    }

    void Update()
    {
        if (followCamera == null)
        {
            return; // 无相机，无法转换坐标
        }

        Vector3 screenPos = Input.mousePosition;
        // 使用物体当前的 z 值作为目标 z，避免把物体移动到相机近裁剪面
        float currentZ = transform.position.z;
        // ScreenToWorldPoint 需要一个包含合适 z 的屏幕坐标（相对于相机）
        // 对于透视相机，z 应为物体到相机的距离；这里用物体到相机的世界距离
        float distanceToCamera = Mathf.Abs(currentZ - followCamera.transform.position.z);
        screenPos.z = distanceToCamera;

        Vector3 worldPos = followCamera.ScreenToWorldPoint(screenPos);
        // 保持原始 z（以防某些设置导致 z 被改变）

        worldPos.z = 0;
        worldPos /= 20;
        //worldPos.x = Mathf.Sign(worldPos.x) * Mathf.Sqrt(Mathf.Abs(worldPos.x));
        //worldPos.y = Mathf.Sign(worldPos.y) * Mathf.Sqrt(Mathf.Abs(worldPos.y));

        if (Mathf.Abs(worldPos.x) < 0.05f) worldPos.x = 0;
        if (Mathf.Abs(worldPos.y) < 0.05f) worldPos.y = 0;

        if (worldPos.magnitude > 1) worldPos = worldPos.normalized;

        worldPos.z = currentZ;

        transform.position = worldPos;
    }
}
