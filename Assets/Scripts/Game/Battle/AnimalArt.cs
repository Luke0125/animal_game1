using System.Collections.Generic;
using UnityEngine;

namespace FedAndFound.Game.BattleView
{
    /// <summary>12종 + 사슴의 픽셀 스프라이트를 이미지 파일 없이 코드로 그린다(32×32, 오른쪽을 봄).
    /// 도형(타원·사각형)으로 몸을 그린 뒤 1px 외곽선을 자동으로 둘러 픽셀아트처럼 보이게 한다.
    /// 아트를 교체할 땐 Get()이 파일 스프라이트를 돌려주게만 바꾸면 된다.</summary>
    public static class AnimalArt
    {
        const int N = 32;
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string speciesId)
        {
            if (Cache.TryGetValue(speciesId, out var s)) return s;
            var c = new Canvas32();
            Draw(speciesId, c);
            c.Outline(new Color(0.12f, 0.09f, 0.08f));
            return Cache[speciesId] = c.ToSprite();
        }

        // ================= 종별 그림 (y=0이 바닥, 머리는 오른쪽) =================

        static void Draw(string id, Canvas32 c)
        {
            switch (id)
            {
                case "rabbit":
                {
                    Color fur = H(0xB58A63), light = H(0xE9D8C4), inner = H(0xE8A3A8);
                    c.Ellipse(13, 8, 8, 6, fur); c.Ellipse(13, 6, 6, 3, light);        // 몸, 배
                    c.Ellipse(5, 10, 2.5f, 2.5f, light);                               // 꼬리
                    c.Ellipse(21, 13, 5, 4.5f, fur);                                    // 머리
                    c.Ellipse(19, 23, 1.6f, 6, fur); c.Ellipse(23, 23, 1.6f, 6, fur);   // 긴 귀
                    c.Ellipse(19, 23, 0.7f, 4.5f, inner); c.Ellipse(23, 23, 0.7f, 4.5f, inner);
                    c.Rect(10, 1, 3, 3, fur); c.Rect(16, 1, 3, 3, fur);                // 발
                    Eye(c, 23, 14); c.Px(26, 12, inner);                                // 눈, 코
                    break;
                }
                case "turtle":
                {
                    Color shell = H(0x4F7A3A), shellDark = H(0x36572A), skin = H(0x9DB86A);
                    c.Rect(8, 1, 3, 4, skin); c.Rect(19, 1, 3, 4, skin);               // 다리
                    c.Ellipse(24, 7, 4, 3, skin);                                        // 머리
                    c.Ellipse(15, 8, 10, 7, shell); c.Rect(5, 3, 21, 2, shellDark);     // 등딱지
                    for (int x = 9; x <= 21; x += 6) { c.Ellipse(x, 10, 2.2f, 2, shellDark); }
                    c.Px(3, 5, skin); c.Px(4, 5, skin);                                  // 꼬리
                    Eye(c, 26, 8);
                    break;
                }
                case "gazelle":
                {
                    Color fur = H(0xC98B4E), light = H(0xF1E3CF), stripe = H(0x5B3A22), horn = H(0x3A2A1E);
                    c.Rect(9, 1, 2, 9, fur); c.Rect(12, 1, 2, 9, fur); c.Rect(19, 1, 2, 9, fur); c.Rect(22, 1, 2, 9, fur); // 가는 다리
                    c.Ellipse(16, 12, 9, 4.5f, fur); c.Ellipse(16, 10, 7, 2, light); c.Rect(9, 12, 14, 1, stripe); // 몸, 옆줄
                    c.Rect(23, 14, 3, 6, fur);                                           // 목
                    c.Ellipse(26, 20, 3.5f, 2.5f, fur);                                  // 머리
                    c.Line(24, 22, 21, 30, horn); c.Line(26, 22, 23, 30, horn);         // 뒤로 휜 뿔
                    c.Px(6, 13, stripe); c.Px(7, 14, stripe);                            // 꼬리
                    Eye(c, 27, 21);
                    break;
                }
                case "rhino":
                {
                    Color skin = H(0x8E8C8A), dark = H(0x6B6966), horn = H(0xE6DCC8);
                    c.Rect(7, 1, 4, 6, dark); c.Rect(19, 1, 4, 6, dark);                // 굵은 다리
                    c.Ellipse(14, 10, 11, 7, skin); c.Rect(4, 5, 21, 2, dark);          // 큰 몸
                    c.Ellipse(25, 10, 5, 4, skin);                                       // 머리
                    c.Line(29, 11, 31, 17, horn); c.Line(28, 11, 30, 16, horn);         // 코뿔
                    c.Px(26, 16, horn); c.Px(25, 17, skin); c.Px(23, 15, dark);          // 작은 뿔, 귀
                    Eye(c, 25, 11);
                    break;
                }
                case "lion":
                {
                    Color fur = H(0xD9A857), mane = H(0x8A4E1F), light = H(0xF0D59A);
                    c.Rect(7, 1, 3, 7, fur); c.Rect(17, 1, 3, 7, fur);                  // 다리
                    c.Ellipse(13, 10, 9, 5, fur); c.Ellipse(13, 8, 7, 2, light);        // 몸
                    c.Line(4, 11, 1, 16, fur); c.Ellipse(1, 17, 1.5f, 1.5f, mane);      // 꼬리 + 술
                    c.Ellipse(23, 14, 7, 7, mane);                                       // 갈기
                    c.Ellipse(24, 14, 4, 4, fur); c.Ellipse(26, 12, 2, 1.5f, light);    // 얼굴, 주둥이
                    Eye(c, 25, 15); c.Px(28, 12, mane);
                    break;
                }
                case "leopard":
                {
                    Color fur = H(0xE0B04F), spot = H(0x4A3218), light = H(0xF4E2B0);
                    c.Rect(8, 1, 3, 7, fur); c.Rect(18, 1, 3, 7, fur);
                    c.Ellipse(14, 10, 9, 4.5f, fur); c.Ellipse(14, 8, 7, 1.5f, light);
                    c.Line(5, 11, 1, 5, fur); c.Line(1, 5, 2, 3, fur);                   // 긴 꼬리
                    c.Ellipse(24, 13, 4.5f, 4, fur); c.Px(22, 17, fur); c.Px(26, 17, fur); // 머리, 귀
                    foreach (var (x, y) in new[] { (9, 11), (12, 13), (15, 10), (18, 12), (11, 9), (20, 10), (16, 13), (23, 14) }) c.Px(x, y, spot);
                    Eye(c, 25, 14);
                    break;
                }
                case "snake":
                {
                    Color scale = H(0x5C9A45), dark = H(0x3E6E2E), belly = H(0xD8D08A), tongue = H(0xD0403A);
                    c.Ellipse(12, 4, 9, 3, scale); c.Ellipse(12, 3, 7, 1, belly);      // 똬리 아래
                    c.Ellipse(15, 8, 7, 2.5f, scale);                                    // 똬리 위
                    c.Rect(20, 8, 3, 10, scale); c.Rect(21, 8, 1, 10, dark);            // 세운 목
                    c.Ellipse(24, 19, 4, 2.5f, scale);                                   // 머리
                    c.Line(28, 19, 30, 20, tongue); c.Px(30, 18, tongue);                // 혀
                    for (int x = 6; x <= 18; x += 4) c.Px(x, 5, dark);
                    Eye(c, 25, 20);
                    break;
                }
                case "badger":
                {
                    Color fur = H(0x3A3634), white = H(0xE8E4DC), gray = H(0x77716B);
                    c.Rect(8, 1, 4, 4, fur); c.Rect(19, 1, 4, 4, fur);                  // 짧은 다리
                    c.Ellipse(15, 7, 11, 4.5f, fur);                                     // 낮고 넓은 몸
                    c.Rect(6, 10, 18, 2, white);                                         // 흰 등줄 (벌꿀오소리)
                    c.Ellipse(26, 7, 4, 3.5f, fur); c.Rect(23, 9, 6, 2, white);         // 머리
                    c.Px(4, 6, gray); c.Px(3, 7, gray);
                    Eye(c, 27, 7);
                    break;
                }
                case "fox":
                {
                    Color fur = H(0xD9702E), white = H(0xF4EDE3), dark = H(0x3A2618);
                    c.Rect(9, 1, 2, 6, dark); c.Rect(18, 1, 2, 6, dark);                // 검은 발
                    c.Ellipse(14, 10, 7.5f, 4, fur);                                     // 몸
                    c.Ellipse(4, 12, 4, 3, fur); c.Ellipse(1.5f, 13, 1.5f, 1.5f, white); // 풍성한 꼬리 + 흰 끝
                    c.Ellipse(22, 14, 4, 3.5f, fur); c.Ellipse(21, 12, 3, 2, white);    // 머리, 흰 턱
                    c.Line(25, 15, 29, 13, fur); c.Px(29, 13, dark);                     // 뾰족한 주둥이, 코
                    c.Line(20, 17, 19, 21, fur); c.Line(23, 17, 23, 21, fur);            // 뾰족 귀
                    Eye(c, 23, 15);
                    break;
                }
                case "hedgehog":
                {
                    Color spine = H(0x6E5237), tip = H(0xC9B79A), face = H(0xD8B58C);
                    c.Ellipse(14, 7, 10, 6.5f, spine);                                   // 가시 몸
                    for (int x = 6; x <= 22; x += 2) c.Line(x, 10, x - 2, 14 + (x % 4), spine);
                    for (int x = 6; x <= 22; x += 3) c.Px(x - 2, 13 + (x % 4), tip);
                    c.Ellipse(24, 5, 4, 3, face); c.Px(28, 5, H(0x2A1E14));             // 얼굴, 코
                    c.Rect(10, 0, 2, 2, face); c.Rect(19, 0, 2, 2, face);
                    Eye(c, 24, 6);
                    break;
                }
                case "monkey":
                {
                    Color fur = H(0x7A5234), face = H(0xE0B894), dark = H(0x4E331F);
                    c.Line(8, 3, 3, 8, fur); c.Line(3, 8, 4, 12, fur);                   // 말린 꼬리
                    c.Ellipse(14, 8, 6, 7, fur); c.Ellipse(15, 7, 3.5f, 4, face);        // 앉은 몸, 배
                    c.Rect(19, 8, 5, 2, fur); c.Rect(10, 0, 3, 3, dark); c.Rect(16, 0, 3, 3, dark); // 팔, 발
                    c.Ellipse(16, 19, 5, 5, fur); c.Ellipse(17, 18, 3.5f, 3.5f, face);   // 머리, 얼굴
                    c.Ellipse(11, 20, 1.8f, 1.8f, face);                                 // 귀
                    Eye(c, 16, 19); Eye(c, 19, 19);
                    break;
                }
                case "boar":
                {
                    Color fur = H(0x5A4030), ridge = H(0x2E2018), snout = H(0xB08070), tusk = H(0xF4EEDC);
                    c.Rect(8, 1, 3, 5, ridge); c.Rect(18, 1, 3, 5, ridge);
                    c.Ellipse(14, 9, 10, 6, fur);
                    for (int x = 7; x <= 20; x += 2) c.Px(x, 15, ridge);               // 등 갈기
                    c.Ellipse(24, 9, 5, 4, fur); c.Rect(27, 7, 3, 3, snout);            // 머리, 코
                    c.Line(27, 7, 29, 11, tusk);                                         // 엄니
                    c.Px(22, 13, ridge); c.Px(23, 14, ridge);                            // 귀
                    Eye(c, 25, 11);
                    break;
                }
                default: // 사슴 (손님)
                {
                    Color fur = H(0xA8723F), spot = H(0xF1E2C8), antler = H(0x6B4A2B);
                    c.Rect(9, 1, 2, 8, fur); c.Rect(19, 1, 2, 8, fur);
                    c.Ellipse(15, 11, 8, 4.5f, fur);
                    foreach (var (x, y) in new[] { (11, 12), (14, 13), (17, 12), (13, 10), (19, 11) }) c.Px(x, y, spot);
                    c.Rect(21, 13, 3, 6, fur); c.Ellipse(25, 19, 3.5f, 2.5f, fur);     // 목, 머리
                    c.Line(23, 21, 20, 28, antler); c.Line(21, 25, 18, 26, antler);     // 뿔
                    c.Line(25, 21, 27, 28, antler); c.Line(26, 25, 29, 26, antler);
                    c.Px(7, 12, spot);
                    Eye(c, 26, 20);
                    break;
                }
            }
        }

