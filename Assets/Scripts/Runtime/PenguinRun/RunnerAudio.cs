using System.Collections.Generic;
using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun
{
    internal sealed class RunnerAudio : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clips = new();
        private AudioSource bgmSource;
        private AudioSource ambienceSource;
        private AudioSource sfxSource;
        private bool sfxEnabled = true;

        private bool bgmEnabled = true;
        private string runBgmTrack = PlayerSave.DefaultRunBgmTrack;
        private string ambienceTrack = PlayerSave.DefaultAmbienceTrack;
        private string sfxStyle = PlayerSave.DefaultSfxStyle;
        private string baseRunClipName;

        public void Initialize(
            bool daily,
            bool sfxEnabledFlag,
            bool bgmEnabledFlag,
            string runBgmTrackId = null,
            string ambienceTrackId = null,
            string sfxStyleId = null)
        {
            sfxEnabled = sfxEnabledFlag;
            bgmEnabled = bgmEnabledFlag;
            runBgmTrack = string.IsNullOrEmpty(runBgmTrackId) ? PlayerSave.DefaultRunBgmTrack : runBgmTrackId;
            ambienceTrack = string.IsNullOrEmpty(ambienceTrackId) ? PlayerSave.DefaultAmbienceTrack : ambienceTrackId;
            sfxStyle = string.IsNullOrEmpty(sfxStyleId) ? PlayerSave.DefaultSfxStyle : sfxStyleId;
            bgmSource = gameObject.AddComponent<AudioSource>();
            ambienceSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();

            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = daily ? 0.32f : 0.36f;
            baseRunClipName = ResolveRunBgmClipName(daily);
            bgmSource.clip = LoadClip(baseRunClipName);
            if (bgmEnabled && bgmSource.clip != null)
            {
                bgmSource.Play();
            }

            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ambienceSource.volume = 0.18f;
            ambienceSource.clip = LoadClip(ResolveAmbienceClipName());
            if (bgmEnabled && ambienceSource.clip != null)
            {
                ambienceSource.Play();
            }

            if (!bgmEnabled)
            {
                bgmSource.volume = 0f;
                ambienceSource.volume = 0f;
            }
        }

        public void UpdateRunState(float speed01, bool running, bool dashActive, bool magnetActive, bool shieldActive)
        {
            if (bgmSource != null)
            {
                bgmSource.pitch = Mathf.Lerp(bgmSource.pitch, running ? 1f + speed01 * 0.045f + (dashActive ? 0.04f : 0f) : 0.96f, Time.deltaTime * 2.2f);
                if (bgmEnabled)
                {
                    var baseVol = running ? 0.34f : 0.28f;
                    bgmSource.volume = Mathf.Lerp(bgmSource.volume, baseVol, Time.deltaTime * 2f);
                }
            }

            if (ambienceSource != null)
            {
                if (bgmEnabled)
                {
                    ambienceSource.volume = Mathf.Lerp(ambienceSource.volume, running ? 0.14f + speed01 * 0.1f : 0.16f, Time.deltaTime * 2f);
                }

                ambienceSource.pitch = Mathf.Lerp(ambienceSource.pitch, dashActive ? 1.12f : 1f, Time.deltaTime * 2f);
            }
        }

        public void PlayJump() => Play("jump", 0.82f);

        public void PlayLand() => Play("land", 0.7f);

        public void PlaySlide() => Play("slide", 0.62f);

        public void PlayCoin(int combo)
        {
            var pitch = Mathf.Clamp(0.92f + combo * 0.055f, 0.92f, 1.35f);
            Play("coin_pickup", 0.55f, pitch);
        }

        public void PlayPowerUp(PowerUpKind kind)
        {
            switch (kind)
            {
                case PowerUpKind.Dash:
                    Play("dash_pickup", 0.78f);
                    break;
                case PowerUpKind.Magnet:
                    Play("magnet_pickup", 0.7f);
                    break;
                case PowerUpKind.Shield:
                    Play("shield_bounce", 0.62f, 1.12f);
                    break;
                case PowerUpKind.ScoreStar:
                    Play("score_star", 0.72f);
                    break;
                case PowerUpKind.GlideFeather:
                    Play("glide_feather", 0.66f);
                    break;
                case PowerUpKind.DoubleFishSnack:
                    Play("magnet_pickup", 0.74f, 1.08f);
                    break;
                case PowerUpKind.TimeHourglass:
                    Play("score_star", 0.62f, 0.9f);
                    break;
                case PowerUpKind.BubbleShield:
                    Play("shield_bounce", 0.68f, 1.22f);
                    break;
                case PowerUpKind.SeahorseBoost:
                    Play("dash_pickup", 0.72f, 1.15f);
                    break;
                case PowerUpKind.CloudWalk:
                    Play("glide_feather", 0.70f, 1.08f);
                    break;
                case PowerUpKind.WindRider:
                    Play("glide_feather", 0.74f, 0.92f);
                    break;
                case PowerUpKind.FishBomb:
                    Play("ice_crack", 0.62f, 1.18f);
                    break;
                case PowerUpKind.SecondHeart:
                    Play("score_star", 0.78f, 1.1f);
                    break;
                // ── 新道具音效（复用现有音频，音调区分） ──────────
                case PowerUpKind.IceMirror:
                    Play("ice_crack", 0.58f, 0.78f);   // 冰晶音调低沉
                    break;
                case PowerUpKind.AuroraChain:
                    Play("magnet_pickup", 0.76f, 1.28f); // 磁吸升调
                    break;
                case PowerUpKind.FogLantern:
                    Play("glide_feather", 0.65f, 0.82f); // 雾气低沉
                    break;
                case PowerUpKind.TreantArmor:
                    Play("shield_bounce", 0.72f, 0.85f); // 厚重护甲
                    break;
                case PowerUpKind.CoralBounce:
                    Play("shield_bounce", 0.68f, 1.32f); // 珊瑚弹跳高调
                    break;
                case PowerUpKind.ThunderFeather:
                    Play("dash_pickup", 0.82f, 1.22f);   // 雷电冲刺
                    break;
            }
        }

        public void PlayShieldBreak() => Play("shield_bounce", 0.76f, 0.92f);

        public void PlayCrash() => Play("crash", 0.86f);

        public void PlayIceCrack() => Play("ice_crack", 0.68f);

        public void PlayCheckpoint() => Play("checkpoint", 0.58f);

        public void PlayPlayerHurt() => Play("ice_crack", 0.74f, 0.94f);

        public void PlayBossLand() => Play("boss_land", 0.95f, 0.95f);

        public void PlayBossShieldBreak() => Play("boss_shield_break", 0.85f, 1.05f);

        public void PlayBossDefeat() => Play("boss_defeat", 0.9f, 1f);

        public void PlayPerfectDodge() => Play("score_star", 0.65f, 1.25f);

        public void PlayPause() => Play("ui_select", 0.55f, 0.85f);

        public void EnterBossMusic(BossDefinition boss)
        {
            if (bgmSource == null || boss == null) return;
            var clipName = ResolveBossBgmClipName(boss);
            var clip = LoadClip(clipName);
            if (clip == null || bgmSource.clip == clip) return;

            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.pitch = boss.Silhouette == BossSilhouette.StormEagle ? 1.08f : 1f;
            bgmSource.volume = bgmEnabled ? 0.4f : 0f;
            if (bgmEnabled) bgmSource.Play();
        }

        public void ExitBossMusic()
        {
            if (bgmSource == null || string.IsNullOrEmpty(baseRunClipName)) return;
            var clip = LoadClip(baseRunClipName);
            if (clip == null || bgmSource.clip == clip) return;

            bgmSource.Stop();
            bgmSource.clip = clip;
            bgmSource.pitch = 1f;
            bgmSource.volume = bgmEnabled ? 0.34f : 0f;
            if (bgmEnabled) bgmSource.Play();
        }

        private void Play(string clipName, float volume, float pitch = 1f)
        {
            if (!sfxEnabled || sfxSource == null)
            {
                return;
            }

            var clip = LoadClip(ResolveSfxClipName(clipName));
            if (clip == null)
            {
                return;
            }

            sfxSource.pitch = pitch;
            sfxSource.PlayOneShot(clip, volume);
        }

        private AudioClip LoadClip(string name)
        {
            if (clips.TryGetValue(name, out var cached))
            {
                return cached;
            }

            var clip = Resources.Load<AudioClip>($"PenguinRun/{name}");
            clips[name] = clip;
            return clip;
        }

        private string ResolveRunBgmClipName(bool daily)
        {
            if (daily && runBgmTrack == PlayerSave.RunBgmEndless)
            {
                // 日常挑战默认保留剧情感更强的曲目，但允许玩家主动切换。
                return "bgm_story";
            }

            if (runBgmTrack == PlayerSave.RunBgmRandom)
            {
                var randomPool = new[]
                {
                    "bgm_endless",
                    "bgm_story",
                    "bgm_open_wisdom",
                    "bgm_open_winter",
                    "bgm_open_swing",
                };
                return randomPool[Random.Range(0, randomPool.Length)];
            }

            return runBgmTrack switch
            {
                PlayerSave.RunBgmStory => "bgm_story",
                PlayerSave.RunBgmOpenWisdom => "bgm_open_wisdom",
                PlayerSave.RunBgmOpenWinter => "bgm_open_winter",
                PlayerSave.RunBgmOpenSwing => "bgm_open_swing",
                _ => "bgm_endless",
            };
        }

        private string ResolveBossBgmClipName(BossDefinition boss)
        {
            // 优先使用 BossDefinition 中的数据驱动字段；缺失时加载专属文件失败则回退到旧映射
            if (!string.IsNullOrEmpty(boss.BgmClipName))
            {
                var dedicated = LoadClip(boss.BgmClipName);
                if (dedicated != null) return boss.BgmClipName;
            }

            // 回退映射（确保旧资源/新资源都能工作）
            return boss.Silhouette switch
            {
                BossSilhouette.SnowKing => "bgm_open_winter",
                BossSilhouette.CedarSentinel => "bgm_open_swing",
                BossSilhouette.AuroraSerpent => "bgm_open_wisdom",
                BossSilhouette.MistGuardian => "bgm_story",
                BossSilhouette.CoralKraken => "bgm_open_swing",
                BossSilhouette.StormEagle => "bgm_endless",
                _ => "bgm_endless",
            };
        }

        private string ResolveAmbienceClipName()
        {
            return ambienceTrack switch
            {
                PlayerSave.AmbienceOpenCavern => "ambience_open_cavern",
                PlayerSave.AmbienceOpenCyber => "ambience_open_cyber",
                _ => "ambience_wind",
            };
        }

        private string ResolveSfxClipName(string original)
        {
            if (sfxStyle != PlayerSave.SfxStyleOpenPack)
            {
                return original;
            }

            return original switch
            {
                "ui_select" => "alt_ui_select",
                "coin_pickup" => "alt_coin_pickup",
                "checkpoint" => "alt_checkpoint",
                "dash_pickup" => "alt_dash_pickup",
                "shield_bounce" => "alt_shield_bounce",
                "score_star" => "alt_score_star",
                _ => original,
            };
        }
    }
}
