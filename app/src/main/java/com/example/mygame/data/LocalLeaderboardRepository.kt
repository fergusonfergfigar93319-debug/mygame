package com.example.mygame.data

import android.content.Context
import com.example.mygame.data.model.LeaderboardEntry
import org.json.JSONArray
import org.json.JSONObject
import java.util.UUID

class LocalLeaderboardRepository(context: Context) : LeaderboardRepository {

    private val prefs =
        context.applicationContext.getSharedPreferences(PREFS_NAME, Context.MODE_PRIVATE)

    override fun getTopEntries(limit: Int, sort: LeaderboardSort): List<LeaderboardEntry> {
        val all = loadAll().sortedWith(comparatorFor(sort))
        return all.take(limit)
    }

    override fun submit(entry: LeaderboardEntry): LeaderboardSubmitResult {
        val all = loadAll().toMutableList()
        all += entry
        val trimmed =
            if (all.size > MAX_STORED) all.sortedBy { it.timestampMillis }.drop(all.size - MAX_STORED) else all
        saveAll(trimmed)
        val ranked = trimmed.sortedWith(comparatorFor(LeaderboardSort.ByTotalScore))
        val rank = ranked.indexOfFirst { it.id == entry.id } + 1
        val madeTop20 = ranked.take(20).any { it.id == entry.id }
        return LeaderboardSubmitResult(rankByScore = rank.coerceAtLeast(1), madeTop20 = madeTop20)
    }

    override fun getBestEntry(): LeaderboardEntry? =
        loadAll().maxByOrNull { it.totalScore }

    override fun getMostRecentEntry(): LeaderboardEntry? =
        loadAll().maxByOrNull { it.timestampMillis }

    override fun getAverageTotalScore(): Double {
        val all = loadAll()
        if (all.isEmpty()) return 0.0
        return all.sumOf { it.totalScore.toDouble() } / all.size
    }

    private fun comparatorFor(sort: LeaderboardSort): Comparator<LeaderboardEntry> =
        when (sort) {
            LeaderboardSort.ByTotalScore -> compareByDescending<LeaderboardEntry> { it.totalScore }
                .thenByDescending { it.distanceScoreUnits }

            LeaderboardSort.ByDistance -> compareByDescending<LeaderboardEntry> { it.distanceScoreUnits }
                .thenByDescending { it.totalScore }

            LeaderboardSort.BySurvivalTime -> compareByDescending<LeaderboardEntry> { it.survivalSeconds }
                .thenByDescending { it.totalScore }
        }

    private fun loadAll(): List<LeaderboardEntry> {
        val raw = prefs.getString(KEY_ENTRIES, null) ?: return emptyList()
        return try {
            val arr = JSONArray(raw)
            buildList {
                for (i in 0 until arr.length()) {
                    add(entryFromJson(arr.getJSONObject(i)))
                }
            }
        } catch (_: Exception) {
            emptyList()
        }
    }

    private fun saveAll(list: List<LeaderboardEntry>) {
        val arr = JSONArray()
        list.forEach { arr.put(it.toJson()) }
        prefs.edit().putString(KEY_ENTRIES, arr.toString()).apply()
    }

    private fun LeaderboardEntry.toJson(): JSONObject = JSONObject().apply {
        put("id", id)
        put("playerId", playerId)
        put("nickname", nickname)
        put("totalScore", totalScore)
        put("distanceScoreUnits", distanceScoreUnits.toDouble())
        put("fishSnacks", fishSnacks)
        put("beaconCount", beaconCount)
        put("lorePageCount", lorePageCount)
        put("survivalSeconds", survivalSeconds.toDouble())
        put("timestampMillis", timestampMillis)
        put("rescuedTuanTuan", rescuedTuanTuan)
        put("mode", mode)
    }

    private fun entryFromJson(o: JSONObject): LeaderboardEntry = LeaderboardEntry(
        id = o.getString("id"),
        playerId = o.optString("playerId", ""),
        nickname = o.getString("nickname"),
        totalScore = o.getInt("totalScore"),
        distanceScoreUnits = o.getDouble("distanceScoreUnits").toFloat(),
        fishSnacks = o.optInt("fishSnacks", 0),
        beaconCount = o.optInt("beaconCount", 0),
        lorePageCount = o.optInt("lorePageCount", 0),
        survivalSeconds = o.getDouble("survivalSeconds").toFloat(),
        timestampMillis = o.getLong("timestampMillis"),
        rescuedTuanTuan = o.optBoolean("rescuedTuanTuan", false),
        mode = o.optString("mode", "endless_polar_night"),
    )

    companion object {
        private const val PREFS_NAME = "gugu_gaga_leaderboard"
        private const val KEY_ENTRIES = "entries_json"
        private const val MAX_STORED = 80

        fun newEntryId(): String = UUID.randomUUID().toString()
    }
}
