using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>
    /// 大果：从棕榈树掉落，可作为种子种植棕榈树（替代橡果用于沙地）。
    /// </summary>
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

            // 让其行为类似橡果，可以放置树苗物块
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 10;
            Item.useAnimation = 15;
            Item.autoReuse = true;
            Item.consumable = true;
            Item.createTile = TileID.Saplings;
        }

        public override bool? UseItem(Player player) {
            // 仅允许在沙地系（普通沙、珍珠沙、猩红沙、暗影沙）放置；
            // 沙地系树苗会自动长成棕榈树。
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
            // 不提供合成配方，只能从棕榈树掉落获取。
        }
    }
}
