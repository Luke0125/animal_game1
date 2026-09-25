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
            // 양피지 두루마리에 쓴 이야기
            var layout = UIFactory.Window(root, new Vector2(0.18f, 0.3f), new Vector2(0.82f, 0.82f), parchment: true, padding: 48);
            UIFactory.Label(layout, " ", 10);
            UIFactory.Label(layout, Pages[page], 30, TextAnchor.MiddleCenter, Theme.Ink);
            UIFactory.Label(layout, " ", 10);
            UIFactory.Label(layout, $"{page + 1} / {Pages.Length}", 18, TextAnchor.MiddleCenter, new Color(0.5f, 0.38f, 0.25f));

            var row = UIFactory.HGroup(layout, 16, new RectOffset(160, 160, 0, 0));
            UIFactory.FixedHeight(row, 56);
            UIFactory.Button(row, "건너뛰기", () => gm.ShowFront(FrontScreen.SpeciesSelect), fontSize: 20);
            UIFactory.Button(row, page == Pages.Length - 1 ? "동료 고르기" : "다음", () => gm.NextIntroPage(Pages.Length), bg: new Color(0.55f, 0.43f, 0.2f), fontSize: 22);
        }
    }
}
