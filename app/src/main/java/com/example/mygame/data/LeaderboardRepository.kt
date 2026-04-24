package com.example.mygame.data

import com.example.mygame.data.model.LeaderboardEntry

enum class LeaderboardSort {
    ByTotalScore,
    ByDistance,
    BySurvivalTime,
}

/** 本地榜第一阶段；后续可替换为在线实现而不改 UI 调用方。 */
interface LeaderboardRepository {
    fun getTopEntries(limit: Int = 20, sort: LeaderboardSort = LeaderboardSort.ByTotalScore): List<LeaderboardEntry>
    fun submit(entry: LeaderboardEntry): LeaderboardSubmitResult
    fun getBestEntry(): LeaderboardEntry?
    fun getMostRecentEntry(): LeaderboardEntry?
    fun getAverageTotalScore(): Double
}

data class LeaderboardSubmitResult(
    val rankByScore: Int,
    val madeTop20: Boolean,
)
