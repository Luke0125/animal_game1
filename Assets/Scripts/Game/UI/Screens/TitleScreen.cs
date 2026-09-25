using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>타이틀 (7단계). 뒤로는 BackdropView의 들판과 걸어가는 동물들이 보인다.</summary>
    public static class TitleScreen
    {
        public static void Build(RectTransform root, GameManager gm)
        {
            // 로고: 그림자 글자를 뒤에 깔아 풍경 위에서도 또렷하게
            var shadow = UIFactory.Label(root, "Fed & Found", 96, TextAnchor.MiddleCenter, new Color(0.15f, 0.08f, 0.03f, 0.8f), bold: true, stretch: false);
            UIFactory.Stretch(shadow.rectTransform, new Vector2(0, 0.74f), new Vector2(1, 0.92f));
            shadow.rectTransform.offsetMin = new Vector2(5, -5); shadow.rectTransform.offsetMax = new Vector2(5, -5);
            var logo = UIFactory.Label(root, "Fed & Found", 96, TextAnchor.MiddleCenter, Theme.Gold, bold: true, stretch: false);
            UIFactory.Stretch(logo.rectTransform, new Vector2(0, 0.74f), new Vector2(1, 0.92f));
            var sub = UIFactory.Label(root, "지켜서 데려갈 것인가, 먹고 살아남을 것인가", 26, TextAnchor.MiddleCenter, Color.white, stretch: false);
            UIFactory.Stretch(sub.rectTransform, new Vector2(0, 0.68f), new Vector2(1, 0.74f));

            var v = UIFactory.Window(root, new Vector2(0.36f, 0.22f), new Vector2(0.64f, 0.66f));
            UIFactory.Button(v, "새 게임", () => gm.ShowFront(FrontScreen.Intro), bg: new Color(0.55f, 0.43f, 0.2f), fontSize: 26);
            UIFactory.Button(v, "인트로 건너뛰고 시작", () => gm.ShowFront(FrontScreen.SpeciesSelect), fontSize: 21);
            UIFactory.Button(v, "게임 방법", () => gm.ShowFront(FrontScreen.HowTo), fontSize: 21);
            var demo = UIFactory.Button(v, $"데모 모드: {(gm.DemoMode ? "ON" : "OFF")}", gm.ToggleDemoMode,
                bg: gm.DemoMode ? new Color(0.45f, 0.2f, 0.45f) : Theme.PanelLight, fontSize: 19);
            UIFactory.Tooltip(demo, "발표 5분 시연용: 맵·전투 화면에 빠른 진행 버튼(보라색)이 나온다.\n순서는 docs/DEMO_SCRIPT.md");
            UIFactory.Button(v, "종료", gm.QuitGame, fontSize: 19);
        }
    }
}
