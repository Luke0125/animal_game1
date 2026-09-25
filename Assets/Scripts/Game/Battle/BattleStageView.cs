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
        GameObject _world;
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
            Scenery.Build(_world.transform, _gm.Run.Terrain);
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
            if (!Active || !Input.GetMouseButtonDown(0) || UI.PauseMenu.IsOpen) return;
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

    }
}
