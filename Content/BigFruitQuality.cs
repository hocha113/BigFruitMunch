using Terraria;
using Terraria.ID;

namespace BigFruitMunch.Content
{
    /// <summary>切割机 roll & 嚼的爽档位</summary>
    public enum BigFruitQuality
    {
        Withered = 0,
        Common = 1,
        Excellent = 2,
        Rare = 3,
        Epic = 4,
        Legendary = 5,
        Mythic = 6,
    }

    /// <summary>品质附带的机制/喜剧风味</summary>
    public enum BigFruitFlavor
    {
        None,
        Placebo,
        Floaty,
        LotusMouth,
        GoldenMouth,
        Ascension,
    }

    public static class BigFruitQualityExtensions
    {
        public static int ToRarity(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => ItemRarityID.Gray,        //-1
            BigFruitQuality.Common => ItemRarityID.White,       //0
            BigFruitQuality.Excellent => ItemRarityID.Green,       //2
            BigFruitQuality.Rare => ItemRarityID.Blue,        //1
            BigFruitQuality.Epic => ItemRarityID.Purple,      //11
            BigFruitQuality.Legendary => ItemRarityID.Yellow,      //8
            BigFruitQuality.Mythic => ItemRarityID.Red,         //10
            _ => ItemRarityID.White,
        };

        public static Color ToTint(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => new Color(150, 150, 150), //灰
            BigFruitQuality.Common => new Color(255, 255, 255), //白
            BigFruitQuality.Excellent => new Color(120, 230, 120), //绿
            BigFruitQuality.Rare => new Color(120, 160, 255), //蓝
            BigFruitQuality.Epic => new Color(200, 120, 255), //紫
            BigFruitQuality.Legendary => new Color(255, 215, 80),  //金
            BigFruitQuality.Mythic => new Color(255, 80, 80),   //红
            _ => Color.White,
        };

        /// <summary>Mk1 概率</summary>
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

        /// <summary>Mk2 概率，偏高品质</summary>
        public static float DropChance(this BigFruitQuality q, bool upgradedCutter) {
            if (!upgradedCutter) return q.DropChance();

            return q switch {
                BigFruitQuality.Withered => 0.05f,
                BigFruitQuality.Common => 0.32f,
                BigFruitQuality.Excellent => 0.28f,
                BigFruitQuality.Rare => 0.20f,
                BigFruitQuality.Epic => 0.08f,
                BigFruitQuality.Legendary => 0.04f,
                BigFruitQuality.Mythic => 0.03f,
                _ => 0f,
            };
        }

        public static BigFruitQuality Roll(bool upgradedCutter = false) {
            float r = Main.rand.NextFloat();
            float c = 0f;

            c += BigFruitQuality.Withered.DropChance(upgradedCutter);
            if (r < c) return BigFruitQuality.Withered;

            c += BigFruitQuality.Common.DropChance(upgradedCutter);
            if (r < c) return BigFruitQuality.Common;

            c += BigFruitQuality.Excellent.DropChance(upgradedCutter);
            if (r < c) return BigFruitQuality.Excellent;

            c += BigFruitQuality.Rare.DropChance(upgradedCutter);
            if (r < c) return BigFruitQuality.Rare;

            c += BigFruitQuality.Epic.DropChance(upgradedCutter);
            if (r < c) return BigFruitQuality.Epic;

            c += BigFruitQuality.Legendary.DropChance(upgradedCutter);
            if (r < c) return BigFruitQuality.Legendary;

            return BigFruitQuality.Mythic;
        }

        public static int ToAddictionGain(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => 0,
            BigFruitQuality.Common => 1,
            BigFruitQuality.Excellent => 1,
            BigFruitQuality.Rare => 2,
            BigFruitQuality.Epic => 2,
            BigFruitQuality.Legendary => 3,
            BigFruitQuality.Mythic => 4,
            _ => 1,
        };

        /// <summary>嚼的爽 1~6，干瘪 -1</summary>
        public static int ToBuffLevel(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => -1,
            BigFruitQuality.Common => 1,
            BigFruitQuality.Excellent => 2,
            BigFruitQuality.Rare => 3,
            BigFruitQuality.Epic => 4,
            BigFruitQuality.Legendary => 5,
            BigFruitQuality.Mythic => 6,
            _ => -1,
        };

        public static BigFruitFlavor ToFlavor(this BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => BigFruitFlavor.Placebo,
            BigFruitQuality.Common => BigFruitFlavor.None,
            BigFruitQuality.Excellent => BigFruitFlavor.None,
            BigFruitQuality.Rare => BigFruitFlavor.Floaty,
            BigFruitQuality.Epic => BigFruitFlavor.LotusMouth,
            BigFruitQuality.Legendary => BigFruitFlavor.GoldenMouth,
            BigFruitQuality.Mythic => BigFruitFlavor.Ascension,
            _ => BigFruitFlavor.None,
        };
    }
}
