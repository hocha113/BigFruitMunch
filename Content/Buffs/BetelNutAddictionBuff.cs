using BigFruitMunch.Content.Players;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Buffs
{
    /// <summary>成瘾 UI 标记，真计数在 BetelNutPlayer</summary>
    public class BetelNutAddictionBuff : ModBuff
    {
        public override string Texture => "BigFruitMunch/Content/Buffs/BetelNutAddiction";

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
            if (Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers) return;

            Player player = Main.player[Main.myPlayer];
            if (player == null || !player.active) return;

            var betel = player.GetModPlayer<BetelNutPlayer>();
            if (betel == null) return;
            buffName = Language.GetTextValue(
                "Mods.BigFruitMunch.Buffs.BetelNutAddictionBuff.DisplayNameWithCount",
                buffName,
                betel.AddictionCount);
        }
    }
}
