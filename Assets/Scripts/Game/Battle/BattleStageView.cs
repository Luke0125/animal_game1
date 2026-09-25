using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FedAndFound.Core;
using FedAndFound.Game.Field;
using UnityEngine;
using UnityEngine.EventSystems;
using Terrain = FedAndFound.Core.Terrain;

namespace FedAndFound.Game.BattleView
{
    /// <summary>사이드뷰 전투 무대 (레퍼런스 전투 화면). 월드 공간이라 Overlay Canvas(전투 UI) 뒤에 그려지고, UI 가운데 빈 곳으로 보인다.
    /// ScreenRouter는 이벤트마다 UI를 통째로 다시 그리므로 애니메이션은 여기(지속 오브젝트)에서만 한다.
    /// GameManager.OnBattleEvent로 한 줄씩 재생되는 이벤트에 맞춰 돌진·피격·데미지 숫자·정화·기절 연출을 한다.</summary>
    public sealed class BattleStageView : MonoBehaviour
    {
        const float UnitScale = 2.4f, FeetY = -0.9f, Spacing = 2.3f, FirstX = 2.3f;

        GameManager _gm;
        GameObject _world, _bgRoot;
        Battle _built;
        readonly Dictionary<Unit, UnitView> _views = new Dictionary<Unit, UnitView>();
        static Sprite _white, _glow, _tri, _circle;

        public static void Create(GameManager gm)
        {
            var go = new GameObject("BattleStageView");
            go.AddComponent<BattleStageView>().Init(gm);
        }

        void Init(GameManager gm)
        {
            _gm = gm;
            _world = new GameObject("BattleWorld");
            _world.transform.SetParent(transform, false);
            gm.OnChanged += Sync;
            gm.OnBattleEvent += OnEvent;
            Sync();
        }

        void OnDestroy()
        {
            if (_gm == null) return;
            _gm.OnChanged -= Sync;
            _gm.OnBattleEvent -= OnEvent;
        }

        bool Active => _gm.Run != null && _gm.Run.Phase == RunPhase.Battle && _gm.Run.CurrentBattle != null;

        // ================= 상태 동기화 =================

        void Sync()
        {
            _world.SetActive(Active);
            if (!Active) return;
            var b = _gm.Run.CurrentBattle;
            if (b != _built) Build(b);

            var cur = b.CurrentActor;
            var targets = TargetableUnits(b);
            foreach (var v in _views.Values)
            {
                v.SetMarker(v.Unit == cur && !_gm.Animating && b.Outcome == BattleOutcome.Ongoing, targets.Contains(v.Unit));
                if (!_gm.Animating) v.SnapToTruth(); // 재생이 끝나면 실제 상태로 맞춘다(놓친 연출 보정)
            }
        }

        void Build(Battle b)
        {
            _built = b;
            foreach (Transform child in _world.transform) Destroy(child.gameObject);
            _views.Clear();
            BuildBackground(_gm.Run.Terrain);
            for (int i = 0; i < b.Allies.Count; i++) AddUnit(b.Allies[i], new Vector3(-FirstX - Spacing * i, FeetY + (i % 2) * 0.25f, 0));
            for (int i = 0; i < b.Enemies.Count; i++) AddUnit(b.Enemies[i], new Vector3(FirstX + Spacing * i, FeetY + (i % 2) * 0.25f, 0));
        }

        void AddUnit(Unit u, Vector3 pos)
        {
            var go = new GameObject(u.ToString());
            go.transform.SetParent(_world.transform, false);
            go.transform.localPosition = pos;
            var v = go.AddComponent<UnitView>();
            v.Setup(u, u.IsBoss ? UnitScale * 1.35f : UnitScale);
            _views[u] = v;
        }

        // ================= 배경 (지형별) =================

