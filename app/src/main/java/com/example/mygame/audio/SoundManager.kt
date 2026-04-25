package com.example.mygame.audio

import android.content.Context
import android.media.AudioAttributes
import android.media.MediaPlayer
import android.media.SoundPool
import android.os.Handler
import android.os.Looper
import com.example.mygame.R
import com.example.mygame.data.SaveRepository
import kotlin.random.Random

/**
 * 轻量游戏音频管理器。
 *
 * SFX 使用 [SoundPool] 保持低延迟；BGM 使用 [MediaPlayer] 循环播放。
 * 实例由 Activity 层持有，避免在 Composable 重组时反复创建音频对象。
 */
class SoundManager(context: Context) {

    private val appContext = context.applicationContext
    private val soundPool: SoundPool

    private val samples = mutableMapOf<Sfx, Int>()
    private val ready = mutableSetOf<Int>()

    private var bgmPlayer: MediaPlayer? = null
    private var activeBgm: BgmTrack = BgmTrack.None
    private var bgmBaseVolume: Float = 0.28f
    private val mainHandler = Handler(Looper.getMainLooper())
    private var duckRestoreRunnable: Runnable? = null

    private var bgmUserEnabled: Boolean = true
    private var sfxUserEnabled: Boolean = true

    init {
        val attrs =
            AudioAttributes
                .Builder()
                .setUsage(AudioAttributes.USAGE_GAME)
                .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
                .build()
        soundPool =
            SoundPool
                .Builder()
                .setMaxStreams(12)
                .setAudioAttributes(attrs)
                .build()

        soundPool.setOnLoadCompleteListener { _, sampleId, status ->
            if (status == 0) ready += sampleId
        }

        Sfx.entries.forEach { sfx ->
            samples[sfx] = soundPool.load(appContext, sfx.resId, 1)
        }
    }

    enum class BgmTrack {
        None,
        Story,
        Endless,
    }

    private enum class Sfx(val resId: Int) {
        Jump(R.raw.jump),
        Land(R.raw.land),
        EatFish(R.raw.eat_fish),
        ShieldBounce(R.raw.shield_bounce),
        BossLand(R.raw.boss_land),
        BossShieldBreak(R.raw.boss_shield_break),
        BossDefeat(R.raw.boss_defeat),
        UiSelect(R.raw.ui_select),
        PowerUp(R.raw.power_up),
        MagnetPickup(R.raw.magnet_pickup),
        CoinPickup(R.raw.coin_pickup),
        IceCrack(R.raw.ice_crack),
    }

    fun setBgm(track: BgmTrack) {
        if (track == activeBgm) {
            val playing = runCatching { bgmPlayer?.isPlaying == true }.getOrDefault(false)
            if (playing) return
        }
        duckRestoreRunnable?.let { mainHandler.removeCallbacks(it) }
        duckRestoreRunnable = null
        runCatching {
            bgmPlayer?.run {
                if (isPlaying) stop()
                release()
            }
        }
        bgmPlayer = null
        if (track == BgmTrack.None) {
            activeBgm = BgmTrack.None
            return
        }
        val res =
            when (track) {
                BgmTrack.Story -> R.raw.bgm_story
                BgmTrack.Endless -> R.raw.bgm_endless
                BgmTrack.None -> return
            }
        val mp = MediaPlayer.create(appContext, res) ?: return
        mp.isLooping = true
        bgmBaseVolume = 0.28f
        runCatching {
            mp.setOnErrorListener { _, _, _ -> true }
            mp.start()
        }
        bgmPlayer = mp
        activeBgm = track
        applyBgmUserVolume()
    }

    fun syncAudioFromSave(save: SaveRepository) {
        bgmUserEnabled = save.getBgmEnabled()
        sfxUserEnabled = save.getSfxEnabled()
        applyBgmUserVolume()
    }

    fun setBgmEnabled(enabled: Boolean) {
        bgmUserEnabled = enabled
        applyBgmUserVolume()
    }

    fun setSfxEnabled(enabled: Boolean) {
        sfxUserEnabled = enabled
    }

    private fun applyBgmUserVolume() {
        val p = bgmPlayer ?: return
        if (bgmUserEnabled) {
            runCatching {
                p.setVolume(bgmBaseVolume, bgmBaseVolume)
                if (!p.isPlaying) p.start()
            }
        } else {
            runCatching {
                p.setVolume(0f, 0f)
                if (p.isPlaying) p.pause()
            }
        }
    }

    /** 重踩、破盾等强反馈事件会短暂压低 BGM，让打击声更突出。 */
    fun duckBgmOnHeavyStomp(targetFraction: Float = 0.3f, restoreDelayMs: Long = 200L) {
        if (!bgmUserEnabled) return
        val p = bgmPlayer ?: return
        duckRestoreRunnable?.let { mainHandler.removeCallbacks(it) }
        val target = (bgmBaseVolume * targetFraction).coerceIn(0.02f, bgmBaseVolume)
        runCatching { p.setVolume(target, target) }
        val runnable =
            Runnable {
                if (bgmUserEnabled) {
                    runCatching { p.setVolume(bgmBaseVolume, bgmBaseVolume) }
                }
                duckRestoreRunnable = null
            }
        duckRestoreRunnable = runnable
        mainHandler.postDelayed(runnable, restoreDelayMs)
    }

    fun pauseBgm() = runCatching { if (bgmPlayer?.isPlaying == true) bgmPlayer?.pause() }

    fun resumeBgm() {
        if (!bgmUserEnabled) return
        runCatching { bgmPlayer?.start() }
    }

    fun playJump() = play(Sfx.Jump, volume = 1f)

    fun playLand() = play(Sfx.Land, volume = 0.78f)

    fun playEatFish() = play(Sfx.EatFish, volume = 1f, pitch = 0.9f + Random.nextFloat() * 0.2f)

    fun playStomp() = play(Sfx.Land, volume = 0.95f, pitch = 1.12f)

    fun playShieldBounce() = play(Sfx.ShieldBounce, volume = 0.6f, pitch = 1.2f)

    fun playBossLand() = play(Sfx.BossLand, volume = 0.9f, pitch = 0.85f)

    fun playBossShieldBreak() = play(Sfx.BossShieldBreak, volume = 0.95f, pitch = 1.35f)

    fun playBossDefeat() = play(Sfx.BossDefeat, volume = 0.75f, pitch = 0.95f)

    fun playUiSelect() = play(Sfx.UiSelect, volume = 0.42f)

    fun playPowerUp() = play(Sfx.PowerUp, volume = 0.68f)

    fun playMagnetPickup() = play(Sfx.MagnetPickup, volume = 0.66f)

    fun playCoinPickup() = play(Sfx.CoinPickup, volume = 0.46f, pitch = 0.96f + Random.nextFloat() * 0.14f)

    fun playIceCrack() = play(Sfx.IceCrack, volume = 0.72f)

    private fun play(
        sfx: Sfx,
        volume: Float,
        pitch: Float = 1f,
    ) {
        if (!sfxUserEnabled) return
        val id = samples[sfx] ?: return
        if (id == 0 || id !in ready) return
        soundPool.play(id, volume, volume, 1, 0, pitch.coerceIn(0.5f, 2f))
    }

    fun release() {
        duckRestoreRunnable?.let { mainHandler.removeCallbacks(it) }
        duckRestoreRunnable = null
        runCatching {
            bgmPlayer?.run {
                if (isPlaying) stop()
                release()
            }
        }
        bgmPlayer = null
        activeBgm = BgmTrack.None
        soundPool.release()
    }
}
