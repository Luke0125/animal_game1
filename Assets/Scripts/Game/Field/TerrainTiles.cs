using System;
using System.Collections.Generic;
using FedAndFound.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Terrain = FedAndFound.Core.Terrain;

namespace FedAndFound.Game.Field
{
    /// <summary>지형 4종(들판/늪/설산/사막, §11)의 타일을 이미지 파일 없이 코드로 그린다.
    /// 16×16 픽셀 텍스처 + Point 필터(픽셀아트, `.claude/skills/2d-pixel-perfect` 참고).
    /// 나중에 아트를 교체할 땐 이 클래스의 Tile만 바꾸면 된다(7단계).</summary>
    public sealed class TerrainTiles
    {
        public const int Px = 16;

        public Tile[] Ground;   // 바닥 변형 3종 (반복 무늬가 덜 보이게)
        public Tile Path;
        public Tile[] Deco;     // 장식(나무·갈대·전나무·선인장 등) — 길 밖에만 놓는다
        public Tile Water;      // 늪의 웅덩이 등 (없으면 null)
        public Tile Cliff, CliffDark; // 떠 있는 섬의 절벽(맵 아트 2차): 윗줄은 바닥색 턱, 아래로 갈수록 어두운 암석
        public Color Sky;       // 섬 뒤 하늘색
        public Color Backdrop;  // 카메라 배경색
        public Color PathAccent;

        static readonly Dictionary<Terrain, TerrainTiles> Cache = new Dictionary<Terrain, TerrainTiles>();

        public static TerrainTiles Get(Terrain t)
        {
            if (!Cache.TryGetValue(t, out var tiles)) Cache[t] = tiles = Build(t);
            return tiles;
        }

        static TerrainTiles Build(Terrain t)
        {
            var rng = new System.Random(1000 + (int)t);
            var r = new TerrainTiles();
            switch (t)
            {
                case Terrain.Plains:
                    r.Backdrop = C(0x2E4A2A); r.Sky = C(0x8EC3EA);
                    Cliffs(r, rng, C(0x5E9E4A), C(0x7A5A3A), C(0x5A4028));
                    r.Ground = GroundSet(rng, C(0x5E9E4A), C(0x6FB356), C(0x4E8A3E));
                    r.Path = Speckled(rng, C(0xC9A66B), C(0xB08F58), 0.25f);
                    r.PathAccent = C(0xF3E3B5);
                    r.Deco = new[] { MakeTile(DrawTree(C(0x3F7F35), C(0x2C5E26), C(0x6B4A2B))), MakeTile(DrawFlowers(rng, C(0xF2E35C), C(0xE86A8A))), MakeTile(DrawBush(C(0x4A8C3C), C(0x356B2C))) };
                    break;
                case Terrain.Swamp:
                    r.Backdrop = C(0x1F2B22); r.Sky = C(0x8FA8A0);
                    Cliffs(r, rng, C(0x4A5A3A), C(0x4A4034), C(0x33291F));
                    r.Ground = GroundSet(rng, C(0x4A5A3A), C(0x55663F), C(0x3E4D31));
                    r.Path = Planks(C(0x7A5A3A), C(0x5A402A));
                    r.PathAccent = C(0xB9D39A);
                    r.Water = Speckled(rng, C(0x355B55), C(0x4B7A70), 0.15f);
                    r.Deco = new[] { MakeTile(DrawReeds(rng, C(0x8FA35A), C(0x6B4A2B))), MakeTile(DrawLilyPad(C(0x355B55), C(0x6FA650))), MakeTile(DrawBush(C(0x3C5230), C(0x2A3B22))) };
                    break;
                case Terrain.SnowMountain:
                    r.Backdrop = C(0x2A3440); r.Sky = C(0x9CC0E8);
                    Cliffs(r, rng, C(0xE6EEF4), C(0x7C8894), C(0x5E6873));
                    r.Ground = GroundSet(rng, C(0xE6EEF4), C(0xF4F8FB), C(0xCFDCE6));
                    r.Path = Speckled(rng, C(0x9FB2C2), C(0x8A9DAE), 0.3f);
                    r.PathAccent = C(0x5AA0E0);
                    r.Deco = new[] { MakeTile(DrawPine(C(0x2F5A48), C(0xF4F8FB))), MakeTile(DrawRock(C(0x7C8894), C(0x5E6873), C(0xF4F8FB))), MakeTile(DrawPine(C(0x264B3C), C(0xE6EEF4))) };
                    break;
                default: // 사막
                    r.Backdrop = C(0x4A3A22); r.Sky = C(0xF2C48A);
                    Cliffs(r, rng, C(0xE3C27A), C(0xB5723F), C(0x8E5530));
                    r.Ground = GroundSet(rng, C(0xE3C27A), C(0xEBCD8A), C(0xD4B26A));
                    r.Path = Speckled(rng, C(0xC49A55), C(0xB08845), 0.3f);
                    r.PathAccent = C(0xFFF1C4);
                    r.Deco = new[] { MakeTile(DrawCactus(C(0x4E8C4A), C(0x3A6B37))), MakeTile(DrawRock(C(0xA87F52), C(0x7F5E3C), C(0xC9A06A))), MakeTile(DrawBones(C(0xF2EEE0))) };
                    break;
            }
            return r;
        }

