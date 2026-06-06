namespace BigFruitMunch.Content
{
    /// <summary>打包消耗数，日常/礼盒共用，和成天下以后也照这个</summary>
    public static class BigFruitPackRecipeCounts
    {
        public static int ForQuality(BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => 10,
            BigFruitQuality.Common => 20,
            BigFruitQuality.Excellent => 20,
            BigFruitQuality.Rare => 15,
            BigFruitQuality.Epic => 10,
            BigFruitQuality.Legendary => 5,
            BigFruitQuality.Mythic => 3,
            _ => 1,
        };
    }
}
