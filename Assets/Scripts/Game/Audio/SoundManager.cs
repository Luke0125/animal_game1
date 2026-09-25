using System.Collections.Generic;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.Audio
{
    public enum Sfx { Click, Step, Hit, Heavy, Miss, Heal, Purify, Defeat, Faint, Skill, Gem, Victory, Lose, Node }

    /// <summary>효과음·배경음을 오디오 파일 없이 코드로 합성한다(사인파·노이즈 + 음량 곡선).
    /// 전투 이벤트(GameManager.OnBattleEvent)·버튼 클릭(UIFactory)·발걸음(FieldView)에서 부른다.
    /// M 키 또는 타이틀의 "소리" 버튼으로 켜고 끈다. 실제 음원으로 바꿀 땐 Clip()만 교체하면 된다.</summary>
    public sealed class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }
        public static bool Muted { get; private set; }

        const int Rate = 44100;
        AudioSource _sfx, _bgm;
        readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        float _lastClick;

        public static void Create(GameManager gm)
        {
            var go = new GameObject("SoundManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SoundManager>();
            Instance.Init(gm);
        }

        void Init(GameManager gm)
        {
            if (FindFirstObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            _sfx = gameObject.AddComponent<AudioSource>();
            _sfx.playOnAwake = false; _sfx.volume = 0.55f;
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.loop = true; _bgm.volume = 0.16f; _bgm.clip = MakeBgm(); _bgm.Play();
            gm.OnBattleEvent += OnBattleEvent;
        }

        public static void Play(Sfx s, float volume = 1f)
        {
            if (Instance == null || Muted) return;
            if (s == Sfx.Click) { if (Time.unscaledTime - Instance._lastClick < 0.05f) return; Instance._lastClick = Time.unscaledTime; }
            Instance._sfx.pitch = s == Sfx.Step || s == Sfx.Hit ? Random.Range(0.92f, 1.08f) : 1f; // 반복음이 덜 기계적으로
            Instance._sfx.PlayOneShot(Instance.Clip(s), volume);
        }

        public static void ToggleMute()
        {
            Muted = !Muted;
            if (Instance != null) Instance._bgm.mute = Muted;
        }

        void Update() { if (Input.GetKeyDown(KeyCode.M)) ToggleMute(); }

        void OnBattleEvent(BattleEvent e)
        {
            switch (e.Type)
            {
                case BattleEventType.Damage: Play(e.Value >= 20 ? Sfx.Heavy : Sfx.Hit); break;
                case BattleEventType.Miss: Play(Sfx.Miss); break;
                case BattleEventType.Heal: Play(Sfx.Heal, 0.7f); break;
                case BattleEventType.Purified: Play(Sfx.Purify); break;
                case BattleEventType.Defeated: Play(Sfx.Defeat); break;
                case BattleEventType.Faint: Play(Sfx.Faint); break;
                case BattleEventType.Skill: case BattleEventType.Sustain: Play(Sfx.Skill, 0.7f); break;
                case BattleEventType.End: Play(e.Text.Contains("승리") ? Sfx.Victory : Sfx.Lose); break;
            }
        }

        // ================= 합성 =================

        AudioClip Clip(Sfx s)
        {
            if (_clips.TryGetValue(s, out var c)) return c;
            float[] d = s switch
            {
                Sfx.Click => Tone(0.05f, 880, 660, 0.35f, square: true),
                Sfx.Step => Noise(0.05f, 0.25f, lowpass: 0.15f),
                Sfx.Hit => Mix(Noise(0.12f, 0.6f, lowpass: 0.3f), Tone(0.12f, 180, 90, 0.5f)),
                Sfx.Heavy => Mix(Noise(0.22f, 0.8f, lowpass: 0.2f), Tone(0.22f, 120, 50, 0.7f)),
                Sfx.Miss => Noise(0.18f, 0.3f, lowpass: 0.6f, rising: true),
                Sfx.Heal => Arp(new[] { 523f, 659f, 784f }, 0.09f, 0.35f),
                Sfx.Purify => Arp(new[] { 659f, 784f, 988f, 1319f }, 0.08f, 0.4f, sparkle: true),
                Sfx.Defeat => Mix(Tone(0.35f, 220, 70, 0.6f), Noise(0.3f, 0.3f, lowpass: 0.1f)),
                Sfx.Faint => Tone(0.5f, 330, 110, 0.45f),
                Sfx.Skill => Tone(0.18f, 440, 880, 0.35f, square: true),
                Sfx.Gem => Arp(new[] { 784f, 988f, 1175f, 1568f }, 0.06f, 0.35f, sparkle: true),
                Sfx.Victory => Arp(new[] { 523f, 659f, 784f, 1047f, 784f, 1047f }, 0.12f, 0.4f),
                Sfx.Lose => Arp(new[] { 392f, 330f, 262f, 196f }, 0.2f, 0.4f),
                _ => Tone(0.12f, 660, 990, 0.35f), // Node
            };
            var clip = AudioClip.Create(s.ToString(), d.Length, 1, Rate, false);
            clip.SetData(d, 0);
            return _clips[s] = clip;
        }

        /// <summary>주파수가 f0→f1로 미끄러지는 음 + 짧은 어택/지수 감쇠.</summary>
        static float[] Tone(float dur, float f0, float f1, float vol, bool square = false)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            double phase = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                phase += 2 * Mathf.PI * Mathf.Lerp(f0, f1, t) / Rate;
                float w = (float)System.Math.Sin(phase);
                if (square) w = w > 0 ? 0.6f : -0.6f;
                d[i] = w * vol * Env(t, n);
            }
            return d;
        }

        static float[] Noise(float dur, float vol, float lowpass, bool rising = false)
        {
            int n = (int)(dur * Rate);
            var d = new float[n];
            var rng = new System.Random(n);
            float y = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float a = rising ? lowpass * (0.3f + t) : lowpass;
                y += ((float)rng.NextDouble() * 2 - 1 - y) * a; // 간단한 저역 통과 필터
                d[i] = y * vol * 2f * Env(t, n);
            }
            return d;
        }

        /// <summary>음을 차례로 울리는 아르페지오 (정화·회복·승리 징글).</summary>
        static float[] Arp(float[] notes, float step, float vol, bool sparkle = false)
        {
            float dur = step * notes.Length + 0.3f;
            int n = (int)(dur * Rate), stepN = (int)(step * Rate);
            var d = new float[n];
            for (int k = 0; k < notes.Length; k++)
            {
                int start = k * stepN, len = Mathf.Min(n - start, (int)(0.35f * Rate));
                for (int i = 0; i < len; i++)
                {
                    float t = (float)i / Rate;
                    float env = Mathf.Min(1, t * 200f) * Mathf.Exp(-t * 9f);
                    float w = Mathf.Sin(2 * Mathf.PI * notes[k] * t) + (sparkle ? 0.3f * Mathf.Sin(2 * Mathf.PI * notes[k] * 2 * t) : 0);
                    d[start + i] += w * vol * env;
                }
            }
            return d;
        }

        static float Env(float t, int n) => Mathf.Min(1f, t * n / (Rate * 0.004f)) * Mathf.Exp(-t * 4f);

        static float[] Mix(float[] a, float[] b)
        {
            var d = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < d.Length; i++) d[i] = (i < a.Length ? a[i] : 0) + (i < b.Length ? b[i] : 0);
            return d;
        }

        /// <summary>배경음: 5음 음계로 된 잔잔한 16마디 루프(부드러운 패드 + 오르골 멜로디).</summary>
        static AudioClip MakeBgm()
        {
            float[] scale = { 261.6f, 293.7f, 329.6f, 392f, 440f, 523.3f, 587.3f, 659.3f };
            int[] melody = { 0, 2, 4, 5, 4, 2, 3, 1, 0, 2, 4, 7, 6, 4, 3, 2, 4, 5, 7, 5, 4, 2, 1, 0, 2, 3, 4, 2, 1, 0, 1, 2 };
            float[] chords = { 130.8f, 110f, 146.8f, 98f };
            float beat = 0.5f;
            int n = (int)(melody.Length * beat * Rate);
            var d = new float[n];
            int beatN = (int)(beat * Rate);
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                int bar = i / (beatN * 8) % chords.Length;
                float root = chords[bar];
                float edge = Mathf.Min(1f, Mathf.Min(i, n - i) / (0.03f * Rate)); // 루프 이음새에서 딸깍 소리 방지
                d[i] += 0.12f * edge * (Mathf.Sin(2 * Mathf.PI * root * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * root * 1.5f * t)); // 패드
            }
            for (int k = 0; k < melody.Length; k++)
            {
                float f = scale[melody[k]] * 2f;
                int start = k * beatN;
                for (int i = 0; i < beatN && start + i < n; i++)
                {
                    float t = (float)i / Rate;
                    d[start + i] += 0.22f * Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 5f); // 오르골
                }
            }
            var clip = AudioClip.Create("Bgm", n, 1, Rate, false);
            clip.SetData(d, 0);
            return clip;
        }
    }
}
