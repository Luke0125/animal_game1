using System.Collections.Generic;
using System.Linq;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.Meta
{
    /// <summary>업적 영구 기록 + 장착한 칭호 (PlayerPrefs). 판정은 Core의 Achievements가 한다.</summary>
    public static class AchievementStore
    {
        static HashSet<string> _unlocked;
        public static string EquippedTitleId { get; private set; }

        public static HashSet<string> Unlocked
        {
            get
            {
                if (_unlocked == null)
                {
                    var raw = PlayerPrefs.GetString("ach_unlocked", "");
                    _unlocked = new HashSet<string>(raw.Split(',').Where(x => x.Length > 0));
                    EquippedTitleId = PlayerPrefs.GetString("ach_title", "");
                }
                return _unlocked;
            }
        }

        /// <summary>처음 달성이면 true.</summary>
        public static bool Unlock(string id)
        {
            if (!Unlocked.Add(id)) return false;
            PlayerPrefs.SetString("ach_unlocked", string.Join(",", _unlocked));
            if (string.IsNullOrEmpty(EquippedTitleId)) Equip(id); // 첫 칭호는 자동 장착
            PlayerPrefs.Save();
            return true;
        }

        public static void Equip(string id)
        {
            if (!Unlocked.Contains(id)) return;
            EquippedTitleId = id;
            PlayerPrefs.SetString("ach_title", id);
            PlayerPrefs.Save();
        }

        public static string EquippedTitle
        {
            get { _ = Unlocked; var a = Achievements.Get(EquippedTitleId); return a?.Title; }
        }
    }
}
