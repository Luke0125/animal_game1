using System.Collections;
using FedAndFound.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FedAndFound.Game.UI
{
    /// <summary>ScreenRouter가 화면을 통째로 다시 그려도 살아남는 전역 오버레이.
    /// 전투 이벤트가 재생될 때마다(GameManager.OnBattleEvent) 종류에 맞는 색으로 화면을 짧게 물들여
    /// "무슨 일이 일어났다"는 걸 즉각적으로 체감시킨다(3단계 연출, HP바 트윈 대신 채택한 저비용 대안).</summary>
    public sealed class EventFeedOverlay : MonoBehaviour
    {
        Image _flash;
        GameManager _gm;

        public static void Create(Transform canvasParent, GameManager gm)
        {
            var go = new GameObject("EventFeedOverlay", typeof(RectTransform));
            go.transform.SetParent(canvasParent, false);
            UIFactory.Stretch((RectTransform)go.transform);
            go.AddComponent<EventFeedOverlay>().Init(gm);
        }

        void Init(GameManager gm)
        {
            _gm = gm;
            var rt = UIFactory.Panel(transform, "Flash", new Color(0, 0, 0, 0));
            _flash = rt.GetComponent<Image>();
            _flash.sprite = null; // 화면 전체 번쩍임은 둥근 모서리 없이
            _flash.raycastTarget = false; // 클릭을 막지 않는다
            gm.OnBattleEvent += HandleEvent;
        }

        void OnDestroy() { if (_gm != null) _gm.OnBattleEvent -= HandleEvent; }

        void HandleEvent(BattleEvent e)
        {
            Color c = e.Type switch
            {
                BattleEventType.Damage => e.Target != null && e.Target.IsEnemy
                    ? new Color(0.4f, 0.8f, 0.4f, 0.16f)   // 적이 맞음 = 좋은 일
                    : new Color(0.85f, 0.25f, 0.25f, 0.2f), // 아군이 맞음 = 경고
                BattleEventType.Purified => new Color(0.4f, 0.6f, 0.9f, 0.2f),
                BattleEventType.Defeated => new Color(0.9f, 0.6f, 0.2f, 0.18f),
                BattleEventType.Heal => new Color(0.4f, 0.9f, 0.5f, 0.15f),
                BattleEventType.Faint => new Color(0.6f, 0.1f, 0.1f, 0.28f),
                _ => new Color(0, 0, 0, 0),
            };
            if (c.a <= 0f) return;
            StopAllCoroutines();
            StartCoroutine(Flash(c));
        }

        IEnumerator Flash(Color c)
        {
            float t = 0, dur = 0.22f;
            _flash.color = c;
            while (t < dur)
            {
                t += Time.deltaTime;
                _flash.color = Color.Lerp(c, new Color(c.r, c.g, c.b, 0), t / dur);
                yield return null;
            }
            _flash.color = new Color(c.r, c.g, c.b, 0);
        }
    }
}