        static void Eye(Canvas32 c, int x, int y) { c.Px(x, y, new Color(0.08f, 0.06f, 0.06f)); }

        static Color H(int hex) => new Color(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f);

        /// <summary>32×32 픽셀 캔버스와 도형 그리기.</summary>
        sealed class Canvas32
        {
            readonly Color[] _px = new Color[N * N];

            public void Px(int x, int y, Color c) { if (x >= 0 && x < N && y >= 0 && y < N) _px[y * N + x] = c; }

            public void Rect(int x0, int y0, int w, int h, Color c)
            {
                for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) Px(x, y, c);
            }

            public void Ellipse(float cx, float cy, float rx, float ry, Color c)
            {
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float dx = (x - cx) / rx, dy = (y - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Px(x, y, c);
                    }
            }

            public void Line(int x0, int y0, int x1, int y1, Color c)
            {
                int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), 1);
                for (int i = 0; i <= steps; i++)
                    Px(Mathf.RoundToInt(Mathf.Lerp(x0, x1, (float)i / steps)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, (float)i / steps)), c);
            }

            /// <summary>그림 둘레에 1px 외곽선 + 윗부분 살짝 밝게(입체감).</summary>
            public void Outline(Color line)
            {
                var src = (Color[])_px.Clone();
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        if (src[y * N + x].a > 0)
                        {
                            bool topEdge = y + 1 >= N || src[(y + 1) * N + x].a == 0;
                            if (topEdge) _px[y * N + x] = Color.Lerp(src[y * N + x], Color.white, 0.25f);
                            continue;
                        }
                        bool near = false;
                        for (int d = 0; d < 4 && !near; d++)
                        {
                            int nx = x + (d == 0 ? 1 : d == 1 ? -1 : 0), ny = y + (d == 2 ? 1 : d == 3 ? -1 : 0);
                            near = nx >= 0 && nx < N && ny >= 0 && ny < N && src[ny * N + nx].a > 0;
                        }
                        if (near) _px[y * N + x] = line;
                    }
            }

            public Sprite ToSprite()
            {
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                tex.SetPixels(_px);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0f), N); // 발밑 기준, 1유닛 = 32px
            }
        }
    }
}
