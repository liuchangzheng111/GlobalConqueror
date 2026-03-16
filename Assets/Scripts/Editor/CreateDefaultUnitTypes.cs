using UnityEngine;
using UnityEditor;
using GlobalConqueror.Models;

namespace GlobalConqueror.Editor
{
    /// <summary>
    /// 编辑器工具：创建默认兵种配置
    /// </summary>
    public static class CreateDefaultUnitTypes
    {
        [MenuItem("GlobalConqueror/创建默认兵种配置")]
        public static void Create()
        {
            string folder = "Assets/Data/UnitTypes";
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
            {
                AssetDatabase.CreateFolder("Assets", "Data");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Data/UnitTypes"))
            {
                AssetDatabase.CreateFolder("Assets/Data", "UnitTypes");
            }

            CreateUnitType(folder, "Infantry", "步兵", 2, 1, 10, 10, 100, 50, 0);
            CreateUnitType(folder, "Cavalry", "骑兵", 3, 1, 12, 8, 150, 80, 0);
            CreateUnitType(folder, "Artillery", "炮兵", 1, 2, 15, 5, 200, 120, 20);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"已创建默认兵种配置于 {folder}");
        }

        private static void CreateUnitType(string folder, string fileName, string displayName,
            int moveRange, int attackRange, int atk, int def,
            int gold, int industry, int science)
        {
            string path = $"{folder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<UnitTypeConfig>(path);
            if (existing != null)
            {
                Debug.Log($"已存在 {path}，跳过");
                return;
            }

            var config = ScriptableObject.CreateInstance<UnitTypeConfig>();
            config.unitTypeName = displayName;
            config.movementRange = moveRange;
            config.attackRange = attackRange;
            config.attackStrength = atk;
            config.health = def;
            config.goldCost = gold;
            config.industryCost = industry;
            config.scienceCost = science;
            config.mountainMoveCost = 2;

            AssetDatabase.CreateAsset(config, path);
        }
    }
}
