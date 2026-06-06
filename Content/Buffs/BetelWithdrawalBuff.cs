using BigFruitMunch.Content.Players;
using System;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Buffs
{
    /// <summary>戒断 L1~5，PostUpdateBuffs 续命，不存盘</summary>
    public abstract class BetelWithdrawalBuffBase : ModBuff
    {
        public abstract int Level { get; } //1..5

        public override string Texture => $"BigFruitMunch/Content/Buffs/BetelWithdrawal{Level}";

        public override LocalizedText DisplayName => Mod.GetLocalization(
            $"Buffs.BetelWithdrawal.Level{Level}.DisplayName");

        public override LocalizedText Description => Mod.GetLocalization(
            $"Buffs.BetelWithdrawal.Level{Level}.Description");

        public override void SetStaticDefaults() {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex) {
            var betel = player.GetModPlayer<BetelNutPlayer>();
            if (betel == null || betel.CravingLevel != Level) {
                player.ClearBuff(Type);
                return;
            }

            switch (Level) {
                case 1:
                    player.moveSpeed -= 0.02f;
                    break;

                case 2:
                    player.moveSpeed -= 0.05f;
                    player.GetDamage(DamageClass.Generic) -= 0.02f;
                    break;

                case 3:
                    player.moveSpeed -= 0.08f;
                    player.GetDamage(DamageClass.Generic) -= 0.05f;
                    break;

                case 4:
                    player.moveSpeed -= 0.12f;
                    player.GetDamage(DamageClass.Generic) -= 0.10f;
                    player.lifeRegen = Math.Min(player.lifeRegen, 0);
                    break;

                case 5:
                    player.moveSpeed -= 0.18f;
                    player.GetDamage(DamageClass.Generic) -= 0.20f;
                    player.GetCritChance(DamageClass.Generic) -= 5f;
                    player.lifeRegen = Math.Min(player.lifeRegen, 0);
                    player.lifeRegen -= 8;
                    break;
            }
        }

        public static int GetTypeForLevel(int level) => level switch {
            1 => ModContent.BuffType<BetelWithdrawal1>(),
            2 => ModContent.BuffType<BetelWithdrawal2>(),
            3 => ModContent.BuffType<BetelWithdrawal3>(),
            4 => ModContent.BuffType<BetelWithdrawal4>(),
            5 => ModContent.BuffType<BetelWithdrawal5>(),
            _ => 0,
        };
    }

    public class BetelWithdrawal1 : BetelWithdrawalBuffBase { public override int Level => 1; }
    public class BetelWithdrawal2 : BetelWithdrawalBuffBase { public override int Level => 2; }
    public class BetelWithdrawal3 : BetelWithdrawalBuffBase { public override int Level => 3; }
    public class BetelWithdrawal4 : BetelWithdrawalBuffBase { public override int Level => 4; }
    public class BetelWithdrawal5 : BetelWithdrawalBuffBase { public override int Level => 5; }
}
