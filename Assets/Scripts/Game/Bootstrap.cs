using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FedAndFound.Game
{
    /// <summary>씬 파일에는 아무것도 두지 않는다 — 카메라·EventSystem·Canvas·GameManager를
    /// 전부 코드로 생성한다(CLAUDE.md: "씬/프리팹 YAML 편집 최소화"). 빈 씬 하나만 있으면 된다.</summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 5;
                cam.backgroundColor = new Color(0.11f, 0.13f, 0.15f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                camGo.AddComponent<AudioListener>();
            }

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var screenRoot = new GameObject("ScreenRoot", typeof(RectTransform));
            screenRoot.transform.SetParent(canvasGo.transform, false);
            UI.UIFactory.Stretch((RectTransform)screenRoot.transform);

            var gmGo = new GameObject("GameManager");
            var gm = gmGo.AddComponent<GameManager>();
            gmGo.AddComponent<ScreenRouter>().Init(gm, (RectTransform)screenRoot.transform);

            // 탑뷰 필드(4단계): 월드 공간이라 Canvas(Overlay) 아래에 그려지고, 맵 HUD의 빈 가운데로 보인다
            Field.FieldView.Create(gm);
            // 사이드뷰 전투 무대(동물 스프라이트·연출): 전투 UI의 빈 가운데로 보인다
            BattleView.BattleStageView.Create(gm);
            // 그 밖의 화면 배경: 지형 풍경 + 걸어가는 동물들
            BattleView.BackdropView.Create(gm);

            // ScreenRoot보다 나중에 만들어야 화면 갱신으로 지워지는 UI 위에 플래시가 겹쳐 보인다.
            UI.EventFeedOverlay.Create(canvasGo.transform, gm);
            UI.Tooltip.Create(canvasGo.transform); // 맨 위: 마우스 올리면 설명
            Audio.SoundManager.Create(gm);          // 코드로 합성한 효과음·배경음 (M 키로 끄기)
        }
    }
}
