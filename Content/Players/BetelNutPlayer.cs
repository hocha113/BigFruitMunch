using BigFruitMunch.Content.Buffs;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BigFruitMunch.Content.Players
{
    /// <summary>
    /// 持久化追踪每位玩家的"槟榔成瘾"状态，并据此推导出"渴求/戒断等级"。
    /// 该等级会同时驱动 <see cref="BetelWithdrawalBuffBase"/> 的数值惩罚与
    /// 屏幕级 shader 的扭曲强度。
    /// </summary>
    public class BetelNutPlayer : ModPlayer
    {
        /// <summary>累计嚼食的"上瘾度"（按品质加权），存盘字段。</summary>
        public int AddictionCount;

        /// <summary>距上次嚼食的 tick 计数。每帧 +1，OnChew 时归零。存盘字段（不允许通过登出绕过戒断）。</summary>
        public int WithdrawalTicks;

        /// <summary>当前帧推导出的渴求/戒断等级（0..5），仅运行时使用。</summary>
        public int CravingLevel;

        /// <summary>嚼食后短暂的"舒爽"残留 tick，用于驱动屏幕正向反馈 shader。</summary>
        public int RecentChewFlashTicks;
        public const int ChewFlashMaxTicks = 90;

        /// <summary>
        /// 每级戒断要求的"调整后 tick 阈值"（60 tick = 1 秒）。
        /// 实际触发时间会再被 AddictionCount 缩短（越成瘾越快上头）。
        /// </summary>
        private static readonly int[] WithdrawalBaseTicks =
        {
            0,        // L0：刚嚼完 / 未上瘾
            3600,     //  L1：~60s    —— 嘴馋
            10800,    //  L2：~180s   —— 渴求
            25200,    //  L3：~420s   —— 焦躁
            54000,    //  L4：~900s   —— 戒断
            108000,   //  L5：~1800s  —— 极度戒断
        };

        /// <summary>"长期未嚼食"开始衰减的阈值（约 5 分钟）。</summary>
        private const int DecayStartTicks = 18000;
        /// <summary>之后每多少 tick 自动 AddictionCount -1（约 1 分钟）。</summary>
        private const int DecayPerTicks = 3600;

        public override void SaveData(TagCompound tag) {
            tag["AddictionCount"] = AddictionCount;
            tag["WithdrawalTicks"] = WithdrawalTicks;
        }

        public override void LoadData(TagCompound tag) {
            AddictionCount = tag.GetInt("AddictionCount");
            WithdrawalTicks = tag.GetInt("WithdrawalTicks");
        }

        public override void OnEnterWorld() {
            CravingLevel = ComputeCravingLevel();
        }

        /// <summary>
        /// 嚼食一颗大果的统一入口。<paramref name="addictionGain"/> 决定上瘾度增量
        /// （Withered 传 0、神话级传 3 等）。无论加多少都会重置戒断计时与触发正向反馈。
        /// </summary>
        public void OnChew(int addictionGain) {
            if (addictionGain > 0) {
                AddictionCount += addictionGain;
            }
            WithdrawalTicks = 0;
            RecentChewFlashTicks = ChewFlashMaxTicks;

            if (addictionGain > 0 && Player.whoAmI == Main.myPlayer) {
                byte r = (byte)Math.Min(255, 180 + AddictionCount * 2);
                byte gb = (byte)Math.Max(40, 200 - AddictionCount * 3);
                string text = Language.GetTextValue("Mods.BigFruitMunch.Common.AddictionGain", addictionGain);
                CombatText.NewText(Player.Hitbox, new Color(r, gb, gb), text);
            }
        }

        public override void PostUpdate() {
            WithdrawalTicks++;

            // 长期未嚼食：每 DecayPerTicks 自然衰减一点上瘾度，给予"戒掉"激励
            if (AddictionCount > 0 && WithdrawalTicks > DecayStartTicks
                && ((WithdrawalTicks - DecayStartTicks) % DecayPerTicks) == 0) {
                AddictionCount = Math.Max(0, AddictionCount - 1);
            }

            CravingLevel = ComputeCravingLevel();

            if (RecentChewFlashTicks > 0) {
                RecentChewFlashTicks--;
            }
        }

        public override void PostUpdateBuffs() {
            if (AddictionCount > 0) {
                Player.AddBuff(ModContent.BuffType<BetelNutAddictionBuff>(), 60 * 5);
            }

            int withdrawalType = BetelWithdrawalBuffBase.GetTypeForLevel(CravingLevel);
            if (withdrawalType > 0) {
                Player.AddBuff(withdrawalType, 60 * 5);
            }
        }

        public override void ModifyScreenPosition() {
            if (Player.whoAmI != Main.myPlayer) return;
            if (CravingLevel < 3) return;

            float strength = (CravingLevel - 2) * 1.2f;
            Main.screenPosition += new Vector2(
                (Main.rand.NextFloat() - 0.5f) * 2f * strength,
                (Main.rand.NextFloat() - 0.5f) * 2f * strength);
        }

        /// <summary>
        /// 根据 AddictionCount 与 WithdrawalTicks 推导出戒断等级。
        /// AddictionCount 越大，同样不嚼食时间会更快到达高级。
        /// </summary>
        public int ComputeCravingLevel() {
            if (AddictionCount <= 0) return 0;

            float speedMult = 1f + 0.04f * (float)Math.Sqrt(Math.Max(0, AddictionCount));
            float adjustedTicks = WithdrawalTicks * speedMult;

            int level = 0;
            for (int i = WithdrawalBaseTicks.Length - 1; i >= 0; i--) {
                if (adjustedTicks >= WithdrawalBaseTicks[i]) {
                    level = i;
                    break;
                }
            }
            return level;
        }

        /// <summary>归一化到 0..1 的戒断屏幕扭曲强度目标值，供 shader 系统平滑追踪。</summary>
        public float TargetWithdrawalIntensity => CravingLevel <= 0 ? 0f : CravingLevel / 5f;
    }
}
