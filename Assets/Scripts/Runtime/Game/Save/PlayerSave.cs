using System;
using PenguinRun.Game;
using UnityEngine;

namespace PenguinRun.Game.Save
{
    /// <summary>
    /// 集中管理玩家档案与设置；之前散落在 Android SharedPreferences 的字段全部迁到 PlayerPrefs。
    /// 命名遵循原 Kotlin SaveRepository，跨平台一致。
    /// </summary>
    public static class PlayerSave
    {
        private const string KeyBestScore = "best_score";
        private const string KeyTotalFishSnacks = "total_fish_snacks";
        private const string KeyPlayerNickname = "player_nickname";
        private const string KeyPlayerId = "player_anonymous_id";
        private const string KeyBgmEnabled = "bgm_enabled";
        private const string KeySfxEnabled = "sfx_enabled";
        private const string KeyLobbyBgmTrack = "lobby_bgm_track";
        private const string KeyRunBgmTrack = "run_bgm_track";
        private const string KeyAmbienceTrack = "ambience_track";
        private const string KeySfxStyle = "sfx_style";
        private const string KeyHapticIntensity = "haptic_intensity";
        private const string KeyCampDash = "camp_dash_level";
        private const string KeyCampTuan = "camp_tuan_level";
        private const string KeyCampPolar = "camp_polar_level";
        private const string KeyCampMagnet = "camp_magnet_level";
        private const string KeyCampFishGain = "camp_fishgain_level";
        private const string KeyCampScoreBonus = "camp_scorebonus_level";
        private const string KeyCampLifetimeInvested = "camp_lifetime_invested_fish";
        private const string KeyCosmeticScarf = "cosmetic_scarf_id";
        private const string KeyCosmeticHat = "cosmetic_hat_id";
        private const string KeyCosmeticUnlockedScarves = "cosmetic_scarf_unlocked_csv";
        private const string KeyCosmeticUnlockedHats = "cosmetic_hat_unlocked_csv";
        private const string KeyBoosterDoubleFish = "booster_double_fish";
        private const string KeyBoosterScoreBoost = "booster_score_boost";
        private const string KeyBoosterLuckyStart = "booster_lucky_start";
        private const string KeyPendingDoubleFish = "pending_booster_double_fish";
        private const string KeyPendingScoreBoost = "pending_booster_score_boost";
        private const string KeyPendingLuckyStart = "pending_booster_lucky_start";
        private const string KeyShopVisited = "shop_visited";
        private const string KeyShopBundleClaimed = "shop_bundle_claimed_";
        private const string KeyResumeLevel = "resume_level";
        private const string KeyRescuedTuanTuan = "rescued_tuantuan";
        private const string KeyTakamatsuDefeated = "takamatsu_defeated";
        private const string KeyCompanionDror = "companion_dror_unlocked";
        private const string KeyVisitedCamp = "visited_camp";
        private const string KeyRunner3DTutorialDone = "runner3d_tutorial_done";
        private const string KeySeenCampUnlockNotice = "seen_camp_unlock_notice";
        private const string KeySelectedDifficulty = "selected_difficulty";
        private const string KeyDefeatedBossCsv = "defeated_boss_ids_csv";

        public const string DefaultNickname = "咕咕嘎嘎";

        /// <summary>每个营地强化条目的最高等级。提升到 5 级带来更平滑的成长曲线。</summary>
        public const int CampMaxLevel = 5;

        /// <summary>未解锁/默认装扮 id（玩家初次进入即可使用）。</summary>
        public const string DefaultScarfId = "scarf_cyan";
        public const string DefaultHatId = "hat_blue";

        public const string LobbyBgmCozy = "cozy";
        public const string LobbyBgmStory = "story";
        public const string LobbyBgmEndless = "endless";
        public const string LobbyBgmOpenWisdom = "open_wisdom";
        public const string LobbyBgmOpenSwing = "open_swing";
        public const string LobbyBgmNone = "none";

        public const string RunBgmEndless = "endless";
        public const string RunBgmStory = "story";
        public const string RunBgmOpenWisdom = "open_wisdom";
        public const string RunBgmOpenWinter = "open_winter";
        public const string RunBgmOpenSwing = "open_swing";
        public const string RunBgmRandom = "random";

        public const string AmbienceClassicWind = "wind";
        public const string AmbienceOpenCavern = "open_cavern";
        public const string AmbienceOpenCyber = "open_cyber";

        public const string SfxStyleClassic = "classic";
        public const string SfxStyleOpenPack = "open_pack";

        public const string DefaultLobbyBgmTrack = LobbyBgmOpenSwing;
        public const string DefaultRunBgmTrack = RunBgmOpenWisdom;
        public const string DefaultAmbienceTrack = AmbienceClassicWind;
        public const string DefaultSfxStyle = SfxStyleClassic;

