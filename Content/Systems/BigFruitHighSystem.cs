using BigFruitMunch.Content.Players;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Systems
{
    /// <summary>上头 shader 强度平滑，跟 BetelWithdrawalSystem 一对</summary>
    public class BigFruitHighSystem : ModSystem
    {
        [VaultLoaden("BigFruitMunch/Assets/Effects/")]
        public static Effect BigFruitHighScreen { get; set; }

        public static float SmoothedIntensity;

        public static float SmoothedFlash;

        public static float ShaderTime;

        private const float IntensityLerpUp = 0.05f;
        private const float IntensityLerpDown = 0.02f;
        private const float FlashLerpUp = 0.12f;
        private const float FlashLerpDown = 0.04f;

        public override void PostUpdateEverything() {
            if (Main.dedServ) return;

            var local = Main.LocalPlayer;
            float targetIntensity = 0f;
            float targetFlash = 0f;
            if (local != null && local.active) {
                var betel = local.GetModPlayer<BetelNutPlayer>();
                if (betel != null) {
                    targetIntensity = betel.TargetHighIntensity;
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
