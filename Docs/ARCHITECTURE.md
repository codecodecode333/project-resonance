# Architecture

## 최소 아키텍처 원칙

1. 게임 규칙은 UI, Sprite, Animation, VFX, Input에 의존하지 않는다.
2. ScriptableObject는 변하지 않는 정적 Definition 데이터에 사용한다.
3. HP, AP, 현재 위치, Status 지속시간 같은 mutable runtime state는 ScriptableObject에 저장하지 않는다.
4. Unit의 상태와 표현을 분리한다.
5. Grid 논리와 Grid 표현을 분리한다.
6. 기존 프로젝트의 거대한 `BattleController` 구조를 다시 만들지 않는다.
7. `LegacyReference`는 읽기 전용으로 유지하고 기존 알고리즘과 규칙만 선별적으로 참고한다.
8. 실제 출시가 목표이므로 필요 이상의 추상화와 계층을 만들지 않는다.

## 상태와 표현의 경계

`UnitState`가 담당할 런타임 상태:

- HP
- AP
- GridPosition
- Status

`UnitView`가 담당할 표현:

- Sprite
- Animation
- VFX

Unit의 공간 상태 변경은 최초 배치/제거를 담당하는 `UnitPlacementService`와 검증된 이동을 담당하는 `UnitMovementService`가 조율한다. 두 서비스는 `UnitState.Position`과 양방향 `GridOccupancy`를 일치시키며 외부 public 위치 setter를 제공하지 않는다.

`Riftchord.Runtime`은 계속 Unity 참조 없이 Domain만 보관한다. 별도 `Riftchord.Presentation`은 Domain을 읽어 좌표를 변환하고 Tilemap에 표현하며, Editor 전용 생성기는 저장된 PNG/Tile/Scene을 만드는 저작 도구다. Tilemap은 게임 규칙의 source of truth가 아니다.

실제 구현 클래스 구조는 Combat Prototype을 진행하며 필요한 범위에서 결정한다.

007 표현 리팩터링은 `GridPresentationMapper`와 Domain을 유지하고, `IsometricBlockGridPresenter`로 Top/Side split을 대체했다. 완성형 블록(윗면 128×64 / 캔버스 128×128 / PPU 128)을 하나의 Terrain Tilemap에 Height+1장 쌓는다. 아래층은 표현용이며 논리 Surface를 늘리지 않는다. 이웃 cliff 조합·게임 규칙 변경 없이 블록 단위 가독성과 제작 단순성을 검증한다.