        // ================= 스프라이트 공용 =================

        public static Sprite MakeSprite(Color[] px, int size = Px)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels(px);
            tex.Apply();
            // pixelsPerUnit = size → 한 칸(1 유닛)에 딱 맞음
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Tile MakeTile(Color[] px)
        {
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = MakeSprite(px);
            tile.colliderType = Tile.ColliderType.None;
            return tile;
        }

        /// <summary>외곽선 있는 원(노드·플레이어 표시용). size 픽셀짜리 스프라이트.</summary>
        public static Sprite Circle(int size, Color fill, Color outline)
        {
            var px = new Color[size * size];
            float c = (size - 1) / 2f, rOut = size / 2f - 0.5f, rIn = rOut - Mathf.Max(1f, size / 12f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    px[y * size + x] = d > rOut ? Color.clear : d > rIn ? outline : fill;
                }
            return MakeSprite(px, size);
        }

        static Color C(int hex) => new Color(((hex >> 16) & 0xFF) / 255f, ((hex >> 8) & 0xFF) / 255f, (hex & 0xFF) / 255f);

        static Color[] Fill(Color c)
        {
            var px = new Color[Px * Px];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            return px;
        }

        static void Set(Color[] px, int x, int y, Color c) { if (x >= 0 && x < Px && y >= 0 && y < Px) px[y * Px + x] = c; }

        static void FillRect(Color[] px, int x0, int y0, int w, int h, Color c)
        {
            for (int y = y0; y < y0 + h; y++) for (int x = x0; x < x0 + w; x++) Set(px, x, y, c);
        }

        static void Disk(Color[] px, float cx, float cy, float r, Color c)
        {
            for (int y = 0; y < Px; y++)
                for (int x = 0; x < Px; x++)
                    if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) Set(px, x, y, c);
        }

        static Tile Speckled(System.Random rng, Color baseC, Color speck, float density) => MakeTile(SpeckledPx(rng, baseC, speck, density));

        static Color[] SpeckledPx(System.Random rng, Color baseC, Color speck, float density)
        {
            var px = Fill(baseC);
            for (int i = 0; i < px.Length; i++) if (rng.NextDouble() < density) px[i] = speck;
            return px;
        }

        static Tile[] GroundSet(System.Random rng, Color baseC, Color light, Color dark)
        {
            var set = new Tile[3];
            for (int k = 0; k < set.Length; k++)
            {
                var px = SpeckledPx(rng, baseC, dark, 0.08f + 0.04f * k);
                for (int i = 0; i < px.Length; i++) if (rng.NextDouble() < 0.06) px[i] = light;
                set[k] = MakeTile(px);
            }
            return set;
        }

        static Tile Planks(Color wood, Color gap)
        {
            var px = Fill(wood);
            for (int y = 0; y < Px; y += 4) FillRect(px, 0, y, Px, 1, gap);
            Set(px, 3, 2, gap); Set(px, 11, 6, gap); Set(px, 6, 10, gap); Set(px, 13, 14, gap);
            return MakeTile(px);
        }

        /// <summary>절벽 타일 2종: 윗줄(바닥색 턱 + 암석 결)과 아랫줄(더 어두운 암석, 아래로 갈수록 흐려짐).</summary>
        static void Cliffs(TerrainTiles r, System.Random rng, Color lip, Color rock, Color dark)
        {
            var top = Fill(rock);
            FillRect(top, 0, 12, Px, 4, lip);                                  // 위쪽 턱(풀·눈·모래)
            for (int x = 0; x < Px; x += 1 + rng.Next(3)) Set(top, x, 11, lip); // 들쭉날쭉한 가장자리
            for (int y = 2; y < 11; y += 4) FillRect(top, 0, y, Px, 1, dark);  // 지층
            for (int i = 0; i < 4; i++) FillRect(top, rng.Next(Px), rng.Next(2, 10), 1, 3, dark); // 균열
            r.Cliff = MakeTile(top);

            var bottom = Fill(dark);
            for (int y = 3; y < Px; y += 5) FillRect(bottom, 0, y, Px, 1, rock);
            for (int y = 0; y < 5; y++) for (int x = 0; x < Px; x++) if (rng.NextDouble() < 0.5 - y * 0.08) Set(bottom, x, y, Color.clear); // 아래 끝이 부서진 느낌
            r.CliffDark = MakeTile(bottom);
        }

