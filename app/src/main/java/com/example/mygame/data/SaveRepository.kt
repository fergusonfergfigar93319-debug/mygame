package com.example.mygame.data

import android.content.Context
import com.example.mygame.game.level.GameLevel
import java.util.UUID

class SaveRepository(context: Context) {

    private val prefs =
        context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    fun getBestScore(): Int = prefs.getInt(KEY_BEST_SCORE, 0)

    fun getRescuedTuanTuan(): Boolean = prefs.getBoolean(KEY_RESCUED_TUANTUAN, false)

    fun saveBestScore(score: Int) {
        prefs.edit().putInt(KEY_BEST_SCORE, score).apply()
    }

    fun saveRescuedTuanTuan() {
        prefs.edit().putBoolean(KEY_RESCUED_TUANTUAN, true).apply()
    }

    /** 上次进行游戏的关卡；通关第一关后会写入冰湖，便于下次启动直接接续。 */
    fun getResumeLevel(): GameLevel =
        when (prefs.getString(KEY_RESUME_LEVEL, VALUE_CEDAR)) {
            VALUE_ICE -> GameLevel.IceLakeEchoValley
            else -> GameLevel.CedarVillageRuins
        }

    fun setResumeLevel(level: GameLevel) {
        val v = when (level) {
            GameLevel.IceLakeEchoValley -> VALUE_ICE
            GameLevel.CedarVillageRuins -> VALUE_CEDAR
        }
        prefs.edit().putString(KEY_RESUME_LEVEL, v).apply()
    }

    fun getPlayerNickname(): String =
        prefs.getString(KEY_PLAYER_NICKNAME, DEFAULT_NICKNAME) ?: DEFAULT_NICKNAME

    fun setPlayerNickname(name: String) {
        val trimmed = name.trim().ifBlank { DEFAULT_NICKNAME }
        prefs.edit().putString(KEY_PLAYER_NICKNAME, trimmed).apply()
    }

    /** 匿名玩家稳定 id，供联网榜/合并历史使用；本地生成一次后持久化。 */
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
        const val KEY_RESUME_LEVEL = "resume_level"
        const val VALUE_CEDAR = "cedar"
        const val VALUE_ICE = "ice"
        const val KEY_PLAYER_NICKNAME = "player_nickname"
        const val KEY_PLAYER_ID = "player_anonymous_id"
        const val DEFAULT_NICKNAME = "咕咕嘎嘎"
    }
}
