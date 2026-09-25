using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>타이틀 (7단계). 새 게임은 인트로부터, 데모 모드는 발표 5분 시연용 치트 패널을 켠다.</summary>
    public static class TitleScreen
    {
        public static void Build(RectTransform root, GameManager gm)
        {
            var layout = UIFactory.VGroup(root, 18, new RectOffset(560, 560, 150, 60));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, "Fed & Found", 72, TextAnchor.MiddleCenter, bold: true);
            UIFactory.Label(layout, "지켜서 데려갈 것인가, 먹고 살아남을 것인가", 24, TextAnchor.MiddleCenter, Theme.TextDim);
            UIFactory.Label(layout, " ", 20);

            UIFactory.Button(layout, "새 게임", () => gm.ShowFront(FrontScreen.Intro), bg: Theme.Accent, fontSize: 26);
            UIFactory.Button(layout, "인트로 건너뛰고 시작", () => gm.ShowFront(FrontScreen.SpeciesSelect), fontSize: 22);
            UIFactory.Button(layout, "게임 방법", () => gm.ShowFront(FrontScreen.HowTo), fontSize: 22);
            UIFactory.Button(layout, $"데모 모드: {(gm.DemoMode ? "ON" : "OFF")}", gm.ToggleDemoMode,
                bg: gm.DemoMode ? Theme.Selected : Theme.PanelLight, fontSize: 20);
            if (gm.CheatsEnabled)
                UIFactory.Label(layout, "데모 패널 사용 가능 — 맵 오른쪽 · 전투 화면 위쪽 보라색 버튼 (5분 시연: docs/DEMO_SCRIPT.md)",
                    15, TextAnchor.MiddleCenter, Theme.TextDim);
            UIFactory.Button(layout, "종료", gm.QuitGame, fontSize: 20);
        }
    }
}
