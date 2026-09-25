using System.Collections.Generic;
using System.Linq;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>파티 3마리 선택 + 0스테이지(튜토리얼) 포함 여부. RunState 생성 전 단계라
    /// GameManager.Run이 null일 때만 그려진다.</summary>
    public static class SpeciesSelectScreen
    {
        static readonly List<string> _selected = new List<string>();
        static bool _includeTutorial = true;

        public static void Build(RectTransform root, GameManager gm)
        {
            var layout = UIFactory.Window(root, new Vector2(0.04f, 0.16f), new Vector2(0.96f, 0.97f), "함께 떠날 동료 3마리를 골라줘", padding: 28);
            UIFactory.Label(layout, "카드에 마우스를 올리면 스킬과 홀로서기 설명이 나와요. 식성을 섞으면 원석 시너지를 모두 쓸 수 있어요.", 16, TextAnchor.MiddleCenter, Theme.TextDim);
            if (gm.LastError != null) UIFactory.Label(layout, gm.LastError, 18, TextAnchor.MiddleCenter, Theme.Danger);

            // 4열 × 3행 카드 (식성별로 한 줄씩: 초식 / 육식 / 잡식)
            foreach (var diet in new[] { Diet.Herbivore, Diet.Carnivore, Diet.Omnivore })
            {
                var row = UIFactory.HGroup(layout, 10);
                UIFactory.FixedHeight(row, 150);
                foreach (var s in SpeciesDb.Roster.Where(x => x.Diet == diet)) Card(row, s);
            }

            var bottom = UIFactory.HGroup(layout, 12, new RectOffset(80, 80, 6, 0));
            UIFactory.FixedHeight(bottom, 52);
            UIFactory.Button(bottom, "타이틀로", () => gm.ShowFront(FrontScreen.Title), fontSize: 18);
            var tut = UIFactory.Button(bottom, (_includeTutorial ? "[v] " : "[  ] ") + "0스테이지(튜토리얼) 포함",
                () => { _includeTutorial = !_includeTutorial; gm.Refresh(); }, fontSize: 18);
            UIFactory.Tooltip(tut, "손님 사슴과 함께 약한 보스 1마리를 상대하는 연습 전투. 끄면 유물 1개만 받고 1스테이지부터 시작.");
            UIFactory.Button(bottom, $"출발! ({_selected.Count}/3)", () => gm.StartRun(_selected.ToList(), _includeTutorial),
                interactable: _selected.Count == 3, bg: new Color(0.55f, 0.43f, 0.2f), fontSize: 22);
        }

        static void Card(Transform parent, SpeciesData s)
        {
            bool picked = _selected.Contains(s.Id);
            var btn = UIFactory.Button(parent, "", () => Toggle(s.Id), bg: picked ? new Color(0.62f, 0.5f, 0.22f) : new Color(0.22f, 0.18f, 0.14f));
            UIFactory.Tooltip(btn, InfoText.Species(s));
            var h = UIFactory.HGroup(btn.transform, 8, new RectOffset(10, 10, 8, 8));
            UIFactory.Stretch((RectTransform)h);
            UIFactory.Icon(h, AnimalArt(s), 110);
            var v = UIFactory.VGroup(h, 2);
            UIFactory.Label(v, (picked ? "[선택] " : "") + s.Name, 22, TextAnchor.MiddleLeft, picked ? Color.white : Theme.Gold, bold: true);
            UIFactory.Label(v, $"{DietName(s.Diet)} · 속도 {s.SpeedRank}위", 14, TextAnchor.MiddleLeft, Theme.TextDim);
            UIFactory.Label(v, $"HP {s.Hp}  공격 {s.Atk}  방어 {s.Def}  정화 {s.Purify}", 14, TextAnchor.MiddleLeft);
            UIFactory.Label(v, $"스킬: {s.Skill.Name}", 15, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.6f));
            if (s.SoloSkill != null) UIFactory.Label(v, $"홀로: {s.SoloSkill.Name}", 14, TextAnchor.MiddleLeft, new Color(0.7f, 0.85f, 1f));
            // 자식 글자·그림이 마우스를 가로채지 않게 (툴팁·클릭은 카드 전체에서)
            foreach (var g in btn.GetComponentsInChildren<UnityEngine.UI.Graphic>()) if (g.gameObject != btn.gameObject) g.raycastTarget = false;
        }

        static Sprite AnimalArt(SpeciesData s) => FedAndFound.Game.BattleView.AnimalArt.Get(s.Id);

        static void Toggle(string id)
        {
            if (_selected.Contains(id)) _selected.Remove(id);
            else if (_selected.Count < 3) _selected.Add(id);
            GameManager.Instance.Refresh();
        }

        public static string DietName(Diet d) => d switch { Diet.Herbivore => "초식", Diet.Carnivore => "육식", _ => "잡식" };
    }
}
