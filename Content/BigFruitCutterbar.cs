using BigFruitMunch.Content.TileProcessors;
using InnoVault.TileProcessors;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace BigFruitMunch.Content
{
    /// <summary>Mk1 切割机物品，3x3 放置，切果+打包日常装</summary>
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
            CreateRecipe()
                .AddIngredient(ItemID.Extractinator, 1)
                .AddIngredient<BigFruitleaf>(5)
                .AddTile(TileID.WorkBenches)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.IronBar, 10)
                .AddIngredient(ItemID.Sickle, 1)
                .AddIngredient<BigFruitleaf>(5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    /// <summary>Mk1 物块，右键切大果掉 2 去皮</summary>
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
            if (!TileProcessorLoader.AutoPositionGetTP(i, j, out BigFruitCutterbarTP tp)) {
                if (TileProcessorLoader.TileProcessorSafeGetTopLeft(i, j, out Point16 point)) {
                    tp = TileProcessorLoader.AddInWorld(Type, point, null) as BigFruitCutterbarTP;
                }
            }

            tp?.OpenUI(Main.LocalPlayer);
            return true;
        }
    }
}
