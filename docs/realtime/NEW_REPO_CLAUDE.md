# Fed & Found: Action — Claude 작업 가이드

> 새 레포 루트에 이 파일을 `CLAUDE.md`로 복사한다. 기준 문서는 `docs/PROTOTYPE_SPEC.md`(턴제 레포의 `docs/realtime/PROTOTYPE_SPEC.md` 복사본).
> 이전 턴제판 레포(`Luke0125/animal_game1`)는 **참고용**이다. 필요한 파일만 골라 복사하고, 전체를 읽지 않는다.

## 목적
턴제판과 비교하기 위한 **실시간 탑다운 액션 로그라이크 시험판**. 그래픽은 최소, **조작감·전투 기능은 상용 수준**(명세 3장 합격 기준). 멀티플레이 없음.

## 구조
- `Assets/Scripts/Core/` — 순수 C# 규칙 (asmdef `FedAndFound.Action.Core`, `noEngineReferences: true`). UnityEngine 사용 금지. 수치는 전부 `Balance.cs`.
- `Assets/Scripts/Game/` — MonoBehaviour (asmdef `FedAndFound.Action.Game`, Core + Unity.InputSystem + UnityEngine.UI 참조).
  - `Bootstrap.cs`가 씬 로드 시 카메라·EventSystem·Canvas·매니저를 코드로 생성 (씬 파일은 비워 둔다).
  - Player / Combat / Enemies / Boss / Rooms / CameraRig / Fx / UI / Audio 폴더.
- `Tests/CoreSim/` — Unity 없이 Core 검증: `cd Tests/CoreSim && dotnet run -c Release`. Core 수정 후 반드시 실행.
- 한글 폰트: `Assets/Resources/Fonts/NanumGothic.ttf` (런타임 `Resources.Load`).

## 개발 규칙
- 입력: **Input System 패키지**, 액션은 코드에서 생성.
- 물리: Physics2D, 레이어로 아군/적/아군 투사체/적 투사체/벽 구분. 히트박스(공격)와 허트박스(피격) 분리.
- 모든 적 공격은 예고(0.4초 이상)가 먼저. 스폰 직후 무해 시간.
- 투사체·데미지 숫자·이펙트는 오브젝트 풀링.
- 조작감 수치(가속, 대시, 무적 시간, hitstop, 넉백, 입력 버퍼)는 `Balance`에 이름 붙은 상수로.
- 단계(명세 12장 P1~P5)마다 커밋·푸시하고, `docs/PROTOTYPE_SPEC.md` 표에 상태를 적는다.

## 컨벤션
- C# 9까지(Unity 호환), record/init 금지. 네임스페이스 `FedAndFound.Action.Core` / `FedAndFound.Action.Game`.
- 코드 주석·UI 텍스트는 한국어, 식별자는 영어.
- 외부 에셋·코드를 가져오면 `docs/REFERENCES.md`에 출처를 적는다(수업 규정).
