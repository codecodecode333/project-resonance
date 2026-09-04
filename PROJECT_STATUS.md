# Project Status

- Updated: 2026-09-05
- Branch: `main`
- Implementation base: `5785a69` (007 block presentation refactor)
- Review base: `main` HEAD (RIFTCHORD rename)

## Project Rename

Project renamed from ProjectResonance to RIFTCHORD.

- C# root namespace: `Riftchord`
- Assemblies renamed to `Riftchord.*`
- Unity productName: `RIFTCHORD`
- GitHub repository: `codecodecode333/riftchord`
- Existing Unity asset GUIDs preserved
- Unity compile errors: 0
- Unity EditMode Tests: 84/84 passed
- Assembly reference errors: 0
- Missing Script: 0

## Current Milestone

`Combat Prototype 007 — Single Block Presentation Refactor` — **Complete**

- 기존 Domain/Mapper/데모 높이 유지. 새 IsometricBlockGridPresenter → TerrainBlockTilemap 하나 + 빈 Overlay
- 완성형 GrassBlock/Variation PNG·Tile 2종: 윗면 128×64 기준, 캔버스 128×128, PPU 128, Point, 무압축, pivot (0.5,0.75). 새 생성 원본·프롬프트·Editor 준비 도구 포함
- Height 0/1/2는 동일 블록 1/2/3층(B안); 10×8, 높이별 53/14/13칸, 시각 블록 120장. 아래층은 논리 Surface가 아님. 좌표 mod 5 변형 적용
- 기존 Top/Side Presenter·Tile·평면 원본 제거. 새 실제 URP 캡처와 동일 맵의 split 비교 캡처를 Docs/Images에 보관
- 저장 Scene 재오픈·Bootstrap 렌더·층 연결·선명도 확인. 대화형 Play/Console 검증은 미실시; 수동 절차는 GRID_SYSTEM 문서에 기록
- Unity EditMode Tests: 84/84 passed (Presentation: 2/2); compile errors: 0, test exit code: 0
- 초기 변형 RGB 알파 오류는 기본형 마스크 공유로 수정; 최종 생성/캡처 exit 0. 기존 라이선스 토큰 갱신 오류 1건/실행은 검증을 차단하지 않음
- Domain diff 0, LegacyReference 8파일 SHA-256 일치. 새 Unit/Input/Combat 시스템 없음
- 시각 평가: 단차·측면은 선명하지만 평지 Cell 경계는 split보다 약하고 채도·반복 무늬가 강함. block 방식의 우위는 아직 미확정
- Git handoff: 구현 commit `5785a69` 및 상태 기록 commit `0a3a798` 완료. RIFTCHORD rename 작업과 함께 `main`에 push.

## Next Milestone (Proposed)

`Combat Prototype 008 — Unit Presentation`

먼저 이 007 리팩터링 commit과 두 캡처를 ChatGPT에서 리뷰해 표현 방향·Cell 경계/채도를 확정한다. 이후 008에서 UnitState 위치를 정적 UnitView로 표시하는 최소 단계 제안. 96×128 Unit은 참고 기준만 있으며 미구현이다.
