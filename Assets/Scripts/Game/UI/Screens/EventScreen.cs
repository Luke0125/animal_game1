using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>선택지형 랜덤 이벤트(§15).</summary>
    public static class EventScreen
    {
        public static void Build(RectTransform root, GameManager gm)
        {
            var ev = gm.Run.CurrentEvent;
            var layout = UIFactory.Window(root, new Vector2(0.2f, 0.22f), new Vector2(0.8f, 0.9f), parchment: true, padding: 44);
            UIFactory.Label(layout, "?", 64, TextAnchor.MiddleCenter, new Color(0.5f, 0.3f, 0.15f), bold: true);
            UIFactory.Label(layout, ev.Title, 32, TextAnchor.MiddleCenter, Theme.Ink, bold: true);
            UIFactory.Label(layout, ev.Body, 21, TextAnchor.MiddleCenter, new Color(0.35f, 0.25f, 0.15f));
            UIFactory.Label(layout, " ", 8);

            for (int i = 0; i < ev.Options.Count; i++)
            {
                int idx = i;
                var opt = ev.Options[i];
                UIFactory.Button(layout, opt.Text, () => gm.ChooseEvent(idx), opt.CanChoose(gm.Run), bg: new Color(0.42f, 0.3f, 0.18f), fontSize: 21);
            }
        }
    }
}
