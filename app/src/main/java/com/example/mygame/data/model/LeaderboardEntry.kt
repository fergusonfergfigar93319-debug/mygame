package com.example.mygame.data.model

data class LeaderboardEntry(
    val id: String,
    /** 与 [com.example.mygame.data.SaveRepository.getOrCreatePlayerId] 对应，便于日后远端去重/合并 */
    val playerId: String,
    val nickname: String,
    val totalScore: Int,
    /** 游戏内路程单位（与 playerX/10 一致，便于阅读） */
    val distanceScoreUnits: Float,
    val fishSnacks: Int,
    val beaconCount: Int,
    val lorePageCount: Int,
    val survivalSeconds: Float,
    val timestampMillis: Long,
    val rescuedTuanTuan: Boolean,
    val mode: String = "endless_polar_night",
    /**
     * 每日挑战桶（如 `yyyy-MM-dd`）；`null` 表示休闲无尽，不计入某日榜。
     */
    val challengeBucket: String? = null,
)
