using FedAndFound.Core;
using FedAndFound.Game.UI.Screens;
using UnityEngine;

namespace FedAndFound.Game
{
    /// <summary>GameManager.OnChanged가 울릴 때마다 화면을 통째로 지우고 현재 Phase에 맞는
    /// 화면을 새로 그린다. 상태가 바뀔 때마다 매번 다시 그리므로 화면쪼가리들이 상태를 들고 있지 않아도 된다.</summary>
    public sealed class ScreenRouter : MonoBehaviour
    {
        RectTransform _root;
        GameManager _gm;

        public void Init(GameManager gm, RectTransform root)
        {
            _gm = gm; _root = root;
            gm.OnChanged += Render;
            Render();
        }

        void Render()
        {
            for (int i = _root.childCount - 1; i >= 0; i--)
                Destroy(_root.GetChild(i).gameObject);

            if (_gm.Run == null) { SpeciesSelectScreen.Build(_root, _gm); return; }

            switch (_gm.Run.Phase)
            {
                case RunPhase.RelicEquip: RelicEquipScreen.Build(_root, _gm); break;
                case RunPhase.Map: MapScreen.Build(_root, _gm); break;
                case RunPhase.Battle: BattleScreen.Build(_root, _gm); break;
                case RunPhase.PostBattle: PostBattleScreen.Build(_root, _gm); break;
                case RunPhase.Event: EventScreen.Build(_root, _gm); break;
                case RunPhase.Farewell: FarewellScreen.Build(_root, _gm); break;
                case RunPhase.Victory: ResultScreen.Build(_root, _gm, true); break;
                case RunPhase.GameOver: ResultScreen.Build(_root, _gm, false); break;
            }
        }

        void OnDestroy() { if (_gm != null) _gm.OnChanged -= Render; }
    }
}
