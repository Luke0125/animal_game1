# Fed & Found — Claude 작업 가이드

> **토큰 절약 규칙**: `docs/game-design-document.md`(30KB)는 통째로 읽지 말 것. 필요한 절(§)만 `grep -n "## 9" docs/game-design-document.md` 식으로 찾아서 부분만 읽는다. 이 파일과 `docs/ROADMAP.md`가 요약본이다.

## 구조
- `Assets/Scripts/Core/` — **순수 C# 게임 규칙** (asmdef `FedAndFound.Core`, `noEngineReferences: true`). UnityEngine 사용 금지.
  - `Balance.cs` 모든 수치(제안값). 밸런스 조정은 여기서만.
  - `Data/Species.cs` 12종+사슴 스탯/스킬, `Data/Relics.cs` 유물 18종 정의, `Data/Synergies.cs` 원석→시너지 3종(무리사냥/생명의 순환/적응)
  - `Battle/Battle.cs` 전투 엔진: `new Battle(allies, enemies, ctx)` → `CurrentActor`(아군) → `Submit(BattleAction)` → `DrainEvents()`로 연출 재생. 적 턴은 자동 진행. 유지형 스킬은 `SetSustain`(행동 소모 X)
  - `Run/RunState.cs` 런 상태머신: `Phase`(RelicEquip→Map→Battle→PostBattle/Event→…→Farewell→다음 스테이지) 보고 화면 전환
- `Assets/Scripts/Game/` — MonoBehaviour/UI (asmdef `FedAndFound.Game`, Core+UnityEngine.UI 참조). Core를 **호출만** 하고 규칙을 다시 구현하지 않는다.
  - `Bootstrap.cs` 씬 로드 시 카메라·EventSystem·Canvas·GameManager를 코드로 생성(씬 파일은 완전히 비워둠)
  - `GameManager.cs` RunState 보유 + 상태 변경 메서드(전부 끝에 `Refresh()` 호출) + `OnChanged` 이벤트
  - `ScreenRouter.cs` `OnChanged`마다 화면 전체를 지우고 `Run.Phase`에 맞는 `UI/Screens/*` 화면을 다시 그림
  - `UI/UIFactory.cs` 코드로 uGUI 조립(Panel/Label/Button/Bar/VGroup/HGroup/ScrollList). 새 화면도 이것만 쓴다.
  - `UI/Screens/*` 화면 8개: 종 선택/유물 장착/맵/전투/전투 후/이벤트/작별/결과
  - `Field/*` 탑뷰 필드(4단계): `FieldView`(월드 Tilemap+파티 말, Map 단계에서만 표시, 걸어서 노드 진입), `FieldLayout`(칸 좌표·길·BFS), `TerrainTiles`(지형 4종 타일을 코드로 생성). 맵 화면 HUD는 가운데를 비워 필드가 보이게 한다.
- `.claude/skills/` — Unity 공식 플러그인에서 이 프로젝트에 필요한 스킬 5개만 발췌(uGUI, unity-cli, 픽셀퍼펙트, 타일맵, TMP 한글폰트). 한글 폰트: `Assets/Resources/Fonts/NanumGothic.ttf`(런타임 Resources.Load용).
- `docs/UI_REFERENCE.md` — 참고 화면 요약. `docs/REFERENCES.md` — 사용 에셋/AI 출처(추가 시 갱신 필수).
- `Tests/CoreSim/` — Unity 없이 검증: `cd Tests/CoreSim && dotnet run -c Release` (규칙 테스트 + 봇 3종 각 2000판 자동 플레이: 사냥/정화 성향·무작위, `Bots.cs`). Core 수정 후 반드시 실행. 밸런스 목표·현황은 ROADMAP 6단계.

## 규칙 요약 (GDD v0.7)
- 파티 3마리(+0스테이지 손님 사슴). 스테이지 0→1→2→3, 클리어마다 1마리 포식(배고픔 회복)/집 보내기(유물 슬롯+1), 유물 2개 획득, 전원 HP 완전 회복·부활. 3스테이지 클리어 = 게임 클리어.
- 노드: 보스 전 3회 이동, 이벤트 최대 1회. 이동마다 배고픔 감소. 잡몹 노드당 1~3마리.
- 전투: 속도 순위(1=가장 빠름) 순, 동률은 적 우선, 선제 판정은 전체 유닛 기준. 행동 = 공격/방어/정화/스킬. 전원 기절 = 게임 오버.
- 정화 확률 = 저주 게이지(적 HP 손실 비례) + 정화 효율. 물리치기→고기, 정화→열매, 잡몹은 무작위 식성 조각, 보스 정화→만능 조각. 조각 3 = 원석 → 식성별 시너지 패시브 해금(중첩 없음, §20-8 확정).
- 적도 종별 스킬 사용(확률). 이벤트 7종(`Run/RandomEvent.cs`).
- 배고픔 0 → 능력치 50%. 음식은 배고픔만, HP는 [휴식]으로만.

## 컨벤션
- C# 9까지(Unity 호환), record/init 금지. 네임스페이스 `FedAndFound.Core` / `FedAndFound.Game`.
- 코드 주석·UI 텍스트는 한국어, 식별자는 영어. GDD 근거는 `// §7` 형태로 표기.
- 미정 사항(GDD §20)은 `Balance` 상수 또는 `TODO(§20-n)`으로 남긴다.
