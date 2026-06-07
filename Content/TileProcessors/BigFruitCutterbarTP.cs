using BigFruitMunch.Content.UIs;
using InnoVault.TileProcessors;
using System;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BigFruitMunch.Content.TileProcessors
{
    public abstract class BigFruitCutterbarTPBase : TileProcessor
    {
        public const int QualityCount = 7;
        public const int TenRollCost = 10;
        public const int LastRollCount = 10;

        public int StoredBigFruitCount;
        public int OperatingPlayer = -1;
        public int[] PendingDecorticatedCounts { get; private set; } = new int[QualityCount];
        public BigFruitQuality[] LastRollQualities { get; private set; } = CreateEmptyLastRolls();

        public abstract bool IsUpgradedCutter { get; }

        public Vector2 Center => PosInWorld + Size * 0.5f;

        public override void SetProperty() {
            PendingDecorticatedCounts = new int[QualityCount];
            LastRollQualities = CreateEmptyLastRolls();
            OperatingPlayer = -1;
        }

        public override bool? RightClick(int i, int j, Tile tile, Player player) {
            OpenUI(player);
            return true;
        }

        public void OpenUI(Player player) {
            if (player == null || !player.active || player.whoAmI != Main.myPlayer) {
                return;
            }

            if (IsOccupiedByOther(player)) {
                CombatText.NewText(player.Hitbox, new Color(220, 170, 90),
                    Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.Occupied"));
                return;
            }

            OperatingPlayer = player.whoAmI;
            SendData();
            BigFruitCutterGachaUI.Instance.Open(this);
        }

        public void CloseUI(Player player) {
            if (player != null && OperatingPlayer == player.whoAmI) {
                OperatingPlayer = -1;
                SendData();
            }
        }

        public override void Update() {
            if (OperatingPlayer < 0) {
                return;
            }

            Player player = Main.player[OperatingPlayer];
            if (player == null || !player.active || player.dead || player.DistanceSQ(Center) > BigFruitCutterGachaUI.MaxInteractionDistanceSquared) {
                OperatingPlayer = -1;
                SendData();
            }
        }

        public bool IsOccupiedByOther(Player player) {
            if (OperatingPlayer < 0 || OperatingPlayer == player.whoAmI) {
                return false;
            }

            Player operating = Main.player[OperatingPlayer];
            return operating != null && operating.active && !operating.dead;
        }

        public int DepositAllFrom(Player player) {
            int bigFruitType = ModContent.ItemType<BigFruit>();
            int deposited = 0;
            //inventory[58] 与 Main.mouseItem 同步但为独立对象，跳过最后一槽避免重复计数
            int invLimit = player.whoAmI == Main.myPlayer ? player.inventory.Length - 1 : player.inventory.Length;
            for (int i = 0; i < invLimit; i++) {
                Item item = player.inventory[i];
                if (item == null || item.IsAir || item.type != bigFruitType || item.stack <= 0) {
                    continue;
                }

                deposited += item.stack;
                item.TurnToAir();
            }

            //鼠标持有的物品不在 inventory 数组内，需要单独处理
            if (player.whoAmI == Main.myPlayer && Main.mouseItem != null && !Main.mouseItem.IsAir
                && Main.mouseItem.type == bigFruitType && Main.mouseItem.stack > 0) {
                deposited += Main.mouseItem.stack;
                Main.mouseItem.TurnToAir();
            }

            if (deposited > 0) {
                StoredBigFruitCount += deposited;
                SendData();
            }

            return deposited;
        }

        public int WithdrawAllTo(Player player) {
            int count = StoredBigFruitCount;
            if (count <= 0) {
                return 0;
            }

            StoredBigFruitCount = 0;
            GiveOrDrop(player, ModContent.ItemType<BigFruit>(), count);
            SendData();
            return count;
        }

        public int ClaimAllTo(Player player) {
            int total = 0;
            for (int i = 0; i < PendingDecorticatedCounts.Length; i++) {
                int count = PendingDecorticatedCounts[i];
                if (count <= 0) {
                    continue;
                }

                BigFruitQuality quality = (BigFruitQuality)i;
                GiveOrDrop(player, DecorticateBigFruitBase.GetTypeForQuality(quality), count);
                PendingDecorticatedCounts[i] = 0;
                total += count;
            }

            if (total > 0) {
                SendData();
            }

            return total;
        }

        public bool TryRoll(int rollCount, out BigFruitQuality[] results) {
            results = Array.Empty<BigFruitQuality>();
            if (rollCount <= 0 || StoredBigFruitCount < rollCount) {
                return false;
            }

            StoredBigFruitCount -= rollCount;
            results = new BigFruitQuality[rollCount];
            LastRollQualities = CreateEmptyLastRolls();

            for (int i = 0; i < rollCount; i++) {
                BigFruitQuality quality = BigFruitQualityExtensions.Roll(IsUpgradedCutter);
                results[i] = quality;
                if (i < LastRollQualities.Length) {
                    LastRollQualities[i] = quality;
                }

                PendingDecorticatedCounts[(int)quality] += 2;
            }

            SendData();
            return true;
        }

        public int PendingTotal() {
            int total = 0;
            for (int i = 0; i < PendingDecorticatedCounts.Length; i++) {
                total += PendingDecorticatedCounts[i];
            }
            return total;
        }

        public override void OnKill() {
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                return;
            }

            DropItem(ModContent.ItemType<BigFruit>(), StoredBigFruitCount);
            StoredBigFruitCount = 0;

            for (int i = 0; i < PendingDecorticatedCounts.Length; i++) {
                int count = PendingDecorticatedCounts[i];
                if (count <= 0) {
                    continue;
                }

                DropItem(DecorticateBigFruitBase.GetTypeForQuality((BigFruitQuality)i), count);
                PendingDecorticatedCounts[i] = 0;
            }
        }

        public override void SendData(ModPacket data) {
            data.Write(StoredBigFruitCount);
            data.Write(OperatingPlayer);
            for (int i = 0; i < QualityCount; i++) {
                data.Write(PendingDecorticatedCounts[i]);
            }
            for (int i = 0; i < LastRollCount; i++) {
                data.Write((byte)LastRollQualities[i]);
            }
        }

        public override void ReceiveData(BinaryReader reader, int whoAmI) {
            StoredBigFruitCount = reader.ReadInt32();
            OperatingPlayer = reader.ReadInt32();
            EnsureArrays();
            for (int i = 0; i < QualityCount; i++) {
                PendingDecorticatedCounts[i] = reader.ReadInt32();
            }
            for (int i = 0; i < LastRollCount; i++) {
                LastRollQualities[i] = (BigFruitQuality)reader.ReadByte();
            }
        }

        public override void SaveData(TagCompound tag) {
            tag["StoredBigFruitCount"] = StoredBigFruitCount;
            tag["PendingDecorticatedCounts"] = PendingDecorticatedCounts;
            tag["LastRollQualities"] = Array.ConvertAll(LastRollQualities, q => (int)q);
        }

        public override void LoadData(TagCompound tag) {
            StoredBigFruitCount = tag.GetInt("StoredBigFruitCount");
            EnsureArrays();

            if (tag.TryGet("PendingDecorticatedCounts", out int[] pending)) {
                for (int i = 0; i < Math.Min(QualityCount, pending.Length); i++) {
                    PendingDecorticatedCounts[i] = pending[i];
                }
            }

            if (tag.TryGet("LastRollQualities", out int[] lastRolls)) {
                for (int i = 0; i < Math.Min(LastRollCount, lastRolls.Length); i++) {
                    LastRollQualities[i] = (BigFruitQuality)Math.Clamp(lastRolls[i], 0, QualityCount - 1);
                }
            }
        }

        private void EnsureArrays() {
            if (PendingDecorticatedCounts == null) {
                PendingDecorticatedCounts = new int[QualityCount];
            }
            else if (PendingDecorticatedCounts.Length != QualityCount) {
                int[] resized = new int[QualityCount];
                Array.Copy(PendingDecorticatedCounts, resized, Math.Min(PendingDecorticatedCounts.Length, resized.Length));
                PendingDecorticatedCounts = resized;
            }

            if (LastRollQualities == null) {
                LastRollQualities = CreateEmptyLastRolls();
            }
            else if (LastRollQualities.Length != LastRollCount) {
                BigFruitQuality[] resized = CreateEmptyLastRolls();
                Array.Copy(LastRollQualities, resized, Math.Min(LastRollQualities.Length, resized.Length));
                LastRollQualities = resized;
            }
        }

        private void GiveOrDrop(Player player, int itemType, int stack) {
            if (stack <= 0) {
                return;
            }

            int remaining = stack;
            while (remaining > 0) {
                int give = Math.Min(remaining, Item.CommonMaxStack);
                Item item = new Item(itemType, give);
                player.QuickSpawnItem(new EntitySource_TileInteraction(player, Position.X, Position.Y), item, item.stack);
                remaining -= give;
            }
        }

        private void DropItem(int itemType, int stack) {
            int remaining = stack;
            while (remaining > 0) {
                int drop = Math.Min(remaining, Item.CommonMaxStack);
                int itemIndex = Item.NewItem(new EntitySource_WorldGen(), Center, itemType, drop);
                if (Main.netMode == NetmodeID.Server && itemIndex >= 0) {
                    NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex);
                }
                remaining -= drop;
            }
        }

        private static BigFruitQuality[] CreateEmptyLastRolls() {
            BigFruitQuality[] result = new BigFruitQuality[LastRollCount];
            Array.Fill(result, BigFruitQuality.Withered);
            return result;
        }
    }

    public class BigFruitCutterbarTP : BigFruitCutterbarTPBase
    {
        public override int TargetTileID => ModContent.TileType<BigFruitCutterbarTile>();
        public override bool IsUpgradedCutter => false;
    }

    public class BigFruitCutterbarMk2TP : BigFruitCutterbarTPBase
    {
        public override int TargetTileID => ModContent.TileType<BigFruitCutterbarMk2Tile>();
        public override bool IsUpgradedCutter => true;
    }
}
