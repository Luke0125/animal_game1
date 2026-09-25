using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FedAndFound.Game.UI
{
    /// <summary>마우스를 올리면 설명이 뜨는 툴팁 (스킬·유물·시너지·동물). 화면이 통째로 다시 그려져도 살아남는
    /// 전역 오버레이 하나를 두고, 각 UI 요소에는 TooltipTrigger만 붙인다(UIFactory.Tooltip).</summary>
    public sealed class Tooltip : MonoBehaviour
    {
        static Tooltip _inst;
        RectTransform _canvasRect, _box;
        Text _text;
        TooltipTrigger _owner;

        public static void Create(Transform canvas)
        {
            var go = new GameObject("Tooltip", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            UIFactory.Stretch((RectTransform)go.transform);
            _inst = go.AddComponent<Tooltip>();
            _inst._canvasRect = (RectTransform)canvas;

            var box = UIFactory.Panel(go.transform, "Box", Color.white, stretch: false);
            var img = box.GetComponent<Image>();
            img.sprite = UISkin.GoldFrame; img.type = Image.Type.Sliced; img.raycastTarget = false;
            box.pivot = new Vector2(0, 1);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            var fitter = box.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var lay = box.gameObject.AddComponent<VerticalLayoutGroup>();
            lay.padding = new RectOffset(16, 16, 12, 12);
            lay.childControlWidth = lay.childControlHeight = true;

            _inst._text = UIFactory.Label(box, "", 17, TextAnchor.UpperLeft, stretch: false);
            _inst._text.raycastTarget = false;
            _inst._text.horizontalOverflow = HorizontalWrapMode.Wrap;
            var le = _inst._text.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 420;
            _inst._box = box;
            box.gameObject.SetActive(false);
        }

        public static void Show(TooltipTrigger owner, string text)
        {
            if (_inst == null || string.IsNullOrEmpty(text)) return;
            _inst._owner = owner;
            _inst._text.text = text;
            _inst._box.gameObject.SetActive(true);
            _inst._box.SetAsLastSibling();
            _inst.Follow();
        }

        public static void Hide(TooltipTrigger owner)
        {
            if (_inst == null || _inst._owner != owner) return;
            _inst._owner = null;
            _inst._box.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_box.gameObject.activeSelf)
            {
                if (_owner == null) { _box.gameObject.SetActive(false); return; } // 화면이 다시 그려져 대상이 사라짐
                Follow();
            }
        }

        /// <summary>마우스 오른쪽 아래에 띄우되, 화면 밖으로 나가면 반대편으로 뒤집는다.</summary>
        void Follow()
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, Input.mousePosition, null, out var p);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_box);
            var size = _box.rect.size;
            var half = _canvasRect.rect.size / 2;
            float x = p.x + 18, y = p.y - 18;
            if (x + size.x > half.x) x = p.x - 18 - size.x;
            if (y - size.y < -half.y) y = p.y + 18 + size.y;
            _box.anchoredPosition = new Vector2(x, y);
        }
    }

    /// <summary>이 UI 요소에 마우스를 올리면 Tooltip에 글을 띄운다.</summary>
    public sealed class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string Text;
        public void OnPointerEnter(PointerEventData e) => Tooltip.Show(this, Text);
        public void OnPointerExit(PointerEventData e) => Tooltip.Hide(this);
        void OnDisable() => Tooltip.Hide(this);
    }
}
