using System;
using PenguinRun.Game.UI.Storybook;
using UnityEngine;

namespace PenguinRun.Game.UI.Lobby
{
    /// <summary>Owns the single active lobby panel and makes open/close operations idempotent.</summary>
    public sealed class LobbyPanelRouter
    {
        private readonly Transform panelLayer;
        private readonly Action onChanged;
        private readonly Action bringUiToFront;
        private GameObject currentPanel;

        public LobbyPanelRouter(Transform panelLayer, Action onChanged, Action bringUiToFront)
        {
            this.panelLayer = panelLayer;
            this.onChanged = onChanged;
            this.bringUiToFront = bringUiToFront;
        }

        public bool HasOpenPanel => currentPanel != null;
        public string CurrentPanelName { get; private set; }

        public void Open(GameObject panel, string panelName)
        {
            if (panel == null)
            {
                Close();
                return;
            }

            if (ReferenceEquals(currentPanel, panel))
            {
                CurrentPanelName = panelName ?? string.Empty;
                panel.SetActive(true);
                bringUiToFront?.Invoke();
                onChanged?.Invoke();
                return;
            }

            CloseCurrent(notify: false);
            currentPanel = panel;
            CurrentPanelName = panelName ?? string.Empty;

            if (panelLayer != null && panel.transform.parent != panelLayer)
                panel.transform.SetParent(panelLayer, false);

            panel.SetActive(true);

            if (panel.TryGetComponent<RectTransform>(out var panelRect))
                StorybookUi.PlayPageTurnIn(panelRect);

            bringUiToFront?.Invoke();
            onChanged?.Invoke();
        }

        public void Close()
        {
            CloseCurrent(notify: true);
        }

        private void CloseCurrent(bool notify)
        {
            if (currentPanel == null) return;

            var closing = currentPanel;
            currentPanel = null;
            CurrentPanelName = null;

            if (Application.isPlaying && closing.activeInHierarchy
                && closing.TryGetComponent<RectTransform>(out var rect))
            {
                // 短退场（淡出+收缩）后销毁；PlayPanelPopOut 首帧即关闭交互，
                // 快速导航不会同时点到两页。
                rect.PlayPanelPopOut(() =>
                {
                    if (closing != null) UnityEngine.Object.Destroy(closing);
                });
            }
            else
            {
                closing.SetActive(false);
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(closing);
                else
                    UnityEngine.Object.DestroyImmediate(closing);
            }

            if (notify) onChanged?.Invoke();
        }
    }
}
