using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>棕榈树掉，沙地当树苗种</summary>
    public class BigFruit : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(copper: 50);
            Item.rare = ItemRarityID.White;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 10;
            Item.useAnimation = 15;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.createTile = TileID.Saplings;
            Item.autoReuse = true;
        }

        public override bool? UseItem(Player player) {
            int tx = Player.tileTargetX;
            int ty = Player.tileTargetY;

            if (!WorldGen.InWorld(tx, ty + 1)) return false;

            Tile below = Main.tile[tx, ty + 1];
            if (!below.HasTile) return false;

            ushort t = below.TileType;
            bool sandLike = t == TileID.Sand
                            || t == TileID.Pearlsand
                            || t == TileID.Ebonsand
                            || t == TileID.Crimsand;
            return sandLike;
        }

        public override void AddRecipes() {
        }
    }
}
