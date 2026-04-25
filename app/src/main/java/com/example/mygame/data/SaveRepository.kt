package com.example.mygame.data

import android.content.Context
import com.example.mygame.game.level.GameLevel
import com.example.mygame.game.modes.EndlessBalanceConfig
import java.util.UUID

class SaveRepository(context: Context) {

    private val prefs =
        context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    fun getBestScore(): Int = prefs.getInt(KEY_BEST_SCORE, 0)

    fun saveBestScore(score: Int) {
        prefs.edit().putInt(KEY_BEST_SCORE, score).apply()
    }

    fun getRescuedTuanTuan(): Boolean = prefs.getBoolean(KEY_RESCUED_TUANTUAN, false)

    fun saveRescuedTuanTuan() {
        prefs.edit().putBoolean(KEY_RESCUED_TUANTUAN, true).apply()
    }

    /** 主线抵达冰湖后与 Dror 同行；也用于无尽模式中显示跟宠。 */
    fun getCompanionDrorUnlocked(): Boolean = prefs.getBoolean(KEY_COMPANION_DROR, false)

    fun saveCompanionDrorUnlocked() {
        prefs.edit().putBoolean(KEY_COMPANION_DROR, true).apply()
    }

    fun getTakamatsuDefeated(): Boolean = prefs.getBoolean(KEY_TAKAMATSU_DEFEATED, false)

    fun setTakamatsuDefeated(defeated: Boolean = true) {
        prefs.edit().putBoolean(KEY_TAKAMATSU_DEFEATED, defeated).apply()
    }

    fun getBgmEnabled(): Boolean = prefs.getBoolean(KEY_BGM_ENABLED, true)

