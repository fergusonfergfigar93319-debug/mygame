package com.example.mygame.data

import android.content.Context
import com.example.mygame.game.level.GameLevel
import java.util.UUID

class SaveRepository(context: Context) {

    private val prefs =
        context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    fun getBestScore(): Int = prefs.getInt(KEY_BEST_SCORE, 0)

    fun getRescuedTuanTuan(): Boolean = prefs.getBoolean(KEY_RESCUED_TUANTUAN, false)

    /** 主线抵达冰湖后与 Dror 同行；也用于无尽模式中显示跟宠。 */
    fun getCompanionDrorUnlocked(): Boolean = prefs.getBoolean(KEY_COMPANION_DROR, false)

    fun saveCompanionDrorUnlocked() {
        prefs.edit().putBoolean(KEY_COMPANION_DROR, true).apply()
    }

    fun saveBestScore(score: Int) {
        prefs.edit().putInt(KEY_BEST_SCORE, score).apply()
    }

    fun saveRescuedTuanTuan() {
        prefs.edit().putBoolean(KEY_RESCUED_TUANTUAN, true).apply()
    }

    /** 上次进行游戏的关卡；主线推进后会写入，便于下次启动直接接续。 */
    fun getResumeLevel(): GameLevel =
        when (prefs.getString(KEY_RESUME_LEVEL, VALUE_CEDAR)) {
            VALUE_ICE -> GameLevel.IceLakeEchoValley
            VALUE_MIST -> GameLevel.MistDike
            else -> GameLevel.CedarVillageRuins
        }

    fun setResumeLevel(level: GameLevel) {
        val v = when (level) {
            GameLevel.CedarVillageRuins -> VALUE_CEDAR
            GameLevel.IceLakeEchoValley -> VALUE_ICE
            GameLevel.MistDike -> VALUE_MIST
        }
        prefs.edit().putString(KEY_RESUME_LEVEL, v).apply()
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

    private companion object {
        const val PREFS_NAME = "gugu_gaga_game"
        const val KEY_BEST_SCORE = "best_score"
        const val KEY_RESCUED_TUANTUAN = "rescued_tuantuan"
        const val KEY_COMPANION_DROR = "companion_dror_unlocked"
        const val KEY_RESUME_LEVEL = "resume_level"
        const val VALUE_CEDAR = "cedar"
        const val VALUE_ICE = "ice"
        const val VALUE_MIST = "mist"
        const val KEY_PLAYER_NICKNAME = "player_nickname"
        const val KEY_PLAYER_ID = "player_anonymous_id"
        const val DEFAULT_NICKNAME = "咕咕嘎嘎"
    }
}