        /// <summary>노드 돌판(레퍼런스의 둥근 돌 발판): 위는 밝은 면, 아래는 두께. color로 물들인다.</summary>
        public static Sprite StoneDisc()
        {
            if (_disc != null) return _disc;
            const int n = 32;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x - 15.5f) / 15f;
                    float top = (y - 18f) / 10f, side = (y - 13f) / 10f;
                    bool inTop = dx * dx + top * top <= 1f, inSide = dx * dx + side * side <= 1f || (Mathf.Abs(dx) <= 1f && y >= 13 && y <= 18);
                    if (inTop) px[y * n + x] = dx * dx + top * top > 0.8f ? new Color(0.72f, 0.7f, 0.66f) : Color.Lerp(Color.white, new Color(0.9f, 0.88f, 0.84f), (18 - y) / 10f);
                    else if (inSide) px[y * n + x] = new Color(0.55f, 0.52f, 0.48f);
                }
            return _disc = MakeSprite(px, n);
        }
        static Sprite _disc;

        // ================= 장식 그림 (투명 배경) =================

        static Color[] Clear() => Fill(Color.clear);

        static Color[] DrawTree(Color leaf, Color leafDark, Color trunk)
        {
            var px = Clear();
            FillRect(px, 7, 1, 2, 5, trunk);
            Disk(px, 7.5f, 9.5f, 5.5f, leafDark);
            Disk(px, 7f, 10f, 4.5f, leaf);
            return px;
        }

        static Color[] DrawBush(Color leaf, Color leafDark)
        {
            var px = Clear();
            Disk(px, 5f, 5f, 3.5f, leafDark); Disk(px, 10f, 5f, 3.5f, leafDark);
            Disk(px, 5f, 5.5f, 2.5f, leaf); Disk(px, 10f, 5.5f, 2.5f, leaf);
            return px;
        }

        static Color[] DrawFlowers(System.Random rng, Color a, Color b)
        {
            var px = Clear();
            for (int i = 0; i < 4; i++)
            {
                int x = 2 + rng.Next(12), y = 2 + rng.Next(12);
                var c = i % 2 == 0 ? a : b;
                Set(px, x, y, c); Set(px, x + 1, y, c); Set(px, x, y + 1, c); Set(px, x - 1, y, c); Set(px, x, y - 1, c);
            }
            return px;
        }

        static Color[] DrawReeds(System.Random rng, Color stem, Color tip)
        {
            var px = Clear();
            for (int i = 0; i < 5; i++)
            {
                int x = 2 + i * 3, h = 7 + rng.Next(6);
                FillRect(px, x, 1, 1, h, stem);
                FillRect(px, x, 1 + h, 1, 2, tip);
            }
            return px;
        }

        static Color[] DrawLilyPad(Color water, Color pad)
        {
            var px = Clear();
            Disk(px, 7.5f, 7.5f, 6.5f, water);
            Disk(px, 7f, 8f, 3.5f, pad);
            Set(px, 7, 8, water); Set(px, 8, 9, water);
            return px;
        }

        static Color[] DrawPine(Color needle, Color snow)
        {
            var px = Clear();
            FillRect(px, 7, 1, 2, 3, new Color(0.35f, 0.25f, 0.15f));
            for (int y = 4; y < 15; y++)
            {
                int half = Mathf.Max(1, (15 - y) * 6 / 11);
                FillRect(px, 8 - half, y, half * 2, 1, needle);
                if (y % 4 == 3) { Set(px, 8 - half, y, snow); Set(px, 8 + half - 1, y, snow); }
            }
            FillRect(px, 7, 14, 2, 1, snow);
            return px;
        }

        static Color[] DrawRock(Color rock, Color shade, Color top)
        {
            var px = Clear();
            Disk(px, 7.5f, 5.5f, 5f, shade);
            Disk(px, 7f, 6.5f, 4f, rock);
            FillRect(px, 5, 9, 4, 1, top);
            return px;
        }

        static Color[] DrawCactus(Color body, Color shade)
        {
            var px = Clear();
            FillRect(px, 7, 1, 3, 13, body); FillRect(px, 9, 1, 1, 13, shade);
            FillRect(px, 3, 6, 4, 2, body); FillRect(px, 3, 8, 2, 4, body);
            FillRect(px, 10, 8, 3, 2, body); FillRect(px, 11, 10, 2, 3, body);
            return px;
        }

        static Color[] DrawBones(Color bone)
        {
            var px = Clear();
            FillRect(px, 4, 7, 8, 2, bone);
            FillRect(px, 3, 6, 2, 4, bone); FillRect(px, 11, 6, 2, 4, bone);
            return px;
        }
    }
}
