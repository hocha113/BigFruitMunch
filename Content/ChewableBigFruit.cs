using BigFruitMunch.Content.Buffs;
using BigFruitMunch.Content.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>
    /// 可嚼大果基类 —— 由 去皮大果 在烹饪锅烧制而来，可食用获得"嚼的爽！"对应等级 Buff 与槟榔成瘾计数。
    /// </summary>
    public abstract class ChewableBigFruitBase : ModItem
    {
        public abstract BigFruitQuality Quality { get; }

        public override string Texture => "BigFruitMunch/Content/ChewableBigFruit";

        public override LocalizedText DisplayName => Mod.GetLocalization(
            $"Items.ChewableBigFruit.{Quality}.DisplayName");

        public override LocalizedText Tooltip => Mod.GetLocalization(
            $"Items.ChewableBigFruit.{Quality}.Tooltip");

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
        }

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(copper: (int)(50 * (1 + (int)Quality * 0.8f)));
            Item.rare = Quality.ToRarity();

            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item2;
            Item.consumable = true;
            Item.autoReuse = true;
        }

        public override bool? UseItem(Player player) {
            // 按品质给不同上瘾增量：神话级 >> 干瘪
            var betel = player.GetModPlayer<BetelNutPlayer>();
            betel.OnChew(GetChewAmountForQuality(Quality));

            // 干瘪：什么效果都没有，只重置戒断
            int level = Quality.ToBuffLevel();
            if (level >= 0) {
                int buffType = ChewSatisfactionBuffBase.GetTypeForLevel(level);
                player.AddBuff(buffType, 60 * 60 * 3); // 3 分钟
            }
            return true;
        }

        /// <summary>不同品质的"上瘾度"增量。</summary>
        private static int GetChewAmountForQuality(BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => 0, // 干瘪：纯粹的口腔运动，不上瘾
            BigFruitQuality.Common => 1,
            BigFruitQuality.Excellent => 1,
            BigFruitQuality.Rare => 1,
            BigFruitQuality.Epic => 2,
            BigFruitQuality.Legendary => 2,
            BigFruitQuality.Mythic => 3,
            _ => 1,
        };

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

        public static int GetTypeForQuality(BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => ModContent.ItemType<ChewableBigFruit_Withered>(),
            BigFruitQuality.Common => ModContent.ItemType<ChewableBigFruit_Common>(),
            BigFruitQuality.Excellent => ModContent.ItemType<ChewableBigFruit_Excellent>(),
            BigFruitQuality.Rare => ModContent.ItemType<ChewableBigFruit_Rare>(),
            BigFruitQuality.Epic => ModContent.ItemType<ChewableBigFruit_Epic>(),
            BigFruitQuality.Legendary => ModContent.ItemType<ChewableBigFruit_Legendary>(),
            BigFruitQuality.Mythic => ModContent.ItemType<ChewableBigFruit_Mythic>(),
            _ => 0,
        };
    }

    public class ChewableBigFruit_Withered : ChewableBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Withered; }
    public class ChewableBigFruit_Common : ChewableBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Common; }
    public class ChewableBigFruit_Excellent : ChewableBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Excellent; }
    public class ChewableBigFruit_Rare : ChewableBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Rare; }
    public class ChewableBigFruit_Epic : ChewableBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Epic; }
    public class ChewableBigFruit_Legendary : ChewableBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Legendary; }
    public class ChewableBigFruit_Mythic : ChewableBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Mythic; }
}
