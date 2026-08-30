using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelMapReader : MonoBehaviour
{
    [Header("Information Reference")]
    public List<TextAsset> jsons;
    public List<LevelMapInfo> levelMapInfos;

    // 添加这个特性，可以在 Inspector 面板右键组件直接执行此方法
    [ContextMenu("一键从 JSON 读取并覆盖 ScriptableObject")]
    public void LoadMapsFromJsons()
    {
        if (jsons == null || levelMapInfos == null)
        {
            Debug.LogError("JSON 列表或 LevelMapInfo 列表未初始化！");
            return;
        }

        int count = Mathf.Min(jsons.Count, levelMapInfos.Count);
        if (jsons.Count != levelMapInfos.Count)
        {
            Debug.LogWarning($"[警告] JSON 列表({jsons.Count})与目标列表({levelMapInfos.Count})长度不一致！将只处理前 {count} 项。");
        }

        for (int i = 0; i < count; i++)
        {
            if (jsons[i] != null && levelMapInfos[i] != null)
            {
                // 读取 JSON 文本并解析到对应的 ScriptableObject
                levelMapInfos[i].LoadFromJson(jsons[i].text);
                
                Debug.Log($"[成功] 读取 {jsons[i].name}.json 并覆写到了 {levelMapInfos[i].name}");

#if UNITY_EDITOR
                // 【核心】标记该 ScriptableObject 被修改过了，否则 Unity 重启或打包后数据会丢失
                EditorUtility.SetDirty(levelMapInfos[i]);
#endif
            }
            else
            {
                Debug.LogWarning($"[跳过] 索引 {i} 存在空引用 (JSON 缺失或目标 ScriptableObject 缺失)");
            }
        }

#if UNITY_EDITOR
        // 保存所有被标记为 Dirty 的资产到硬盘上
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green><b>所有地图数据处理完毕并已物理保存！</b></color>");
#endif
    }
}
