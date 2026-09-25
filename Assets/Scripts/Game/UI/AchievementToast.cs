using System.Collections;
using System.Collections.Generic;
using FedAndFound.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FedAndFound.Game.UI
{
    /// <summary>업적 달성 알림: 화면 위쪽 가운데에 금테 창이 내려왔다가 사라진다. 여러 개면 차례로.</summary>
    public sealed class AchievementToast : MonoBehaviour
    {
        readonly Queue<AchievementData> _queue = new Queue<AchievementData>();
        RectTransform _box;
        Text _title, _body;
        bool _showing;

        public static void Create(Transform canvas, GameManager gm)
        {
            var go = new GameObject("AchievementToast", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            UIFactory.Stretch((RectTransform)go.transform);
            var t = go.AddComponent<AchievementToast>();
            var v = UIFactory.Window(go.transform, new Vector2(0.33f, 0.84f), new Vector2(0.67f, 0.98f), padding: 16);
            t._box = (RectTransform)v.parent;
            foreach (var g in t._box.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            t._box.GetComponent<Image>().raycastTarget = false;
            t._title = UIFactory.Label(v, "", 24, TextAnchor.MiddleCenter, Theme.Gold, bold: true);
            t._body = UIFactory.Label(v, "", 17, TextAnchor.MiddleCenter);
            t._title.raycastTarget = t._body.raycastTarget = false;
            t._box.gameObject.SetActive(false);
            gm.OnAchievement += t.Enqueue;
        }

        void Enqueue(AchievementData a)
        {
            _queue.Enqueue(a);
            if (!_showing) StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            _showing = true;
            while (_queue.Count > 0)
            {
                var a = _queue.Dequeue();
                _title.text = $"업적 달성!  {a.Name}";
                _body.text = $"칭호 「{a.Title}」 획득 — {a.Desc}";
                Audio.SoundManager.Play(Audio.Sfx.Gem);
                _box.gameObject.SetActive(true);
                _box.SetAsLastSibling();
                // 위에서 톡 내려와서 잠시 머물렀다가 올라간다 (일시정지 중에도 보이도록 unscaled 시간)
                for (float t = 0; t < 3f; t += Time.unscaledDeltaTime)
                {
                    float y = t < 0.25f ? Mathf.Lerp(160, 0, t / 0.25f) : t > 2.7f ? Mathf.Lerp(0, 160, (t - 2.7f) / 0.3f) : 0;
                    _box.anchoredPosition = new Vector2(0, y);
                    yield return null;
                }
                _box.gameObject.SetActive(false);
            }
            _showing = false;
        }
    }
}
