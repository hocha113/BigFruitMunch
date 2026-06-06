using BigFruitMunch.Content.Systems;
using BigFruitMunch.Content.TileProcessors;
using InnoVault.UIHandles;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;

namespace BigFruitMunch.Content.UIs
{
    internal class BigFruitCutterGachaUI : UIHandle
    {
        private enum GachaState
        {
            Idle,
            Rolling,
            RevealCards,
            ResultReady,
        }

        public static BigFruitCutterGachaUI Instance => UIHandleLoader.GetUIHandleOfType<BigFruitCutterGachaUI>();
        public const float MaxInteractionDistanceSquared = 30f * 16f * 30f * 16f;

        private const int PanelWidth = 860;
        private const int PanelHeight = 500;
        private const int RollTenTicks = 72;
        private const int RevealTicksPerCard = 8;

        private BigFruitCutterbarTPBase boundTP;
        private GachaState state;
        private int stateTimer;
        private BigFruitQuality[] displayResults = Array.Empty<BigFruitQuality>();
        private int revealedCards;

        private Rectangle closeButton;
        private Rectangle depositButton;
        private Rectangle withdrawButton;
        private Rectangle claimButton;
        private Rectangle rollTenButton;
        private Rectangle rollOneButton;
        private Rectangle leftPanel;
        private Rectangle stagePanel;
        private Rectangle resultPanel;
        private Rectangle stageCore;
        private Rectangle statusStrip;
        private bool suppressClickAfterDrag;

        public override LayersModeEnum LayersMode => LayersModeEnum.Vanilla_Mouse_Text;
        public override bool CloseOnEscape => true;
        public override bool AutoUpdateHitBox => true;
        public override bool BlockMouseWhenHovered => true;
        public override bool CanDrag => true;
        public override MouseButtonType DragMouseButton => MouseButtonType.Left;
        public override Rectangle? DragHandleRect => new Rectangle((int)DrawPosition.X + 12, (int)DrawPosition.Y + 8, PanelWidth - 74, 54);
        public override SoundStyle? OpenSound => SoundID.MenuOpen with { Volume = 0.65f };
        public override SoundStyle? CloseSound => SoundID.MenuClose with { Volume = 0.55f };

        public void Open(BigFruitCutterbarTPBase tp) {
            if (boundTP != null && boundTP != tp) {
                boundTP.CloseUI(Main.LocalPlayer);
            }

            boundTP = tp;
            state = GachaState.Idle;
            stateTimer = 0;
            revealedCards = BigFruitCutterbarTPBase.LastRollCount;
            displayResults = tp?.LastRollQualities ?? Array.Empty<BigFruitQuality>();

            Size = new Vector2(PanelWidth, PanelHeight);
            if (DrawPosition == Vector2.Zero)
                DrawPosition = new Vector2((Main.screenWidth - PanelWidth) * 0.5f, (Main.screenHeight - PanelHeight) * 0.5f);
            base.Open();
        }

        protected override void OnClose() {
            boundTP?.CloseUI(Main.LocalPlayer);
            boundTP = null;
            state = GachaState.Idle;
        }

