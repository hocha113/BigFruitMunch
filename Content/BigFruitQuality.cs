using Microsoft.Xna.Framework;
using Terraria.ID;

namespace BigFruitMunch.Content
{
    /// <summary>
    /// 大果品质等级。用于切割机的随机产物以及可嚼大果对应的 Buff 等级。
    /// </summary>
    public enum BigFruitQuality
    {
        /// <summary>干瘪 - 灰色 - 10%</summary>
        Withered = 0,
        /// <summary>普通 - 白色 - 40%</summary>
        Common = 1,
        /// <summary>优秀 - 绿色 - 25%</summary>
        Excellent = 2,
        /// <summary>稀有 - 蓝色 - 15%</summary>
        Rare = 3,
        /// <summary>史诗 - 紫色 - 5%</summary>
        Epic = 4,
        /// <summary>传奇 - 金色 - 3%</summary>
        Legendary = 5,
        /// <summary>神话 - 红色 - 2%</summary>
        Mythic = 6,
    }

    public static class BigFruitQualityExtensions
    {
        /// <summary>对应的稀有度颜色（与 ItemRarityID 对齐，用于物品稀有度显示）。</summary>
        public static int ToRarity(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => ItemRarityID.Gray,        // -1
            BigFruitQuality.Common => ItemRarityID.White,       // 0
            BigFruitQuality.Excellent => ItemRarityID.Green,       // 2
            BigFruitQuality.Rare => ItemRarityID.Blue,        // 1
            BigFruitQuality.Epic => ItemRarityID.Purple,      // 11
            BigFruitQuality.Legendary => ItemRarityID.Yellow,      // 8
            BigFruitQuality.Mythic => ItemRarityID.Red,         // 10
            _ => ItemRarityID.White,
        };

        /// <summary>用于绘制时的着色（"高级着色器滤镜" 简化版本）。</summary>
        public static Color ToTint(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => new Color(150, 150, 150), // 灰
            BigFruitQuality.Common => new Color(255, 255, 255), // 白
            BigFruitQuality.Excellent => new Color(120, 230, 120), // 绿
            BigFruitQuality.Rare => new Color(120, 160, 255), // 蓝
            BigFruitQuality.Epic => new Color(200, 120, 255), // 紫
            BigFruitQuality.Legendary => new Color(255, 215, 80),  // 金
            BigFruitQuality.Mythic => new Color(255, 80, 80),   // 红
            _ => Color.White,
        };

        /// <summary>切割机产出该品质的概率（0-1）。</summary>
        public static float DropChance(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => 0.10f,
            BigFruitQuality.Common => 0.40f,
            BigFruitQuality.Excellent => 0.25f,
            BigFruitQuality.Rare => 0.15f,
            BigFruitQuality.Epic => 0.05f,
            BigFruitQuality.Legendary => 0.03f,
            BigFruitQuality.Mythic => 0.02f,
            _ => 0f,
        };

        /// <summary>对应的【嚼的爽！】Buff 等级（0-5）。返回 -1 表示干瘪不给予任何 Buff。</summary>
        public static int ToBuffLevel(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => -1, // 干瘪：吃了什么效果都没有
            BigFruitQuality.Common => 0,
            BigFruitQuality.Excellent => 1,
            BigFruitQuality.Rare => 2,
            BigFruitQuality.Epic => 3,
            BigFruitQuality.Legendary => 4,
            BigFruitQuality.Mythic => 5,
            _ => -1,
        };
    }
}
