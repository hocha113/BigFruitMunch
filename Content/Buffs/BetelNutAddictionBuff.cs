using BigFruitMunch.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Buffs
{
    /// <summary>
    /// 槟榔成瘾计数 Buff —— 仅作为 UI 标记显示累计成瘾度。
    /// 实际计数存储在 <see cref="BetelNutPlayer"/>，本 Buff 不参与数值惩罚
    /// （惩罚由 <see cref="BetelWithdrawalBuffBase"/> 各级戒断 Buff 提供）。
    /// </summary>
    public class BetelNutAddictionBuff : ModBuff
    {
        // TODO 制作独立的 Buff 图标。当前先复用"大果"的贴图作为占位。
        public override string Texture => "BigFruitMunch/Content/BigFruit";

        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            var betel = player.GetModPlayer<BetelNutPlayer>();
            if (betel == null || betel.AddictionCount <= 0) {
                player.ClearBuff(Type);
            }
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare) {
            var betel = Main.LocalPlayer?.GetModPlayer<BetelNutPlayer>();
            if (betel == null) return;
            buffName = $"{buffName} ({betel.AddictionCount})";
        }
    }
}
