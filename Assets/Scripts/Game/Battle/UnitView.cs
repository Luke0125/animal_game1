using System.Collections;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.BattleView
{
    /// <summary>전투 무대 위 동물 1마리: 스프라이트 + 그림자 + (적) 저주 오라 + HP바 + 차례/대상 표시 + 연출.
    /// HP바는 이벤트 재생에 맞춰 줄어드는 "표시용 HP"를 쓴다(Core는 한 턴을 통째로 계산하므로).</summary>
    public sealed class UnitView : MonoBehaviour
    {
        public Unit Unit { get; private set; }

        SpriteRenderer _body, _shadow, _aura, _hpFill, _hpBack, _marker;
        TextMesh _name;
        float _scale, _phase, _flash, _shownRatio;
        int _displayHp;
        Vector3 _offset;
        bool _gone, _fainted;
        Color _tint = Color.white;
        const float BarWidth = 1.3f;

        public void Setup(Unit u, float scale)
        {
            Unit = u; _scale = scale; _phase = Random.value * 6f;
            _displayHp = u.Hp; _shownRatio = u.HpRatio;

            _shadow = Child("Shadow", BattleStageView.GlowSprite, 5);
            _shadow.color = new Color(0, 0, 0, 0.45f);
            _shadow.transform.localScale = new Vector3(scale * 0.9f, scale * 0.22f, 1);

            if (u.IsEnemy)
            {
                // 저주받은 동물: 붉은 오라 + 어두운 색조 (레퍼런스의 "타락한" 느낌)
                _aura = Child("Curse", BattleStageView.GlowSprite, 8);
                _aura.color = new Color(0.9f, 0.1f, 0.1f, 0.5f);
                _aura.transform.localPosition = new Vector3(0, scale * 0.45f, 0);
                _aura.transform.localScale = Vector3.one * scale * 1.5f;
                _tint = new Color(0.78f, 0.62f, 0.66f);
            }

            _body = Child("Body", AnimalArt.Get(u.Species.Id), 10);
            _body.flipX = u.IsEnemy; // 적은 왼쪽을 본다
            _body.transform.localScale = Vector3.one * scale;
            _body.color = _tint;

            float barY = scale * 0.95f + 0.25f;
            _hpBack = Child("HpBack", BattleStageView.WhiteSprite, 20);
            _hpBack.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            _hpBack.transform.localPosition = new Vector3(-BarWidth / 2, barY, 0);
            _hpBack.transform.localScale = new Vector3(BarWidth, 0.14f, 1);
            _hpFill = Child("HpFill", BattleStageView.WhiteSprite, 21);
            _hpFill.transform.localPosition = new Vector3(-BarWidth / 2, barY, 0);

            _name = MakeText(transform, u.IsBoss ? $"[보스] {u.Name}" : u.Name, new Vector3(0, barY + 0.3f, 0), 0.9f, Color.white, 22);

            _marker = Child("Marker", BattleStageView.TriangleSprite, 23);
            _marker.transform.localScale = new Vector3(0.5f, 0.45f, 1);
            _marker.gameObject.SetActive(false);
            UpdateBar(true);
        }

        SpriteRenderer Child(string name, Sprite s, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.transform.SetParent(transform, false);
            sr.sprite = s; sr.sortingOrder = order;
            return sr;
        }

        // ================= 매 프레임 =================

        void Update()
        {
            if (_body == null) return;
            if (!_gone && !_fainted)
            {
                float bob = Mathf.Sin(Time.time * 2.2f + _phase) * 0.035f * _scale;
                _body.transform.localPosition = _offset + new Vector3(0, bob, 0);
                _body.color = Color.Lerp(_tint, new Color(1f, 0.35f, 0.3f), _flash);
            }
            _flash = Mathf.MoveTowards(_flash, 0, Time.deltaTime * 4f);
            if (_aura != null && !_gone) _aura.color = new Color(0.9f, 0.1f, 0.1f, 0.35f + 0.15f * Mathf.Sin(Time.time * 3f + _phase));
            if (_marker.gameObject.activeSelf)
                _marker.transform.localPosition = new Vector3(0, _scale * 0.95f + 0.75f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.15f, 0);
            UpdateBar(false);
        }

        void UpdateBar(bool snap)
        {
            float target = Unit.MaxHp <= 0 ? 0 : Mathf.Clamp01((float)_displayHp / Unit.MaxHp);
            _shownRatio = snap ? target : Mathf.MoveTowards(_shownRatio, target, Time.deltaTime * 1.5f); // HP바가 부드럽게 줄어든다
            _hpFill.transform.localScale = new Vector3(BarWidth * _shownRatio, 0.14f, 1);
            _hpFill.color = _shownRatio > 0.3f ? new Color(0.35f, 0.8f, 0.4f) : new Color(0.9f, 0.3f, 0.25f);
        }

        // ================= 외부에서 부르는 연출 =================

        public void SetMarker(bool current, bool targetable)
        {
            _marker.gameObject.SetActive(!_gone && !_fainted && (current || targetable));
            _marker.color = targetable ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.9f, 0.3f);
        }

        /// <summary>이벤트 재생이 끝났을 때 실제 상태로 맞춘다 (연출이 누락됐어도 화면이 틀리지 않게).</summary>
        public void SnapToTruth()
        {
            _displayHp = Unit.Hp;
            if (Unit.Removed && !_gone) { _gone = true; gameObject.SetActive(false); }
            else if (!Unit.IsEnemy && Unit.Fainted && !_fainted) Faint();
        }

        public bool Contains(Vector2 p)
        {
            var pos = transform.position;
            return Mathf.Abs(p.x - pos.x) <= _scale * 0.5f && p.y >= pos.y - 0.2f && p.y <= pos.y + _scale;
        }

        public void Lunge(int dir) { if (!_gone && !_fainted) StartCoroutine(LungeCo(dir)); }

        IEnumerator LungeCo(int dir)
        {
            for (float t = 0; t < 0.2f; t += Time.deltaTime)
            {
                _offset = new Vector3(Mathf.Sin(t / 0.2f * Mathf.PI) * 0.8f * dir, 0, 0);
                yield return null;
            }
            _offset = Vector3.zero;
        }

        public void Hit(int dmg)
        {
            _displayHp = Mathf.Max(0, _displayHp - dmg);
            _flash = 1f;
            Popup($"-{dmg}", Color.white, 1.3f);
            if (!_gone && !_fainted) StartCoroutine(ShakeCo());
        }

        IEnumerator ShakeCo()
        {
            yield return new WaitForSeconds(0.08f); // 돌진이 닿는 순간에 맞춤
            for (float t = 0; t < 0.22f; t += Time.deltaTime)
            {
                _offset = new Vector3(Random.Range(-0.12f, 0.12f), Random.Range(-0.04f, 0.04f), 0) * _scale * 0.5f;
                yield return null;
            }
            _offset = Vector3.zero;
        }

        public void Heal(int v)
        {
            _displayHp = Mathf.Min(Unit.MaxHp, _displayHp + v);
            Popup($"+{v}", new Color(0.5f, 1f, 0.55f), 1.1f);
        }

        public void Defeat()
        {
            if (_gone) return;
            _gone = true;
            Popup("물리침!", new Color(1f, 0.7f, 0.3f), 1f);
            StartCoroutine(FadeCo(sink: true, glow: false));
        }

        public void Purify()
        {
            if (_gone) return;
            _gone = true;
            Popup("정화!", new Color(0.6f, 0.85f, 1f), 1.2f);
            StartCoroutine(FadeCo(sink: false, glow: true));
        }

        IEnumerator FadeCo(bool sink, bool glow)
        {
            yield return new WaitForSeconds(0.15f);
            if (_aura != null) _aura.enabled = false;
            _hpBack.enabled = _hpFill.enabled = false;
            _marker.gameObject.SetActive(false);
            var start = _body.transform.localPosition;
            var from = glow ? new Color(1f, 1f, 0.85f) : _tint; // 정화: 저주가 걷히며 원래 빛깔 → 빛
            for (float t = 0; t < 0.7f; t += Time.deltaTime)
            {
                float k = t / 0.7f;
                _body.color = new Color(from.r, from.g, from.b, 1 - k);
                _shadow.color = new Color(0, 0, 0, 0.45f * (1 - k));
                _body.transform.localPosition = start + new Vector3(0, sink ? -0.4f * k : 0.8f * k, 0);
                if (sink) _body.transform.localRotation = Quaternion.Euler(0, 0, (Unit.IsEnemy ? -1 : 1) * 25f * k);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        public void Faint()
        {
            if (_fainted) return;
            _fainted = true;
            _marker.gameObject.SetActive(false);
            _hpFill.transform.localScale = new Vector3(0, 0.14f, 1);
            _body.transform.localRotation = Quaternion.Euler(0, 0, Unit.IsEnemy ? -80f : 80f); // 쓰러져 눕는다
            _body.transform.localPosition = new Vector3(Unit.IsEnemy ? 0.4f : -0.4f, 0.1f, 0) * _scale * 0.5f;
            _body.color = new Color(0.55f, 0.55f, 0.58f, 0.85f);
            Popup("기절", new Color(0.8f, 0.8f, 0.85f), 1f);
        }

        /// <summary>머리 위로 떠올랐다 사라지는 글자 (데미지 숫자 등).</summary>
        public void Popup(string text, Color color, float size = 1f)
        {
            if (!isActiveAndEnabled) return;
            var tm = MakeText(transform.parent, text, transform.localPosition + new Vector3(Random.Range(-0.2f, 0.2f), _scale * 0.8f, 0), size, color, 40);
            tm.gameObject.AddComponent<FloatingText>(); // 유닛이 사라져도 글자는 끝까지 떠오르고 스스로 지워진다
        }

        /// <summary>그림자 글자를 뒤에 깐 월드 텍스트 (배경 위에서도 읽히게).</summary>
        static TextMesh MakeText(Transform parent, string text, Vector3 localPos, float size, Color color, int order)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var main = AddText(go, text, color, order, size);
            var sh = new GameObject("Shadow");
            sh.transform.SetParent(go.transform, false);
            sh.transform.localPosition = new Vector3(0.04f, -0.04f, 0);
            AddText(sh, text, new Color(0, 0, 0, 0.8f), order - 1, size);
            return main;
        }

        static TextMesh AddText(GameObject go, string text, Color color, int order, float size)
        {
            var tm = go.AddComponent<TextMesh>();
            tm.font = UI.Theme.FontBold;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.045f * size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = tm.font.material;
            mr.sortingOrder = order;
            return tm;
        }
    }
}
