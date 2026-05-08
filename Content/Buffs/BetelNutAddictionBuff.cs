using BigFruitMunch.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Buffs
{
    /// <summary>
    /// 槟榔成瘾计数 Buff —— 显示当前累计成瘾次数。
    /// 实际计数存储在 BetelNutPlayer，本 Buff 仅作为UI显示。
    /// 当玩家计数 &gt; 0 时由 BetelNutPlayer 自动维持。
    /// </summary>
    public class BetelNutAddictionBuff : ModBuff
    {
        // TODO 制作独立的 Buff 图标。当前先复用"大果"的贴图作为占位。
        public override string Texture => "BigFruitMunch/Content/BigFruit";

        public override void SetStaticDefaults() {
            // 表现为负面效果（debuff），但不会实际造成伤害——仅作为社会形象提醒
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;       // 计数实际由 ModPlayer 持久化，本 Buff 每帧重新加上
            Main.buffNoTimeDisplay[Type] = true; // 不显示时间，显示成瘾次数
        }

        public override void Update(Player player, ref int buffIndex) {
            // Buff 本身没有任何机制效果，仅作为可视化指示。
            // 后续策划可以在此处根据 BetelNutPlayer.AddictionCount 添加额外效果（例如：
            //   - 计数达到一定阈值时降低生命回复、提升上瘾"渴求"等）。
            var betel = player.GetModPlayer<BetelNutPlayer>();
            if (betel.AddictionCount <= 0) {
                player.ClearBuff(Type);
            }
        }
    }
}
