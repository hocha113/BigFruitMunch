using BigFruitMunch.Content.Systems;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace BigFruitMunch.Content.RenderHandles
{
    /// <summary>戒断屏幕后处理，BetelWithdrawalScreen.fx 双 pass 写回 screenTarget1</summary>
    public class BetelWithdrawalRenderHandle : RenderHandle
    {
        public override float Weight => 100f;

        public override int ScreenSlot => 0;

        public override void EndCaptureDraw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            Effect effect = BetelWithdrawalSystem.BetelWithdrawalScreen;
            if (effect == null) return;
            if (screenSwap == null || screenTarget1 == null) return;

            float intensity = BetelWithdrawalSystem.SmoothedIntensity;
            float flash = BetelWithdrawalSystem.SmoothedFlash;

            if (intensity < 0.005f && flash < 0.005f) return;

            float t = BetelWithdrawalSystem.ShaderTime;

            float heartRate = 2f + intensity * 8f;
            float hbPulse = 0.5f + 0.5f * (float)Math.Sin(t * heartRate);

            Vector3 tint = Vector3.Lerp(
                new Vector3(1.00f, 0.85f, 0.70f),
                new Vector3(0.55f, 0.65f, 0.75f),
                Math.Clamp(intensity, 0f, 1f));

            effect.Parameters["uTime"]?.SetValue(t);
            effect.Parameters["uIntensity"]?.SetValue(intensity);
            effect.Parameters["uFlash"]?.SetValue(flash);
            effect.Parameters["uPulse"]?.SetValue(hbPulse);
            effect.Parameters["uTint"]?.SetValue(tint);
            effect.Parameters["uVignette"]?.SetValue(0.2f + 0.8f * intensity);
            effect.Parameters["uChromatic"]?.SetValue(intensity > 0.3f ? (intensity - 0.3f) * 1.4f : 0f);
            effect.Parameters["uWaveAmp"]?.SetValue(intensity > 0.55f ? (intensity - 0.55f) * 0.04f : 0f);
            effect.Parameters["uWaveFreq"]?.SetValue(8f + intensity * 6f);
            effect.Parameters["uNoise"]?.SetValue(intensity > 0.6f ? (intensity - 0.6f) * 0.35f : 0f);
            effect.Parameters["uDesat"]?.SetValue(Math.Max(0f, intensity - 0.5f) * 1.2f);
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
