package com.example.mygame.game.modes

import java.util.Calendar
import java.util.Locale
import java.util.TimeZone

/**
 * 每日无尽挑战：同一自然日、同一设备时区下，所有玩家共用同一关卡随机序列（与无尽拼接 RNG 绑定）。
 *
 * **Seed 规则（v1）**
 * - `challengeBucket`：设备默认时区日历的 ISO 日期 `yyyy-MM-dd`（与 [todayBucketLocal] 一致）。
 * - 种子字节串：`"$challengeBucket|$SEED_SALT_V1"`，再折叠为 64-bit，供 [kotlin.random.Random] 使用。
 *
 * 日后若改权重或片段几何，应递增盐或版本号，避免与历史「今日」混淆。
 */
object EndlessDailyChallenge {

    const val SEED_SALT_V1: String = "gugu-endless-daily-v1"

    /** 本地时区「今天」的桶 id，用于存档与榜分割。 */
    fun todayBucketLocal(): String {
        val cal = Calendar.getInstance(TimeZone.getDefault(), Locale.US)
        val y = cal.get(Calendar.YEAR)
        val m = cal.get(Calendar.MONTH) + 1
        val d = cal.get(Calendar.DAY_OF_MONTH)
        return String.format(Locale.US, "%04d-%02d-%02d", y, m, d)
    }

    /** 与 [todayBucketLocal] 当前日一致的 RNG 种子（当日恒定，次日变更）。 */
    fun seedForToday(): Long = seedForBucket(todayBucketLocal())

    fun seedForBucket(challengeBucket: String): Long = stableSeed64("$challengeBucket|$SEED_SALT_V1")

    /** FNV-1a 风格 64-bit，跨进程稳定、与 JVM 版本无关。 */
    private fun stableSeed64(text: String): Long {
        var hash = -0x340d631b7bdddcdbL
        val bytes = text.toByteArray(Charsets.UTF_8)
        for (b in bytes) {
            hash = hash xor (b.toLong() and 0xff)
            hash *= 0x100000001b3L
        }
        return hash
    }
}

/** 无尽入口：休闲每局重新播种；每日挑战每局从当日同一种子重头抽取。 */
enum class EndlessRunPreset {
    Casual,
    DailyChallenge,
}
