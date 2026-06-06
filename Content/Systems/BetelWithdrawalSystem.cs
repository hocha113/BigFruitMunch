using BigFruitMunch.Content.Players;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Systems
{
    /// <summary>戒断 shader 强度平滑，绑全局静态避免重生瞬变</summary>
    public class BetelWithdrawalSystem : ModSystem
    {
        [VaultLoaden("BigFruitMunch/Assets/Effects/")]
        public static Effect BetelWithdrawalScreen { get; set; }

        public static float SmoothedIntensity;

        public static float SmoothedFlash;

        public static float ShaderTime;

        private const float IntensityLerpUp = 0.012f;
        private const float IntensityLerpDown = 0.020f;
        private const float FlashLerpUp = 0.08f;
        private const float FlashLerpDown = 0.025f;

        public override void PostUpdateEverything() {
            if (Main.dedServ) return;

            var local = Main.LocalPlayer;
            float targetIntensity = 0f;
            float targetFlash = 0f;
            if (local != null && local.active) {
                var betel = local.GetModPlayer<BetelNutPlayer>();
                if (betel != null) {
                    targetIntensity = betel.TargetWithdrawalIntensity;
                    targetFlash = betel.RecentChewFlashTicks / (float)BetelNutPlayer.ChewFlashMaxTicks;
                }
            }

            SmoothedIntensity = MoveTowards(SmoothedIntensity, targetIntensity,
                targetIntensity > SmoothedIntensity ? IntensityLerpUp : IntensityLerpDown);
            SmoothedFlash = MoveTowards(SmoothedFlash, targetFlash,
                targetFlash > SmoothedFlash ? FlashLerpUp : FlashLerpDown);

            ShaderTime += 1f / 60f;
            if (ShaderTime > 1e6f) {
                ShaderTime = 0f;
            }
        }

        private static float MoveTowards(float current, float target, float maxDelta) {
            if (current < target) return Math.Min(target, current + maxDelta);
            if (current > target) return Math.Max(target, current - maxDelta);
            return current;
        }
    }
}
