using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Buffs
{
    /// <summary>
    /// "嚼的爽！" Buff 基类，分为 6 级（0-5）。
    /// 每级提供逐级递增的属性加成；具体数值在 Apply* 中定义，方便后续策划调整。
    /// </summary>
    public abstract class ChewSatisfactionBuffBase : ModBuff
    {
        public abstract int Level { get; }

        // TODO 为不同等级 Buff 制作独立的图标。当前先复用"可嚼大果"的贴图作为占位。
        public override string Texture => "BigFruitMunch/Content/ChewableBigFruit";

        public override LocalizedText DisplayName => Mod.GetLocalization(
            $"Buffs.ChewSatisfaction.Level{Level}.DisplayName");

        public override LocalizedText Description => Mod.GetLocalization(
            $"Buffs.ChewSatisfaction.Level{Level}.Description");

        public override void SetStaticDefaults() {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            // 数值参考：每级线性递增；占位实现，可按需要修改
            float dmgMult = 0.03f * (Level + 1);  // L0:+3% ... L5:+18%
            float speedMult = 0.02f * (Level + 1);  // L0:+2% ... L5:+12%
            int defenseAdd = Level;                // L0:0   ... L5:+5
            int lifeRegen = Level;                // L0:0   ... L5:+5

            player.GetDamage(DamageClass.Generic) += dmgMult;
            player.moveSpeed += speedMult;
            player.statDefense += defenseAdd;
            player.lifeRegen += lifeRegen;

            // 高级品质给予暴击奖励
            if (Level >= 2) player.GetCritChance(DamageClass.Generic) += 2 * (Level - 1); // L2:+2 ... L5:+8
        }

        public static int GetTypeForLevel(int level) => level switch {
            0 => ModContent.BuffType<ChewSatisfaction0>(),
            1 => ModContent.BuffType<ChewSatisfaction1>(),
            2 => ModContent.BuffType<ChewSatisfaction2>(),
            3 => ModContent.BuffType<ChewSatisfaction3>(),
            4 => ModContent.BuffType<ChewSatisfaction4>(),
            5 => ModContent.BuffType<ChewSatisfaction5>(),
            _ => 0,
        };
    }

    public class ChewSatisfaction0 : ChewSatisfactionBuffBase { public override int Level => 0; }
    public class ChewSatisfaction1 : ChewSatisfactionBuffBase { public override int Level => 1; }
    public class ChewSatisfaction2 : ChewSatisfactionBuffBase { public override int Level => 2; }
    public class ChewSatisfaction3 : ChewSatisfactionBuffBase { public override int Level => 3; }
    public class ChewSatisfaction4 : ChewSatisfactionBuffBase { public override int Level => 4; }
    public class ChewSatisfaction5 : ChewSatisfactionBuffBase { public override int Level => 5; }
}
