using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unit;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLevelMapInfo", menuName = "LevelMap/LevelMapInfo", order = 3)]
public class LevelMapInfo : ScriptableObject
{
    [Header("Grid Info")]
    public int gridWidth;
    public int gridHeight;
    public int[] grids;

    [Header("Flag Positions")]
    public Vector2Int[] playerFlagPositions;
    public Vector2Int[] enemyFlagPositions;

    [Header("Origin Enemy")]
    // 最终供外部使用的真正的数据（包含对预告体的引用）
    public List<OriginEnemyInfo> originEnemyInfos;

    public void LoadFromJson(string _json)
    {
        if (string.IsNullOrEmpty(_json)) return;

        // 1. 去除注释，保留换行符（JsonUtility 会自动忽略多余的换行和空格）
        string pattern = @"(""(?:[^""\\]|\\.)*"")|/\*[\s\S]*?\*/|//.*";
        string cleanJson = Regex.Replace(_json, pattern, m => m.Groups[1].Value);

        // 2. 用专属的数据结构反序列化 JSON
        LevelMapJsonData data = JsonUtility.FromJson<LevelMapJsonData>(cleanJson);
        if (data == null) return;

        // 3. 将反序列化的基础数据赋值给当前 ScriptableObject
        this.gridWidth = data.gridWidth;
        this.gridHeight = data.gridHeight;
        this.playerFlagPositions = data.playerFlagPositions;
        this.enemyFlagPositions = data.enemyFlagPositions;

        // 4. 处理网格数据（直接使用一维数组接收）
        if (data.grids != null)
        {
            this.grids = new int[data.grids.Length];
            Array.Copy(data.grids, this.grids, data.grids.Length);
        }

        // 5. 将读取到的纯数据 (OriginEnemyInfoData) 转换并装配为你需要的对象形式 (OriginEnemyInfo)
        this.originEnemyInfos = new List<OriginEnemyInfo>();
        if (data.originEnemyInfos != null)
        {
            foreach (var enemyData in data.originEnemyInfos)
            {
                this.originEnemyInfos.Add(new OriginEnemyInfo(enemyData));
            }
        }
    }

    /// <summary>
    /// 添加一个辅助方法供外部在以前使用二维数组的地方调用
    /// </summary>
    public int GetGrid(int x, int y)
    {
        if (grids == null || x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) return 0;
        return grids[y * gridWidth + x];
    }

    [Serializable]
    public class OriginEnemyInfo
    {
        public UnitInfo enemyUnit;
        public Vector2Int originPosition;

        public OriginEnemyInfo(OriginEnemyInfoData data)
        {
            this.originPosition = data.position;

#if UNITY_EDITOR
            // 拼接预制体的相对路径
            string prefabPath = $"Assets/Scriptable Object/Unit/{data.unitName}.asset";
            
            // 使用 AssetDatabase 根据路径加载 GameObject
            this.enemyUnit = UnityEditor.AssetDatabase.LoadAssetAtPath<UnitInfo>(prefabPath);

            // 如果策划配错了名字或者路径不对，打印警告方便排查
            if (this.enemyUnit == null)
            {
                Debug.LogWarning($"[JSON解析] 无法在路径 {prefabPath} 找到对应的预制体，请检查 unitName: {data.unitName}");
            }
#else
            Debug.LogError("AssetDatabase 只能在 Unity Editor 环境下使用。如果需要在游戏打包后动态加载，请将预制体移至 Resources 文件夹并使用 Resources.Load");
#endif
        }
    }

    // 记录Json版本的敌人信息
    [Serializable]
    public class OriginEnemyInfoData
    {
        public string unitName; 
        public Vector2Int position;
        
        public OriginEnemyInfoData(string unitName, Vector2Int pos)
        {
            this.unitName = unitName;
            this.position = pos;
        }

        public OriginEnemyInfoData()
        {
            this.unitName = string.Empty;
            this.position = Vector2Int.zero;
        }
    }

    // 用于解析 JSON 的内部数据结构
    [Serializable]
    private class LevelMapJsonData
    {
        public int gridWidth;
        public int gridHeight;
        // 一维数组接收 JSON 里的跨行数据
        public int[] grids;

        public Vector2Int[] playerFlagPositions;
        public Vector2Int[] enemyFlagPositions;
        public List<OriginEnemyInfoData> originEnemyInfos;
    }
}
