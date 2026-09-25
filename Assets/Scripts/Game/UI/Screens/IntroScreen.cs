using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>인트로 (§11 스토리 설정안: 연구소 탈출 + 오염으로 인한 광폭화). 글 페이지를 넘기는 방식의 간단한 컷신.</summary>
    public static class IntroScreen
    {
        static readonly string[] Pages =
        {
            "도시 외곽의 한 연구소.\n실험실 우리 안에는 서로 다른 동물들이 갇혀 있었다.",
            "어느 폭풍우 치던 밤, 정전으로 우리 문이 열렸다.\n동물들은 서로를 부르며 숲으로 달아났다.",
            "하지만 바깥 세상은 달라져 있었다.\n연구소에서 흘러나온 오염된 물이 들판과 늪, 설산과 사막을 적시고,\n그 물을 마신 동물들은 '저주'에 걸려 무엇이든 공격했다.",
            "저주받은 동물은 쓰러뜨릴 수도, 저주를 풀어 줄 수도 있다.\n쓰러뜨리면 고기를, 풀어 주면 열매를 얻는다.",
            "집까지 가는 길은 멀고, 먹을 것은 늘 모자라다.\n한 고비를 넘길 때마다 동료 한 마리와 헤어져야 한다 —\n집으로 먼저 보낼 것인가, 살아남기 위해 먹을 것인가.",
            "모두를 지켜서 집으로.\n그 여정이 지금 시작된다.",
        };

        public static void Build(RectTransform root, GameManager gm)
        {
            int page = Mathf.Clamp(gm.IntroPage, 0, Pages.Length - 1);
            var layout = UIFactory.VGroup(root, 24, new RectOffset(260, 260, 260, 80));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, Pages[page], 30, TextAnchor.MiddleCenter);
            UIFactory.Label(layout, $"{page + 1} / {Pages.Length}", 18, TextAnchor.MiddleCenter, Theme.TextDim);

            var row = UIFactory.HGroup(layout, 16, new RectOffset(300, 300, 0, 0));
            UIFactory.FixedHeight(row, 56);
            UIFactory.Button(row, "건너뛰기", () => gm.ShowFront(FrontScreen.SpeciesSelect), fontSize: 20);
            UIFactory.Button(row, page == Pages.Length - 1 ? "동료 고르기" : "다음", () => gm.NextIntroPage(Pages.Length), bg: Theme.Accent, fontSize: 22);
        }
    }
}
