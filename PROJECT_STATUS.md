# Project Status

- Updated: 2026-09-03
- Branch: `main`
- Implementation base: `13599c1` (already pushed)
- Review base: `f71d5c5` (Combat Prototype 006 implementation)

## Current Milestone

`Combat Prototype 006 — Unit Movement Domain` — **Complete**

- `UnitMovementService`: Unit/Occupancy 양방향 일관성 확인 → PathFinder 검증 → 최종 재배치 → internal Unit 위치 갱신
- `GridOccupancy.TryRelocate`와 `UnitState.MoveFromTo(expectedFrom, target)` 추가; public Position setter 없음
- 중간 Cell 점유 없이 Start → Target 한 번 적용, 아군 점유 보존, 제자리 빈 경로 no-op, 실패 시 전체 상태 보존
- Path는 이동 완료 후 반환하는 향후 Presentation route; ReachabilityFinder/PathFinder/UnitPlacementService 및 LegacyReference 미변경
- Unity EditMode Tests: 82/82 passed (Movement: 19/19, ally regression passed)
- Unity compile errors: 0; test exit code: 0. 라이선스 토큰 갱신 오류 로그 1건은 테스트를 차단하지 않음
- Git handoff: `f71d5c5` pushed to `origin/main` on 2026-09-03; this follow-up updates only the handoff record

## Next Milestone (Proposed)

`Combat Prototype 007 — Isometric Grid Presentation`

현재 구현 commit을 ChatGPT에서 리뷰한 뒤 설계 확인 후 진행. 이번 작업에서는 Presentation/Animation/Input, AP/Turn, Hazard/OnEnter/Interruption 및 관련 프레임워크를 구현하지 않음.
