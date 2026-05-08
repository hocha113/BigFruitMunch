using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BigFruitMunch.Content
{
    /// <summary>
    /// 大果切割机（物品形态）—— 放置后形成 3x3 物块，可右键切开"大果"。
    /// 配方：
    ///   - 提炼机 x1 + 大果叶 x5  （工作台）
    ///   - 铁锭 x10 + 镰刀 x1 + 大果叶 x5  （工作台）
    /// </summary>
    public class BigFruitCutterbar : ModItem
    {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 1;
        }

        public override void SetDefaults() {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Blue;
            Item.createTile = ModContent.TileType<BigFruitCutterbarTile>();
        }

        public override void AddRecipes() {
            // 配方 1：提炼机 + 大果叶
            CreateRecipe()
                .AddIngredient(ItemID.Extractinator, 1)
                .AddIngredient<BigFruitleaf>(5)
                .AddTile(TileID.WorkBenches)
                .Register();

            // 配方 2：铁锭 + 镰刀 + 大果叶
            CreateRecipe()
                .AddIngredient(ItemID.IronBar, 10)
                .AddIngredient(ItemID.Sickle, 1)
                .AddIngredient<BigFruitleaf>(5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    /// <summary>
    /// 大果切割机（物块形态，3x3）。
    /// 玩家右键并背包内有"大果"时，会消耗 1 个大果，按品质表掉落 2 个对应品质的"去皮大果"。
    /// </summary>
    public class BigFruitCutterbarTile : ModTile
    {
        public override void SetStaticDefaults() {
            Main.tileSolid[Type] = false;
            Main.tileSolidTop[Type] = false;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileTable[Type] = false;

            TileID.Sets.HasOutlines[Type] = false;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.AvoidedByNPCs[Type] = true;

            DustType = DustID.WoodFurniture;
            AdjTiles = new int[] { Type };

            // 3x3 物块设置
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.AnchorBottom = new AnchorData(
                AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.Table,
                TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(160, 110, 50),
                Language.GetText("Mods.BigFruitMunch.Tiles.BigFruitCutterbarTile.MapEntry"));

            RegisterItemDrop(ModContent.ItemType<BigFruitCutterbar>());
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override void MouseOver(int i, int j) {
            Player p = Main.LocalPlayer;
            p.cursorItemIconEnabled = true;
            p.cursorItemIconID = ModContent.ItemType<BigFruitCutterbar>();
            p.noThrow = 2;
        }

        public override bool RightClick(int i, int j) {
            Player player = Main.LocalPlayer;

            int bigFruitType = ModContent.ItemType<BigFruit>();

            // 在玩家整个背包查找一个大果
            for (int k = 0; k < player.inventory.Length; k++) {
                Item it = player.inventory[k];
                if (it != null && !it.IsAir && it.type == bigFruitType && it.stack > 0) {
                    it.stack--;
                    if (it.stack <= 0) it.TurnToAir();

                    BigFruitQuality q = RollQuality();
                    int outType = DecorticateBigFruitBase.GetTypeForQuality(q);

                    // 切开变成两半，所以掉落 2 个对应品质 的去皮大果
                    Vector2 dropPos = new Vector2(i * 16 + 24, j * 16 + 8);
                    Item.NewItem(new EntitySource_TileInteraction(player, i, j),
                        (int)dropPos.X, (int)dropPos.Y, 16, 16, outType, 2);

                    SoundEngine.PlaySound(SoundID.Grab, dropPos);

                    // 给玩家额外反馈：在切割机上方弹一个粒子簇
                    for (int d = 0; d < 6; d++) {
                        Dust.NewDust(new Vector2(i * 16, j * 16 - 4), 48, 48,
                            DustID.WoodFurniture, 0f, -2f, 0, default, 1.1f);
                    }

                    return true;
                }
            }

            // 没有大果时不消耗，不弹任何东西
            return false;
        }

        /// <summary>按用户给定的概率表抽取一个品质。</summary>
        private static BigFruitQuality RollQuality() {
            float r = Main.rand.NextFloat();
            // 累积概率：依次 10/40/25/15/5/3/2 = 100
            float c = 0f;
            c += 0.10f; if (r < c) return BigFruitQuality.Withered;
            c += 0.40f; if (r < c) return BigFruitQuality.Common;
            c += 0.25f; if (r < c) return BigFruitQuality.Excellent;
            c += 0.15f; if (r < c) return BigFruitQuality.Rare;
            c += 0.05f; if (r < c) return BigFruitQuality.Epic;
            c += 0.03f; if (r < c) return BigFruitQuality.Legendary;
            return BigFruitQuality.Mythic;
        }
    }
}
