using BigFruitMunch.Content.Players;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Systems
{
    /// <summary>
    /// 维护"戒断屏幕扭曲"的平滑强度参数，并加载对应着色器资源。
    /// 实际后处理由 <c>BetelWithdrawalRenderHandle</c> 完成。
    /// 这里只负责把 <see cref="BetelNutPlayer.TargetWithdrawalIntensity"/>
    /// 与 <see cref="BetelNutPlayer.RecentChewFlashTicks"/> 平滑到全局静态字段，
    /// 避免直接绑在 ModPlayer 上时画面在重生 / 切换角色时出现突变。
    /// </summary>
    public class BetelWithdrawalSystem : ModSystem
    {
        [VaultLoaden("BigFruitMunch/Assets/Effects/")]
        public static Effect BetelWithdrawalScreen { get; set; }

        /// <summary>平滑后的戒断强度，0..1，驱动 shader 各项整体效果。</summary>
        public static float SmoothedIntensity;

        /// <summary>平滑后的"刚嚼食"正向反馈，0..1。</summary>
        public static float SmoothedFlash;

        /// <summary>累计时间（秒），传给 shader 做正弦/噪点动效。</summary>
        public static float ShaderTime;

        // 平滑步长（每帧朝目标值移动多少）
        private const float IntensityLerpUp = 0.012f;   // 上升慢一点，避免吓人
        private const float IntensityLerpDown = 0.020f; // 缓解时下降略快
        private const float FlashLerpUp = 0.08f;        // 嚼食后的爽感来得快
        private const float FlashLerpDown = 0.025f;     // 但回落得慢

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
