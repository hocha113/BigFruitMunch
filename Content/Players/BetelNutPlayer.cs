using BigFruitMunch.Content.Buffs;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BigFruitMunch.Content.Players
{
    /// <summary>
    /// 持久化追踪每位玩家的"槟榔成瘾计数"，并在每帧维持成瘾Buff显示。
    /// </summary>
    public class BetelNutPlayer : ModPlayer
    {
        public int AddictionCount;

        public void AddAddictionCount(int amount = 1) {
            AddictionCount += amount;
            if (AddictionCount < 0) AddictionCount = 0;
        }

        public override void SaveData(TagCompound tag) {
            tag["AddictionCount"] = AddictionCount;
        }

        public override void LoadData(TagCompound tag) {
            AddictionCount = tag.GetInt("AddictionCount");
        }

        public override void PostUpdateBuffs() {
            // 只要 AddictionCount > 0，就长期附加成瘾计数 Buff（每帧续 5 秒）
            if (AddictionCount > 0) {
                Player.AddBuff(ModContent.BuffType<BetelNutAddictionBuff>(), 60 * 5);
            }
        }
    }
}
