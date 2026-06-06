using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Buffs
{
    /// <summary>嚼的爽图标层，数值在 BetelNutPlayer 里算，这里不存盘</summary>
    public abstract class ChewSatisfactionBuffBase : ModBuff
    {
        public abstract int Level { get; }

        public override string Texture => $"BigFruitMunch/Content/Buffs/ChewSatisfaction{Level}";

        public override LocalizedText DisplayName => Mod.GetLocalization(
            $"Buffs.ChewSatisfaction.Level{Level}.DisplayName");

        public override LocalizedText Description => Mod.GetLocalization(
            $"Buffs.ChewSatisfaction.Level{Level}.Description");

        public override void SetStaticDefaults() {
            Main.debuff[Type] = false;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public static int GetTypeForLevel(int level) => level switch {
            1 => ModContent.BuffType<ChewSatisfaction1>(),
            2 => ModContent.BuffType<ChewSatisfaction2>(),
            3 => ModContent.BuffType<ChewSatisfaction3>(),
            4 => ModContent.BuffType<ChewSatisfaction4>(),
            5 => ModContent.BuffType<ChewSatisfaction5>(),
            6 => ModContent.BuffType<ChewSatisfaction6>(),
            _ => 0,
        };
    }

    public class ChewSatisfaction1 : ChewSatisfactionBuffBase { public override int Level => 1; }
    public class ChewSatisfaction2 : ChewSatisfactionBuffBase { public override int Level => 2; }
    public class ChewSatisfaction3 : ChewSatisfactionBuffBase { public override int Level => 3; }
    public class ChewSatisfaction4 : ChewSatisfactionBuffBase { public override int Level => 4; }
    public class ChewSatisfaction5 : ChewSatisfactionBuffBase { public override int Level => 5; }
    public class ChewSatisfaction6 : ChewSatisfactionBuffBase { public override int Level => 6; }
}
