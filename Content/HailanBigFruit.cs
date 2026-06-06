using BigFruitMunch.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>肉前入门款，普通品质上头 1 分钟</summary>
    public class HailanBigFruit : ModItem
    {
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
            player.GetModPlayer<BetelNutPlayer>().Chew(BigFruitQuality.Common, 60 * 60);
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