        void BuildBackground(Terrain t)
        {
            _bgRoot = new GameObject("Background");
            _bgRoot.transform.SetParent(_world.transform, false);
            var tiles = TerrainTiles.Get(t);
            var (skyTop, skyBottom, hill) = t switch
            {
                Terrain.Swamp => (C(0.30f, 0.38f, 0.36f), C(0.62f, 0.66f, 0.55f), C(0.22f, 0.32f, 0.25f)),
                Terrain.SnowMountain => (C(0.45f, 0.62f, 0.86f), C(0.88f, 0.93f, 0.98f), C(0.70f, 0.78f, 0.88f)),
                Terrain.Desert => (C(0.93f, 0.62f, 0.35f), C(1.00f, 0.88f, 0.66f), C(0.80f, 0.55f, 0.32f)),
                _ => (C(0.40f, 0.66f, 0.92f), C(0.80f, 0.90f, 0.95f), C(0.30f, 0.52f, 0.30f)),
            };

            // 하늘 그라데이션
            var sky = Quad(_bgRoot.transform, "Sky", Gradient(skyTop, skyBottom), -100);
            sky.transform.localPosition = new Vector3(0, 2.2f, 0);
            sky.transform.localScale = new Vector3(22f, 7.5f, 1);

            // 먼 언덕(원을 겹쳐 능선처럼)
            var rng = new System.Random((int)t * 97 + 3);
            for (int i = 0; i < 9; i++)
            {
                var h = new GameObject("Hill").AddComponent<SpriteRenderer>();
                h.transform.SetParent(_bgRoot.transform, false);
                h.sprite = CircleSprite; h.sortingOrder = -90;
                h.color = Color.Lerp(hill, skyBottom, 0.35f + 0.25f * (i % 2));
                float w = 3.5f + (float)rng.NextDouble() * 3f;
                h.transform.localPosition = new Vector3(-10 + i * 2.6f, -0.9f + (float)rng.NextDouble() * 0.6f, 0);
                h.transform.localScale = new Vector3(w, w * 0.8f, 1);
            }

            // 바닥: 필드와 같은 지형 타일
            for (int y = -6; y <= -1; y++)
                for (int x = -11; x <= 11; x++)
                {
                    var g = new GameObject("Ground").AddComponent<SpriteRenderer>();
                    g.transform.SetParent(_bgRoot.transform, false);
                    g.sprite = tiles.Ground[rng.Next(tiles.Ground.Length)].sprite;
                    g.sortingOrder = -80;
                    g.transform.localPosition = new Vector3(x, y + 0.2f, 0);
                    g.color = y == -1 ? Color.Lerp(Color.white, Color.black, 0.08f) : Color.white;
                }

            // 뒤쪽 장식(나무·선인장 등) — 유닛과 겹치지 않게 윗줄에만, 조금 어둡게
            for (int i = 0; i < 12; i++)
            {
                var d = new GameObject("Deco").AddComponent<SpriteRenderer>();
                d.transform.SetParent(_bgRoot.transform, false);
                d.sprite = tiles.Deco[rng.Next(tiles.Deco.Length)].sprite;
                d.sortingOrder = -70;
                d.color = new Color(0.78f, 0.8f, 0.82f);
                float s = 1.6f + (float)rng.NextDouble() * 0.8f;
                d.transform.localPosition = new Vector3(-10 + i * 1.8f + (float)rng.NextDouble(), -0.25f + (float)rng.NextDouble() * 0.3f, 0);
                d.transform.localScale = new Vector3(s, s, 1);
            }
        }

        // ================= 이벤트 연출 =================

