using System;
using UnityEngine;
using UnityEngine.UI;

namespace FedAndFound.Game.UI
{
    /// <summary>씬/프리팹 YAML을 직접 편집하지 않고 코드로 uGUI를 조립하기 위한 헬퍼 (CLAUDE.md 지침).
    /// 모든 화면(Screens/*)은 이 팩토리만으로 구성한다.</summary>
    public static class UIFactory
    {
        public static RectTransform Stretch(RectTransform rt, Vector2? min = null, Vector2? max = null)
        {
            rt.anchorMin = min ?? Vector2.zero;
            rt.anchorMax = max ?? Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        static RectTransform NewRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Panel(Transform parent, string name, Color color, bool stretch = true)
        {
            var rt = NewRect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (stretch) Stretch(rt);
            return rt;
        }

        public static Text Label(Transform parent, string text, int size = 24, TextAnchor align = TextAnchor.MiddleLeft,
            Color? color = null, bool bold = false, bool stretch = true)
        {
            var rt = NewRect(parent, "Label");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = bold ? Theme.FontBold : Theme.Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color ?? Theme.Text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (stretch) Stretch(rt);
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, bool interactable = true,
            Color? bg = null, int fontSize = 24)
        {
            var rt = NewRect(parent, "Button_" + label);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = interactable ? (bg ?? Theme.PanelLight) : Theme.Disabled;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.interactable = interactable;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 44; le.preferredHeight = 44;
            Label(rt, label, fontSize, TextAnchor.MiddleCenter, interactable ? Theme.Text : Theme.TextDim);
            return btn;
        }

        /// <summary>비율(0~1) 채움 막대. 위에 수치 텍스트를 얹을 수 있다.</summary>
        public static Image Bar(Transform parent, float ratio01, Color fillColor, string overlayText = null, int textSize = 16)
        {
            var back = Panel(parent, "Bar", Theme.Panel);
            var le = back.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 18; le.preferredHeight = 18;
            var fillRect = NewRect(back, "Fill");
            Stretch(fillRect);
            var fill = fillRect.gameObject.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = Mathf.Clamp01(ratio01);
            if (overlayText != null) Label(back, overlayText, textSize, TextAnchor.MiddleCenter);
            return fill;
        }

        static T Group<T>(Transform parent, float spacing, RectOffset padding, bool horizontal) where T : HorizontalOrVerticalLayoutGroup
        {
            var rt = NewRect(parent, horizontal ? "HGroup" : "VGroup");
            var g = rt.gameObject.AddComponent<T>();
            g.spacing = spacing;
            g.padding = padding ?? new RectOffset(0, 0, 0, 0);
            g.childForceExpandWidth = true;       // 가로/세로 그룹 모두 자식이 컨테이너 너비를 채운다
            g.childForceExpandHeight = horizontal; // 가로 그룹은 행 높이를 채우고, 세로 그룹은 자식의 고유 높이를 유지(ContentSizeFitter로 합산)
            g.childControlWidth = true;
            g.childControlHeight = true;
            var fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = horizontal ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
            return rt.gameObject.GetComponent<T>();
        }

        public static RectTransform VGroup(Transform parent, float spacing = 6, RectOffset padding = null) =>
            (RectTransform)Group<VerticalLayoutGroup>(parent, spacing, padding, false).transform;

        public static RectTransform HGroup(Transform parent, float spacing = 6, RectOffset padding = null) =>
            (RectTransform)Group<HorizontalLayoutGroup>(parent, spacing, padding, true).transform;

        /// <summary>세로 스크롤 목록. content(자식을 여기 넣을 것)를 반환한다.</summary>
        public static RectTransform ScrollList(Transform parent, float height = -1)
        {
            var rt = NewRect(parent, "Scroll");
            if (height > 0)
            {
                var le = rt.gameObject.AddComponent<LayoutElement>();
                le.preferredHeight = height; le.minHeight = height;
            }
            else Stretch(rt);
            var scroll = rt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true;

            var viewport = Panel(rt, "Viewport", new Color(0, 0, 0, 0));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = VGroup(viewport, 4, new RectOffset(2, 2, 2, 2));
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1); // VGroup이 이미 PreferredSize ContentSizeFitter를 붙여준다
            content.sizeDelta = new Vector2(0, content.sizeDelta.y);
            content.anchoredPosition = Vector2.zero;

            scroll.content = content;
            scroll.viewport = viewport;
            return content;
        }

        public static void FixedHeight(RectTransform rt, float h)
        {
            var le = rt.gameObject.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = h; le.minHeight = h;
        }
    }
}
