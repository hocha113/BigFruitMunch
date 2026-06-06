using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.NPCs
{
    /// <summary>镇民被喂后上头打折，实例本地状态，联机也只影响投喂者这边</summary>
    public class HighTownNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public int HighTicks;

        private const float ShopDiscount = 0.7f;

        private const int HighLines = 3;

        /// <summary>喂一口，播粒子</summary>
        public void FeedHigh(NPC npc, int ticks) {
            HighTicks = Math.Max(HighTicks, ticks);
            if (Main.dedServ) {
                return;
            }
            for (int i = 0; i < 16; i++) {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height,
                    DustID.PinkFairy, 0f, -1.5f, 120, default, 1.2f);
                d.noGravity = true;
            }
        }

        public override void PostAI(NPC npc) {
            if (HighTicks <= 0) {
                return;
            }
            HighTicks--;

            if (Main.dedServ) {
                return;
            }
            if (Main.rand.NextBool(20)) {
                Dust d = Dust.NewDustDirect(npc.position, npc.width, npc.height,
                    DustID.PinkFairy, 0f, -1f, 150, default, 0.9f);
                d.noGravity = true;
                d.velocity *= 0.4f;
            }
            if (Main.rand.NextBool(150)) {
                int n = Main.rand.Next(1, 1 + HighLines);
                string text = Language.GetTextValue($"Mods.BigFruitMunch.Common.NPCHigh.Line{n}");
                CombatText.NewText(npc.Hitbox, new Color(255, 180, 120), text);
            }
        }

        public override void ModifyActiveShop(NPC npc, string shopName, Item[] items) {
            if (HighTicks <= 0) {
                return;
            }
            foreach (Item item in items) {
                if (item == null || item.type <= ItemID.None || item.value <= 0) {
                    continue;
                }
                item.shopCustomPrice = Math.Max(1, (int)(item.value * ShopDiscount));
            }
        }
    }
}
