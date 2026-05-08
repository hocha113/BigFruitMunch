using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace BigFruitMunch.Content
{
    /// <summary>
    /// 加载并应用 <c>Assets/Effects/BigFruitQualityFilter.fx</c> 的着色器，
    /// 为 <see cref="BigFruitQuality"/> 提供差异化的物品图标外观（背包/世界皆可）。
    /// 着色器加载失败时所有调用都会安全降级（直接返回 false，调用方走普通绘制）。
    /// </summary>
    public sealed class BigFruitQualityShader : ModSystem
    {
        private static Asset<Effect> _filter;

        public static Effect Effect => _filter?.Value;

        public override void Load()
        {
            if (Main.dedServ) return;
            _filter = ModContent.Request<Effect>("BigFruitMunch/Assets/Effects/BigFruitQualityFilter",
                AssetRequestMode.ImmediateLoad);
        }

        public override void Unload()
        {
            _filter = null;
        }

        /// <summary>
        /// 各品质对应的滤镜参数。0 表示该品质不需要这个特性。
        /// 字段顺序：(去饱和, 描边强度, 流光强度, 脉冲速度 rad/s, 色散强度, 整体强度)
        /// </summary>
        private static (float Desat, float Outline, float Shimmer, float Pulse, float Chromatic, float Intensity)
            GetParams(BigFruitQuality q) => q switch
        {
            // 干瘪：去饱和 + 整体偏暗，无任何动态特效
            BigFruitQuality.Withered  => (0.7f, 0.00f, 0.00f, 0.0f, 0.0f, 1.0f),
            // 普通：基本无效果，仅保留极轻微的tint
            BigFruitQuality.Common    => (0.0f, 0.00f, 0.00f, 0.0f, 0.0f, 0.4f),
            // 优秀：轻微描边脉冲
            BigFruitQuality.Excellent => (0.0f, 0.55f, 0.00f, 3.0f, 0.0f, 1.0f),
            // 稀有：更强描边
            BigFruitQuality.Rare      => (0.0f, 0.80f, 0.00f, 4.0f, 0.0f, 1.0f),
            // 史诗：开始出现流光带
            BigFruitQuality.Epic      => (0.0f, 1.00f, 0.45f, 5.0f, 0.0f, 1.0f),
            // 传奇：金光强化
            BigFruitQuality.Legendary => (0.0f, 1.20f, 0.75f, 6.0f, 0.2f, 1.0f),
            // 神话：满配 + 色散 + 闪点
            BigFruitQuality.Mythic    => (0.0f, 1.40f, 1.00f, 7.0f, 1.0f, 1.0f),
            _ => (0f, 0f, 0f, 0f, 0f, 0f),
        };

        /// <summary>设置滤镜参数为指定品质（不会切换 SpriteBatch，调用者自行 End/Begin）。</summary>
        public static bool ApplyParams(BigFruitQuality quality, Vector2 texSize)
        {
            Effect e = Effect;
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

        /// <summary>
        /// 在 Inventory（UI 矩阵）中绘制带滤镜的物品图标。
        /// 调用前 SpriteBatch 必须处于 active 状态。绘制完后 SpriteBatch 会回到 Deferred/AlphaBlend 默认状态。
        /// </summary>
        public static bool DrawIconWithFilter(SpriteBatch sb, Texture2D tex, Vector2 position, Rectangle frame,
            Color color, Vector2 origin, float scale, BigFruitQuality quality)
        {
            if (!ApplyParams(quality, new Vector2(tex.Width, tex.Height))) return false;

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, Effect, Main.UIScaleMatrix);

            sb.Draw(tex, position, frame, color, 0f, origin, scale, SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            return true;
        }

        /// <summary>
        /// 在世界中绘制带滤镜的物品（用于掉落物图标）。使用 GameViewMatrix 变换。
        /// 调用前 SpriteBatch 必须处于 active 状态，结束后保持原始 Begin 状态。
        /// </summary>
        public static bool DrawWorldWithFilter(SpriteBatch sb, Texture2D tex, Vector2 worldPos, Rectangle? frame,
            Color color, float rotation, Vector2 origin, float scale, BigFruitQuality quality)
        {
            if (!ApplyParams(quality, new Vector2(tex.Width, tex.Height))) return false;

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, Effect, Main.GameViewMatrix.TransformationMatrix);

            sb.Draw(tex, worldPos, frame, color, rotation, origin, scale, SpriteEffects.None, 0f);

            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }
    }
}