    fun setBgmEnabled(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_BGM_ENABLED, enabled).apply()
    }

    fun getSfxEnabled(): Boolean = prefs.getBoolean(KEY_SFX_ENABLED, true)

    fun setSfxEnabled(enabled: Boolean) {
        prefs.edit().putBoolean(KEY_SFX_ENABLED, enabled).apply()
    }

    /** 上次进行游戏的关卡；主线推进后会写入，便于下次启动直接接续。 */
    fun getResumeLevel(): GameLevel =
        when (prefs.getString(KEY_RESUME_LEVEL, VALUE_CEDAR)) {
            VALUE_ICE -> GameLevel.IceLakeEchoValley
            VALUE_MIST -> GameLevel.MistDike
            else -> GameLevel.CedarVillageRuins
        }

    fun setResumeLevel(level: GameLevel) {
        val value =
            when (level) {
                GameLevel.CedarVillageRuins -> VALUE_CEDAR
                GameLevel.IceLakeEchoValley -> VALUE_ICE
                GameLevel.MistDike -> VALUE_MIST
            }
        prefs.edit().putString(KEY_RESUME_LEVEL, value).apply()
    }

    fun getPlayerNickname(): String =
        prefs.getString(KEY_PLAYER_NICKNAME, DEFAULT_NICKNAME) ?: DEFAULT_NICKNAME

    fun setPlayerNickname(name: String) {
        val trimmed = name.trim().ifBlank { DEFAULT_NICKNAME }
        prefs.edit().putString(KEY_PLAYER_NICKNAME, trimmed).apply()
    }

    /** 匿名玩家稳定 id，供联网榜或合并历史使用；本地生成一次后持久化。 */
    fun getOrCreatePlayerId(): String {
        val existing = prefs.getString(KEY_PLAYER_ID, null)
        if (!existing.isNullOrBlank()) return existing
        val created = UUID.randomUUID().toString()
        prefs.edit().putString(KEY_PLAYER_ID, created).apply()
        return created
    }

    fun getDailyChallengeAttemptCount(bucket: String): Int =
        prefs.getInt(KEY_DAILY_ATTEMPTS_PREFIX + bucket, 0).coerceAtLeast(0)

    fun incrementDailyChallengeAttempt(bucket: String): Int {
        val next = getDailyChallengeAttemptCount(bucket) + 1
        prefs.edit().putInt(KEY_DAILY_ATTEMPTS_PREFIX + bucket, next).apply()
        return next
    }

    fun getDailyChallengeBestScore(bucket: String): Int =
        prefs.getInt(KEY_DAILY_BEST_PREFIX + bucket, 0).coerceAtLeast(0)

    fun updateDailyChallengeBestScore(bucket: String, score: Int): Boolean {
        val previous = getDailyChallengeBestScore(bucket)
        if (score <= previous) return false
        prefs.edit().putInt(KEY_DAILY_BEST_PREFIX + bucket, score).apply()
        return true
    }

    fun hasSeenCampUnlockNotice(): Boolean = prefs.getBoolean(KEY_SEEN_CAMP_UNLOCK_NOTICE, false)

    fun markCampUnlockNoticeSeen() {
        prefs.edit().putBoolean(KEY_SEEN_CAMP_UNLOCK_NOTICE, true).apply()
    }

    fun hasVisitedCamp(): Boolean = prefs.getBoolean(KEY_VISITED_CAMP, false)

    fun markCampVisited() {
        prefs.edit().putBoolean(KEY_VISITED_CAMP, true).apply()
    }

    fun getTotalFishSnacks(): Int = maxOf(0, prefs.getInt(KEY_TOTAL_FISH_SNACKS, 0))

    fun addFishSnacks(delta: Int) {
        if (delta <= 0) return
        val next = (getTotalFishSnacks() + delta).coerceAtMost(Int.MAX_VALUE / 2)
        prefs.edit().putInt(KEY_TOTAL_FISH_SNACKS, next).apply()
    }

    fun getCampDashLevel(): Int = prefs.getInt(KEY_CAMP_DASH, 0).coerceIn(0, CAMP_MAX_LEVEL)

    fun getCampTuanLevel(): Int = prefs.getInt(KEY_CAMP_TUAN, 0).coerceIn(0, CAMP_MAX_LEVEL)

    fun getCampPolarIntuitionLevel(): Int = prefs.getInt(KEY_CAMP_POLAR, 0).coerceIn(0, CAMP_MAX_LEVEL)

    fun getCampMagnetLevel(): Int = prefs.getInt(KEY_CAMP_MAGNET, 0).coerceIn(0, CAMP_MAX_LEVEL)

    /** 鱼干冲刺单段持续时间，包含营地强化。 */
    fun getFishDashDurationSeconds(): Float = 7f + getCampDashLevel() * 1f

    /** 团团雪球掩护持续时间，包含营地强化。 */
    fun getTuanAssistDurationSeconds(): Float = 4.5f + getCampTuanLevel() * 0.55f

    /** 极光磁针吸附持续时间，包含营地强化。 */
    fun getMagnetDurationSeconds(): Float = 10f + getCampMagnetLevel() * 1.5f

    /**
     * 无尽航道生成用：与 [EndlessBalanceConfig.rewardSpacingWorldWidthMultiplier] 相乘后，
     * 值越小越常插入补给休整段。
     */
    fun getCampRewardSpacingWidthMultiplier(): Float {
        val level = getCampPolarIntuitionLevel()
        return EndlessBalanceConfig.rewardSpacingWorldWidthMultiplier * (1f - 0.1f * level)
    }

    fun getCampUpgradeCost(kind: CampUpgradeKind, fromLevel: Int): Int? {
        if (fromLevel < 0 || fromLevel >= CAMP_MAX_LEVEL) return null
        return when (fromLevel) {
            0 -> if (kind == CampUpgradeKind.Magnet) 55 else 40
            1 -> if (kind == CampUpgradeKind.Magnet) 120 else 100
            else -> null
        }
    }

    /** 支付鱼干并升级一级；已满级或鱼干不足时返回 false。 */
    fun tryPurchaseCampUpgrade(kind: CampUpgradeKind): Boolean {
        val key =
            when (kind) {
                CampUpgradeKind.Dash -> KEY_CAMP_DASH
                CampUpgradeKind.Tuan -> KEY_CAMP_TUAN
                CampUpgradeKind.Polar -> KEY_CAMP_POLAR
                CampUpgradeKind.Magnet -> KEY_CAMP_MAGNET
            }
        val currentLevel =
            when (kind) {
                CampUpgradeKind.Dash -> getCampDashLevel()
                CampUpgradeKind.Tuan -> getCampTuanLevel()
                CampUpgradeKind.Polar -> getCampPolarIntuitionLevel()
                CampUpgradeKind.Magnet -> getCampMagnetLevel()
            }
        if (currentLevel >= CAMP_MAX_LEVEL) return false
        val cost = getCampUpgradeCost(kind, currentLevel) ?: return false
        val total = getTotalFishSnacks()
        if (total < cost) return false
        prefs
            .edit()
            .putInt(KEY_TOTAL_FISH_SNACKS, total - cost)
            .putInt(key, currentLevel + 1)
            .apply()
        return true
    }

    private companion object {
        const val PREFS_NAME = "gugu_gaga_game"
        const val KEY_BEST_SCORE = "best_score"
        const val KEY_RESCUED_TUANTUAN = "rescued_tuantuan"
        const val KEY_TAKAMATSU_DEFEATED = "takamatsu_defeated"
        const val KEY_BGM_ENABLED = "bgm_enabled"
        const val KEY_SFX_ENABLED = "sfx_enabled"
        const val KEY_COMPANION_DROR = "companion_dror_unlocked"
        const val KEY_RESUME_LEVEL = "resume_level"
        const val VALUE_CEDAR = "cedar"
        const val VALUE_ICE = "ice"
        const val VALUE_MIST = "mist"
        const val KEY_PLAYER_NICKNAME = "player_nickname"
        const val KEY_PLAYER_ID = "player_anonymous_id"
        const val DEFAULT_NICKNAME = "咕咕嘎嘎"
        const val KEY_TOTAL_FISH_SNACKS = "total_fish_snacks"
        const val KEY_CAMP_DASH = "camp_dash_level"
        const val KEY_CAMP_TUAN = "camp_tuan_level"
        const val KEY_CAMP_POLAR = "camp_polar_level"
        const val KEY_CAMP_MAGNET = "camp_magnet_level"
        const val KEY_DAILY_ATTEMPTS_PREFIX = "daily_attempts_"
        const val KEY_DAILY_BEST_PREFIX = "daily_best_"
        const val KEY_SEEN_CAMP_UNLOCK_NOTICE = "seen_camp_unlock_notice"
        const val KEY_VISITED_CAMP = "visited_camp"
        const val CAMP_MAX_LEVEL = 2
    }
}

/** 补给营地中的局外强化。 */
enum class CampUpgradeKind {
    Dash,
    Tuan,
    Polar,
    Magnet,
}
