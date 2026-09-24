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
            var layout = UIFactory.VGroup(root, 10, new RectOffset(24, 24, 20, 20));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, "파티 3마리를 골라줘 (동료 데려다주기)", 30, TextAnchor.MiddleCenter, bold: true);
            if (gm.LastError != null) UIFactory.Label(layout, gm.LastError, 18, TextAnchor.MiddleCenter, Theme.Danger);

            var list = UIFactory.ScrollList(layout, 560);
            foreach (var s in SpeciesDb.Roster)
            {
                bool picked = _selected.Contains(s.Id);
                var row = UIFactory.HGroup(list, 10);
                UIFactory.FixedHeight(row, 46);
                var info = $"{s.Name}  [{DietName(s.Diet)}]  HP{s.Hp} ATK{s.Atk} DEF{s.Def} PUR{s.Purify}  스킬:{s.Skill.Name}"
                    + (s.SoloSkill != null ? $"  / 홀로:{s.SoloSkill.Name}" : "");
                UIFactory.Button(row, (picked ? "✔ " : "") + info, () => Toggle(s.Id),
                    bg: picked ? Theme.Selected : Theme.PanelLight, fontSize: 18);
            }

            var tutRow = UIFactory.HGroup(layout, 10);
            UIFactory.FixedHeight(tutRow, 44);
            UIFactory.Button(tutRow, (_includeTutorial ? "✔ " : "") + "0스테이지(튜토리얼) 포함",
                () => { _includeTutorial = !_includeTutorial; gm.Refresh(); }, bg: Theme.PanelLight);

            UIFactory.Label(layout, $"선택됨: {_selected.Count}/3", 20, TextAnchor.MiddleCenter, Theme.TextDim);
            UIFactory.Button(layout, "시작", () => gm.StartRun(_selected.ToList(), _includeTutorial),
                interactable: _selected.Count == 3, bg: Theme.Accent, fontSize: 26);
        }

        static void Toggle(string id)
        {
            if (_selected.Contains(id)) _selected.Remove(id);
            else if (_selected.Count < 3) _selected.Add(id);
            GameManager.Instance.Refresh();
        }

        public static string DietName(Diet d) => d switch { Diet.Herbivore => "초식", Diet.Carnivore => "육식", _ => "잡식" };
    }
}
