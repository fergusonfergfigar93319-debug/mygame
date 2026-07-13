using UnityEngine;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Immutable lobby snapshot; presentation never reaches into save stores directly.</summary>
    public readonly struct LobbyViewModel
    {
        public readonly string CurrentStoryTitle;
        public readonly string CurrentStoryBody;
        public readonly string PrimaryActionLabel;
        public readonly float StoryProgress01;
        public readonly string CompanionHint;
        public readonly int Fish;
        public readonly int MissionBadge;
        public readonly int BondLevel;
        public readonly float BondProgress;
        public readonly string BondHelperLabel;
        public readonly float CampProgress;

        public LobbyViewModel(string title, string body, string action, float progress,
            string companionHint, int fish, int missionBadge, int bondLevel = 0,
            float bondProgress = 0f, string bondHelperLabel = "", float campProgress = 0f)
        {
            CurrentStoryTitle = title ?? string.Empty;
            CurrentStoryBody = body ?? string.Empty;
            PrimaryActionLabel = action ?? string.Empty;
            StoryProgress01 = Mathf.Clamp01(progress);
            CompanionHint = companionHint ?? string.Empty;
            Fish = Mathf.Max(0, fish);
            MissionBadge = Mathf.Max(0, missionBadge);
            BondLevel = Mathf.Max(0, bondLevel);
            BondProgress = Mathf.Clamp01(bondProgress);
            BondHelperLabel = bondHelperLabel ?? string.Empty;
            CampProgress = Mathf.Clamp01(campProgress);
        }
    }
}
