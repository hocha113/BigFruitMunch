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
    /// <summary>Mk2 肉后，能切果+打礼盒</summary>
    public class BigFruitCutterbarMk2 : ModItem
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
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.LightRed;
            Item.createTile = ModContent.TileType<BigFruitCutterbarMk2Tile>();
        }

        public override void AddRecipes() {
            CreateRecipe()
                .AddIngredient<BigFruitCutterbar>(1)
                .AddIngredient(ItemID.MythrilBar, 5)
                .AddIngredient(ItemID.SoulofLight, 3)
                .AddIngredient(ItemID.SoulofNight, 3)
                .AddTile(TileID.MythrilAnvil)
                .Register();

            CreateRecipe()
                .AddIngredient<BigFruitCutterbar>(1)
                .AddIngredient(ItemID.OrichalcumBar, 5)
                .AddIngredient(ItemID.SoulofLight, 3)
                .AddIngredient(ItemID.SoulofNight, 3)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    /// <summary>Mk2 物块，切果+礼盒合成站</summary>
    public class BigFruitCutterbarMk2Tile : ModTile
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
            AdjTiles = new int[] { Type, ModContent.TileType<BigFruitCutterbarTile>() };

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.newTile.Width = 4;
            TileObjectData.newTile.AnchorBottom = new AnchorData(
                AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.Table,
                TileObjectData.newTile.Width, 0);
            TileObjectData.newTile.LavaDeath = true;
            TileObjectData.newTile.UsesCustomCanPlace = true;
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(200, 140, 60),
                Language.GetText("Mods.BigFruitMunch.Tiles.BigFruitCutterbarMk2Tile.MapEntry"));

            RegisterItemDrop(ModContent.ItemType<BigFruitCutterbarMk2>());
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override void MouseOver(int i, int j) {
            Player p = Main.LocalPlayer;
            p.cursorItemIconEnabled = true;
            p.cursorItemIconID = ModContent.ItemType<BigFruitCutterbarMk2>();
            p.noThrow = 2;
        }

        public override bool RightClick(int i, int j) {
            if (!TileProcessorLoader.AutoPositionGetTP(i, j, out BigFruitCutterbarMk2TP tp)) {
                if (TileProcessorLoader.TileProcessorSafeGetTopLeft(i, j, out Point16 point)) {
                    tp = TileProcessorLoader.AddInWorld(Type, point, null) as BigFruitCutterbarMk2TP;
                }
            }

            tp?.OpenUI(Main.LocalPlayer);
            return true;
        }
    }
}
