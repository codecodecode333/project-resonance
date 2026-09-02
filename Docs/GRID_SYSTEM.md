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

기본 이동 가능 여부는 다음 규칙을 사용한다.

`abs(from.Height - to.Height) <= 1`

Height 차이가 0 또는 1이면 이동 가능하고, 차이가 2이면 이동할 수 없다. 현재 MVP에서 평지 이동과 Height 1 차이 이동은 모두 거리 1이며 추가 Movement Cost는 없다. `GridTraversal`이 인접한 두 Cell의 이 규칙을 판정하고, `ReachabilityFinder`의 BFS가 이 판정을 소비한다.

## Pass-through and Stop-at

`GridTraversal` 판정은 중간 경로로 통과할 수 있는지와 최종 위치로 정지할 수 있는지를 분리한다.

- 빈 Walkable Cell: 통과 가능, 정지 가능
- 아군 Unit 점유 Cell: 통과 가능, 정지 불가
- 적군 Unit 점유 Cell: 통과 불가, 정지 불가
- `UnitRegistry`에서 해석할 수 없는 Entity 점유 Cell: 안전하게 Blocking으로 처리
- Mover 자신이 목적지를 점유한 호출: 유효하지 않은 상태로 보고 통과를 거절

`CanPassThrough` 판정은 Bounds, 4방향 인접, 목적지 Walkable, Height 차이, Occupancy와 Team을 조합한다. `CanStopAt`은 단일 Cell을 판정하므로 Height edge를 검사하지 않고 Bounds, Walkable, 빈 Occupancy만 확인한다.

## Reachability BFS

`ReachabilityFinder.FindReachableCells(mover, maxDistance)`는 배치된 Unit의 현재 위치에서 거리 안에 이동을 끝낼 수 있는 Cell들을 반환한다. 시작 Cell은 제외하고, 거리 0은 빈 결과다. null Mover, 음수 거리, 미배치 또는 해당 Grid 밖에 배치된 Mover는 명시적으로 예외를 발생시킨다. 결과 순서는 API 계약이 아니다.

모든 Traversal Edge Cost는 평지·오르막·내리막 모두 1이다. BFS는 Queue와 최초 방문 거리 Dictionary를 사용하고, 탐색용 Visited와 최종 Reachable 결과를 분리한다.

- 공간 후보는 `GetOrthogonalNeighbors`, 탐색 허용은 `CanPassThrough`, 목적지 포함 여부는 `CanStopAt`으로 결정한다. BFS가 Height/Walkable/Team/Occupancy 규칙을 직접 재구현하지 않는다.
- 아군 Cell은 탐색 가능하지만 목적지가 아니며, 뒤쪽 빈 Cell은 거리 안이면 목적지가 될 수 있다. 적군/Unknown Occupancy는 해당 경로의 탐색을 차단한다.
- 계산은 순수 조회이며 Unit 위치, Terrain, Occupancy, Registry를 변경하지 않는다. ReachabilityFinder는 predecessor를 저장하지 않으며 경로 복원은 별도 `PathFinder`의 책임이다.

## Path Reconstruction

`PathFinder.TryFindPath(mover, target, maxDistance, out path)`는 특정 Target까지 비용 1의 BFS 최단 경로를 계산한다. 최초 발견 시 `cameFrom[next] = current`를 기록하고, Target에서 Start까지 역추적한 뒤 순서를 뒤집는다. ReachabilityFinder를 호출하거나 공용 탐색 엔진으로 합치지 않는다.

- 성공 Path는 실제 밟을 Step 순서이며 **Start 제외 / Target 포함**이다. 동일 길이의 여러 최단 경로 중 선택 순서는 보장하지 않는다.
- `Start == Target`은 `CanStopAt(start)` 검사 없이 빈 Path로 성공한다. 그 외 Target은 `CanStopAt`을 만족해야 하며, 거리 초과·Grid 밖 Target·경로 없음은 `false`와 빈 Path를 반환한다.
- 공간 후보는 `GetOrthogonalNeighbors`, 각 Step은 `CanPassThrough`로 판정한다. Height/Walkable/Team/Occupancy 규칙을 직접 검사하지 않는다. 아군은 중간 Step이 될 수 있지만, 아군·적군·Unknown 점유 Target은 불가하다.
- 모든 Edge Cost는 평지·오르막·내리막 모두 1이며 `maxDistance`는 최대 Step 수다. null Mover, 음수 거리, 미배치 또는 Grid 밖 Mover는 Reachability와 같은 예외 정책을 사용한다.
- Path Query는 Unit/Terrain/Occupancy/Registry 상태를 변경하지 않는다. 반환 Path를 실제 이동으로 적용하는 기능은 다음 단계의 별도 책임이다.

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

## Traversal Composition

`CanPassThrough(from, to)`는 다음 조건을 합성한다.

1. Mover가 유효하고 from과 to가 Grid Bounds 내부인가
2. from과 to가 4방향으로 인접했는가
3. 목적지 Terrain의 `CellState.IsWalkable`이 true인가
4. `abs(from.Height - to.Height) <= 1`인가
5. 목적지가 비었거나 Mover가 아닌 아군 Unit으로 점유되었는가

`GetOrthogonalNeighbors`는 계속 순수 공간 인접 조회만 담당한다.

## Explicit Non-Goals

이번 단계에서는 다음을 구현하지 않는다.

- Tilemap, GridPresenter, World Position 변환, TerrainSide, Overlay
- UnitView, ObstacleState, Dynamic Terrain
- 실제 Movement, A*, Dijkstra, Weighted Movement
- LOS, AP, Skill, Combat, Status, Hazard
- AI, Enemy Intent, Map Generation
- Camera, Input, UI, VFX, Save, Mobile 기능

## Future Extension Points

- Runtime `ObstacleState`
- Dynamic Terrain과 Surface Modifier
- 경로를 적용하는 실제 Movement
- Ghost, Flying, Jump, Teleport 등을 조합할 `TraversalContext` 또는 `MovementCapability`
- Wall, Door, Phaseable Wall 등 Cell 사이를 막는 Edge/Link 단위 규칙
- LOS
- Map Generation
- Isometric Tilemap Presentation
- Height를 왜곡하지 않는 `TraversalLink` 계열 연결: 계단, 점프, 일방통행, 사다리, 특수 이동
