using BigFruitMunch.Content.Buffs;
using BigFruitMunch.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>
    /// 海南原味大果：前期工作台配方，食用后获得 5 分钟 0 级"嚼的爽！"效果。
    /// </summary>
    public class HailanBigFruit : ModItem
    {
        public override LocalizedText DisplayName => Mod.GetLocalization("Items.HainanOriginalBigFruit.DisplayName");

        public override LocalizedText Tooltip => Mod.GetLocalization("Items.HainanOriginalBigFruit.Tooltip");

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(copper: 30);
            Item.rare = ItemRarityID.Green;

            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item2;
            Item.consumable = true;
            Item.autoReuse = true;
        }

        public override bool? UseItem(Player player) {
            player.GetModPlayer<BetelNutPlayer>().OnChew(1);
            player.AddBuff(ModContent.BuffType<ChewSatisfaction0>(), 60 * 60 * 5);
            return true;
        }

        public override void AddRecipes() {
            CreateRecipe(2)
                .AddIngredient<BigFruit>(1)
                .AddIngredient<BigFruitleaf>(2)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
