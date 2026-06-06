using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Systems
{
    /// <summary>右键摇棕榈树，自己搞掉落和晃动</summary>
    public enum PalmShakeMessage : byte
    {
        ShakeRequest = 0,
        ShakeVisual = 1,
    }

    public static class PalmShake
    {
        public const float ShakeReachPixels = 16f * 12f;

        private const int VisualDuration = 44;
        internal const int ShakeCooldownTicks = VisualDuration;
        private const float BigFruitLeafDropChance = 0.20f;
        private const float BigFruitDropChance = 0.16f;
        private const float MaxTreeSwayPixels = 10f;

        private static readonly Dictionary<Point, ShakeVisual> ActiveVisuals = new();
        private static readonly Dictionary<Point, ulong> LastShakeTicks = new();

        private struct ShakeVisual
        {
            public int BaseX;
            public int BaseY;
            public int TopX;
            public int TopY;
            public int Direction;
            public ulong StartTick;
        }

        /// <summary>沙地棕榈才算，out 树干底</summary>
        public static bool IsBigFruitPalm(int x, int y, out int baseX, out int baseY) {
            baseX = x;
            baseY = y;
            if (!WorldGen.InWorld(x, y)) {
                return false;
            }

            WorldGen.GetTreeBottom(x, y, out baseX, out baseY);
            if (!WorldGen.InWorld(baseX, baseY + 1)) {
                return false;
            }

            Tile trunk = Main.tile[baseX, baseY];
            if (!trunk.HasTile) {
                return false;
            }

            Tile ground = Main.tile[baseX, baseY + 1];
            if (!ground.HasTile) {
                return false;
            }

            ushort g = ground.TileType;
            return g == TileID.Sand
                || g == TileID.Pearlsand
                || g == TileID.Ebonsand
                || g == TileID.Crimsand;
        }

        /// <summary>右键入口，多人走包</summary>
        public static void RequestShakeFromClient(int clickX, int clickY) {
            if (Main.netMode == NetmodeID.SinglePlayer) {
                TryShakePalm(clickX, clickY, Main.LocalPlayer, playLocalEffects: true);
                return;
            }

            ModPacket packet = ModContent.GetInstance<global::BigFruitMunch.BigFruitMunch>().GetPacket();
            packet.Write((byte)PalmShakeMessage.ShakeRequest);
            packet.Write(clickX);
            packet.Write(clickY);
            packet.Send();
        }

        public static void HandleShakeRequest(BinaryReader reader, int fromWho) {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            if (Main.netMode != NetmodeID.Server) return;
            if (fromWho < 0 || fromWho >= Main.maxPlayers) return;

            Player player = Main.player[fromWho];
            if (player == null || !player.active || player.dead) return;

            TryShakePalm(x, y, player, playLocalEffects: false);
        }

        public static void HandleShakeVisual(BinaryReader reader) {
            int baseX = reader.ReadInt32();
            int baseY = reader.ReadInt32();
            int topX = reader.ReadInt32();
            int topY = reader.ReadInt32();
            int direction = reader.ReadInt32();
            int playerWho = reader.ReadInt32();
            bool playTreeEffects = reader.ReadBoolean();

            if (playTreeEffects) {
                DoShakeFX(baseX, baseY, topX, topY, direction);
            }

            if (playerWho >= 0 && playerWho < Main.maxPlayers) {
                TriggerShakeAnimation(Main.player[playerWho], topX, topY);
            }
        }

        private static bool TryShakePalm(int clickX, int clickY, Player player, bool playLocalEffects) {
            if (!WorldGen.InWorld(clickX, clickY)) return false;

            Tile tile = Main.tile[clickX, clickY];
            if (!tile.HasTile || tile.TileType != TileID.PalmTree) return false;

            if (!IsBigFruitPalm(clickX, clickY, out int baseX, out int baseY)) return false;

            Vector2 tileCenter = new Vector2(clickX * 16 + 8, clickY * 16 + 8);
            float allowed = ShakeReachPixels + (Main.netMode == NetmodeID.Server ? 16f * 10f : 0f);
            if (Vector2.DistanceSquared(player.Center, tileCenter) > allowed * allowed) return false;

            FindTreeTop(clickX, clickY, out int topX, out int topY);

            int direction = Math.Sign((topX * 16 + 8) - player.Center.X);
            if (direction == 0) direction = player.direction;

            Point treeBase = new Point(baseX, baseY);
            bool canDropLoot = !IsShakeOnCooldown(treeBase);
            if (canDropLoot) {
                LastShakeTicks[treeBase] = Main.GameUpdateCount;
                DropPalmLoot(topX, topY);
            }

            if (playLocalEffects) {
                if (canDropLoot) {
                    DoShakeFX(baseX, baseY, topX, topY, direction);
                }
                TriggerShakeAnimation(player, topX, topY);
            }
            else {
                SendShakeVisual(baseX, baseY, topX, topY, direction, player.whoAmI, canDropLoot);
            }

            return true;
        }

        private static bool IsShakeOnCooldown(Point treeBase) {
            if (!LastShakeTicks.TryGetValue(treeBase, out ulong lastTick)) {
                return false;
            }

            ulong now = Main.GameUpdateCount;
            if (now < lastTick || now - lastTick >= ShakeCooldownTicks) {
                LastShakeTicks.Remove(treeBase);
                return false;
            }

            return true;
        }

        private static void FindTreeTop(int x, int y, out int topX, out int topY) {
            topX = x;
            topY = y;
            for (int guard = 0; guard < 80 && topY > 1; guard++) {
                Tile above = Main.tile[topX, topY - 1];
                if (!above.HasTile || above.TileType != TileID.PalmTree) break;
                topY--;
            }
        }

        private static void DropPalmLoot(int topX, int topY) {
            IEntitySource source = WorldGen.GetItemSource_FromTreeShake(topX, topY);

            if (Main.rand.NextFloat() < BigFruitLeafDropChance) {
                SpawnDrop(source, topX, topY, ModContent.ItemType<BigFruitleaf>(), Main.rand.Next(1, 5));
            }

            if (Main.rand.NextFloat() < BigFruitDropChance) {
                SpawnDrop(source, topX, topY, ModContent.ItemType<BigFruit>(), Main.rand.Next(1, 4));
            }
        }

        private static void SpawnDrop(IEntitySource source, int topX, int topY, int itemType, int stack) {
            int itemIndex = Item.NewItem(source, topX * 16 - 8, topY * 16 - 8, 32, 32, itemType, stack);
            if (itemIndex < 0 || itemIndex >= Main.maxItems) return;

            Item item = Main.item[itemIndex];
            item.velocity = new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), Main.rand.NextFloat(-3.2f, -1.3f));

            if (Main.netMode == NetmodeID.Server) {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, itemIndex, 1f);
            }
        }

        private static void SendShakeVisual(int baseX, int baseY, int topX, int topY, int direction, int playerWho, bool playTreeEffects) {
            ModPacket packet = ModContent.GetInstance<global::BigFruitMunch.BigFruitMunch>().GetPacket();
            packet.Write((byte)PalmShakeMessage.ShakeVisual);
            packet.Write(baseX);
            packet.Write(baseY);
            packet.Write(topX);
            packet.Write(topY);
            packet.Write(direction);
            packet.Write(playerWho);
            packet.Write(playTreeEffects);
            packet.Send();
        }

        private static void TriggerShakeAnimation(Player player, int topX, int topY) {
            if (Main.dedServ) return;
            if (player == null || !player.active) return;
            player.GetModPlayer<PalmShakePlayer>().BeginShakeAnimation(topX, topY);
        }

        /// <summary>树晃+粒子+震屏，纯客户端</summary>
        private static void DoShakeFX(int baseX, int baseY, int topX, int topY, int direction) {
            if (Main.dedServ) return;

            Point treeBase = new Point(baseX, baseY);
            ActiveVisuals[treeBase] = new ShakeVisual {
                BaseX = baseX,
                BaseY = baseY,
                TopX = topX,
                TopY = topY,
                Direction = direction == 0 ? 1 : direction,
                StartTick = Main.GameUpdateCount,
            };

            Vector2 topWorld = new Vector2(topX * 16 + 8, topY * 16 + 8);
            SoundEngine.PlaySound(SoundID.Grass with { Pitch = -0.2f, Volume = 0.95f }, topWorld);

            for (int k = 0; k < 34; k++) {
                Vector2 pos = topWorld + new Vector2(Main.rand.NextFloat(-30f, 30f), Main.rand.NextFloat(-34f, 20f));
                Vector2 velocity = new Vector2(direction * Main.rand.NextFloat(0.6f, 3f), Main.rand.NextFloat(-1.8f, 3f));
                int dustId = Main.rand.NextBool() ? DustID.GrassBlades : DustID.JungleGrass;
                Dust dust = Dust.NewDustPerfect(pos, dustId, velocity, 0, default, Main.rand.NextFloat(0.9f, 1.5f));
                dust.noGravity = Main.rand.NextBool(3);
            }

            PunchCameraModifier shake = new PunchCameraModifier(
                topWorld,
                ((float)(Main.rand.NextDouble() * Math.PI * 2.0)).ToRotationVector2(),
                2.5f, 4.5f, 16, 1400f, "BigFruitPalmShake");
            Main.instance.CameraModifiers.Add(shake);
        }

        /// <summary>frameX 88~132 是树冠，DrawTrees 单独画</summary>
        private static bool IsPalmCrownMarker(int frameX) => frameX >= 88 && frameX <= 132;

        /// <summary>晃动时 PreDraw 接管，坐标对齐原版 DrawTrees/323</summary>
        public static bool TryDrawShakingPalmTile(int i, int j, int type, SpriteBatch spriteBatch) {
            if (type != TileID.PalmTree) return true;
            if (!TryGetTreeSwayOffset(i, j, out float offsetX)) return true;

            if (IsPalmCrownMarker(Main.tile[i, j].TileFrameX)) {
                DrawPalmCrown(spriteBatch, i, j, offsetX);
                return false;
            }

            DrawPalmTrunkTile(i, j, spriteBatch, offsetX);
            return false;
        }

        private static Vector2 GetTileDrawScreenOffset() =>
            Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);

        private static void DrawPalmTrunkTile(int tileX, int tileY, SpriteBatch spriteBatch, float swayX) {
            Tile tile = Main.tile[tileX, tileY];
            int type = TileID.PalmTree;

            Main.instance.LoadTiles(type);
            Texture2D trunkTex = TextureAssets.Tile[type].Value;
            Color color = Lighting.GetColor(tileX, tileY);

            Vector2 unscaledPosition = Main.Camera.UnscaledPosition;
            Vector2 screenOffset = GetTileDrawScreenOffset();
            Vector2 trunkDst = new Vector2(
                tileX * 16 - unscaledPosition.X - 2f + tile.TileFrameY + swayX,
                tileY * 16 - unscaledPosition.Y) + screenOffset;

            int palmBiome = GetPalmTreeBiome(tileX, tileY);
            short drawFrameY = Math.Abs(palmBiome) >= 8
                ? (short)(22 * (palmBiome < 0 ? 1 : 0))
                : (short)(22 * palmBiome);

            Rectangle trunkSrc = new(tile.TileFrameX, drawFrameY, 20, 20);
            spriteBatch.Draw(trunkTex, trunkDst, trunkSrc, color);
        }

        /// <summary>抄 DrawTrees 树冠那段</summary>
        private static void DrawPalmCrown(SpriteBatch spriteBatch, int x, int y, float swayX) {
            Tile tile = Main.tile[x, y];
            int frameX = tile.TileFrameX;

            int topVariant = 0;
            switch (frameX) {
                case 110: topVariant = 1; break;
                case 132: topVariant = 2; break;
            }

            int treeTextureIndex = 15;
            int topWidth = 80;
            int topHeight = 80;
            int topOffsetX = 32;
            int topOffsetY = 0;

            int palmTreeBiome = GetPalmTreeBiome(x, y);
            int srcY = palmTreeBiome * 82;

            if (palmTreeBiome >= 4 && palmTreeBiome <= 7) {
                treeTextureIndex = 21;
                topWidth = 114;
                topHeight = 98;
                srcY = (palmTreeBiome - 4) * 98;
                topOffsetX = 48;
                topOffsetY = 2;
            }

            if (Math.Abs(palmTreeBiome) >= 8) {
                srcY = 0;
                if (palmTreeBiome < 0) {
                    topWidth = 114;
                    topHeight = 98;
                    topOffsetX = 48;
                    topOffsetY = 2;
                }

                treeTextureIndex = Math.Abs(palmTreeBiome) - 8;
                treeTextureIndex *= -2;
                if (palmTreeBiome < 0) {
                    treeTextureIndex--;
                }
            }

            if (treeTextureIndex < 0 || treeTextureIndex >= TextureAssets.TreeTop.Length) {
                treeTextureIndex = 15;
            }

            Texture2D topTex = TextureAssets.TreeTop[treeTextureIndex].Value;
            if (topTex == null) return;

            //DrawTrees 用 UnscaledPosition，不加 offScreenRange
            Vector2 unscaledPosition = Main.Camera.UnscaledPosition - new Vector2(Main.offScreenRange, Main.offScreenRange);//至于为什么要减去(192, 192)，我也不知道，反正是这个固定的偏移值
            Vector2 position = new Vector2(
                x * 16 - unscaledPosition.X - topOffsetX + tile.TileFrameY + topWidth / 2f + swayX * 1f,//swayX不要乘以2，1倍的摇动频率刚刚好
                y * 16 - unscaledPosition.Y + 16 + topOffsetY);

            Color color = Lighting.GetColor(x, y);
            Rectangle src = new(topVariant * (topWidth + 2), srcY, topWidth, topHeight);
            spriteBatch.Draw(topTex, position, src, color, 0f, new Vector2(topWidth / 2f, topHeight), 1f, SpriteEffects.None, 0f);
        }

        /// <summary>同 GetPalmTreeBiome</summary>
        private static int GetPalmTreeBiome(int tileX, int tileY) {
            int y = tileY;
            while (WorldGen.InWorld(tileX, y) && Main.tile[tileX, y].HasTile && Main.tile[tileX, y].TileType == TileID.PalmTree) {
                y++;
            }

            return GetPalmTreeVariant(tileX, y);
        }

        private static int GetPalmTreeVariant(int x, int groundY) {
            if (!WorldGen.InWorld(x, groundY)) return 0;

            Tile ground = Main.tile[x, groundY];
            if (!ground.HasTile) return 0;

            int variant = ground.TileType switch {
                TileID.Sand => 0,
                TileID.Pearlsand => 1,
                TileID.Ebonsand => 2,
                TileID.Crimsand => 3,
                _ => 0,
            };

            if (WorldGen.IsPalmOasisTree(x)) {
                variant += 4;
            }

            return variant;
        }

        private static bool TryGetTreeSwayOffset(int i, int j, out float offsetX) {
            offsetX = 0f;
            if (!IsBigFruitPalm(i, j, out int baseX, out int baseY)) return false;

            Point treeBase = new Point(baseX, baseY);
            if (!ActiveVisuals.TryGetValue(treeBase, out ShakeVisual visual)) return false;

            int elapsed = (int)(Main.GameUpdateCount - visual.StartTick);
            if (elapsed < 0 || elapsed >= VisualDuration) {
                ActiveVisuals.Remove(treeBase);
                return false;
            }

            int treeHeight = Math.Max(1, visual.BaseY - visual.TopY);
            float heightFactor = MathHelper.Clamp((visual.BaseY - j) / (float)treeHeight, 0f, 1f);
            float t = elapsed / (float)VisualDuration;
            float decay = 1f - t;
            float wave = MathF.Sin(t * MathHelper.TwoPi * 3.25f);

            offsetX = visual.Direction * MaxTreeSwayPixels * heightFactor * wave * decay;
            return Math.Abs(offsetX) > 0.05f;
        }
    }

    public class PalmTreeShakeGlobalTile : GlobalTile
    {
        public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch) {
            return PalmShake.TryDrawShakingPalmTile(i, j, type, spriteBatch);
        }
    }

    /// <summary>本地右键检测 + 摇树手臂</summary>
    public class PalmShakePlayer : ModPlayer
    {
        private const int ShakeAnimDuration = 44;

        private int shakeAnimTimer;

        private int shakeRepeatTimer;

        private int shakeTreeTopX;
        private int shakeTreeTopY;

        public void BeginShakeAnimation(int treeTopX, int treeTopY) {
            shakeAnimTimer = ShakeAnimDuration;
            shakeTreeTopX = treeTopX;
            shakeTreeTopY = treeTopY;
        }

        public override void PostUpdate() {
            UpdateShakeAnimation();

            if (Main.dedServ) return;
            if (Player.whoAmI != Main.myPlayer) return;
            if (!Main.hasFocus || Main.gamePaused) return;
            if (Main.drawingPlayerChat) return;
            if (Player.dead) return;
            if (Main.LocalPlayer.mouseInterface) return;
            if (!Main.mouseRight) {
                shakeRepeatTimer = 0;
                return;
            }

            if (!Main.mouseRightRelease && shakeRepeatTimer > 0) {
                shakeRepeatTimer--;
                return;
            }

            if (Player.itemAnimation > 0) return;
            Point tile = Main.MouseWorld.ToTileCoordinates();
            if (!WorldGen.InWorld(tile.X, tile.Y)) {
                return;
            }

            Tile t = Main.tile[tile.X, tile.Y];
            if (!t.HasTile || t.TileType != TileID.PalmTree) return;

            Vector2 tileCenter = new Vector2(tile.X * 16 + 8, tile.Y * 16 + 8);
            if (Vector2.DistanceSquared(Player.Center, tileCenter) > PalmShake.ShakeReachPixels * PalmShake.ShakeReachPixels) return;

            PalmShake.RequestShakeFromClient(tile.X, tile.Y);
            shakeRepeatTimer = PalmShake.ShakeCooldownTicks;
            Main.mouseRightRelease = false;
        }

        /// <summary>复合手臂够树晃，本地视觉</summary>
        private void UpdateShakeAnimation() {
            if (shakeAnimTimer <= 0) return;

            int timer = shakeAnimTimer;
            shakeAnimTimer--;

            if (Main.dedServ || !Player.active || Player.dead) {
                shakeAnimTimer = 0;
                return;
            }
            if (Player.itemAnimation > 0) {
                shakeAnimTimer = 0;
                return;
            }

            Vector2 treeTop = new Vector2(shakeTreeTopX * 16 + 8, shakeTreeTopY * 16 + 8);
            Player.direction = treeTop.X >= Player.Center.X ? 1 : -1;

            //rotation 0=下垂，指向=方向角-π/2；WrapAngle 走最短路径
            float reach = MathHelper.WrapAngle((treeTop - Player.MountedCenter).ToRotation() - MathHelper.PiOver2);

            float t = 1f - timer / (float)ShakeAnimDuration;
            float lift = MathF.Sin(t * MathHelper.Pi);
            float wobble = MathF.Sin(t * MathHelper.TwoPi * 4f) * 0.42f * lift; //来回用力摇晃

            float front = reach * lift + wobble;
            float back = reach * lift + wobble * 0.6f;                   //后手略滞后，显得是双手发力

            Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, front);
            Player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, back);
        }
    }
}
