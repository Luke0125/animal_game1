# 레퍼런스 (수업 규정: 사용한 라이브러리·에셋·AI는 반드시 명시)

| 항목 | 용도 | 출처/라이선스 |
|---|---|---|
| Unity 6 | 게임 엔진 | unity.com |
| Claude Code (Anthropic) | 코드 작성 보조 | claude.com/claude-code |
| Unity Agent Plugin 0.1.6-beta (스킬 일부: `.claude/skills/`) | Claude용 Unity 작업 가이드 | Unity Technologies, Unity Companion License (`.claude/skills/UNITY_PLUGIN_LICENSE.md`) |
| 나눔고딕 (NAVER) | 한글 UI 폰트 | SIL OFL 1.1 (`Assets/Fonts/NANUM_FONT_LICENSE.txt`) |
| 게임 디자인 레퍼런스 | 전투 구조 | Slay the Spire 2, Darkest Dungeon (GDD §1) |
| 동물 스프라이트 13종 · 전투 배경 | 사이드뷰 전투 화면 | 외부 에셋 없음 — `AnimalArt.cs`/`BattleStageView.cs`가 코드로 픽셀을 직접 그림(Claude Code 작성). 화면 배치는 팀이 준 참고 이미지(`docs/UI_REFERENCE.md`)를 따름 |
| 효과음·배경음 | 게임 사운드 전체 | 외부 음원 없음 — `SoundManager.cs`가 코드로 파형을 합성(Claude Code 작성) |
| 맵 섬·절벽·노드 돌판·원석 아이콘 | 맵 화면 | 외부 에셋 없음 — 코드로 그림. 배치는 팀 참고 이미지 1번(`docs/UI_REFERENCE.md`)을 따름 |
| 필드 타일·장식 그래픽 | 탑뷰 맵 지형 4종 | 외부 에셋 없음 — `TerrainTiles.cs`가 코드로 픽셀을 직접 그림(Claude Code 작성) |
