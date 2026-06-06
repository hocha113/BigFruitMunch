using BigFruitMunch.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>Mk1 打包，肉前</summary>
    public abstract class DailyPackBigFruitBase : ModItem
    {
        public abstract BigFruitQuality Quality { get; }

        public override string Texture => Quality switch {
            BigFruitQuality.Common => "BigFruitMunch/Content/UnifyTheRealmWhite",
            BigFruitQuality.Excellent => "BigFruitMunch/Content/UnifyTheRealmGreen",
            BigFruitQuality.Rare => "BigFruitMunch/Content/UnifyTheRealmBlue",
            BigFruitQuality.Epic => "BigFruitMunch/Content/UnifyTheRealmViolet",
            BigFruitQuality.Legendary => "BigFruitMunch/Content/UnifyTheRealmYellow",
            BigFruitQuality.Mythic => "BigFruitMunch/Content/UnifyTheRealmRed",
            _ => "BigFruitMunch/Content/UnifyTheRealmGrey",
        };

        public override LocalizedText DisplayName => Mod.GetLocalization(
            $"Items.DailyPackBigFruit.{Quality}.DisplayName");

        public override LocalizedText Tooltip => Mod.GetLocalization(
            $"Items.DailyPackBigFruit.{Quality}.Tooltip");

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 10;
        }

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 28;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(silver: (int)(8 * (1 + (int)Quality * 1.5f)));
            Item.rare = Quality.ToRarity();
            Item.noUseGraphic = true;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item2;
            Item.consumable = true;
            Item.autoReuse = true;
        }

        public override bool? UseItem(Player player) {
            player.GetModPlayer<BetelNutPlayer>().Chew(Quality, 60 * 60 * 5);
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ChewableBigFruitBase.GetTypeForQuality(Quality),
                    BigFruitPackRecipeCounts.ForQuality(Quality))
                .AddTile<BigFruitCutterbarTile>()
                .Register();
        }

        public static int GetTypeForQuality(BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => ModContent.ItemType<DailyPackBigFruit_Withered>(),
            BigFruitQuality.Common => ModContent.ItemType<DailyPackBigFruit_Common>(),
            BigFruitQuality.Excellent => ModContent.ItemType<DailyPackBigFruit_Excellent>(),
            BigFruitQuality.Rare => ModContent.ItemType<DailyPackBigFruit_Rare>(),
            BigFruitQuality.Epic => ModContent.ItemType<DailyPackBigFruit_Epic>(),
            BigFruitQuality.Legendary => ModContent.ItemType<DailyPackBigFruit_Legendary>(),
            BigFruitQuality.Mythic => ModContent.ItemType<DailyPackBigFruit_Mythic>(),
            _ => 0,
        };
    }

    public class DailyPackBigFruit_Withered : DailyPackBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Withered; }
    public class DailyPackBigFruit_Common : DailyPackBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Common; }
    public class DailyPackBigFruit_Excellent : DailyPackBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Excellent; }
    public class DailyPackBigFruit_Rare : DailyPackBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Rare; }
    public class DailyPackBigFruit_Epic : DailyPackBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Epic; }
    public class DailyPackBigFruit_Legendary : DailyPackBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Legendary; }
    public class DailyPackBigFruit_Mythic : DailyPackBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Mythic; }
}
