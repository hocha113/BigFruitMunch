using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>品质物品图标滤镜，加载失败就 return false 走普通绘制</summary>
    public sealed class BigFruitQualityShader : ModSystem
    {
        [VaultLoaden("BigFruitMunch/Assets/Effects/")]
        public static Effect BigFruitQualityFilter { get; set; }
        /// <summary>去饱和, 描边, 流光, 脉冲rad/s, 色散, 强度</summary>
        private static (float Desat, float Outline, float Shimmer, float Pulse, float Chromatic, float Intensity)
            GetParams(BigFruitQuality q) => q switch {
                BigFruitQuality.Withered => (0.7f, 0.00f, 0.00f, 0.0f, 0.0f, 1.0f),
                BigFruitQuality.Common => (0.0f, 0.00f, 0.00f, 0.0f, 0.0f, 0.4f),
                BigFruitQuality.Excellent => (0.0f, 0.55f, 0.00f, 3.0f, 0.0f, 1.0f),
                BigFruitQuality.Rare => (0.0f, 0.80f, 0.00f, 4.0f, 0.0f, 1.0f),
                BigFruitQuality.Epic => (0.0f, 1.00f, 0.45f, 5.0f, 0.0f, 1.0f),
                BigFruitQuality.Legendary => (0.0f, 1.20f, 0.75f, 6.0f, 0.2f, 1.0f),
                BigFruitQuality.Mythic => (0.0f, 1.40f, 1.00f, 7.0f, 1.0f, 1.0f),
                _ => (0f, 0f, 0f, 0f, 0f, 0f),
            };

        /// <summary>只设参数，不切 SpriteBatch</summary>
        public static bool ApplyParams(BigFruitQuality quality, Vector2 texSize) {
            Effect e = BigFruitQualityFilter;
            if (e == null) return false;

            var p = GetParams(quality);
            Vector3 tint = quality.ToTint().ToVector3();
            float time = (float)Main.GameUpdateCount / 60f;

            e.Parameters["uTime"]?.SetValue(time);
            e.Parameters["uTint"]?.SetValue(tint);
            e.Parameters["uTexSize"]?.SetValue(texSize);
            e.Parameters["uIntensity"]?.SetValue(p.Intensity);
            e.Parameters["uDesaturate"]?.SetValue(p.Desat);
            e.Parameters["uOutlineStrength"]?.SetValue(p.Outline);
            e.Parameters["uShimmerStrength"]?.SetValue(p.Shimmer);
            e.Parameters["uPulseSpeed"]?.SetValue(p.Pulse);
            e.Parameters["uChromatic"]?.SetValue(p.Chromatic);
            return true;
        }

        /// <summary>背包格绘制，会 End/Begin 切 immediate</summary>
        public static bool DrawIconWithFilter(SpriteBatch sb, Texture2D tex, Vector2 position, Rectangle frame,
            Color color, Vector2 origin, float scale, BigFruitQuality quality) {
            if (!ApplyParams(quality, new Vector2(tex.Width, tex.Height))) return false;

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, BigFruitQualityFilter, Main.UIScaleMatrix);

            sb.Draw(tex, position, frame, color, 0f, origin, scale, SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            return true;
        }

        /// <summary>世界掉落物绘制，GameViewMatrix</summary>
        public static bool DrawWorldWithFilter(SpriteBatch sb, Texture2D tex, Vector2 worldPos, Rectangle? frame,
            Color color, float rotation, Vector2 origin, float scale, BigFruitQuality quality) {
            if (!ApplyParams(quality, new Vector2(tex.Width, tex.Height))) return false;

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, BigFruitQualityFilter, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(tex, worldPos, frame, color, rotation, origin, scale, SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }
    }
}
