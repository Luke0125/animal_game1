using UnityEngine;

namespace FedAndFound.Game.Meta
{
    /// <summary>기본 환경설정 (ESC 메뉴). PlayerPrefs에 저장되어 다음 실행에도 유지된다.</summary>
    public static class Settings
    {
        public static int MusicVolume { get; private set; } = 5;  // 0~10
        public static int SfxVolume { get; private set; } = 7;    // 0~10
        public static bool FastBattle { get; private set; }       // 전투 연출 2배속
        public static bool Fullscreen => Screen.fullScreen;

        public static void Load()
        {
            MusicVolume = PlayerPrefs.GetInt("set_music", 5);
            SfxVolume = PlayerPrefs.GetInt("set_sfx", 7);
            FastBattle = PlayerPrefs.GetInt("set_fast", 0) == 1;
            Apply();
        }

        public static void SetMusic(int v) { MusicVolume = Mathf.Clamp(v, 0, 10); Save(); }
        public static void SetSfx(int v) { SfxVolume = Mathf.Clamp(v, 0, 10); Save(); }
        public static void ToggleFastBattle() { FastBattle = !FastBattle; Save(); }
        public static void ToggleFullscreen() { Screen.fullScreen = !Screen.fullScreen; }

        static void Save()
        {
            PlayerPrefs.SetInt("set_music", MusicVolume);
            PlayerPrefs.SetInt("set_sfx", SfxVolume);
            PlayerPrefs.SetInt("set_fast", FastBattle ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        static void Apply() => Audio.SoundManager.SetVolumes(MusicVolume / 10f, SfxVolume / 10f);
    }
}
