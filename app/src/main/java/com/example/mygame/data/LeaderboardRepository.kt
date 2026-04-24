package com.example.mygame.data

import com.example.mygame.data.model.LeaderboardEntry

enum class LeaderboardSort {
    ByTotalScore,
    ByDistance,
    BySurvivalTime,
}

/** 本地榜第一阶段；后续可替换为在线实现而不改 UI 调用方。 */
interface LeaderboardRepository {
    /**
     * @param challengeBucket 若指定，只返回该桶内记录（例如 [com.example.mygame.game.modes.EndlessDailyChallenge.todayBucketLocal]）。
     */
    fun getTopEntries(
        limit: Int = 20,
        sort: LeaderboardSort = LeaderboardSort.ByTotalScore,
        challengeBucket: String? = null,
    ): List<LeaderboardEntry>
    fun submit(entry: LeaderboardEntry): LeaderboardSubmitResult
    fun getBestEntry(): LeaderboardEntry?
    fun getMostRecentEntry(): LeaderboardEntry?
    fun getAverageTotalScore(): Double
}

data class LeaderboardSubmitResult(
    val rankByScore: Int,
    val madeTop20: Boolean,
    /** 与提交条目相同 [com.example.mygame.data.model.LeaderboardEntry.challengeBucket] 下的名次；休闲模式为 null */
    val rankInChallengeBucket: Int? = null,
)
