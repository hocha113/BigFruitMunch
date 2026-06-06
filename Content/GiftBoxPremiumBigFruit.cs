using BigFruitMunch.Content.Players;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>Mk2 礼盒，肉后</summary>
    public abstract class GiftBoxPremiumBigFruitBase : ModItem
    {
        public abstract BigFruitQuality Quality { get; }

        public override string Texture => "BigFruitMunch/Content/UnifyTheRealmMaple";

        public override LocalizedText DisplayName => Mod.GetLocalization(
            $"Items.GiftBoxPremiumBigFruit.{Quality}.DisplayName");

        public override LocalizedText Tooltip => Mod.GetLocalization(
            $"Items.GiftBoxPremiumBigFruit.{Quality}.Tooltip");

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 10;
        }

        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(silver: (int)(20 * (1 + (int)Quality * 2f)));
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
            player.GetModPlayer<BetelNutPlayer>().Chew(Quality, 60 * 60 * 10);
            return true;
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale) {
            Texture2D tex = TextureAssets.Item[Type].Value;
            if (BigFruitQualityShader.DrawIconWithFilter(spriteBatch, tex, position, frame, drawColor, origin, scale, Quality))
                return false;

            Color tint = MultiplyColor(drawColor, Quality.ToTint());
            spriteBatch.Draw(tex, position, frame, tint, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor,
            ref float rotation, ref float scale, int whoAmI) {
            Texture2D tex = TextureAssets.Item[Type].Value;
            Vector2 worldPos = Item.position - Main.screenPosition
                + new Vector2(Item.width / 2f, Item.height - tex.Height * 0.5f + 2f);

            if (BigFruitQualityShader.DrawWorldWithFilter(spriteBatch, tex, worldPos, null, lightColor,
                    rotation, tex.Size() * 0.5f, scale, Quality))
                return false;

            Color tint = MultiplyColor(lightColor, Quality.ToTint());
            spriteBatch.Draw(tex, worldPos, null, tint, rotation, tex.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            return false;
        }

        private static Color MultiplyColor(Color a, Color b) {
            return new Color(
                (byte)(a.R * b.R / 255),
                (byte)(a.G * b.G / 255),
                (byte)(a.B * b.B / 255),
                a.A);
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient(ChewableBigFruitBase.GetTypeForQuality(Quality),
                    BigFruitPackRecipeCounts.ForQuality(Quality))
                .AddTile<BigFruitCutterbarMk2Tile>()
                .Register();
        }

        public static int GetTypeForQuality(BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => ModContent.ItemType<GiftBoxPremiumBigFruit_Withered>(),
            BigFruitQuality.Common => ModContent.ItemType<GiftBoxPremiumBigFruit_Common>(),
            BigFruitQuality.Excellent => ModContent.ItemType<GiftBoxPremiumBigFruit_Excellent>(),
            BigFruitQuality.Rare => ModContent.ItemType<GiftBoxPremiumBigFruit_Rare>(),
            BigFruitQuality.Epic => ModContent.ItemType<GiftBoxPremiumBigFruit_Epic>(),
            BigFruitQuality.Legendary => ModContent.ItemType<GiftBoxPremiumBigFruit_Legendary>(),
            BigFruitQuality.Mythic => ModContent.ItemType<GiftBoxPremiumBigFruit_Mythic>(),
            _ => 0,
        };
    }

    public class GiftBoxPremiumBigFruit_Withered : GiftBoxPremiumBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Withered; }
    public class GiftBoxPremiumBigFruit_Common : GiftBoxPremiumBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Common; }
    public class GiftBoxPremiumBigFruit_Excellent : GiftBoxPremiumBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Excellent; }
    public class GiftBoxPremiumBigFruit_Rare : GiftBoxPremiumBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Rare; }
    public class GiftBoxPremiumBigFruit_Epic : GiftBoxPremiumBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Epic; }
    public class GiftBoxPremiumBigFruit_Legendary : GiftBoxPremiumBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Legendary; }
    public class GiftBoxPremiumBigFruit_Mythic : GiftBoxPremiumBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Mythic; }
}
