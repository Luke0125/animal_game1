using FedAndFound.Game.Field;
using UnityEngine;
using Terrain = FedAndFound.Core.Terrain;

namespace FedAndFound.Game.BattleView
{
    /// <summary>지형별 풍경(하늘 그라데이션·먼 언덕·바닥 타일·장식). 전투 무대(BattleStageView)와
    /// 다른 화면들의 배경(BackdropView)이 같이 쓴다. 카메라(직교 5, 원점) 기준 좌표.</summary>
    public static class Scenery
    {
        public static GameObject Build(Transform parent, Terrain t)
        {
            var root = new GameObject("Scenery");
            root.transform.SetParent(parent, false);
            var tiles = TerrainTiles.Get(t);
            var (skyTop, skyBottom, hill) = t switch
            {
                Terrain.Swamp => (C(0.30f, 0.38f, 0.36f), C(0.62f, 0.66f, 0.55f), C(0.22f, 0.32f, 0.25f)),
                Terrain.SnowMountain => (C(0.45f, 0.62f, 0.86f), C(0.88f, 0.93f, 0.98f), C(0.70f, 0.78f, 0.88f)),
                Terrain.Desert => (C(0.93f, 0.62f, 0.35f), C(1.00f, 0.88f, 0.66f), C(0.80f, 0.55f, 0.32f)),
                _ => (C(0.40f, 0.66f, 0.92f), C(0.80f, 0.90f, 0.95f), C(0.30f, 0.52f, 0.30f)),
            };

            // 하늘 그라데이션 (텍스처 2×64px, 32px/유닛 → 폭 1/16유닛이라 가로로 크게 늘린다)
            var sky = Quad(root.transform, "Sky", Gradient(skyTop, skyBottom), -100);
            sky.transform.localPosition = new Vector3(0, 2.2f, 0);
            sky.transform.localScale = new Vector3(24f * 16f, 7.6f / 2f, 1);

            // 먼 언덕(원을 겹쳐 능선처럼)
            var rng = new System.Random((int)t * 97 + 3);
            for (int i = 0; i < 9; i++)
            {
                var h = Quad(root.transform, "Hill", BattleStageView.CircleSprite, -90);
                h.color = Color.Lerp(hill, skyBottom, 0.35f + 0.25f * (i % 2));
                float w = 3.5f + (float)rng.NextDouble() * 3f;
                h.transform.localPosition = new Vector3(-10 + i * 2.6f, -0.9f + (float)rng.NextDouble() * 0.6f, 0);
                h.transform.localScale = new Vector3(w, w * 0.8f, 1);
            }

            // 바닥: 필드와 같은 지형 타일
            for (int y = -6; y <= -1; y++)
                for (int x = -12; x <= 12; x++)
                {
                    var g = Quad(root.transform, "Ground", tiles.Ground[rng.Next(tiles.Ground.Length)].sprite, -80);
                    g.transform.localPosition = new Vector3(x, y + 0.2f, 0);
                    g.color = y == -1 ? Color.Lerp(Color.white, Color.black, 0.08f) : Color.white;
                }

            // 뒤쪽 장식(나무·선인장 등) — 지평선 줄에만, 조금 어둡게
            for (int i = 0; i < 12; i++)
            {
                int k = rng.Next(tiles.Deco.Length);
                if (t == Terrain.Swamp && k == 1) k = 0; // 연잎(물웅덩이)은 지평선에 세우면 둥근 판처럼 보여서 갈대로 대신
                var d = Quad(root.transform, "Deco", tiles.Deco[k].sprite, -70);
                d.color = new Color(0.78f, 0.8f, 0.82f);
                float s = 1.6f + (float)rng.NextDouble() * 0.8f;
                d.transform.localPosition = new Vector3(-10 + i * 1.8f + (float)rng.NextDouble(), -0.25f + (float)rng.NextDouble() * 0.3f, 0);
                d.transform.localScale = new Vector3(s, s, 1);
            }
            return root;
        }

        static SpriteRenderer Quad(Transform parent, string name, Sprite s, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(parent, false);
            sr.sprite = s; sr.sortingOrder = order;
            return sr;
        }

        static Sprite Gradient(Color top, Color bottom)
        {
            var px = new Color[2 * 64];
            for (int y = 0; y < 64; y++) { var c = Color.Lerp(bottom, top, y / 63f); px[y * 2] = c; px[y * 2 + 1] = c; }
            var tex = new Texture2D(2, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 2, 64), new Vector2(0.5f, 0.5f), 32); // 크기 1/16 × 2 유닛
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);
    }
}
