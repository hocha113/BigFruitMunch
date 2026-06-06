using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace BigFruitMunch.Content.PRT
{
    /// <summary>嚼的时候嘴部喷汁，加色混合</summary>
    public class ChewJuicePRT : BasePRT
    {
        public override string Texture => "BigFruitMunch/Assets/Masking/DiffusionCircle";

        public override void SetProperty() {
            PRTDrawMode = PRTDrawModeEnum.AdditiveBlend;
            Lifetime = 42;
            Opacity = 1f;
            if (Scale <= 0f) {
                Scale = 0.5f;
            }
        }

        public override void AI() {
            Velocity.Y += 0.16f;
            Velocity *= 0.97f;
            Rotation += 0.12f;

            Opacity = 1f - LifetimeCompletion;
            Scale *= 0.985f;
        }

        public override bool PreDraw(SpriteBatch spriteBatch) {
            Texture2D tex = TexValue;
            Vector2 origin = tex.Size() * 0.5f;
            spriteBatch.Draw(tex, Position - Main.screenPosition, null, Color * Opacity,
                Rotation, origin, Scale, SpriteEffects.None, 0f);
            return false;
        }
    }
}
