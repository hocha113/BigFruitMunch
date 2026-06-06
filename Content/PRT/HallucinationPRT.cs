using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;

namespace BigFruitMunch.Content.PRT
{
    /// <summary>戒断幻觉大果，本地 PRT 不同步</summary>
    public class HallucinationFruitPRT : BasePRT
    {
        public override string Texture => "BigFruitMunch/Assets/Masking/Photosphere";

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AlphaBlend;
            Lifetime = 220;
            Opacity = 0f;
            ShouldKillWhenOffScreen = false;
            if (Scale <= 0f) {
                Scale = 0.8f;
            }
        }

        public override void AI() {
            Velocity.X += (float)Math.Sin(Time * 0.05f) * 0.02f;
            Velocity *= 0.99f;
            Rotation += 0.008f;
            Opacity = (float)Math.Sin(LifetimeCompletion * Math.PI) * 0.5f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = TexValue;
            Vector2 origin = tex.Size() * 0.5f;
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * Opacity,
                Rotation, origin, Scale, SpriteEffects.None, 0f);
            return false;
        }
    }

    /// <summary>戒断漂浮文字，Text 由生成方填</summary>
    public class HallucinationTextPRT : BasePRT
    {
        public string Text = "";

        public override string Texture => "BigFruitMunch/Assets/Masking/DiffusionCircle";

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AlphaBlend;
            Lifetime = 170;
            Opacity = 0f;
            ShouldKillWhenOffScreen = false;
            if (Scale <= 0f) {
                Scale = 1f;
            }
        }

        public override void AI() {
            Velocity *= 0.98f;
            Opacity = (float)Math.Sin(LifetimeCompletion * Math.PI);
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            if (string.IsNullOrEmpty(Text)) {
                return false;
            }
            var font = FontAssets.MouseText.Value;
            Vector2 size = font.MeasureString(Text);
            Vector2 pos = Position - Main.screenPosition;
            spriteBatch.DrawString(font, Text, pos, Color * Opacity, Rotation,
                size * 0.5f, new Vector2(Scale), SpriteEffects.None, 0f);
            return false;
        }
    }
}
