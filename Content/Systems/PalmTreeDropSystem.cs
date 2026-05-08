using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Systems
{
    /// <summary>
    /// 控制棕榈树的掉落：
    /// - 棕榈树根部被砍断时，按概率掉落"大果叶"(100%, 1-4) 和 "大果"(80%, 1-3)
    /// - 同时拦截因砍棕榈树掉落的橡果，使其消失（实现"棕榈树不再掉落橡果"）
    /// </summary>
    public class PalmTreeDropSystem : ModSystem
    {
        /// <summary>当前帧是否有棕榈树正在被破坏（由 KillTile 设定，每帧末重置）。</summary>
        public static bool PalmBreakingThisFrame;

        public override void PostUpdateEverything() {
            PalmBreakingThisFrame = false;
        }
    }

    public class PalmTreeGlobalTile : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem) {
            if (type != TileID.PalmTree) return;
            if (fail || effectOnly) return;

            // 任何一个棕榈树物块被破坏的当帧都标记，便于屏蔽橡果
            PalmTreeDropSystem.PalmBreakingThisFrame = true;

            if (noItem) return;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            // 仅在棕榈树"根部"那一格触发批量掉落，避免每个物块都掉落造成倍数
            if (!IsRootTile(i, j)) return;

            var src = new EntitySource_TileBreak(i, j);

            // 大果叶 100% 掉落 1-4
            int leafCount = Main.rand.Next(1, 5); // 1..4
            int leafType = ModContent.ItemType<BigFruitleaf>();
            for (int k = 0; k < leafCount; k++) {
                Item.NewItem(src, i * 16, j * 16, 16, 16, leafType);
            }

            // 大果 80% 掉落 1-3
            if (Main.rand.NextFloat() < 0.8f) {
                int fruitCount = Main.rand.Next(1, 4); // 1..3
                int fruitType = ModContent.ItemType<BigFruit>();
                for (int k = 0; k < fruitCount; k++) {
                    Item.NewItem(src, i * 16, j * 16, 16, 16, fruitType);
                }
            }
        }

        /// <summary>判断 (i, j) 是否为棕榈树根部物块（其下方为沙土系，且自身是棕榈）。</summary>
        private static bool IsRootTile(int i, int j) {
            if (!WorldGen.InWorld(i, j + 1)) return false;
            Tile below = Main.tile[i, j + 1];
            if (!below.HasTile) return false;
            ushort t = below.TileType;
            return t == TileID.Sand
                || t == TileID.Pearlsand
                || t == TileID.Ebonsand
                || t == TileID.Crimsand;
        }
    }

    /// <summary>
    /// 拦截因砍棕榈树掉出的橡果。借助 PalmTreeDropSystem.PalmBreakingThisFrame 当帧标志。
    /// </summary>
    public class AcornFromPalmSuppressor : GlobalItem
    {
        public override void OnSpawn(Item item, IEntitySource source) {
            if (item.type != ItemID.Acorn) return;
            if (source is not EntitySource_TileBreak) return;
            if (!PalmTreeDropSystem.PalmBreakingThisFrame) return;

            item.TurnToAir();
        }
    }
}
