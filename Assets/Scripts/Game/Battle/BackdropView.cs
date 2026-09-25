using System.Collections.Generic;
using System.Linq;
using FedAndFound.Core;
using UnityEngine;
using Terrain = FedAndFound.Core.Terrain;

namespace FedAndFound.Game.BattleView
{
    /// <summary>맵·전투가 아닌 화면(타이틀·종 선택·유물 장착·전투 후·이벤트·작별·결과)의 배경.
    /// 지금 스테이지의 지형 풍경 위로 동물들이 천천히 걸어간다 — 런 중이면 우리 파티, 아니면 무작위 동물들.
    /// 유물 장착 화면에서는 이 배경이 곧 "지형 미리보기"(§11)가 된다.</summary>
    public sealed class BackdropView : MonoBehaviour
    {
        GameManager _gm;
        GameObject _world;
        string _key;

        public static void Create(GameManager gm)
        {
            var go = new GameObject("BackdropView");
            var v = go.AddComponent<BackdropView>();
            v._gm = gm;
            v._world = new GameObject("BackdropWorld");
            v._world.transform.SetParent(go.transform, false);
            gm.OnChanged += v.Sync;
            v.Sync();
        }

        void OnDestroy() { if (_gm != null) _gm.OnChanged -= Sync; }

        void Sync()
        {
            var run = _gm.Run;
            bool active = run == null || (run.Phase != RunPhase.Map && run.Phase != RunPhase.Battle);
            _world.SetActive(active);
            if (!active) return;

            var terrain = run?.Terrain ?? Terrain.Plains;
            var ids = run != null ? run.Party.Select(u => u.Species.Id).ToList() : TitleParade();
            string key = terrain + ":" + string.Join(",", ids);
            if (key == _key) return; // 같은 풍경·같은 동물이면 그대로 둔다(걷던 위치 유지)
            _key = key;

            foreach (Transform c in _world.transform) Destroy(c.gameObject);
            Scenery.Build(_world.transform, terrain);
            for (int i = 0; i < ids.Count; i++)
            {
                var w = new GameObject("Walker").AddComponent<Walker>();
                w.transform.SetParent(_world.transform, false);
                w.Setup(ids[i], -9f + i * (18f / Mathf.Max(1, ids.Count)), i);
            }
        }

        static List<string> TitleParade()
        {
            var rng = new System.Random(System.DateTime.Now.DayOfYear);
            return SpeciesDb.Roster.OrderBy(_ => rng.Next()).Take(6).Select(s => s.Id).ToList();
        }
    }

    /// <summary>화면 아래쪽을 가로질러 천천히 걷는 동물 (끝에 닿으면 반대편에서 다시 등장).</summary>
    public sealed class Walker : MonoBehaviour
    {
        SpriteRenderer _body;
        float _speed, _phase;

        public void Setup(string speciesId, float startX, int index)
        {
            var shadow = new GameObject("Shadow").AddComponent<SpriteRenderer>();
            shadow.transform.SetParent(transform, false);
            shadow.sprite = BattleStageView.GlowSprite; shadow.sortingOrder = 4;
            shadow.color = new Color(0, 0, 0, 0.35f);
            shadow.transform.localScale = new Vector3(1.4f, 0.3f, 1);

            _body = new GameObject("Body").AddComponent<SpriteRenderer>();
            _body.transform.SetParent(transform, false);
            _body.sprite = AnimalArt.Get(speciesId); _body.sortingOrder = 5;
            _body.transform.localScale = Vector3.one * 1.7f;

            _speed = 0.55f + (index % 3) * 0.12f;
            _phase = index * 1.3f;
            transform.localPosition = new Vector3(startX, -3.9f + (index % 2) * 0.35f, 0);
        }

        void Update()
        {
            var p = transform.localPosition;
            p.x += _speed * Time.deltaTime;
            if (p.x > 11f) p.x = -11f;
            transform.localPosition = p;
            // 통통 튀는 걸음
            _body.transform.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(Time.time * 5f + _phase)) * 0.12f, 0);
        }
    }
}
