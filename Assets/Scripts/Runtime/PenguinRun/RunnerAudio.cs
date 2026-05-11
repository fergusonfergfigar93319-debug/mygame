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
            bgmSource.clip = LoadClip(ResolveRunBgmClipName(daily));
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
