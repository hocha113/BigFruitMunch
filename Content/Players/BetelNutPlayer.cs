using BigFruitMunch.Content.Buffs;
using BigFruitMunch.Content.PRT;
using InnoVault.PRT;
using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace BigFruitMunch.Content.Players
{
    /// <summary>嚼食随机土味吉祥话</summary>
    public enum BlessingType
    {
        None = 0,
        Wealth = 1,
        Guard = 2,
        Enlighten = 3,
    }

    /// <summary>成瘾/戒断/上头/嚼劲都在这，也喂屏幕 shader</summary>
    public class BetelNutPlayer : ModPlayer
    {
        /// <summary>存盘</summary>
        public int AddictionCount;

        /// <summary>距上次嚼食，存盘（登出绕不过）</summary>
        public int WithdrawalTicks;

        public int CravingLevel;

        /// <summary>刚嚼完那口的屏幕闪，喂上头 shader</summary>
        public int RecentChewFlashTicks;
        public const int ChewFlashMaxTicks = 90;

        #region 上头 / 嚼劲（正反馈状态机）

        /// <summary>上头窗口剩余，不存盘</summary>
        public int HighTicks;

        /// <summary>当前上头品质，不存盘</summary>
        public BigFruitQuality HighFlavor;

        /// <summary>连段层数，命中叠停手掉</summary>
        public int ChewComboLayers;

        public int ComboGraceTicks;

        private int comboDecayCounter;

        /// <summary>神话嚼下后的缓降残影计时</summary>
        public int AscensionTicks;

        public const int MaxComboLayers = 12;

        private const int ComboHitGraceTicks = 180;

        private const int ComboDecayIntervalTicks = 24;

        private static readonly float[] HighDamage = { 0f, 0.02f, 0.05f, 0.08f, 0.12f, 0.16f, 0.22f };
        private static readonly float[] HighMove = { 0f, 0.01f, 0.03f, 0.05f, 0.07f, 0.09f, 0.12f };
        private static readonly float[] HighCrit = { 0f, 0f, 0f, 3f, 5f, 8f, 12f };

        private const float ComboDamagePerLayer = 0.015f;
        private const float ComboMovePerLayer = 0.005f;
        private const float ComboCritPerLayer = 1f;

        #endregion

        #region 探险家形态（上头解锁的机动 / 透视 / 寻宝）

        //机动·灵动冲刺（双击方向触发）
        private int dashCooldown;
        private int dashTapTimer;
        private int dashTapDir;
        private bool prevControlLeft;
        private bool prevControlRight;
        private const int DashCooldownTicks = 48;
        private const float DashSpeed = 9.5f;

        //机动·滑翔 / 飞行
        private const float GlideMaxFall = 2.2f;
        private const float FlyAccel = 0.45f;
        private const float FlyMaxRise = 7.5f;

        public bool IsHigh => HighTicks > 0;

        private int HighRank => IsHigh ? (int)HighFlavor : 0;

        public bool CanGlide => HighRank >= (int)BigFruitQuality.Common;
        public bool CanSpelunk => HighRank >= (int)BigFruitQuality.Excellent;
        public bool CanDash => HighRank >= (int)BigFruitQuality.Rare;
        public bool CanDoubleJump => HighRank >= (int)BigFruitQuality.Epic;
        public bool CanCompass => HighRank >= (int)BigFruitQuality.Legendary;
        public bool CanFly => HighRank >= (int)BigFruitQuality.Mythic;

        private float ComboBoost => MaxComboLayers <= 0 ? 0f : (float)ChewComboLayers / MaxComboLayers;
        internal float BigFruitAirJumpDurationMultiplier => 1.55f + 0.3f * ComboBoost;
        internal bool HasBigFruitAirJump => IsHigh && CanDoubleJump && !CanFly;
        internal bool CanUseBigFruitAirJump => HasBigFruitAirJump && !IsExplorerMobilityBusy;

        #endregion

        #region 玄学吉祥话（随机土味增益）

        public BlessingType CurrentBlessing;

        public int BlessingTicks;

        private const float BlessingChance = 0.40f;

        private const int BlessingVariants = 3;

        #endregion

        private const int OneMinuteTicks = 60 * 60;

        /// <summary>L0~L5 阈值：不嚼 1 分钟起戒断，之后每分钟升一级</summary>
        private static readonly int[] WithdrawalBaseTicks =
        {
            0,
            OneMinuteTicks,
            OneMinuteTicks * 2,
            OneMinuteTicks * 3,
            OneMinuteTicks * 4,
            OneMinuteTicks * 5,
        };

        private const int DecayStartTicks = OneMinuteTicks;
        private const int DecayPerTicks = OneMinuteTicks;

        /// <summary>L5 累计时长，够久会幻象嚼一口</summary>
        private int extremeWithdrawalTicks;

        private const int HallucinationLines = 5;

        public override void SaveData(TagCompound tag) {
            tag["AddictionCount"] = AddictionCount;
            tag["WithdrawalTicks"] = WithdrawalTicks;
        }

        public override void LoadData(TagCompound tag) {
            AddictionCount = tag.GetInt("AddictionCount");
            WithdrawalTicks = tag.GetInt("WithdrawalTicks");
        }

        public override void ResetEffects() {
            if (HasBigFruitAirJump) {
                Player.GetJumpState<BigFruitAirJump>().Enable();
            }
        }

        public override void OnEnterWorld() {
            CravingLevel = ComputeCravingLevel();
        }

        /// <summary>嚼大果总入口</summary>
        public void Chew(BigFruitQuality quality, int highDurationTicks) {
            OnChew(quality.ToAddictionGain());
            BeginHigh(quality, highDurationTicks);
            SpawnChewJuice(quality);
            TryBlessing();
            AnnounceExplorerUnlock();

            if (quality == BigFruitQuality.Withered) {
                TryStuckTeeth();
            }
            else if (quality == BigFruitQuality.Mythic && Player.whoAmI == Main.myPlayer) {
                Systems.MythicCelebrationSystem.Trigger(Player.Center);
            }
        }

        /// <summary>播报刚解锁的探险能力</summary>
        private void AnnounceExplorerUnlock() {
            if (Player.whoAmI != Main.myPlayer || !IsHigh) {
                return;
            }
            string key =
                CanFly ? "Fly" :
                CanCompass ? "Compass" :
                CanDoubleJump ? "DoubleJump" :
                CanDash ? "Dash" :
                CanSpelunk ? "Spelunk" :
                CanGlide ? "Glide" : null;
            if (key == null) {
                return;
            }
            string text = Language.GetTextValue($"Mods.BigFruitMunch.Common.Unlock.{key}");
            CombatText.NewText(Player.Hitbox, HighFlavor.ToTint(), text, true);
        }

        private void SpawnChewJuice(BigFruitQuality quality) {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer) {
                return;
            }
            Color col = quality.ToTint();
            Vector2 mouth = Player.MountedCenter + new Vector2(Player.direction * 8f, -6f);
            int count = quality == BigFruitQuality.Withered ? 4 : 9;
            for (int i = 0; i < count; i++) {
                Vector2 vel = new Vector2(
                    Player.direction * Main.rand.NextFloat(0.4f, 3f),
                    Main.rand.NextFloat(-3.2f, -0.4f));
                PRTLoader.NewParticle<ChewJuicePRT>(mouth, vel, col, Main.rand.NextFloat(0.32f, 0.72f));
            }
        }

        private void TryBlessing() {
            if (Player.whoAmI != Main.myPlayer) {
                return;
            }
            if (Main.rand.NextFloat() >= BlessingChance) {
                return;
            }

            BlessingType type = (BlessingType)Main.rand.Next(1, 4);
            CurrentBlessing = type;
            BlessingTicks = 60 * 30;

            int variant = Main.rand.Next(1, 1 + BlessingVariants);
            string text = Language.GetTextValue($"Mods.BigFruitMunch.Common.Blessing.{type}.Line{variant}");
            CombatText.NewText(Player.Hitbox, BlessingColor(type), text);
        }

        private void ApplyBlessing() {
            if (BlessingTicks <= 0) {
                return;
            }
            switch (CurrentBlessing) {
                case BlessingType.Wealth:
                    Player.moveSpeed += 0.06f;
                    break;
                case BlessingType.Guard:
                    Player.statDefense += 12;
                    break;
                case BlessingType.Enlighten:
                    Player.lifeRegen += 6;
                    Player.endurance += 0.08f;
                    break;
            }
        }

        private void UpdateBlessing() {
            if (BlessingTicks <= 0) {
                return;
            }
            BlessingTicks--;
            if (BlessingTicks <= 0) {
                CurrentBlessing = BlessingType.None;
                return;
            }
            if (CurrentBlessing == BlessingType.Wealth && Player.whoAmI == Main.myPlayer) {
                AttractCoins();
            }
        }

        private void AttractCoins() {
            const float range = 260f;
            Vector2 center = Player.Center;
            for (int i = 0; i < Main.maxItems; i++) {
                Item it = Main.item[i];
                if (it == null || !it.active || it.IsAir) {
                    continue;
                }
                if (it.type < ItemID.CopperCoin || it.type > ItemID.PlatinumCoin) {
                    continue;
                }
                Vector2 diff = center - it.Center;
                float dist = diff.Length();
                if (dist > 16f && dist < range) {
                    it.velocity = Vector2.Lerp(it.velocity, Vector2.Normalize(diff) * 9f, 0.25f);
                }
            }
        }

        private static Color BlessingColor(BlessingType type) => type switch {
            BlessingType.Wealth => new Color(255, 215, 80),
            BlessingType.Guard => new Color(140, 200, 255),
            BlessingType.Enlighten => new Color(180, 255, 180),
            _ => Color.White,
        };

        /// <summary>高戒断本地 PRT 幻觉，不同步</summary>
        private void UpdateHallucination() {
            if (Main.dedServ || Player.whoAmI != Main.myPlayer) {
                return;
            }
            if (CravingLevel < 3) {
                return;
            }

            int interval = CravingLevel >= 5 ? 90 : (CravingLevel >= 4 ? 150 : 240);
            if (WithdrawalTicks % interval != 0) {
                return;
            }

            Vector2 around = Player.Center + new Vector2(
                Main.rand.NextFloat(-700f, 700f), Main.rand.NextFloat(-360f, 360f));

            if (Main.rand.NextBool()) {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-0.3f, 0.1f));
                PRTLoader.NewParticle<HallucinationFruitPRT>(around, vel,
                    new Color(180, 90, 160), Main.rand.NextFloat(0.6f, 1.1f));
            }
            else {
                int n = Main.rand.Next(1, 1 + HallucinationLines);
                string text = Language.GetTextValue($"Mods.BigFruitMunch.Common.Hallucination.Line{n}");
                Vector2 vel = new Vector2(0f, Main.rand.NextFloat(-0.5f, -0.1f));
                var prt = PRTLoader.NewParticle<HallucinationTextPRT>(around, vel,
                    new Color(170, 130, 190), Main.rand.NextFloat(0.85f, 1.1f));
                prt.Text = text;
            }
        }

        /// <summary>L5 撑太久幻象嚼一口</summary>
        private void UpdateBreakdown() {
            if (Player.whoAmI != Main.myPlayer) {
                return;
            }

            if (CravingLevel >= 5) {
                extremeWithdrawalTicks++;
                if (extremeWithdrawalTicks >= 60 * 20) {
                    extremeWithdrawalTicks = 0;
                    WithdrawalTicks = WithdrawalBaseTicks[1];
                    RecentChewFlashTicks = ChewFlashMaxTicks;
                    if (!Main.dedServ) {
                        CombatText.NewText(Player.Hitbox, new Color(200, 120, 200),
                            Language.GetTextValue("Mods.BigFruitMunch.Common.PhantomChew"));
                    }
                }
            }
            else {
                extremeWithdrawalTicks = 0;
            }
        }

        /// <summary>成瘾侧，干瘪传 0 也重置戒断计时</summary>
        public void OnChew(int addictionGain) {
            if (addictionGain > 0) {
                AddictionCount += addictionGain;
            }
            WithdrawalTicks = 0;
            RecentChewFlashTicks = ChewFlashMaxTicks;

            if (addictionGain > 0 && Player.whoAmI == Main.myPlayer) {
                byte r = (byte)Math.Min(255, 180 + AddictionCount * 2);
                byte gb = (byte)Math.Max(40, 200 - AddictionCount * 3);
                string text = Language.GetTextValue("Mods.BigFruitMunch.Common.AddictionGain", addictionGain);
                CombatText.NewText(Player.Hitbox, new Color(r, gb, gb), text);
            }
            else if (addictionGain <= 0 && Player.whoAmI == Main.myPlayer) {
                string text = Language.GetTextValue("Mods.BigFruitMunch.Common.PlaceboChew");
                CombatText.NewText(Player.Hitbox, new Color(180, 180, 180), text);
            }
        }

        /// <summary>上头侧，干瘪直接 return</summary>
        public void BeginHigh(BigFruitQuality quality, int durationTicks) {
            if (quality == BigFruitQuality.Withered || durationTicks <= 0) {
                return;
            }

            if (HighTicks <= 0 || (int)quality >= (int)HighFlavor) {
                HighFlavor = quality;
            }
            HighTicks = Math.Max(HighTicks, durationTicks);

            if (ChewComboLayers < MaxComboLayers) {
                ChewComboLayers++;
            }
            ComboGraceTicks = ComboHitGraceTicks;
            comboDecayCounter = 0;

            if (quality.ToFlavor() == BigFruitFlavor.Ascension) {
                AscensionTicks = 180;
            }
        }

        private void TryStuckTeeth() {
            if (Player.whoAmI != Main.myPlayer) {
                return;
            }
            if (Main.rand.NextFloat() < 0.25f) {
                Player.AddBuff(BuffID.Slow, 120);
                CombatText.NewText(Player.Hitbox, new Color(150, 150, 150),
                    Language.GetTextValue("Mods.BigFruitMunch.Common.StuckTeeth"));
            }
        }

        private void GainComboFromHit() {
            if (HighTicks <= 0) {
                return;
            }
            if (ChewComboLayers < MaxComboLayers) {
                ChewComboLayers++;
            }
            ComboGraceTicks = ComboHitGraceTicks;
            comboDecayCounter = 0;
        }

        private void OnFlavorHit(NPC target, bool crit) {
            if (HighTicks <= 0 || Main.dedServ) {
                return;
            }

            switch (HighFlavor.ToFlavor()) {
                case BigFruitFlavor.LotusMouth:
                    for (int i = 0; i < 6; i++) {
                        Dust d = Dust.NewDustDirect(target.position, target.width, target.height,
                            DustID.Blood, 0f, 0f, 80, default, 1.1f);
                        d.velocity = d.velocity * 0.6f + Player.DirectionTo(target.Center) * 2f;
                        d.noGravity = false;
                    }
                    break;

                case BigFruitFlavor.GoldenMouth when crit:
                    for (int i = 0; i < 8; i++) {
                        Dust d = Dust.NewDustDirect(target.position, target.width, target.height,
                            DustID.GoldFlame, 0f, 0f, 120, default, 1.3f);
                        d.noGravity = true;
                        d.velocity *= 1.4f;
                    }
                    break;
            }
        }

        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
            GainComboFromHit();
            OnFlavorHit(target, hit.Crit);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
            GainComboFromHit();
            OnFlavorHit(target, hit.Crit);
        }

        public override void PostUpdate() {
            WithdrawalTicks++;

            if (HighTicks > 0) {
                HighTicks--;

                if (ComboGraceTicks > 0) {
                    ComboGraceTicks--;
                }
                else if (ChewComboLayers > 0) {
                    if (++comboDecayCounter >= ComboDecayIntervalTicks) {
                        comboDecayCounter = 0;
                        ChewComboLayers--;
                    }
                }

                if (HighTicks <= 0) {
                    HighTicks = 0;
                    ChewComboLayers = 0;
                    ComboGraceTicks = 0;
                    comboDecayCounter = 0;
                    HighFlavor = BigFruitQuality.Withered;
                }
            }

            UpdateAscension();
            UpdateTreasureHunter();
            UpdateBlessing();

            if (AddictionCount > 0 && WithdrawalTicks > DecayStartTicks
                && ((WithdrawalTicks - DecayStartTicks) % DecayPerTicks) == 0) {
                AddictionCount = Math.Max(0, AddictionCount - 1);
            }

            CravingLevel = ComputeCravingLevel();

            UpdateHallucination();
            UpdateBreakdown();

            if (Player.whoAmI == Main.myPlayer && CravingLevel is 1 or 2 && WithdrawalTicks % 1800 == 0) {
                string text = CravingLevel == 1
                    ? Language.GetTextValue("Mods.BigFruitMunch.Common.WithdrawalNag.Level1")
                    : Language.GetTextValue("Mods.BigFruitMunch.Common.WithdrawalNag.Level2");
                CombatText.NewText(Player.Hitbox, new Color(220, 170, 90), text);
            }

            if (RecentChewFlashTicks > 0) {
                RecentChewFlashTicks--;
            }
        }

        public override void PreUpdateMovement() {
            UpdateExplorerMobility();
        }

        /// <summary>上头机动，只改本地 velocity</summary>
        private void UpdateExplorerMobility() {
            if (Player.whoAmI != Main.myPlayer) {
                return;
            }

            bool busy = IsExplorerMobilityBusy;

            if (IsHigh && !busy) {
                if (CanFly && Player.controlJump) {
                    Player.velocity.Y -= FlyAccel;
                    if (Player.velocity.Y < -FlyMaxRise) {
                        Player.velocity.Y = -FlyMaxRise;
                    }
                    Player.fallStart = (int)(Player.position.Y / 16f);
                    SpawnGlideDust(true);
                }
                else if (CanGlide && Player.controlJump && Player.velocity.Y > 0f) {
                    float maxFall = GlideMaxFall - 0.8f * ComboBoost;
                    Player.velocity.Y *= 0.55f;
                    if (Player.velocity.Y > maxFall) {
                        Player.velocity.Y = maxFall;
                    }
                    Player.fallStart = (int)(Player.position.Y / 16f);
                    if (Main.rand.NextBool(3)) {
                        SpawnGlideDust(false);
                    }
                }
            }

            UpdateDash(busy);

            prevControlLeft = Player.controlLeft;
            prevControlRight = Player.controlRight;
        }

        private bool IsExplorerMobilityBusy => Player.pulley || Player.mount.Active || Player.tongued
            || Player.grapCount > 0 || Player.gravDir != 1f;

        internal void SpawnBigFruitAirJumpDust() {
            Player.fallStart = (int)(Player.position.Y / 16f);
            if (Main.dedServ) {
                return;
            }

            for (int i = 0; i < 10; i++) {
                Dust d = Dust.NewDustDirect(
                    new Vector2(Player.position.X, Player.position.Y + Player.height - 6f),
                    Player.width, 8, DustID.Cloud, 0f, 1.5f, 120, HighFlavor.ToTint(), 1.2f);
                d.noGravity = true;
                d.velocity *= 0.5f;
            }
        }

        /// <summary>双击方向冲刺</summary>
        private void UpdateDash(bool busy) {
            if (dashCooldown > 0) {
                dashCooldown--;
            }
            if (dashTapTimer > 0) {
                dashTapTimer--;
            }

            if (!IsHigh || !CanDash || busy) {
                return;
            }

            bool rightPressed = Player.controlRight && !prevControlRight;
            bool leftPressed = Player.controlLeft && !prevControlLeft;

            int dir = 0;
            if (rightPressed) {
                if (dashTapTimer > 0 && dashTapDir == 1) {
                    dir = 1;
                }
                else {
                    dashTapTimer = 13;
                    dashTapDir = 1;
                }
            }
            else if (leftPressed) {
                if (dashTapTimer > 0 && dashTapDir == -1) {
                    dir = -1;
                }
                else {
                    dashTapTimer = 13;
                    dashTapDir = -1;
                }
            }

            if (dir != 0 && dashCooldown <= 0) {
                Player.velocity.X = dir * (DashSpeed + 2.5f * ComboBoost);
                if (Player.velocity.Y >= 0f) {
                    Player.velocity.Y = -2.2f;
                }
                Player.immune = true;
                Player.immuneNoBlink = true;
                Player.immuneTime = Math.Max(Player.immuneTime, 18);
                dashCooldown = Math.Max(20, DashCooldownTicks - (int)(18 * ComboBoost));
                dashTapTimer = 0;

                if (!Main.dedServ) {
                    for (int i = 0; i < 12; i++) {
                        Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height,
                            DustID.Smoke, -dir * 2f, 0f, 120, HighFlavor.ToTint(), 1.1f);
                        d.noGravity = true;
                    }
                }
            }
        }

        private void SpawnGlideDust(bool flying) {
            if (Main.dedServ) {
                return;
            }
            Dust d = Dust.NewDustDirect(
                new Vector2(Player.position.X, Player.position.Y + Player.height - 4f),
                Player.width, 6, flying ? DustID.GoldFlame : DustID.Cloud,
                0f, 1f, 120, HighFlavor.ToTint(), flying ? 1.3f : 1f);
            d.noGravity = true;
            d.velocity *= 0.4f;
        }

        private void UpdateAscension() {
            if (AscensionTicks <= 0) {
                return;
            }
            AscensionTicks--;

            if (Player.whoAmI != Main.myPlayer) {
                return;
            }

            if (Player.velocity.Y > 1.5f) {
                Player.velocity.Y *= 0.85f;
            }

            if (!Main.dedServ && Main.rand.NextBool(2)) {
                Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height,
                    DustID.GoldFlame, 0f, 0f, 100, default, 1.2f);
                d.noGravity = true;
                d.velocity *= 0.3f;
            }
        }

        /// <summary>传奇+磁吸掉落，范围看嚼劲</summary>
        private void UpdateTreasureHunter() {
            if (!IsHigh || !CanCompass || Player.whoAmI != Main.myPlayer) {
                return;
            }

            float range = 360f + 360f * ComboBoost;
            Vector2 center = Player.Center;
            for (int i = 0; i < Main.maxItems; i++) {
                Item it = Main.item[i];
                if (it == null || !it.active || it.IsAir) {
                    continue;
                }
                Vector2 diff = center - it.Center;
                float dist = diff.Length();
                if (dist > 16f && dist < range) {
                    it.velocity = Vector2.Lerp(it.velocity, Vector2.Normalize(diff) * 9f, 0.2f);
                }
            }
        }

        public override void PostUpdateRunSpeeds() {
            if (HighTicks > 0 && HighFlavor.ToFlavor() == BigFruitFlavor.Floaty) {
                Player.runSlowdown *= 0.25f;
            }
        }

        public override void PostUpdateEquips() {
            ApplyBlessing();
            ApplyExplorerVision();

            if (HighTicks <= 0) {
                return;
            }

            int fi = (int)HighFlavor;
            if (fi < 0 || fi >= HighDamage.Length) {
                return;
            }

            Player.GetDamage(DamageClass.Generic) += HighDamage[fi] + ChewComboLayers * ComboDamagePerLayer;
            Player.moveSpeed += HighMove[fi] + ChewComboLayers * ComboMovePerLayer;
            Player.GetCritChance(DamageClass.Generic) += HighCrit[fi] + ChewComboLayers * ComboCritPerLayer;
        }

        /// <summary>每帧设原版探测位（会被重置）</summary>
        private void ApplyExplorerVision() {
            if (!IsHigh) {
                return;
            }
            if (CanGlide) {
                Player.nightVision = true;
            }
            if (CanSpelunk) {
                Player.findTreasure = true;
            }
            if (CanDoubleJump) {
                Player.dangerSense = true;
            }
            if (CanCompass) {
                Player.biomeSight = true;
            }
            if (CanFly) {
                Player.detectCreature = true;
            }
        }

        public override void PostUpdateBuffs() {
            if (AddictionCount > 0) {
                Player.AddBuff(ModContent.BuffType<BetelNutAddictionBuff>(), 60 * 5);
            }

            int withdrawalType = BetelWithdrawalBuffBase.GetTypeForLevel(CravingLevel);
            if (withdrawalType > 0) {
                Player.AddBuff(withdrawalType, 60 * 5);
            }

            if (HighTicks > 0) {
                int level = HighFlavor.ToBuffLevel();
                int buffType = ChewSatisfactionBuffBase.GetTypeForLevel(level);
                if (buffType > 0) {
                    Player.AddBuff(buffType, HighTicks);
                }
            }
        }

        public override void ModifyScreenPosition() {
            if (Player.whoAmI != Main.myPlayer) return;
            if (CravingLevel < 3) return;

            float strength = (CravingLevel - 2) * 1.2f;
            Main.screenPosition += new Vector2(
                (Main.rand.NextFloat() - 0.5f) * 2f * strength,
                (Main.rand.NextFloat() - 0.5f) * 2f * strength);
        }

        /// <summary>上头持续期间不会戒断；结束后按分钟推进层级</summary>
        public int ComputeCravingLevel() {
            if (AddictionCount <= 0 || HighTicks > 0) return 0;

            int level = 0;
            for (int i = WithdrawalBaseTicks.Length - 1; i >= 0; i--) {
                if (WithdrawalTicks >= WithdrawalBaseTicks[i]) {
                    level = i;
                    break;
                }
            }
            return level;
        }

        /// <summary>戒断 shader 目标强度 0~1</summary>
        public float TargetWithdrawalIntensity => CravingLevel <= 0 ? 0f : CravingLevel / 5f;

        /// <summary>上头 shader 目标强度，看品质+嚼劲</summary>
        public float TargetHighIntensity {
            get {
                if (HighTicks <= 0) return 0f;
                float comboPart = (float)ChewComboLayers / MaxComboLayers;
                int level = HighFlavor.ToBuffLevel();
                float flavorPart = level <= 0 ? 0f : level / 6f;
                return Math.Clamp(0.18f + 0.5f * comboPart + 0.12f * flavorPart, 0f, 1f);
            }
        }
    }

    public class BigFruitAirJump : ExtraJump
    {
        public override Position GetDefaultPosition() => new After(BlizzardInABottle);

        public override float GetDurationMultiplier(Player player) {
            return player.GetModPlayer<BetelNutPlayer>().BigFruitAirJumpDurationMultiplier;
        }

        public override bool CanStart(Player player) {
            return player.GetModPlayer<BetelNutPlayer>().CanUseBigFruitAirJump;
        }

        public override void OnStarted(Player player, ref bool playSound) {
            player.GetModPlayer<BetelNutPlayer>().SpawnBigFruitAirJumpDust();
        }
    }
}
