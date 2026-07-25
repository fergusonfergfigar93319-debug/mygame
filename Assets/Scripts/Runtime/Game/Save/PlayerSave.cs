using System;
using System.Collections.Generic;
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
        private const string KeyRunnerSlideGateTutorialDone = "runner_slide_gate_tutorial_done";
        private const string KeyOnboardingLane = "onboarding_v1_lane";
        private const string KeyOnboardingJump = "onboarding_v1_jump";
        private const string KeyOnboardingSlide = "onboarding_v1_slide";
        private const string KeyOnboardingCollect = "onboarding_v1_collect";
        private const string KeySelectedDifficulty = "selected_difficulty";
        private const string KeyDefeatedBossCsv = "defeated_boss_ids_csv";
        private const string KeyLoginStreak = "login_streak";
        private const string KeyLastLoginDate = "last_login_date";
        private const string KeyTotalRuns = "total_runs";
        private const string KeyDailyCheckInClaimed = "daily_checkin_claimed_";

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

        /// <summary>
        /// Legacy one-shot slide-gate hint flag. Kept readable for SaveSchema migration into
        /// <see cref="OnboardingSlideDemonstrated"/>; new code must not write it.
        /// </summary>
        public static bool RunnerSlideGateTutorialDone
        {
            get => PlayerPrefs.GetInt(KeyRunnerSlideGateTutorialDone, 0) != 0;
            set => PlayerPrefs.SetInt(KeyRunnerSlideGateTutorialDone, value ? 1 : 0);
        }

        public static bool OnboardingLaneDemonstrated
        {
            get => PlayerPrefs.GetInt(KeyOnboardingLane, 0) != 0;
            set => PlayerPrefs.SetInt(KeyOnboardingLane, value ? 1 : 0);
        }

        public static bool OnboardingJumpDemonstrated
        {
            get => PlayerPrefs.GetInt(KeyOnboardingJump, 0) != 0;
            set => PlayerPrefs.SetInt(KeyOnboardingJump, value ? 1 : 0);
        }

        public static bool OnboardingSlideDemonstrated
        {
            get => PlayerPrefs.GetInt(KeyOnboardingSlide, 0) != 0;
            set => PlayerPrefs.SetInt(KeyOnboardingSlide, value ? 1 : 0);
        }

        public static bool OnboardingCollectDemonstrated
        {
            get => PlayerPrefs.GetInt(KeyOnboardingCollect, 0) != 0;
            set => PlayerPrefs.SetInt(KeyOnboardingCollect, value ? 1 : 0);
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

        /// <summary>
        /// Reads a value owned by the versioned long-term growth layer. Keys are automatically
        /// namespaced so feature stores cannot collide with legacy save fields.
        /// </summary>
        public static int ReadGrowthInt(string key, int defaultValue = 0) =>
            PlayerPrefs.GetInt(GrowthKey(key), defaultValue);

        public static void WriteGrowthInt(string key, int value) =>
            PlayerPrefs.SetInt(GrowthKey(key), value);

        public static string ReadGrowthString(string key, string defaultValue = "") =>
            PlayerPrefs.GetString(GrowthKey(key), defaultValue ?? string.Empty);

        public static void WriteGrowthString(string key, string value) =>
            PlayerPrefs.SetString(GrowthKey(key), value ?? string.Empty);

        private static string GrowthKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A growth save key is required.", nameof(key));
            return "growth_" + key.Trim();
        }

        // ── 登录连续天数 & 每日签到 ──────────────────────────────────────────────

        public static int LoginStreak
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyLoginStreak, 0));
            private set => PlayerPrefs.SetInt(KeyLoginStreak, Mathf.Max(0, value));
        }

        /// <summary>每次进入主菜单时调用，更新连续登录天数并返回是否是今天首次登录。</summary>
        public static bool TryRecordLogin()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var last = PlayerPrefs.GetString(KeyLastLoginDate, string.Empty);
            if (last == today) return false;

            var yesterday = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
            LoginStreak = last == yesterday ? LoginStreak + 1 : 1;
            PlayerPrefs.SetString(KeyLastLoginDate, today);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>今日签到奖励是否已领取。</summary>
        public static bool IsDailyCheckInClaimed()
        {
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            return PlayerPrefs.GetInt(KeyDailyCheckInClaimed + today, 0) != 0;
        }

        /// <summary>领取今日签到奖励，返回奖励鱼干数量（0 表示已领取或失败）。</summary>
        public static int ClaimDailyCheckIn()
        {
            if (IsDailyCheckInClaimed()) return 0;
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var streak = LoginStreak;
            // 连续天数越多奖励越高，最高 7 天循环
            var reward = streak switch
            {
                1 => 10,
                2 => 15,
                3 => 20,
                4 => 25,
                5 => 30,
                6 => 40,
                _ => 50,
            };
            AddFishSnacks(reward);
            PlayerPrefs.SetInt(KeyDailyCheckInClaimed + today, 1);
            PlayerPrefs.Save();
            return reward;
        }

        /// <summary>
        /// 今日若未签到，预览将获得的鱼干数（与 <see cref="ClaimDailyCheckIn"/> 公式一致）。
        /// </summary>
        public static int PreviewDailyCheckInReward()
        {
            var streak = Mathf.Max(1, LoginStreak);
            return streak switch
            {
                1 => 10,
                2 => 15,
                3 => 20,
                4 => 25,
                5 => 30,
                6 => 40,
                _ => 50,
            };
        }

        // ── 总局数 ────────────────────────────────────────────────────────────────

        public static int TotalRuns
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(KeyTotalRuns, 0));
            set => PlayerPrefs.SetInt(KeyTotalRuns, Mathf.Max(0, value));
        }

        public static void IncrementTotalRuns() => TotalRuns = TotalRuns + 1;

        /// <summary>老存档回填：若历史记录条数多于已记总局数，以记录数为准。</summary>
        public static void EnsureTotalRunsSynced()
        {
            var fromRecords = RunRecords.GetStats().TotalRuns;
            if (fromRecords > TotalRuns)
                TotalRuns = fromRecords;
        }

        // ── 玩家等级（基于累计鱼干 + 最高分） ────────────────────────────────────

        /// <summary>根据累计鱼干和最高分计算玩家等级（1-50）。</summary>
        public static int PlayerLevel
        {
            get
            {
                var xp = TotalFishSnacks + BestScore / 10;
                // 每级所需 XP 递增：Lv1=100, Lv2=250, Lv3=450...
                var level = 1;
                var threshold = 0;
                for (var i = 1; i <= 50; i++)
                {
                    threshold += 100 + (i - 1) * 50;
                    if (xp >= threshold) level = i + 1;
                    else break;
                }
                return Mathf.Clamp(level, 1, 50);
            }
        }

        /// <summary>当前等级的 XP 进度（0~1）。</summary>
        public static float PlayerLevelProgress
        {
            get
            {
                var xp = TotalFishSnacks + BestScore / 10;
                var level = PlayerLevel;
                var prevThreshold = 0;
                var nextThreshold = 100;
                var acc = 0;
                for (var i = 1; i <= 50; i++)
                {
                    var needed = 100 + (i - 1) * 50;
                    if (i < level) { acc += needed; prevThreshold = acc; }
                    else if (i == level) { nextThreshold = acc + needed; break; }
                }
                var range = nextThreshold - prevThreshold;
                if (range <= 0) return 1f;
                return Mathf.Clamp01((float)(xp - prevThreshold) / range);
            }
        }

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

        /// <summary>获取所有已击败的 Boss ID 集合</summary>
        public static HashSet<string> GetDefeatedBossIds()
        {
            var raw = PlayerPrefs.GetString(KeyDefeatedBossCsv, string.Empty);
            var result = new HashSet<string>();
            if (string.IsNullOrEmpty(raw)) return result;

            foreach (var token in raw.Split(','))
            {
                var trimmed = token.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            return result;
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