        void OnEvent(BattleEvent e)
        {
            if (!Active) return;
            UnitView actor = e.Actor != null && _views.TryGetValue(e.Actor, out var a) ? a : null;
            UnitView target = e.Target != null && _views.TryGetValue(e.Target, out var t) ? t : null;
            switch (e.Type)
            {
                case BattleEventType.Damage:
                    if (actor != null && target != null && actor != target) actor.Lunge(target.transform.position.x > actor.transform.position.x ? 1 : -1);
                    target?.Hit(e.Value);
                    break;
                case BattleEventType.Heal:
                    if (target != null && e.Value > 0) target.Heal(e.Value);
                    else actor?.Popup("회복", new Color(0.5f, 1f, 0.6f));
                    break;
                case BattleEventType.Miss: target?.Popup("회피!", Color.white); break;
                case BattleEventType.Defeated: target?.Defeat(); break;
                case BattleEventType.Purified: target?.Purify(); break;
                case BattleEventType.PurifyFailed: target?.Popup("정화 실패", new Color(0.7f, 0.8f, 1f)); break;
                case BattleEventType.Faint: actor?.Faint(); break;
                case BattleEventType.Skill:
                    actor?.Popup(SkillLabel(e), new Color(1f, 0.9f, 0.4f));
                    break;
                case BattleEventType.Sustain: actor?.Popup(SkillLabel(e), new Color(0.6f, 0.9f, 1f)); break;
                case BattleEventType.Status:
                    if (actor != null && e.Text.Contains("방어")) actor.Popup("방어", new Color(0.7f, 0.85f, 1f));
                    break;
            }
        }

        static string SkillLabel(BattleEvent e)
        {
            string s = e.Text.Replace(e.Actor.ToString(), "").Trim();
            int arrow = s.IndexOf('→');
            if (arrow > 0 && !s.StartsWith("모방")) s = s.Substring(0, arrow).Trim();
            return s.Length > 12 ? s.Substring(0, 12) : s;
        }

        // ================= 필드 클릭으로 대상 고르기 =================

        HashSet<Unit> TargetableUnits(Battle b)
        {
            var set = new HashSet<Unit>();
            if (_gm.Animating || !_gm.PendingAction.HasValue || b.CurrentActor == null) return set;
            var domain = _gm.PendingAction == ActionType.Skill ? b.EffectiveSkill(b.CurrentActor)?.Target
                       : SkillTarget.Enemy;
            var pool = domain == SkillTarget.Enemy ? b.Enemies : domain == SkillTarget.Ally ? b.Allies : null;
            if (pool != null) foreach (var u in pool.Where(x => x.Active)) set.Add(u);
            return set;
        }

        void Update()
        {
            if (!Active || !Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            var cam = Camera.main;
            if (cam == null) return;
            var b = _gm.Run.CurrentBattle;
            var targets = TargetableUnits(b);
            if (targets.Count == 0) return;
            Vector2 p = cam.ScreenToWorldPoint(Input.mousePosition);
            var hit = _views.Values.Where(v => targets.Contains(v.Unit) && v.Contains(p)).FirstOrDefault();
            if (hit == null) return;
            var action = _gm.PendingAction.Value;
            _gm.SubmitBattleAction(action, hit.Unit);
        }

        // ================= 공용 스프라이트 =================

        internal static Sprite WhiteSprite => _white ??= MakeSprite(Solid(4, Color.white), 4, new Vector2(0, 0.5f));
        internal static Sprite GlowSprite => _glow ??= MakeSprite(Radial(64), 64, new Vector2(0.5f, 0.5f));
        internal static Sprite CircleSprite => _circle ??= TerrainTiles.Circle(64, Color.white, Color.white);
        internal static Sprite TriangleSprite => _tri ??= MakeSprite(Triangle(16), 16, new Vector2(0.5f, 0f));

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
            return Sprite.Create(tex, new Rect(0, 0, 2, 64), new Vector2(0.5f, 0.5f), 64); // 1×1 유닛 → localScale로 늘림
        }

        static Color[] Solid(int n, Color c) { var px = new Color[n * n]; for (int i = 0; i < px.Length; i++) px[i] = c; return px; }

        static Color[] Radial(int n)
        {
            var px = new Color[n * n];
            float c = (n - 1) / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(1 - d) * Mathf.Clamp01(1 - d));
                }
            return px;
        }

        static Color[] Triangle(int n)
        {
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int half = y / 2; // 아래를 가리키는 삼각형 (위가 넓음)
                    bool inside = y < n - 2 && Mathf.Abs(x - n / 2f + 0.5f) <= half * 0.9f;
                    px[y * n + x] = inside ? Color.white : Color.clear;
                }
            return px;
        }

        static Sprite MakeSprite(Color[] px, int n, Vector2 pivot)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), pivot, n);
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);
    }
}
