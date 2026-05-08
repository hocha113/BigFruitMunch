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
    /// 去皮大果基类 —— 由切割机切开后产出，按品质带不同颜色滤镜。
    /// 7 种品质各派生一个非抽象类供 tModLoader 自动注册。
    /// </summary>
    public abstract class DecorticateBigFruitBase : ModItem
    {
        public abstract BigFruitQuality Quality { get; }

        // 复用同一张贴图，颜色通过绘制时的 tint 区分品质
        public override string Texture => "BigFruitMunch/Content/DecorticateBigFruit";

        public override LocalizedText DisplayName => Mod.GetLocalization(
            $"Items.DecorticateBigFruit.{Quality}.DisplayName");

        public override LocalizedText Tooltip => Mod.GetLocalization(
            $"Items.DecorticateBigFruit.{Quality}.Tooltip");

        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 25;
            ItemID.Sets.IsAMaterial[Type] = true;
        }

        public override void SetDefaults() {
            Item.width = 22;
            Item.height = 22;
            Item.maxStack = Item.CommonMaxStack;
            Item.value = Item.sellPrice(copper: (int)(20 * (1 + (int)Quality * 0.6f)));
            Item.rare = Quality.ToRarity();
            Item.useStyle = ItemUseStyleID.None;
            Item.material = true;
        }

        public override void AddRecipes() {
            // 烹饪锅烧制 -> 对应品质的 可嚼大果
            int chewableType = ChewableBigFruitBase.GetTypeForQuality(Quality);
            Recipe.Create(chewableType)
                .AddIngredient(Type, 1)
                .AddTile(TileID.CookingPots)
                .Register();
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale) {
            Texture2D tex = TextureAssets.Item[Type].Value;
            // 优先走着色器；着色器加载失败时降级为 Color 乘法
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

        // ---- 工具方法：根据品质枚举找到对应的 ItemType ----
        public static int GetTypeForQuality(BigFruitQuality q) => q switch {
            BigFruitQuality.Withered => ModContent.ItemType<DecorticateBigFruit_Withered>(),
            BigFruitQuality.Common => ModContent.ItemType<DecorticateBigFruit_Common>(),
            BigFruitQuality.Excellent => ModContent.ItemType<DecorticateBigFruit_Excellent>(),
            BigFruitQuality.Rare => ModContent.ItemType<DecorticateBigFruit_Rare>(),
            BigFruitQuality.Epic => ModContent.ItemType<DecorticateBigFruit_Epic>(),
            BigFruitQuality.Legendary => ModContent.ItemType<DecorticateBigFruit_Legendary>(),
            BigFruitQuality.Mythic => ModContent.ItemType<DecorticateBigFruit_Mythic>(),
            _ => 0,
        };
    }

    public class DecorticateBigFruit_Withered : DecorticateBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Withered; }
    public class DecorticateBigFruit_Common : DecorticateBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Common; }
    public class DecorticateBigFruit_Excellent : DecorticateBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Excellent; }
    public class DecorticateBigFruit_Rare : DecorticateBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Rare; }
    public class DecorticateBigFruit_Epic : DecorticateBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Epic; }
    public class DecorticateBigFruit_Legendary : DecorticateBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Legendary; }
    public class DecorticateBigFruit_Mythic : DecorticateBigFruitBase { public override BigFruitQuality Quality => BigFruitQuality.Mythic; }
}
