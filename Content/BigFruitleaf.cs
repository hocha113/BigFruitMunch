using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>
    /// 大果叶：从棕榈树100%掉落（每次1-4片），用于合成大果切割机等。
    /// </summary>
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
