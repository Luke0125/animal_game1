using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>3스테이지 클리어(FR-5) 또는 전멸(§13) 결과 화면.</summary>
    public static class ResultScreen
    {
        public static void Build(RectTransform root, GameManager gm, bool victory)
        {
            var layout = UIFactory.VGroup(root, 16, new RectOffset(24, 24, 24, 24));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, victory ? "여정 끝 — 모두 집으로 돌아갔다" : "게임 오버 — 파티 전멸", 32, TextAnchor.MiddleCenter, bold: true,
                color: victory ? Theme.Accent : Theme.Danger);

            var log = UIFactory.ScrollList(layout, 300);
            foreach (var line in gm.Run.Log) UIFactory.Label(log, line, 16, TextAnchor.MiddleLeft, Theme.TextDim);

            UIFactory.Button(layout, "새 게임", gm.ResetToSpeciesSelect, bg: Theme.Accent, fontSize: 24);
        }
    }
}
