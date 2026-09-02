# Grid System

이 문서는 Grid, Pathfinding, LOS, Map Generation, Tilemap Presentation 작업의 기준이다.

## Logical Coordinate System

논리 Grid는 2차원이며 `GridPosition(X, Z)`로 Cell을 식별한다.

- `X`: 열
- `Z`: 행
- 좌표 범위: `X = 0..Width-1`, `Z = 0..Depth-1`

`GridPosition`은 Height, World Position, Unity 좌표를 포함하지 않는다.

## Height Model

각 `CellState`가 게임 규칙용 정수 Height를 가진다.

- `0`: Low Ground
- `1`: Mid Ground
- `2`: High Ground

Height는 Unity World Y가 아니다. 화면에서의 높이 간격은 Presentation 계층이 결정한다. 범위를 벗어난 값은 clamp하지 않고 명확하게 거절한다.

## Surface Model

MVP는 **One Surface Per X/Z** 모델을 사용한다. 하나의 `GridPosition`에는 하나의 `CellState`만 존재한다.

같은 X/Z의 다리와 지면, 다층 건물, 지하층, 겹치는 여러 Walkable Surface는 지원하지 않는다.

## Neighbor Model

공간적 인접 Cell은 다음 4방향으로 한정한다.

1. `+X`
2. `-X`
3. `+Z`
4. `-Z`

Diagonal은 지원하지 않는다. 중앙 Cell은 최대 4개, Edge는 최대 3개, Corner는 최대 2개의 인접 Cell을 가진다. 인접 조회는 Height, Walkable, Occupancy를 검사하지 않는다.

## Height Traversal Rule

향후 이동 가능 여부는 다음 규칙을 사용한다.

`abs(from.Height - to.Height) <= 1`

Height 차이가 0 또는 1이면 이동 가능하고, 차이가 2이면 이동할 수 없다. 현재 MVP에서 평지 이동과 Height 1 차이 이동은 모두 거리 1이며 추가 Movement Cost는 없다.

이 규칙은 이번 Minimum Grid Domain에서 구현하지 않는다. Traversable Neighbor 판정은 BFS/Movement 단계의 책임이다.

## Tilemap Mapping

향후 Unity 2D Presentation은 Isometric Z as Y Tilemap을 사용한다.

| Logical value | Tilemap value |
| --- | --- |
| `GridPosition.X` | X |
| `GridPosition.Z` | Y |
| `CellState.Height` | Z |

예를 들어 `GridPosition(3, 5)`, Height `2`는 Presentation에서 `Vector3Int(3, 5, 2)`에 대응한다. 이 변환은 Presentation 규칙이며 Grid Domain에는 포함하지 않는다.

## Tilemap Responsibility

향후 Presentation 구조의 방향은 다음과 같다.

```text
BattleGrid
├─ TerrainTopTilemap
├─ TerrainSideTilemap
├─ OverlayTilemap
├─ Props
├─ Units
└─ VFX
```

- `TerrainTopTilemap`: 지면 윗면
- `TerrainSideTilemap`: Height 차이로 생기는 절벽과 벽면
- `OverlayTilemap`: 이동/공격 범위, 선택 Cell, Enemy Intent, AOE 및 Path Preview
- `Props`, `Units`, `VFX`: 기본적으로 GameObject Presentation

Tilemap은 논리 상태를 표현하며 이동 가능 여부나 게임 규칙을 결정하지 않는다.

## Tile Art Baseline

- 형태: Isometric Diamond
- 종횡비: 2:1
- Prototype source size: 128×64 px
- 기본 PPU: 128

이 값은 Art/Presentation 기준이며 Grid Domain에 하드코딩하지 않는다.

## Source of Truth

`GridState`와 `CellState`는 Terrain/Surface 상태의 Source of Truth다. `GridOccupancy`는 Runtime Entity의 공간 점유 상태를, `UnitState`는 Unit 자체의 Runtime State를 담당한다. `UnitPlacementService`는 Unit 위치와 Occupancy를 함께 갱신해 두 상태를 일치시킨다.

AI, Pathfinding, LOS, Map Generation, Save, Tests는 필요한 Domain 상태를 합성해서 사용한다. Tilemap의 Tile 유무를 조회해 게임 규칙을 판단하지 않으며 Tilemap은 Domain 상태를 읽어 표현한다.

## Terrain State and Runtime Occupancy

`CellState`는 Height와 기본 Walkable 여부 같은 Terrain/Surface 상태만 가진다. `GridOccupancy`는 현재 Grid 공간을 점유하는 Runtime Entity의 별도 Spatial Index다.

Terrain의 `IsWalkable`은 지형 자체가 기본적으로 이동 가능한지를 의미한다. Unit이나 Runtime Obstacle이 Cell을 막더라도 `CellState.IsWalkable`을 변경하지 않는다.

`GridOccupancy`는 `GridPosition`과 일반적인 `EntityId`만 다룬다. Unit, Obstacle, GameObject의 구체 타입은 알지 않는다.

## Runtime Obstacle Identity

향후 게임 규칙 대상인 파괴 가능한 바위, 상자, 얼음벽, HP가 있는 구조물, 일정 시간이 지나면 사라지는 소환물은 `EntityId`와 `GridOccupancy`를 사용할 수 있다. 단순 Decoration에는 `EntityId`가 필요하지 않다.

`GridOccupancy`는 Unit 전용 구조가 아니지만, 현재 단계에서는 `ObstacleState`를 구현하지 않는다.

## Dynamic Walkable Surfaces

Runtime Obstacle과 Walkable Surface 자체를 바꾸는 Terrain Effect는 다르다. 올라갈 수 없는 얼음벽은 Occupancy로 표현할 수 있지만, 솟아오른 대지나 임시 발판처럼 실제 Surface Height를 바꾸는 효과는 Occupancy로 표현하지 않는다.

향후 필요하면 다음 모델을 검토할 수 있다.

`BaseHeight + SurfaceModifier = EffectiveHeight`

현재는 `BaseHeight`, `SurfaceModifier`, `EffectiveHeight`, Terrain Effect System을 구현하지 않으며 `CellState.Height`를 그대로 사용한다.

## Future Traversal Composition

향후 `CanTraverse(from, to)`는 다음 조건을 합성한다.

1. 목적지가 Grid Bounds 내부인가
2. 목적지 Terrain의 `CellState.IsWalkable`이 true인가
3. 목적지가 Blocking Occupancy로 점유되지 않았는가
4. `abs(from.Height - to.Height) <= 1`인가
5. Special Traversal Rule을 만족하는가

`GetOrthogonalNeighbors`는 계속 순수 공간 인접 조회만 담당한다.

## Explicit Non-Goals

이번 단계에서는 다음을 구현하지 않는다.

- Tilemap, GridPresenter, World Position 변환, TerrainSide, Overlay
- UnitView, ObstacleState, Dynamic Terrain
- BFS, A*, Pathfinding, Path Reconstruction, Movement
- LOS, AP, Skill, Combat, Status, Hazard
- AI, Enemy Intent, Map Generation
- Camera, Input, UI, VFX, Save, Mobile 기능

## Future Extension Points

- Runtime `ObstacleState`
- Dynamic Terrain과 Surface Modifier
- Height-aware BFS와 Movement
- LOS
- Map Generation
- Isometric Tilemap Presentation
- Height를 왜곡하지 않는 `TraversalLink` 계열 연결: 계단, 점프, 일방통행, 사다리, 특수 이동
