using UnityEngine;

namespace FedAndFound.Game.BattleView
{
    /// <summary>데미지 숫자 등 떠오르는 글자. 톡 커졌다가 위로 떠오르며 흐려지고 스스로 사라진다.</summary>
    public sealed class FloatingText : MonoBehaviour
    {
        const float Life = 0.9f;
        TextMesh _main, _shadow;
        Vector3 _start;
        Color _base;
        float _t;

        void Start()
        {
            _main = GetComponent<TextMesh>();
            _shadow = transform.GetChild(0).GetComponent<TextMesh>();
            _start = transform.localPosition;
            _base = _main.color;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = _t / Life;
            if (k >= 1f) { Destroy(gameObject); return; }
            transform.localPosition = _start + new Vector3(0, 0.9f * Mathf.Sqrt(k), 0);
            transform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(1.4f, 1f, k / 0.15f) : 1f);
            float a = k < 0.6f ? 1 : 1 - (k - 0.6f) / 0.4f;
            _main.color = new Color(_base.r, _base.g, _base.b, a);
            _shadow.color = new Color(0, 0, 0, a * 0.8f);
        }
    }
}