        public const string ResumeLevelIce = "ice";
        public const string ResumeLevelCedar = "cedar";
        public const string ResumeLevelMist = "mist";

        public static int BestScore
        {
            get => PlayerPrefs.GetInt(KeyBestScore, 0);
            set => PlayerPrefs.SetInt(KeyBestScore, Mathf.Max(0, value));
        }

        public static int TotalFishSnacks
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyTotalFishSnacks, 0));
            private set => PlayerPrefs.SetInt(KeyTotalFishSnacks, Mathf.Max(0, value));
        }

        public static string PlayerNickname
        {
            get
            {
                var s = PlayerPrefs.GetString(KeyPlayerNickname, DefaultNickname);
                return string.IsNullOrWhiteSpace(s) ? DefaultNickname : s;
            }
            set
            {
                var trimmed = (value ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(trimmed)) trimmed = DefaultNickname;
                PlayerPrefs.SetString(KeyPlayerNickname, trimmed);
            }
        }

        public static bool BgmEnabled
        {
            get => PlayerPrefs.GetInt(KeyBgmEnabled, 1) != 0;
            set => PlayerPrefs.SetInt(KeyBgmEnabled, value ? 1 : 0);
        }

        public static bool SfxEnabled
        {
            get => PlayerPrefs.GetInt(KeySfxEnabled, 1) != 0;
            set => PlayerPrefs.SetInt(KeySfxEnabled, value ? 1 : 0);
        }

        public static string LobbyBgmTrack
        {
            get
            {
                var s = PlayerPrefs.GetString(KeyLobbyBgmTrack, DefaultLobbyBgmTrack);
                return s switch
                {
                    LobbyBgmCozy or LobbyBgmStory or LobbyBgmEndless or LobbyBgmOpenWisdom or LobbyBgmOpenSwing or LobbyBgmNone => s,
                    _ => DefaultLobbyBgmTrack,
                };
            }
            set => PlayerPrefs.SetString(KeyLobbyBgmTrack, value);
        }

        public static string RunBgmTrack
        {
            get
            {
                var s = PlayerPrefs.GetString(KeyRunBgmTrack, DefaultRunBgmTrack);
                return s switch
                {
                    RunBgmEndless or RunBgmStory or RunBgmOpenWisdom or RunBgmOpenWinter or RunBgmOpenSwing or RunBgmRandom => s,
                    _ => DefaultRunBgmTrack,
                };
            }
            set => PlayerPrefs.SetString(KeyRunBgmTrack, value);
        }

        public static string AmbienceTrack
        {
            get
            {
                var s = PlayerPrefs.GetString(KeyAmbienceTrack, DefaultAmbienceTrack);
                return s switch
                {
                    AmbienceClassicWind or AmbienceOpenCavern or AmbienceOpenCyber => s,
                    _ => DefaultAmbienceTrack,
                };
            }
            set => PlayerPrefs.SetString(KeyAmbienceTrack, value);
        }

        public static string SfxStyle
        {
            get
            {
                var s = PlayerPrefs.GetString(KeySfxStyle, DefaultSfxStyle);
                return s switch
                {
                    SfxStyleClassic or SfxStyleOpenPack => s,
                    _ => DefaultSfxStyle,
                };
            }
            set => PlayerPrefs.SetString(KeySfxStyle, value);
        }

        public static float HapticIntensity
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(KeyHapticIntensity, 0.7f));
            set => PlayerPrefs.SetFloat(KeyHapticIntensity, Mathf.Clamp01(value));
        }

        public static int CampDashLevel => Mathf.Clamp(PlayerPrefs.GetInt(KeyCampDash, 0), 0, CampMaxLevel);

        public static int CampTuanLevel => Mathf.Clamp(PlayerPrefs.GetInt(KeyCampTuan, 0), 0, CampMaxLevel);

        public static int CampPolarIntuitionLevel => Mathf.Clamp(PlayerPrefs.GetInt(KeyCampPolar, 0), 0, CampMaxLevel);

        public static int CampMagnetLevel => Mathf.Clamp(PlayerPrefs.GetInt(KeyCampMagnet, 0), 0, CampMaxLevel);

        public static int CampFishGainLevel => Mathf.Clamp(PlayerPrefs.GetInt(KeyCampFishGain, 0), 0, CampMaxLevel);

        public static int CampScoreBonusLevel => Mathf.Clamp(PlayerPrefs.GetInt(KeyCampScoreBonus, 0), 0, CampMaxLevel);

        /// <summary>累计为营地强化投入的鱼干数（用于「养成总览」展示，不影响游戏数值）。</summary>
        public static int CampLifetimeInvested
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyCampLifetimeInvested, 0));
            set => PlayerPrefs.SetInt(KeyCampLifetimeInvested, Mathf.Max(0, value));
        }

        public static void AddCampInvested(int amount)
        {
            if (amount <= 0) return;
            CampLifetimeInvested = CampLifetimeInvested + amount;
        }

        public static string SelectedScarfId
        {
            get
            {
                var s = PlayerPrefs.GetString(KeyCosmeticScarf, DefaultScarfId);
                return string.IsNullOrEmpty(s) ? DefaultScarfId : s;
            }
            set => PlayerPrefs.SetString(KeyCosmeticScarf, string.IsNullOrEmpty(value) ? DefaultScarfId : value);
        }

        public static string SelectedHatId
        {
            get
            {
                var s = PlayerPrefs.GetString(KeyCosmeticHat, DefaultHatId);
                return string.IsNullOrEmpty(s) ? DefaultHatId : s;
            }
            set => PlayerPrefs.SetString(KeyCosmeticHat, string.IsNullOrEmpty(value) ? DefaultHatId : value);
        }

        /// <summary>已解锁围巾的 id 列表（包含默认色）。</summary>
        public static System.Collections.Generic.HashSet<string> UnlockedScarves => ReadCsvSet(KeyCosmeticUnlockedScarves, DefaultScarfId);

        /// <summary>已解锁帽子的 id 列表（包含默认色）。</summary>
        public static System.Collections.Generic.HashSet<string> UnlockedHats => ReadCsvSet(KeyCosmeticUnlockedHats, DefaultHatId);

        public static bool IsScarfUnlocked(string id) => string.IsNullOrEmpty(id) || id == DefaultScarfId || UnlockedScarves.Contains(id);
        public static bool IsHatUnlocked(string id) => string.IsNullOrEmpty(id) || id == DefaultHatId || UnlockedHats.Contains(id);

        public static void UnlockScarf(string id) => AddCsvEntry(KeyCosmeticUnlockedScarves, DefaultScarfId, id);
        public static void UnlockHat(string id) => AddCsvEntry(KeyCosmeticUnlockedHats, DefaultHatId, id);

        public static int BoosterDoubleFish
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyBoosterDoubleFish, 0));
            set => PlayerPrefs.SetInt(KeyBoosterDoubleFish, Mathf.Max(0, value));
        }

        public static int BoosterScoreBoost
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyBoosterScoreBoost, 0));
            set => PlayerPrefs.SetInt(KeyBoosterScoreBoost, Mathf.Max(0, value));
        }

        public static int BoosterLuckyStart
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyBoosterLuckyStart, 0));
            set => PlayerPrefs.SetInt(KeyBoosterLuckyStart, Mathf.Max(0, value));
        }

        public static int PendingDoubleFish
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyPendingDoubleFish, 0));
            set => PlayerPrefs.SetInt(KeyPendingDoubleFish, Mathf.Max(0, value));
        }

        public static int PendingScoreBoost
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyPendingScoreBoost, 0));
            set => PlayerPrefs.SetInt(KeyPendingScoreBoost, Mathf.Max(0, value));
        }

        public static int PendingLuckyStart
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyPendingLuckyStart, 0));
            set => PlayerPrefs.SetInt(KeyPendingLuckyStart, Mathf.Max(0, value));
        }

        public static bool ShopVisited
        {
            get => PlayerPrefs.GetInt(KeyShopVisited, 0) != 0;
            set => PlayerPrefs.SetInt(KeyShopVisited, value ? 1 : 0);
        }

        /// <summary>礼盒只允许购买一次：判定与标记。</summary>
        public static bool IsBundleClaimed(string bundleId) =>
            !string.IsNullOrEmpty(bundleId) && PlayerPrefs.GetInt(KeyShopBundleClaimed + bundleId, 0) != 0;

        public static void MarkBundleClaimed(string bundleId)
        {
            if (string.IsNullOrEmpty(bundleId)) return;
            PlayerPrefs.SetInt(KeyShopBundleClaimed + bundleId, 1);
        }

        private static System.Collections.Generic.HashSet<string> ReadCsvSet(string key, string defaultEntry)
        {
            var set = new System.Collections.Generic.HashSet<string>();
            if (!string.IsNullOrEmpty(defaultEntry)) set.Add(defaultEntry);
            var raw = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(raw)) return set;
            foreach (var token in raw.Split(','))
            {
                var trimmed = token.Trim();
                if (!string.IsNullOrEmpty(trimmed)) set.Add(trimmed);
            }
            return set;
        }

        private static void AddCsvEntry(string key, string defaultEntry, string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var set = ReadCsvSet(key, defaultEntry);
            if (!set.Add(id)) return;
            // 写回时不带默认 entry，避免冗余
            var sb = new System.Text.StringBuilder();
            foreach (var s in set)
            {
                if (s == defaultEntry) continue;
                if (sb.Length > 0) sb.Append(',');
                sb.Append(s);
            }
            PlayerPrefs.SetString(key, sb.ToString());
        }

        public static bool RescuedTuanTuan
        {
            get => PlayerPrefs.GetInt(KeyRescuedTuanTuan, 0) != 0;
            set => PlayerPrefs.SetInt(KeyRescuedTuanTuan, value ? 1 : 0);
        }

        public static bool TakamatsuDefeated
        {
            get => PlayerPrefs.GetInt(KeyTakamatsuDefeated, 0) != 0;
            set => PlayerPrefs.SetInt(KeyTakamatsuDefeated, value ? 1 : 0);
        }

        public static bool CompanionDrorUnlocked
        {
            get => PlayerPrefs.GetInt(KeyCompanionDror, 0) != 0;
            set => PlayerPrefs.SetInt(KeyCompanionDror, value ? 1 : 0);
        }

        public static bool VisitedCamp
        {
            get => PlayerPrefs.GetInt(KeyVisitedCamp, 0) != 0;
            set => PlayerPrefs.SetInt(KeyVisitedCamp, value ? 1 : 0);
        }

        public static bool Runner3DTutorialDone
        {
            get => PlayerPrefs.GetInt(KeyRunner3DTutorialDone, 0) != 0;
            set => PlayerPrefs.SetInt(KeyRunner3DTutorialDone, value ? 1 : 0);
        }

        public static bool SeenCampUnlockNotice
        {
            get => PlayerPrefs.GetInt(KeySeenCampUnlockNotice, 0) != 0;
            set => PlayerPrefs.SetInt(KeySeenCampUnlockNotice, value ? 1 : 0);
        }

        public static DifficultyKind SelectedDifficulty
        {
            get => (DifficultyKind)PlayerPrefs.GetInt(KeySelectedDifficulty, (int)DifficultyKind.Normal);
            set => PlayerPrefs.SetInt(KeySelectedDifficulty, (int)value);
        }

        public static string ResumeLevel
        {
            get
            {
                var s = PlayerPrefs.GetString(KeyResumeLevel, ResumeLevelCedar);
                return s switch
                {
                    ResumeLevelIce or ResumeLevelCedar or ResumeLevelMist => s,
                    _ => ResumeLevelCedar,
                };
            }
            set => PlayerPrefs.SetString(KeyResumeLevel, value);
        }

        /// <summary>稳定的匿名玩家 id，本地生成一次后持久化。</summary>
        public static string GetOrCreatePlayerId()
        {
            var existing = PlayerPrefs.GetString(KeyPlayerId, null);
            if (!string.IsNullOrEmpty(existing)) return existing;
            var created = Guid.NewGuid().ToString();
            PlayerPrefs.SetString(KeyPlayerId, created);
            PlayerPrefs.Save();
            return created;
        }

        public static void AddFishSnacks(int delta)
        {
            if (delta <= 0) return;
            var next = (long)TotalFishSnacks + delta;
            if (next > int.MaxValue / 2) next = int.MaxValue / 2;
            TotalFishSnacks = (int)next;
        }

        /// <summary>
        /// 直接消费鱼干；不足时返回 false 不扣减。
        /// </summary>
        public static bool TrySpendFishSnacks(int cost)
        {
            if (cost <= 0) return true;
            var have = TotalFishSnacks;
            if (have < cost) return false;
            TotalFishSnacks = have - cost;
            return true;
        }

        public static void Flush() => PlayerPrefs.Save();

        public static bool HasDefeatedBoss(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return false;
            var raw = PlayerPrefs.GetString(KeyDefeatedBossCsv, string.Empty);
            if (string.IsNullOrEmpty(raw)) return false;
            foreach (var token in raw.Split(','))
            {
                if (token.Trim() == bossId) return true;
            }
            return false;
        }

        public static void MarkBossDefeated(string bossId)
        {
            if (string.IsNullOrEmpty(bossId)) return;
            AddCsvEntry(KeyDefeatedBossCsv, null, bossId);
        }

        public static void SetCampLevel(string key, int value)
        {
            PlayerPrefs.SetInt(key, Mathf.Clamp(value, 0, CampMaxLevel));
        }

        internal static class Keys
        {
            public const string CampDash = KeyCampDash;
            public const string CampTuan = KeyCampTuan;
            public const string CampPolar = KeyCampPolar;
            public const string CampMagnet = KeyCampMagnet;
            public const string CampFishGain = KeyCampFishGain;
            public const string CampScoreBonus = KeyCampScoreBonus;
        }
    }
}
