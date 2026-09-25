using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>게임 방법 한 장 요약 (7단계). 평가 항목 "조작 편의" 대응 — 처음 보는 사람도 1분 안에 규칙을 알 수 있게.</summary>
    public static class HowToScreen
    {
        /// <summary>규칙 한 장 요약 (ESC 메뉴의 "게임 방법"도 같이 쓴다).</summary>
        public static readonly string[] Lines =
        {
            "■ 목표: 동물 3마리로 1~3스테이지를 클리어. 스테이지를 깰 때마다 한 마리와 작별 — 포식(남은 동료 배고픔 회복) 또는 집 보내기(유물 슬롯 +1).",
            $"■ 맵: 방향키/WASD로 걷거나 노드를 클릭. 보스 전까지 {RunState.NodesBeforeBoss}번 이동, ? 이벤트는 스테이지당 1번. 이동할 때마다 배고픔이 줄어든다.",
            "■ 전투: 속도 순서대로 행동. 공격 / 방어 / 정화 / 스킬 중 선택. 적을 많이 깎을수록 정화 확률이 오른다.",
            "■ 물리치면 고기, 정화하면 열매. 초식은 열매, 육식은 고기, 잡식은 둘 다 먹는다.",
            "■ 배고픔 0이면 능력치가 절반. 음식은 배고픔만 채우고, HP는 전투 후 [휴식]으로만 회복한다.",
            $"■ 조각 {Balance.FragmentsPerGem}개 = 원석 1개 → 무리사냥 / 생명의 순환 / 적응 시너지 해금.",
            "■ 전투에서 혼자 남으면 '홀로서기' — 동물의 실제 습성을 살린 스킬로 바뀌고, 작은 동물일수록 필사적으로 강해진다.",
            "■ 매복 예고(노리는 중!)가 뜨면 방어를 눌러 대비하자.",
            "■ 스킬·유물·동물 카드에 마우스를 올리면 자세한 설명이 나온다.",
            "■ 업적을 달성하면 칭호를 얻는다. ESC 메뉴에서 업적 확인·칭호 장착·환경설정을 할 수 있다.",
        };

        public static void Build(RectTransform root, GameManager gm)
        {
            var layout = UIFactory.Window(root, new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.95f), "게임 방법", padding: 36);

            foreach (var l in Lines) UIFactory.Label(layout, l, 20, TextAnchor.MiddleLeft);

            var row = UIFactory.HGroup(layout, 16, new RectOffset(260, 260, 14, 0));
            UIFactory.FixedHeight(row, 56);
            UIFactory.Button(row, "타이틀로", () => gm.ShowFront(FrontScreen.Title), fontSize: 20);
            UIFactory.Button(row, "동료 고르기", () => gm.ShowFront(FrontScreen.SpeciesSelect), bg: Theme.Accent, fontSize: 22);
        }
    }
}