        public override void Update() {
            if (boundTP == null || !boundTP.Active || Main.LocalPlayer == null || !Main.LocalPlayer.active || Main.LocalPlayer.dead) {
                Close();
                return;
            }

            if (Main.LocalPlayer.DistanceSQ(boundTP.Center) > MaxInteractionDistanceSquared) {
                Close();
                return;
            }

            if (Main.keyState.IsKeyDown(Keys.Escape)) {
                Close();
                Main.LocalPlayer.releaseInventory = false;
                return;
            }

            Size = new Vector2(PanelWidth, PanelHeight);
            UpdateLayout();
            UpdateState();

            if (keyLeftPressState == KeyPressState.Pressed) {
                HandleClick();
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (boundTP == null) {
                return;
            }

            float alpha = MathHelper.Clamp(OpenProgress.Current, 0f, 1f);
            UpdateLayout();
            DrawPanel(spriteBatch, alpha);
            DrawHeader(spriteBatch, alpha);
            DrawSectionPanels(spriteBatch, alpha);
            DrawStats(spriteBatch, alpha);
            DrawGachaStage(spriteBatch, alpha);
            DrawResultWall(spriteBatch, alpha);
            DrawButtons(spriteBatch, alpha);
        }

        protected override void OnDragStart() {
            suppressClickAfterDrag = true;
        }

        protected override void OnDragEnd() {
            suppressClickAfterDrag = true;
        }

        private void UpdateState() {
            if (state == GachaState.Idle || state == GachaState.ResultReady) {
                return;
            }

            stateTimer++;
            if (state == GachaState.Rolling && stateTimer >= RollTenTicks) {
                state = GachaState.RevealCards;
                stateTimer = 0;
                revealedCards = 1;
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.8f, Pitch = 0.25f });
            }
            else if (state == GachaState.RevealCards && stateTimer >= RevealTicksPerCard) {
                stateTimer = 0;
                if (revealedCards >= displayResults.Length) {
                    revealedCards = displayResults.Length;
                    state = GachaState.ResultReady;
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.7f });
                    AnnounceBestResult();
                }
                else {
                    revealedCards++;
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f, Pitch = revealedCards * 0.03f });
                }
            }
        }

        private void HandleClick() {
            if (closeButton.Contains(MousePosition.ToPoint())) {
                suppressClickAfterDrag = false;
                Close();
                return;
            }

            if (state is GachaState.Rolling or GachaState.RevealCards) {
                return;
            }

            Player player = Main.LocalPlayer;
            if (depositButton.Contains(MousePosition.ToPoint())) {
                suppressClickAfterDrag = false;
                int deposited = boundTP.DepositAllFrom(player);
                SoundEngine.PlaySound(deposited > 0 ? SoundID.Grab : SoundID.MenuTick);
                return;
            }

            if (withdrawButton.Contains(MousePosition.ToPoint())) {
                suppressClickAfterDrag = false;
                int withdrawn = boundTP.WithdrawAllTo(player);
                SoundEngine.PlaySound(withdrawn > 0 ? SoundID.Grab : SoundID.MenuTick);
                return;
            }

            if (claimButton.Contains(MousePosition.ToPoint())) {
                suppressClickAfterDrag = false;
                int claimed = boundTP.ClaimAllTo(player);
                SoundEngine.PlaySound(claimed > 0 ? SoundID.Grab : SoundID.MenuTick);
                return;
            }

            if (rollTenButton.Contains(MousePosition.ToPoint())) {
                suppressClickAfterDrag = false;
                StartRoll(BigFruitCutterbarTPBase.TenRollCost);
                return;
            }

            if (rollOneButton.Contains(MousePosition.ToPoint())) {
                suppressClickAfterDrag = false;
                StartRoll(1);
                return;
            }

            if (suppressClickAfterDrag) {
                suppressClickAfterDrag = false;
            }
        }

        private void StartRoll(int count) {
            if (!boundTP.TryRoll(count, out BigFruitQuality[] results)) {
                CombatText.NewText(Main.LocalPlayer.Hitbox, new Color(230, 180, 80),
                    Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.NotEnoughFruit"));
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }

            displayResults = results;
            state = GachaState.Rolling;
            stateTimer = 0;
            revealedCards = 0;
            SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.9f, Pitch = boundTP.IsUpgradedCutter ? 0.2f : -0.05f });
        }

        private void AnnounceBestResult() {
            if (displayResults.Length <= 0) {
                return;
            }

            BigFruitQuality best = BigFruitQuality.Withered;
            for (int i = 0; i < displayResults.Length; i++) {
                if ((int)displayResults[i] > (int)best) {
                    best = displayResults[i];
                }
            }

            if (best < BigFruitQuality.Epic) {
                return;
            }

            string itemName = Lang.GetItemNameValue(DecorticateBigFruitBase.GetTypeForQuality(best));
            string key = best == BigFruitQuality.Mythic
                ? "Mods.BigFruitMunch.UI.BigFruitCutterGacha.MythicLine"
                : "Mods.BigFruitMunch.UI.BigFruitCutterGacha.ResultLine";
            CombatText.NewText(Main.LocalPlayer.Hitbox, best.ToTint(), Language.GetTextValue(key, itemName));

            if (best == BigFruitQuality.Mythic) {
                Systems.MythicCelebrationSystem.Trigger(Main.LocalPlayer.Center);
            }
        }

        private void UpdateLayout() {
            UIHitBox = new Rectangle((int)DrawPosition.X, (int)DrawPosition.Y, PanelWidth, PanelHeight);
            closeButton = new Rectangle(UIHitBox.Right - 42, UIHitBox.Y + 14, 26, 26);
            leftPanel = new Rectangle(UIHitBox.X + 24, UIHitBox.Y + 82, 220, 356);
            stagePanel = new Rectangle(leftPanel.Right + 18, UIHitBox.Y + 82, 330, 356);
            resultPanel = new Rectangle(stagePanel.Right + 18, UIHitBox.Y + 82, UIHitBox.Right - stagePanel.Right - 42, 356);
            stageCore = new Rectangle(stagePanel.X + 48, stagePanel.Y + 48, 234, 220);
            statusStrip = new Rectangle(stagePanel.X + 26, stagePanel.Bottom - 62, stagePanel.Width - 52, 40);

            depositButton = new Rectangle(leftPanel.X + 18, leftPanel.Bottom - 132, leftPanel.Width - 36, 34);
            withdrawButton = new Rectangle(leftPanel.X + 18, depositButton.Bottom + 10, leftPanel.Width - 36, 34);
            claimButton = new Rectangle(leftPanel.X + 18, withdrawButton.Bottom + 10, leftPanel.Width - 36, 34);
            rollTenButton = new Rectangle(resultPanel.X + 18, resultPanel.Bottom - 54, resultPanel.Width - 36, 40);
            rollOneButton = new Rectangle(resultPanel.X + 18, rollTenButton.Y - 42, resultPanel.Width - 36, 30);
        }

        private void DrawPanel(SpriteBatch sb, float alpha) {
            Rectangle panel = UIHitBox;
            Color top = boundTP.IsUpgradedCutter ? new Color(42, 27, 64) : new Color(48, 29, 23);
            Color bottom = boundTP.IsUpgradedCutter ? new Color(18, 12, 32) : new Color(20, 12, 10);
            Color edge = boundTP.IsUpgradedCutter ? new Color(255, 150, 80) : new Color(196, 92, 46);
            float breathe = 0.5f + 0.5f * MathF.Sin(GlobalTimer * 1.4f);

            for (int i = 4; i >= 1; i--) {
                Rectangle shadow = panel;
                shadow.Offset(i * 2, i * 3);
                DrawRoundedRect(sb, shadow, Color.Black * (alpha * 0.045f * (5 - i)), 10 + i);
            }

            DrawGradientRoundedRect(sb, panel, top * (0.96f * alpha), bottom * (0.98f * alpha), 10);
            DrawInnerGlow(sb, panel, edge * (alpha * (0.035f + breathe * 0.025f)), 10, 18);
            DrawRoundedRectBorder(sb, panel, edge * (0.68f * alpha), 10, 2);

            Rectangle highlight = new Rectangle(panel.X + 28, panel.Y + 3, panel.Width - 56, 2);
            DrawHorizontalGradient(sb, highlight, Color.Transparent, Color.White * (0.16f * alpha), Color.Transparent);
            DrawCornerOrnaments(sb, panel, edge, alpha);

            Rectangle inner = new Rectangle(panel.X + 12, panel.Y + 12, panel.Width - 24, panel.Height - 24);
            DrawRoundedRectBorder(sb, inner, Color.White * (0.10f * alpha), 8, 1);
        }

        private void DrawSectionPanels(SpriteBatch sb, float alpha) {
            DrawGlassPanel(sb, leftPanel, new Color(80, 42, 28), alpha);
            DrawGlassPanel(sb, stagePanel, boundTP.IsUpgradedCutter ? new Color(70, 44, 94) : new Color(86, 45, 30), alpha);
            DrawGlassPanel(sb, resultPanel, new Color(48, 43, 38), alpha);

            Utils.DrawBorderString(sb, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.PanelInventory"),
                new Vector2(leftPanel.X + 16, leftPanel.Y + 14), new Color(255, 220, 150) * alpha, 0.72f);
            Utils.DrawBorderString(sb, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.PanelMachine"),
                new Vector2(stagePanel.X + 18, stagePanel.Y + 14), new Color(255, 220, 150) * alpha, 0.72f);
            Utils.DrawBorderString(sb, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.PanelResults"),
                new Vector2(resultPanel.X + 16, resultPanel.Y + 14), new Color(255, 220, 150) * alpha, 0.72f);

            DrawSectionDivider(sb, leftPanel, alpha);
            DrawSectionDivider(sb, stagePanel, alpha);
            DrawSectionDivider(sb, resultPanel, alpha);
        }

        private bool DrawShaderPanel(SpriteBatch sb, Rectangle panel, float alpha) {
            Effect effect = BigFruitCutterGachaShader.BigFruitCutterGachaPanel;
            if (effect == null) {
                return false;
            }

            effect.Parameters["uTime"]?.SetValue(GlobalTimer);
            effect.Parameters["uUpgraded"]?.SetValue(boundTP.IsUpgradedCutter ? 1f : 0f);
            effect.Parameters["uRolling"]?.SetValue(state == GachaState.Rolling ? 1f : 0f);
            effect.Parameters["uTexSize"]?.SetValue(new Vector2(panel.Width, panel.Height));

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, effect, Main.UIScaleMatrix);
            sb.Draw(pixel, panel, Color.White * (0.92f * alpha));
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
            return true;
        }

        private void DrawHeader(SpriteBatch sb, float alpha) {
            DynamicSpriteFont font = FontAssets.MouseText.Value;
            string title = Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.Title");
            string subtitle = boundTP.IsUpgradedCutter
                ? Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.SubtitleMk2")
                : Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.Subtitle");
            Utils.DrawBorderString(sb, title, DrawPosition + new Vector2(28, 18), Color.White * alpha, 1.05f);
            Utils.DrawBorderString(sb, subtitle, DrawPosition + new Vector2(30, 48), new Color(255, 210, 150) * alpha, 0.72f);
            DrawButton(sb, closeButton, "X", alpha, true);
        }

        private void DrawStats(SpriteBatch sb, float alpha) {
            Vector2 pos = new Vector2(leftPanel.X + 18, leftPanel.Y + 54);
            string stored = Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.StoredFruit", boundTP.StoredBigFruitCount);
            string pending = Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.PendingResult", boundTP.PendingTotal());
            DrawStatCard(sb, new Rectangle(leftPanel.X + 16, leftPanel.Y + 48, leftPanel.Width - 32, 54),
                stored, new Color(255, 226, 142), alpha);
            DrawStatCard(sb, new Rectangle(leftPanel.X + 16, leftPanel.Y + 112, leftPanel.Width - 32, 54),
                pending, new Color(160, 230, 255), alpha);

            string hint = boundTP.StoredBigFruitCount >= BigFruitCutterbarTPBase.TenRollCost
                ? Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.ReadyHint")
                : Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.NotEnoughFruit");
            Color hintColor = boundTP.StoredBigFruitCount >= BigFruitCutterbarTPBase.TenRollCost
                ? new Color(180, 255, 170)
                : new Color(255, 150, 110);
            Utils.DrawBorderString(sb, hint, new Vector2(leftPanel.X + 18, leftPanel.Y + 180), hintColor * alpha, 0.66f);
            if (boundTP.StoredBigFruitCount < BigFruitCutterbarTPBase.TenRollCost) {
                int missing = BigFruitCutterbarTPBase.TenRollCost - boundTP.StoredBigFruitCount;
                Utils.DrawBorderString(sb, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.MissingFruit", missing),
                    new Vector2(leftPanel.X + 18, leftPanel.Y + 202), new Color(255, 205, 130) * alpha, 0.58f);
            }
        }

        private void DrawGachaStage(SpriteBatch sb, float alpha) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 center = stageCore.Center.ToVector2();
            float charge = state == GachaState.Rolling
                ? MathHelper.Clamp(stateTimer / (float)RollTenTicks, 0f, 1f)
                : state == GachaState.RevealCards ? 1f : 0.15f + 0.08f * MathF.Sin(GlobalTimer * 2.2f);
            Color coreColor = boundTP.IsUpgradedCutter ? new Color(255, 186, 74) : new Color(230, 102, 44);

            if (!DrawShaderPanel(sb, stageCore, alpha)) {
                sb.Draw(pixel, stageCore, new Color(26, 15, 12) * (0.9f * alpha));
            }
            DrawBorder(sb, stageCore, coreColor * (0.38f * alpha), 1);

            Rectangle mouth = new Rectangle((int)center.X - 46, (int)center.Y - 18, 92, 36);
            sb.Draw(pixel, mouth, new Color(18, 11, 9) * (0.85f * alpha));
            DrawBorder(sb, mouth, new Color(255, 210, 128) * (0.6f * alpha), 1);

            string coreText = state == GachaState.Rolling
                ? Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.Rolling")
                : state == GachaState.RevealCards
                    ? Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.Revealing")
                    : Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.Core");
            Vector2 coreSize = FontAssets.MouseText.Value.MeasureString(coreText) * 0.72f;
            Utils.DrawBorderString(sb, coreText, new Vector2(mouth.Center.X, mouth.Center.Y) - coreSize * 0.5f,
                Color.White * alpha, 0.72f);

            DrawStatusStrip(sb, alpha, charge);
        }

        private void DrawResultWall(SpriteBatch sb, float alpha) {
            int columns = 2;
            int slotW = (resultPanel.Width - 48) / columns;
            int slotH = 34;
            int startX = resultPanel.X + 16;
            int startY = resultPanel.Y + 44;

            for (int i = 0; i < BigFruitCutterbarTPBase.LastRollCount; i++) {
                int col = i % columns;
                int row = i / columns;
                Rectangle slot = new Rectangle(startX + col * (slotW + 12), startY + row * (slotH + 6), slotW, slotH);
                bool visible = state != GachaState.Rolling && i < revealedCards && i < displayResults.Length;
                BigFruitQuality quality = visible ? displayResults[i] : BigFruitQuality.Withered;
                float revealT = visible ? RevealProgressFor(i) : 0f;
                DrawResultSlot(sb, slot, i, visible, quality, revealT, alpha);
            }
        }

        private void DrawButtons(SpriteBatch sb, float alpha) {
            bool locked = state is GachaState.Rolling or GachaState.RevealCards;
            DrawButton(sb, depositButton, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.DepositAll"), alpha, !locked, ButtonKind.Secondary);
            DrawButton(sb, withdrawButton, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.WithdrawAll"), alpha, !locked && boundTP.StoredBigFruitCount > 0, ButtonKind.Secondary);
            DrawButton(sb, claimButton, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.ClaimAll"), alpha, !locked && boundTP.PendingTotal() > 0, ButtonKind.Secondary);
            DrawButton(sb, rollOneButton, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.RollOne"), alpha, !locked && boundTP.StoredBigFruitCount >= 1, ButtonKind.Secondary);
            DrawButton(sb, rollTenButton, Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.RollTen"), alpha,
                !locked && boundTP.StoredBigFruitCount >= BigFruitCutterbarTPBase.TenRollCost, ButtonKind.Primary);
        }

        private enum ButtonKind
        {
            Secondary,
            Primary,
        }

        private void DrawButton(SpriteBatch sb, Rectangle rect, string text, float alpha, bool enabled, ButtonKind kind = ButtonKind.Secondary) {
            bool hover = enabled && rect.Contains(MousePosition.ToPoint());
            Rectangle drawRect = rect;
            if (hover) {
                drawRect.Inflate(kind == ButtonKind.Primary ? 2 : 1, 1);
            }

            Color top = kind == ButtonKind.Primary ? new Color(142, 58, 28) : new Color(66, 46, 38);
            Color bottom = kind == ButtonKind.Primary ? new Color(72, 28, 18) : new Color(28, 24, 22);
            Color border = kind == ButtonKind.Primary ? new Color(255, 188, 74) : new Color(201, 132, 74);
            Color glow = kind == ButtonKind.Primary ? new Color(255, 130, 44) : new Color(220, 150, 92);
            if (!enabled) {
                top = new Color(40, 38, 36);
                bottom = new Color(24, 23, 22);
                border = new Color(82, 82, 82);
                glow = Color.Gray;
            }
            if (hover) {
                top = Color.Lerp(top, glow, 0.28f);
                border = Color.Lerp(border, Color.White, 0.25f);
            }

            Rectangle shadow = drawRect;
            shadow.Offset(1, 2);
            DrawRoundedRect(sb, shadow, Color.Black * (0.22f * alpha), 5);
            DrawGradientRoundedRect(sb, drawRect, top * (0.92f * alpha), bottom * (0.94f * alpha), 5);
            DrawRoundedRectBorder(sb, drawRect, border * alpha, 5, kind == ButtonKind.Primary || hover ? 2 : 1);
            DrawInnerGlow(sb, drawRect, glow * ((hover ? 0.08f : 0.035f) * alpha), 5, 7);
            DrawHorizontalGradient(sb, new Rectangle(drawRect.X + 6, drawRect.Y + 2, drawRect.Width - 12, 1),
                Color.Transparent, Color.White * ((hover ? 0.18f : 0.08f) * alpha), Color.Transparent);

            float textScale = kind == ButtonKind.Primary ? 0.76f : 0.66f;
            Vector2 rawSize = FontAssets.MouseText.Value.MeasureString(text);
            textScale = rawSize.X > 0f ? Math.Min(textScale, (drawRect.Width - 16f) / rawSize.X) : textScale;
            Vector2 size = rawSize * textScale;
            Vector2 textPos = new Vector2(drawRect.Center.X, drawRect.Center.Y) - size * 0.5f + new Vector2(0f, 1f);
            Utils.DrawBorderString(sb, text, textPos + new Vector2(1f, 1f), Color.Black * (0.45f * alpha), textScale);
            Utils.DrawBorderString(sb, text, textPos, (enabled ? Color.White : Color.Gray) * alpha, textScale);
        }

        private static void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private static void DrawSectionDivider(SpriteBatch sb, Rectangle panel, float alpha) {
            Rectangle divider = new Rectangle(panel.X + 16, panel.Y + 38, panel.Width - 32, 2);
            DrawHorizontalGradient(sb, divider, Color.Transparent, new Color(255, 180, 96) * (0.30f * alpha), Color.Transparent);
        }

        private static void DrawRoundedRect(SpriteBatch sb, Rectangle rect, Color color, float radius) {
            if (rect.Width <= 0 || rect.Height <= 0 || color.A <= 0) {
                return;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int r = (int)Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f);
            if (r <= 1) {
                sb.Draw(pixel, rect, color);
                return;
            }

            sb.Draw(pixel, new Rectangle(rect.X + r, rect.Y, rect.Width - r * 2, rect.Height), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y + r, r, rect.Height - r * 2), color);
            sb.Draw(pixel, new Rectangle(rect.Right - r, rect.Y + r, r, rect.Height - r * 2), color);

            for (int i = 0; i < r; i++) {
                float t = i / (float)r;
                int cornerWidth = (int)(r * MathF.Sqrt(1f - (1f - t) * (1f - t)));
                if (cornerWidth <= 0) {
                    continue;
                }

                sb.Draw(pixel, new Rectangle(rect.X + r - cornerWidth, rect.Y + i, cornerWidth, 1), color);
                sb.Draw(pixel, new Rectangle(rect.Right - r, rect.Y + i, cornerWidth, 1), color);
                sb.Draw(pixel, new Rectangle(rect.X + r - cornerWidth, rect.Bottom - 1 - i, cornerWidth, 1), color);
                sb.Draw(pixel, new Rectangle(rect.Right - r, rect.Bottom - 1 - i, cornerWidth, 1), color);
            }
        }

        private static void DrawGradientRoundedRect(SpriteBatch sb, Rectangle rect, Color topColor, Color bottomColor, float radius) {
            if (rect.Width <= 0 || rect.Height <= 0) {
                return;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int r = (int)Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f);
            for (int i = 0; i < rect.Height; i++) {
                float t = rect.Height <= 1 ? 0f : i / (float)(rect.Height - 1);
                int inset = 0;
                if (r > 1 && i < r) {
                    float cornerT = i / (float)r;
                    inset = (int)(r * (1f - MathF.Sqrt(1f - (1f - cornerT) * (1f - cornerT))));
                }
                else if (r > 1 && i >= rect.Height - r) {
                    float cornerT = (rect.Height - 1 - i) / (float)r;
                    inset = (int)(r * (1f - MathF.Sqrt(1f - (1f - cornerT) * (1f - cornerT))));
                }

                int width = rect.Width - inset * 2;
                if (width > 0) {
                    sb.Draw(pixel, new Rectangle(rect.X + inset, rect.Y + i, width, 1), Color.Lerp(topColor, bottomColor, t));
                }
            }
        }

        private static void DrawRoundedRectBorder(SpriteBatch sb, Rectangle rect, Color color, float radius, int thickness) {
            if (rect.Width <= 0 || rect.Height <= 0 || thickness <= 0 || color.A <= 0) {
                return;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            int r = (int)Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f);
            if (r <= 1) {
                DrawBorder(sb, rect, color, thickness);
                return;
            }

            sb.Draw(pixel, new Rectangle(rect.X + r, rect.Y, rect.Width - r * 2, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X + r, rect.Bottom - thickness, rect.Width - r * 2, thickness), color);
            sb.Draw(pixel, new Rectangle(rect.X, rect.Y + r, thickness, rect.Height - r * 2), color);
            sb.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y + r, thickness, rect.Height - r * 2), color);

            DrawCornerArc(sb, new Vector2(rect.X + r, rect.Y + r), r, MathHelper.Pi, MathHelper.PiOver2, color, thickness);
            DrawCornerArc(sb, new Vector2(rect.Right - r, rect.Y + r), r, -MathHelper.PiOver2, MathHelper.PiOver2, color, thickness);
            DrawCornerArc(sb, new Vector2(rect.X + r, rect.Bottom - r), r, MathHelper.PiOver2, MathHelper.PiOver2, color, thickness);
            DrawCornerArc(sb, new Vector2(rect.Right - r, rect.Bottom - r), r, 0f, MathHelper.PiOver2, color, thickness);
        }

        private static void DrawCornerArc(SpriteBatch sb, Vector2 center, float radius, float startAngle, float sweep, Color color, int thickness) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle source = new Rectangle(0, 0, 1, 1);
            int segments = Math.Max(4, (int)(radius * sweep / 2f));
            for (int i = 0; i <= segments; i++) {
                float angle = startAngle + sweep * i / segments;
                Vector2 pos = center + angle.ToRotationVector2() * radius;
                sb.Draw(pixel, pos, source, color, 0f, new Vector2(0.5f), thickness, SpriteEffects.None, 0f);
            }
        }

        private static void DrawInnerGlow(SpriteBatch sb, Rectangle rect, Color color, float radius, int glowSize) {
            for (int i = 0; i < glowSize; i++) {
                Rectangle glowRect = rect;
                glowRect.Inflate(-i, -i);
                if (glowRect.Width <= 0 || glowRect.Height <= 0) {
                    break;
                }

                float t = i / (float)Math.Max(1, glowSize);
                DrawRoundedRectBorder(sb, glowRect, color * ((1f - t) * (1f - t)), Math.Max(0f, radius - i), 1);
            }
        }

        private static void DrawHorizontalGradient(SpriteBatch sb, Rectangle rect, Color left, Color center, Color right) {
            if (rect.Width <= 0 || rect.Height <= 0) {
                return;
            }

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            for (int i = 0; i < rect.Width; i++) {
                float t = rect.Width <= 1 ? 0f : i / (float)(rect.Width - 1);
                Color color = t < 0.5f
                    ? Color.Lerp(left, center, t * 2f)
                    : Color.Lerp(center, right, (t - 0.5f) * 2f);
                sb.Draw(pixel, new Rectangle(rect.X + i, rect.Y, 1, rect.Height), color);
            }
        }

        private static void DrawCornerOrnaments(SpriteBatch sb, Rectangle rect, Color color, float alpha) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Color ornament = color * (0.36f * alpha);
            int length = 24;
            int inset = 8;

            DrawCornerOrnament(sb, pixel, new Vector2(rect.X + inset, rect.Y + inset), 1, 1, length, ornament);
            DrawCornerOrnament(sb, pixel, new Vector2(rect.Right - inset, rect.Y + inset), -1, 1, length, ornament);
            DrawCornerOrnament(sb, pixel, new Vector2(rect.X + inset, rect.Bottom - inset), 1, -1, length, ornament);
            DrawCornerOrnament(sb, pixel, new Vector2(rect.Right - inset, rect.Bottom - inset), -1, -1, length, ornament);
        }

        private static void DrawCornerOrnament(SpriteBatch sb, Texture2D pixel, Vector2 corner, int xSign, int ySign, int length, Color color) {
            Rectangle source = new Rectangle(0, 0, 1, 1);
            int horizontalX = xSign > 0 ? (int)corner.X : (int)corner.X - length;
            int verticalY = ySign > 0 ? (int)corner.Y : (int)corner.Y - length;
            sb.Draw(pixel, new Rectangle(horizontalX, (int)corner.Y, length, 2), color);
            sb.Draw(pixel, new Rectangle((int)corner.X, verticalY, 2, length), color);
            sb.Draw(pixel, corner, source, color * 0.75f, MathHelper.PiOver4, new Vector2(0.5f), new Vector2(6f, 6f), SpriteEffects.None, 0f);
        }

        private static void DrawGlassPanel(SpriteBatch sb, Rectangle rect, Color tint, float alpha) {
            Rectangle shadow = rect;
            shadow.Offset(2, 3);
            DrawRoundedRect(sb, shadow, Color.Black * (0.20f * alpha), 7);
            DrawGradientRoundedRect(sb, rect, Color.Lerp(new Color(26, 20, 17), tint, 0.16f) * (0.82f * alpha),
                new Color(14, 12, 11) * (0.86f * alpha), 7);
            DrawInnerGlow(sb, rect, tint * (0.045f * alpha), 7, 10);
            DrawRoundedRectBorder(sb, rect, tint * (0.62f * alpha), 7, 1);
            DrawRoundedRectBorder(sb, new Rectangle(rect.X + 5, rect.Y + 5, rect.Width - 10, rect.Height - 10),
                Color.White * (0.055f * alpha), 5, 1);
            DrawHorizontalGradient(sb, new Rectangle(rect.X + 12, rect.Y + 3, rect.Width - 24, 1),
                Color.Transparent, Color.White * (0.10f * alpha), Color.Transparent);
        }

        private static void DrawStatCard(SpriteBatch sb, Rectangle rect, string text, Color color, float alpha) {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            DrawGradientRoundedRect(sb, rect, new Color(28, 23, 20) * (0.78f * alpha), new Color(12, 11, 10) * (0.78f * alpha), 5);
            DrawRoundedRectBorder(sb, rect, color * (0.42f * alpha), 5, 1);
            DrawRoundedRect(sb, new Rectangle(rect.X, rect.Y, 5, rect.Height), color * (0.82f * alpha), 3);
            sb.Draw(pixel, new Rectangle(rect.X + 6, rect.Y + 2, rect.Width - 12, 1), Color.White * (0.08f * alpha));
            Utils.DrawBorderString(sb, text, new Vector2(rect.X + 15, rect.Y + 16), color * alpha, 0.72f);
        }

        private void DrawStatusStrip(SpriteBatch sb, float alpha, float charge) {
            Color fill = boundTP.IsUpgradedCutter ? new Color(255, 176, 72) : new Color(232, 102, 44);
            DrawGradientRoundedRect(sb, statusStrip, new Color(28, 18, 15) * (0.82f * alpha), new Color(10, 8, 8) * (0.86f * alpha), 5);
            DrawRoundedRectBorder(sb, statusStrip, fill * (0.44f * alpha), 5, 1);
            DrawInnerGlow(sb, statusStrip, fill * (0.035f * alpha), 5, 6);

            int progressWidth = (int)((statusStrip.Width - 8) * MathHelper.Clamp(charge, 0f, 1f));
            if (progressWidth > 0) {
                Rectangle progress = new Rectangle(statusStrip.X + 4, statusStrip.Y + 4, progressWidth, statusStrip.Height - 8);
                DrawGradientRoundedRect(sb, progress, fill * (0.48f * alpha), new Color(92, 36, 20) * (0.42f * alpha), 3);
                DrawHorizontalGradient(sb, new Rectangle(progress.X + 3, progress.Y + 2, Math.Max(1, progress.Width - 6), 1),
                    Color.Transparent, Color.White * (0.16f * alpha), Color.Transparent);
            }

            string status = state switch {
                GachaState.Rolling => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.StatusRolling"),
                GachaState.RevealCards => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.StatusReveal", Math.Min(revealedCards + 1, displayResults.Length), displayResults.Length),
                GachaState.ResultReady => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.StatusReady"),
                _ => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.StatusIdle"),
            };
            Vector2 size = FontAssets.MouseText.Value.MeasureString(status) * 0.62f;
            Utils.DrawBorderString(sb, status, new Vector2(statusStrip.Center.X, statusStrip.Center.Y) - size * 0.5f,
                Color.White * alpha, 0.62f);
        }

        private void DrawResultSlot(SpriteBatch sb, Rectangle slot, int index, bool visible, BigFruitQuality quality, float revealT, float alpha) {
            Color color = visible ? quality.ToTint() : new Color(78, 70, 64);
            float flip = visible ? MathF.Sin(MathHelper.Clamp(revealT, 0f, 1f) * MathHelper.PiOver2) : 0f;
            int drawWidth = visible ? Math.Max(8, (int)(slot.Width * MathHelper.Lerp(0.20f, 1f, flip))) : slot.Width;
            Rectangle drawRect = new Rectangle(slot.Center.X - drawWidth / 2, slot.Y, drawWidth, slot.Height);
            bool high = visible && (int)quality >= (int)BigFruitQuality.Epic;

            Color top = visible ? Color.Lerp(new Color(34, 29, 25), color, 0.18f) : new Color(38, 35, 32);
            Color bottom = visible ? new Color(14, 12, 11) : new Color(21, 20, 19);
            DrawGradientRoundedRect(sb, drawRect, top * ((visible ? 0.76f : 0.72f) * alpha), bottom * (0.78f * alpha), 4);
            if (high) {
                Rectangle glow = new Rectangle(drawRect.X - 3, drawRect.Y - 3, drawRect.Width + 6, drawRect.Height + 6);
                DrawRoundedRect(sb, glow, color * ((0.07f + 0.05f * MathF.Sin(GlobalTimer * 6f + index)) * alpha), 5);
            }
            DrawRoundedRectBorder(sb, drawRect, color * ((visible ? 0.72f : 0.30f) * alpha), 4, high ? 2 : 1);
            DrawHorizontalGradient(sb, new Rectangle(drawRect.X + 5, drawRect.Y + 2, Math.Max(1, drawRect.Width - 10), 1),
                Color.Transparent, Color.White * ((visible ? 0.12f : 0.055f) * alpha), Color.Transparent);

            if (visible) {
                DrawResultItemIcon(sb, drawRect, quality, alpha);

                string label = QualityShortName(quality);
                float scale = label.Length <= 1 ? 0.58f : 0.48f;
                Vector2 labelSize = FontAssets.MouseText.Value.MeasureString(label) * scale;
                Vector2 labelPos = new Vector2(drawRect.Right - labelSize.X - 6, drawRect.Bottom - labelSize.Y - 3);
                Rectangle labelPill = new Rectangle((int)labelPos.X - 3, (int)labelPos.Y + 1, (int)labelSize.X + 7, (int)labelSize.Y + 2);
                DrawRoundedRect(sb, labelPill, Color.Black * (0.35f * alpha), 3);
                Utils.DrawBorderString(sb, label, labelPos, Color.White * alpha, scale);
                return;
            }

            string indexLabel = (index + 1).ToString("00");
            Vector2 indexSize = FontAssets.MouseText.Value.MeasureString(indexLabel) * 0.58f;
            Utils.DrawBorderString(sb, indexLabel, new Vector2(drawRect.Center.X, drawRect.Center.Y) - indexSize * 0.5f,
                new Color(160, 145, 130) * alpha, 0.58f);
        }

        private static void DrawResultItemIcon(SpriteBatch sb, Rectangle rect, BigFruitQuality quality, float alpha) {
            int itemType = DecorticateBigFruitBase.GetTypeForQuality(quality);
            if (itemType <= 0) {
                return;
            }

            Texture2D tex = TextureAssets.Item[itemType].Value;
            Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height);
            Vector2 origin = new Vector2(frame.Width, frame.Height) * 0.5f;
            float availableWidth = Math.Max(8f, rect.Width - 18f);
            float scale = Math.Min((rect.Height - 6f) / frame.Height, availableWidth / frame.Width) * 0.95f;
            if (scale <= 0f) {
                return;
            }
            Vector2 position = rect.Center.ToVector2() + new Vector2(-6f, -1f);
            Color drawColor = Color.White * alpha;

            if (BigFruitQualityShader.DrawIconWithFilter(sb, tex, position, frame, drawColor, origin, scale, quality)) {
                return;
            }

            sb.Draw(tex, position, frame, quality.ToTint() * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        private float RevealProgressFor(int index) {
            if (state != GachaState.RevealCards || index < revealedCards - 1) {
                return 1f;
            }

            if (state == GachaState.RevealCards && index == revealedCards - 1) {
                return MathHelper.Clamp(stateTimer / (float)RevealTicksPerCard, 0f, 1f);
            }

            return 0f;
        }

        private static string QualityShortName(BigFruitQuality quality) => quality switch {
            BigFruitQuality.Withered => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.QualityShort.Withered"),
            BigFruitQuality.Common => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.QualityShort.Common"),
            BigFruitQuality.Excellent => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.QualityShort.Excellent"),
            BigFruitQuality.Rare => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.QualityShort.Rare"),
            BigFruitQuality.Epic => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.QualityShort.Epic"),
            BigFruitQuality.Legendary => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.QualityShort.Legendary"),
            BigFruitQuality.Mythic => Language.GetTextValue("Mods.BigFruitMunch.UI.BigFruitCutterGacha.QualityShort.Mythic"),
            _ => "?",
        };
    }
}
