using BigFruitMunch.Content.Systems;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace BigFruitMunch.Content.RenderHandles
{
    /// <summary>上头屏幕后处理，重戒断时会压强度</summary>
    public class HighRenderHandle : RenderHandle
    {
        public override float Weight => 99f;

        public override int ScreenSlot => 0;

        public override void EndCaptureDraw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            Effect effect = BigFruitHighSystem.BigFruitHighScreen;
            if (effect == null) return;
            if (screenSwap == null || screenTarget1 == null) return;

            float intensity = BigFruitHighSystem.SmoothedIntensity;
            float flash = BigFruitHighSystem.SmoothedFlash;

            float suppress = 1f - Math.Clamp(BetelWithdrawalSystem.SmoothedIntensity, 0f, 1f) * 0.85f;
            intensity *= suppress;
            flash *= suppress;

            if (intensity < 0.005f && flash < 0.005f) return;

            float t = BigFruitHighSystem.ShaderTime;
            float pulse = 0.5f + 0.5f * (float)Math.Sin(t * (6f + intensity * 6f));

            effect.Parameters["uTime"]?.SetValue(t);
            effect.Parameters["uIntensity"]?.SetValue(intensity);
            effect.Parameters["uPulse"]?.SetValue(pulse);
            effect.Parameters["uFlash"]?.SetValue(flash);
            effect.Parameters["uTexSize"]?.SetValue(new Vector2(Main.screenWidth, Main.screenHeight));

            graphicsDevice.SetRenderTarget(screenSwap);
            graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect);
            spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            spriteBatch.End();

            graphicsDevice.SetRenderTarget(screenTarget1);
            graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(screenSwap, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}
