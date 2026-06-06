using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>摇树必掉，做切割机</summary>
    public class BigFruitleaf : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.IsAMaterial[Type] = true;
        }

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 24;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(copper: 20);
            Item.rare = ItemRarityID.White;
            Item.useStyle = ItemUseStyleID.None;
            Item.material = true;
        }
    }
}
