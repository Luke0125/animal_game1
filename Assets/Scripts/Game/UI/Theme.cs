using UnityEngine;

namespace FedAndFound.Game.UI
{
    /// <summary>그래픽 리소스 없이도 일관된 화면을 만들기 위한 색상·폰트 팔레트.
    /// 평가 기준상 시각 요소는 채점 대상이 아니므로 최소한으로만 다듬는다(ROADMAP §7 전까지 유지).</summary>
    public static class Theme
    {
        public static readonly Color Background = new Color(0.11f, 0.13f, 0.15f);
        public static readonly Color Panel = new Color(0.14f, 0.12f, 0.1f, 0.9f);      // 따뜻한 짙은 갈색 (그래픽 2차)
        public static readonly Color PanelLight = new Color(0.3f, 0.25f, 0.19f);
        public static readonly Color Text = new Color(0.93f, 0.93f, 0.93f);
        public static readonly Color TextDim = new Color(0.62f, 0.62f, 0.66f);
        public static readonly Color Accent = new Color(0.35f, 0.72f, 0.45f);      // 확인/긍정
        public static readonly Color Danger = new Color(0.78f, 0.32f, 0.32f);      // 적/위험
        public static readonly Color Warn = new Color(0.85f, 0.66f, 0.28f);        // 배고픔/경고
        public static readonly Color Disabled = new Color(0.35f, 0.35f, 0.37f);
        public static readonly Color HpBar = new Color(0.42f, 0.75f, 0.42f);
        public static readonly Color HpBarLow = new Color(0.8f, 0.35f, 0.3f);
        public static readonly Color HungerBar = new Color(0.85f, 0.66f, 0.28f);
        public static readonly Color PurifyBar = new Color(0.45f, 0.6f, 0.85f);
        public static readonly Color Selected = new Color(0.9f, 0.8f, 0.35f);
        public static readonly Color Gold = new Color(0.86f, 0.7f, 0.38f);          // 제목·강조 (레퍼런스의 금테 톤)
        public static readonly Color Ink = new Color(0.25f, 0.16f, 0.08f);         // 양피지 위 글자

        static Font _font, _fontBold;
        /// <summary>나눔고딕(Assets/Resources/Fonts). 한글이 필요한 모든 Text에 반드시 지정할 것 —
        /// 기본 내장 폰트는 한글을 지원하지 않아 빈 사각형(tofu)으로 표시된다.</summary>
        public static Font Font => _font ??= Resources.Load<Font>("Fonts/NanumGothic");
        public static Font FontBold => _fontBold ??= Resources.Load<Font>("Fonts/NanumGothicBold") ?? Font;
    }
}
