package com.example.mygame.audio

import android.content.Context
import android.media.AudioAttributes
import android.media.MediaPlayer
import android.media.SoundPool
import com.example.mygame.R
import kotlin.random.Random

/**
 * 轻量 SFX，基于 [SoundPool]；在 [android.app.Activity] 层单例创建，避免在 Composable 中重复 [SoundPool] 构造。
 * 背景音乐使用 [MediaPlayer] 循环播放，与 SFX 分离。
 */
class SoundManager(context: Context) {

    private val appContext = context.applicationContext
    private val soundPool: SoundPool

    private var jumpId = 0
    private var landId = 0
    private var eatFishId = 0

    private var jumpReady = false
    private var landReady = false
    private var eatFishReady = false

    private var bgmPlayer: MediaPlayer? = null
    private var activeBgm: BgmTrack = BgmTrack.None

    init {
        val attrs = AudioAttributes.Builder()
            .setUsage(AudioAttributes.USAGE_GAME)
            .setContentType(AudioAttributes.CONTENT_TYPE_SONIFICATION)
            .build()
        soundPool = SoundPool.Builder()
            .setMaxStreams(10)
            .setAudioAttributes(attrs)
            .build()

        soundPool.setOnLoadCompleteListener { _, sampleId, status ->
            if (status != 0) return@setOnLoadCompleteListener
            when (sampleId) {
                jumpId -> jumpReady = true
                landId -> landReady = true
                eatFishId -> eatFishReady = true
            }
        }

        jumpId = soundPool.load(appContext, R.raw.jump, 1)
        landId = soundPool.load(appContext, R.raw.land, 1)
        eatFishId = soundPool.load(appContext, R.raw.eat_fish, 1)
    }

    /** 可切换的循环配乐类型。 */
    enum class BgmTrack {
        None,
        /** 主线。 */
        Story,
        /** 无尽 / 今日挑战。 */
        Endless,
    }

    /**
     * 按界面切换 BGM；相同曲目且已在播则不重启。
     */
    fun setBgm(track: BgmTrack) {
        if (track == activeBgm) {
            val playing = runCatching { bgmPlayer?.isPlaying == true }.getOrDefault(false)
            if (playing) return
        }
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
        val res = when (track) {
            BgmTrack.Story -> R.raw.bgm_story
            BgmTrack.Endless -> R.raw.bgm_endless
            BgmTrack.None -> return
        }
        val mp = MediaPlayer.create(appContext, res) ?: return
        mp.isLooping = true
        runCatching {
            mp.setVolume(0.28f, 0.28f)
            mp.setOnErrorListener { _, _, _ -> true }
        }
        runCatching { mp.start() }
        bgmPlayer = mp
        activeBgm = track
    }

    fun pauseBgm() = runCatching { if (bgmPlayer?.isPlaying == true) bgmPlayer?.pause() }

    fun resumeBgm() = runCatching { bgmPlayer?.start() }

    fun playJump() {
        if (!jumpReady || jumpId == 0) return
        soundPool.play(jumpId, 1f, 1f, 1, 0, 1f)
    }

    fun playLand() {
        if (!landReady || landId == 0) return
        soundPool.play(landId, 0.8f, 0.8f, 1, 0, 1f)
    }

    fun playEatFish() {
        if (!eatFishReady || eatFishId == 0) return
        val pitch = 0.9f + Random.nextFloat() * 0.2f
        soundPool.play(eatFishId, 1f, 1f, 1, 0, pitch)
    }

    /** 踩怪；先复用落地采样做「闷击」，有独立 `stomp.ogg` 时再换资源 id。 */
    fun playStomp() {
        if (!landReady || landId == 0) return
        soundPool.play(landId, 0.95f, 0.95f, 1, 0, 1.12f)
    }

    fun release() {
        runCatching {
            bgmPlayer?.run { if (isPlaying) stop(); release() }
        }
        bgmPlayer = null
        activeBgm = BgmTrack.None
        soundPool.release()
    }
}
