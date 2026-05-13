using BigFruitMunch.Content.Systems;
using InnoVault.RenderHandles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace BigFruitMunch.Content.RenderHandles
{
    /// <summary>
    /// 戒断 / 爽感屏幕级后处理：
    /// 在 <see cref="RenderHandle.EndCaptureDraw"/> 阶段把当前场景 RT (<see cref="RenderHandle.screenTarget1"/>)
    /// 通过 <c>BetelWithdrawalScreen.fx</c> 进行扭曲 / 染色 / 噪点 / 暖光 等效果，
    /// 再写回 <c>screenTarget1</c>，使 FilterManager.EndCapture 的后续合成把它呈现到玩家眼前。
    /// </summary>
    public class BetelWithdrawalRenderHandle : RenderHandle
    {
        // 大权重 → 排在大部分 RenderHandle 之后跑，确保我们能拿到尽量"已经被其它后处理叠加过"的画面
        public override float Weight => 100f;

        // 不需要持久 RT，依赖 RenderHandleLoader 给的 screenSwap 即可
        public override int ScreenSlot => 0;

        public override void EndCaptureDraw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, RenderTarget2D screenSwap) {
            Effect effect = BetelWithdrawalSystem.BetelWithdrawalScreen;
            if (effect == null) return;
            if (screenSwap == null || screenTarget1 == null) return;

            float intensity = BetelWithdrawalSystem.SmoothedIntensity;
            float flash = BetelWithdrawalSystem.SmoothedFlash;

            // 性能短路：没有任何状态时不做任何 RT 操作
            if (intensity < 0.005f && flash < 0.005f) return;

            float t = BetelWithdrawalSystem.ShaderTime;

            // 心跳脉冲：随着戒断等级越深，心跳越快
            float heartRate = 2f + intensity * 8f;
            float hbPulse = 0.5f + 0.5f * (float)Math.Sin(t * heartRate);

            // 戒断染色：从"暖红/橙黄"渐变到"青灰"
            Vector3 tint = Vector3.Lerp(
                new Vector3(1.00f, 0.85f, 0.70f),  // 弱戒断：暖
                new Vector3(0.55f, 0.65f, 0.75f),  // 重戒断：青灰
                Math.Clamp(intensity, 0f, 1f));

            // —— shader 参数 —— //
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

            // —— Pass 1：screenTarget1 -> screenSwap，应用 shader —— //
            graphicsDevice.SetRenderTarget(screenSwap);
            graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect);
            spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
            spriteBatch.End();

            // —— Pass 2：screenSwap -> screenTarget1，把结果"写回原位"，无 shader —— //
            graphicsDevice.SetRenderTarget(screenTarget1);
            graphicsDevice.Clear(Color.Transparent);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone);
            spriteBatch.Draw(screenSwap, Vector2.Zero, Color.White);
            spriteBatch.End();
        }
    }
}
