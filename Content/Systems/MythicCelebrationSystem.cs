using BigFruitMunch.Content.PRT;
using InnoVault.PRT;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Systems
{
    /// <summary>神话出货全屏金光，切割机十连和嚼神话共用</summary>
    public class MythicCelebrationSystem : ModSystem
    {
        private static float timer;

        private const float DecayPerFrame = 1f / 75f;

        private static Asset<Texture2D> glowTex;

        public override void Load() {
            if (!Main.dedServ) {
                glowTex = ModContent.Request<Texture2D>("BigFruitMunch/Assets/Masking/DiffusionCircle");
            }
        }

        public override void Unload() {
            glowTex = null;
        }

        public static void Trigger(Vector2 worldPos) {
            timer = 1f;

            if (Main.dedServ) {
                return;
            }

            const int n = 28;
            for (int i = 0; i < n; i++) {
                float ang = MathHelper.TwoPi * i / n + Main.rand.NextFloat(-0.12f, 0.12f);
                float speed = Main.rand.NextFloat(3f, 8f);
                Vector2 vel = ang.ToRotationVector2() * speed;
                Color c = Color.Lerp(new Color(255, 220, 120), new Color(255, 120, 60), Main.rand.NextFloat());
                PRTLoader.NewParticle<ChewJuicePRT>(worldPos, vel, c, Main.rand.NextFloat(0.6f, 1.3f));
            }
        }

        public override void PostUpdateEverything() {
            if (timer > 0f) {
                timer -= DecayPerFrame;
                if (timer < 0f) {
                    timer = 0f;
                }
            }
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch) {
            if (timer <= 0f || Main.dedServ || glowTex == null) {
                return;
            }

            Texture2D tex = glowTex.Value;
            Vector2 center = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
            Vector2 origin = tex.Size() * 0.5f;
            float scaleX = Main.screenWidth * 1.5f / tex.Width;
            float scaleY = Main.screenHeight * 1.5f / tex.Height;

            //前段爆亮、后段渐隐
            float alpha = (float)Math.Pow(timer, 0.7);
            Color gold = new Color(255, 205, 110) * (alpha * 0.55f);

            spriteBatch.Draw(tex, center, null, gold, 0f, origin,
                new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);
        }
    }
}
