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
            var layout = UIFactory.VGroup(root, 14, new RectOffset(30, 30, 30, 30));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, ev.Title, 28, TextAnchor.MiddleCenter, bold: true);
            UIFactory.Label(layout, ev.Body, 20, TextAnchor.MiddleCenter, Theme.TextDim);

            for (int i = 0; i < ev.Options.Count; i++)
            {
                int idx = i;
                var opt = ev.Options[i];
                UIFactory.Button(layout, opt.Text, () => gm.ChooseEvent(idx), opt.CanChoose(gm.Run), fontSize: 22);
            }
        }
    }
}
