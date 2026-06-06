using BigFruitMunch.Content.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>喂镇民大果，上头打折+胡话</summary>
    public class BetelTreat : ModItem
    {
        public override string Texture => "BigFruitMunch/Content/ChewableBigFruit";

        private const float FeedRange = 260f;
        private const int FeedHighTicks = 60 * 60; //镇民上头 1 分钟

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(copper: 60);
            Item.rare = ItemRarityID.Green;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 24;
            Item.useTime = 24;
            Item.UseSound = SoundID.Item2;
            Item.consumable = true;
            Item.autoReuse = false;
        }

        public override bool CanUseItem(Player player) {
            return FindNearbyTownNPC(player) != null;
        }

        public override bool? UseItem(Player player) {
            if (player.whoAmI != Main.myPlayer) {
                return true;
            }
            NPC target = FindNearbyTownNPC(player);
            if (target == null) {
                return false;
            }
            target.GetGlobalNPC<HighTownNPC>().FeedHigh(target, FeedHighTicks);
            CombatText.NewText(target.Hitbox, new Color(255, 180, 120),
                Language.GetTextValue("Mods.BigFruitMunch.Common.FeedSuccess", target.GivenOrTypeName));
            return true;
        }

        private static NPC FindNearbyTownNPC(Player player) {
            NPC best = null;
            float bestSq = FeedRange * FeedRange;
            for (int i = 0; i < Main.maxNPCs; i++) {
                NPC n = Main.npc[i];
                if (!n.active || !n.townNPC || !n.friendly) {
                    continue;
                }
                float dsq = Vector2.DistanceSquared(player.Center, n.Center);
                if (dsq < bestSq) {
                    bestSq = dsq;
                    best = n;
                }
            }
            return best;
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<BigFruit>(2)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}
