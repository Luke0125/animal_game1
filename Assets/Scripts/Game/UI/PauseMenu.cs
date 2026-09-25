using System.Linq;
using FedAndFound.Core;
using FedAndFound.Game.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FedAndFound.Game.UI
{
    /// <summary>ESC 메뉴: 일시정지 + 업적·칭호 + 환경설정 + 게임 방법 + 타이틀로/종료.
    /// 화면(ScreenRouter)과 별개인 전역 오버레이라 어느 화면에서든 열린다. 열려 있는 동안 Time.timeScale = 0.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        public static bool IsOpen => _inst != null && _inst._open;
        static PauseMenu _inst;

        enum Page { Main, Achievements, Settings, HowTo, ConfirmTitle }

        GameManager _gm;
        RectTransform _root;
        bool _open;
        Page _page;

        public static void Create(Transform canvas, GameManager gm)
        {
            var go = new GameObject("PauseMenu", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            UIFactory.Stretch((RectTransform)go.transform);
            _inst = go.AddComponent<PauseMenu>();
            _inst._gm = gm;
            _inst._root = (RectTransform)go.transform;
            Settings.Load();
        }

        /// <summary>타이틀 화면 버튼에서 특정 페이지로 바로 열기.</summary>
        public static void Open(string page)
        {
            if (_inst == null) return;
            _inst._page = page == "achievements" ? Page.Achievements : page == "settings" ? Page.Settings : Page.Main;
            _inst.SetOpen(true);
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (_open && _page != Page.Main) { _page = Page.Main; Rebuild(); } // 하위 페이지면 한 단계 뒤로
            else { _page = Page.Main; SetOpen(!_open); }
        }

        void SetOpen(bool open)
        {
            _open = open;
            Time.timeScale = open ? 0f : 1f; // 전투 연출·걷기·필드 입력이 멈춘다
            Audio.SoundManager.Play(Audio.Sfx.Click, 0.6f);
            Rebuild();
        }

        void Rebuild()
        {
            for (int i = _root.childCount - 1; i >= 0; i--) Destroy(_root.GetChild(i).gameObject);
            if (!_open) return;
            _root.SetAsLastSibling();
            var tooltip = _root.parent.Find("Tooltip");
            if (tooltip != null) tooltip.SetAsLastSibling(); // 툴팁은 메뉴보다 위

            var dim = UIFactory.Panel(_root, "Dim", new Color(0, 0, 0, 0.6f)); // 뒤 화면 클릭 차단
            dim.GetComponent<Image>().sprite = null;

            switch (_page)
            {
                case Page.Main: MainPage(); break;
                case Page.Achievements: AchievementsPage(); break;
                case Page.Settings: SettingsPage(); break;
                case Page.HowTo: HowToPage(); break;
                case Page.ConfirmTitle: ConfirmPage(); break;
            }
        }

        void Go(Page p) { _page = p; Rebuild(); }

        void MainPage()
        {
            var v = UIFactory.Window(_root, new Vector2(0.37f, 0.18f), new Vector2(0.63f, 0.84f), "일시정지");
            string title = AchievementStore.EquippedTitle;
            UIFactory.Label(v, title != null ? $"칭호 「{title}」" : "칭호 없음", 17, TextAnchor.MiddleCenter, Theme.TextDim);
            UIFactory.Button(v, "계속하기", () => SetOpen(false), bg: new Color(0.55f, 0.43f, 0.2f), fontSize: 22);
            UIFactory.Button(v, $"업적 · 칭호  ({AchievementStore.Unlocked.Count}/{Achievements.All.Count})", () => Go(Page.Achievements), fontSize: 20);
            UIFactory.Button(v, "환경설정", () => Go(Page.Settings), fontSize: 20);
            UIFactory.Button(v, "게임 방법", () => Go(Page.HowTo), fontSize: 20);
            if (_gm.Run != null) UIFactory.Button(v, "타이틀로 (이번 판 포기)", () => Go(Page.ConfirmTitle), fontSize: 18);
            UIFactory.Button(v, "게임 종료", _gm.QuitGame, fontSize: 18);
            UIFactory.Label(v, "ESC: 닫기 / 뒤로", 14, TextAnchor.MiddleCenter, Theme.TextDim);
        }

        void AchievementsPage()
        {
            var v = UIFactory.Window(_root, new Vector2(0.18f, 0.08f), new Vector2(0.82f, 0.94f), "업적 · 칭호");
            UIFactory.Label(v, "달성한 업적을 누르면 그 칭호를 단다. (?는 이벤트에서 특정 선택을 하면 열리는 숨은 업적)", 15, TextAnchor.MiddleCenter, Theme.TextDim);
            var list = UIFactory.ScrollList(v, 560);
            foreach (var a in Achievements.All)
            {
                bool got = AchievementStore.Unlocked.Contains(a.Id);
                bool equipped = AchievementStore.EquippedTitleId == a.Id;
                string label = got
                    ? $"{(equipped ? "[장착] " : "")}{a.Name}  —  칭호 「{a.Title}」   ({a.Desc})"
                    : a.Hidden ? "???  —  숨은 업적 (이벤트에서의 선택)" : $"(잠김) {a.Name}  —  {a.Desc}";
                var row = UIFactory.HGroup(list, 6);
                UIFactory.FixedHeight(row, 42);
                var id = a.Id;
                UIFactory.Button(row, label, () => { AchievementStore.Equip(id); Rebuild(); }, got,
                    bg: equipped ? new Color(0.62f, 0.5f, 0.22f) : new Color(0.22f, 0.18f, 0.14f), fontSize: 16);
            }
            UIFactory.Button(v, "뒤로", () => Go(Page.Main), fontSize: 18);
        }

        void SettingsPage()
        {
            var v = UIFactory.Window(_root, new Vector2(0.33f, 0.2f), new Vector2(0.67f, 0.82f), "환경설정");
            Stepper(v, "배경음", Settings.MusicVolume, x => Settings.SetMusic(x));
            Stepper(v, "효과음", Settings.SfxVolume, x => { Settings.SetSfx(x); Audio.SoundManager.Play(Audio.Sfx.Hit); });
            UIFactory.Button(v, $"음소거 (M 키): {(Audio.SoundManager.Muted ? "켜짐" : "꺼짐")}", () => { Audio.SoundManager.ToggleMute(); Rebuild(); }, fontSize: 18);
            UIFactory.Button(v, $"전투 연출 속도: {(Settings.FastBattle ? "빠름 (2배)" : "보통")}", () => { Settings.ToggleFastBattle(); Rebuild(); }, fontSize: 18);
            UIFactory.Button(v, $"전체화면: {(Settings.Fullscreen ? "켜짐" : "꺼짐")}", () => { Settings.ToggleFullscreen(); Rebuild(); }, fontSize: 18);
            UIFactory.Label(v, "설정은 자동으로 저장된다.", 14, TextAnchor.MiddleCenter, Theme.TextDim);
            UIFactory.Button(v, "뒤로", () => Go(Page.Main), fontSize: 18);
        }

        /// <summary>슬라이더 대신 − 값 + (0~10). 막대로 현재 값을 보여 준다.</summary>
        void Stepper(Transform parent, string name, int value, System.Action<int> set)
        {
            var row = UIFactory.HGroup(parent, 8);
            UIFactory.FixedHeight(row, 44);
            UIFactory.Label(row, name, 19, TextAnchor.MiddleLeft);
            UIFactory.Button(row, "-", () => { set(value - 1); Rebuild(); }, value > 0, fontSize: 22);
            UIFactory.Bar(row, value / 10f, Theme.Gold, $"{value * 10}%", 15);
            UIFactory.Button(row, "+", () => { set(value + 1); Rebuild(); }, value < 10, fontSize: 22);
        }

        void HowToPage()
        {
            var v = UIFactory.Window(_root, new Vector2(0.12f, 0.1f), new Vector2(0.88f, 0.92f), "게임 방법");
            foreach (var l in Screens.HowToScreen.Lines) UIFactory.Label(v, l, 19, TextAnchor.MiddleLeft);
            UIFactory.Label(v, "■ 단축키 — ESC: 일시정지 메뉴 · M: 소리 끄기/켜기", 19, TextAnchor.MiddleLeft);
            UIFactory.Button(v, "뒤로", () => Go(Page.Main), fontSize: 18);
        }

        void ConfirmPage()
        {
            var v = UIFactory.Window(_root, new Vector2(0.34f, 0.36f), new Vector2(0.66f, 0.64f), "타이틀로 돌아갈까?");
            UIFactory.Label(v, "이번 판의 진행은 사라진다. (달성한 업적·칭호는 남는다)", 17, TextAnchor.MiddleCenter);
            var row = UIFactory.HGroup(v, 12);
            UIFactory.FixedHeight(row, 50);
            UIFactory.Button(row, "취소", () => Go(Page.Main), fontSize: 19);
            UIFactory.Button(row, "타이틀로", () => { SetOpen(false); _gm.ResetToTitle(); }, bg: new Color(0.6f, 0.22f, 0.18f), fontSize: 19);
        }

        void OnDestroy() { if (_inst == this) { Time.timeScale = 1f; _inst = null; } }
    }
}
