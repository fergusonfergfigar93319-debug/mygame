using PenguinRun.Game.Save;
using UnityEngine;

namespace PenguinRun.Game.UI
{
    /// <summary>
    /// 主菜单大厅 BGM：随「音乐开关」与「大厅曲目」设置变化。
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MenuLobbyAudio : MonoBehaviour
    {
        private AudioSource src;

        private void Awake()
        {
            src = GetComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
        }

        private void OnEnable()
        {
            GameAudioSettings.LobbyBgmChanged += RefreshNow;
            RefreshNow();
        }

        private void OnDisable() => GameAudioSettings.LobbyBgmChanged -= RefreshNow;

        public void RefreshNow()
        {
            if (src == null) return;
            src.Stop();
            if (!PlayerSave.BgmEnabled) return;

            var track = PlayerSave.LobbyBgmTrack;
            if (track == PlayerSave.LobbyBgmNone) return;

            var clipName = track switch
            {
                PlayerSave.LobbyBgmStory => "bgm_story",
                PlayerSave.LobbyBgmEndless => "bgm_endless",
                PlayerSave.LobbyBgmOpenWisdom => "bgm_open_wisdom",
                PlayerSave.LobbyBgmOpenSwing => "bgm_open_swing",
                _ => "bgm_lobby_cozy",
            };
            var clip = Resources.Load<AudioClip>($"PenguinRun/{clipName}");
            if (clip == null) return;
            src.clip = clip;
            src.volume = 0.4f;
            src.Play();
        }
    }
}
