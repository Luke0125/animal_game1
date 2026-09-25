using UnityEngine;

namespace FedAndFound.Game.UI
{
    /// <summary>이미지 파일 없이 만든 UI 스킨(9-slice 스프라이트). 모서리가 둥근 패널·버튼, 금테 창.
    /// 흰색 바탕으로 만들어 Image.color로 물들이는 스프라이트(Rounded)와, 색이 박힌 장식 창(GoldFrame)이 있다.</summary>
    public static class UISkin
    {
        static Sprite _rounded, _button, _goldFrame, _parchment;

        /// <summary>둥근 사각형: 흰 바탕 + 조금 어두운 테두리 (색은 Image.color로).</summary>
        public static Sprite Rounded => _rounded ??= Make(32, 8, 2, Color.white, new Color(0.62f, 0.62f, 0.62f), 0f);

        /// <summary>버튼: 둥근 사각형 + 윗부분이 살짝 밝은 입체감.</summary>
        public static Sprite Button => _button ??= Make(32, 8, 2, Color.white, new Color(0.5f, 0.5f, 0.5f), 0.18f);

        /// <summary>금테 창(레퍼런스의 테두리 패널): 짙은 갈색 바탕 + 금색 2중 테두리.</summary>
        public static Sprite GoldFrame => _goldFrame ??= MakeFrame(new Color(0.11f, 0.09f, 0.07f, 0.94f), new Color(0.78f, 0.6f, 0.3f), new Color(0.35f, 0.26f, 0.13f));

        /// <summary>양피지 창(인트로·이벤트).</summary>
        public static Sprite Parchment => _parchment ??= MakeFrame(new Color(0.93f, 0.86f, 0.7f, 0.97f), new Color(0.5f, 0.36f, 0.2f), new Color(0.78f, 0.66f, 0.46f));

        static Sprite Make(int n, int radius, int border, Color fill, Color edge, float topLight)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float depth = Depth(x, y, n, radius);
                    if (depth <= 0) { px[y * n + x] = Color.clear; continue; }
                    bool isEdge = depth <= border;
                    var c = isEdge ? edge : fill;
                    if (!isEdge && topLight > 0)
                    {
                        if (y > n * 0.55f) c = Color.Lerp(c, Color.white, topLight);                    // 윗부분 밝게
                        else if (y < n * 0.3f) c = Color.Lerp(c, new Color(0.8f, 0.8f, 0.8f), topLight); // 아랫부분 살짝 어둡게
                    }
                    px[y * n + x] = c;
                }
            return ToSprite(px, n, radius + 1);
        }

        static Sprite MakeFrame(Color fill, Color gold, Color inner)
        {
            const int n = 48, r = 10;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float depth = Depth(x, y, n, r);
                    if (depth <= 0) { px[y * n + x] = Color.clear; continue; }
                    Color c = fill;
                    if (depth <= 3) c = gold;                          // 바깥 금테
                    else if (depth > 5 && depth <= 6) c = inner;       // 안쪽 가는 선
                    px[y * n + x] = c;
                }
            return ToSprite(px, n, r + 4);
        }

        /// <summary>둥근 사각형 안쪽으로 얼마나 들어와 있는지(px). 0 이하 = 바깥.</summary>
        static float Depth(int x, int y, int n, int r)
        {
            float fx = x + 0.5f, fy = y + 0.5f;
            float edge = Mathf.Min(Mathf.Min(fx, fy), Mathf.Min(n - fx, n - fy));
            bool cornerX = fx < r || fx > n - r, cornerY = fy < r || fy > n - r;
            if (!(cornerX && cornerY)) return edge;
            float cx = fx < r ? r : n - r, cy = fy < r ? r : n - r;
            return r - Mathf.Sqrt((fx - cx) * (fx - cx) + (fy - cy) * (fy - cy));
        }

        static Sprite ToSprite(Color[] px, int n, int slice)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(slice, slice, slice, slice));
        }
    }
}
