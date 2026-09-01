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

`GridState`가 Grid 게임 상태의 Source of Truth다. AI, Pathfinding, LOS, Map Generation, Save, Tests는 `GridState`와 `CellState`를 기준으로 동작한다.

Tilemap의 Tile 유무를 조회해 게임 규칙을 판단하지 않는다. Tilemap은 `GridState`를 읽어 표현한다.

## Explicit Non-Goals

이번 단계에서는 다음을 구현하지 않는다.

- Tilemap, GridPresenter, World Position 변환, TerrainSide, Overlay
- Occupancy, UnitState, UnitView
- BFS, A*, Pathfinding, Path Reconstruction, Movement
- LOS, AP, Skill, Combat, Status, Hazard
- AI, Enemy Intent, Map Generation
- Camera, Input, UI, VFX, Save, Mobile 기능

## Future Extension Points

- `UnitState`와 Occupancy
- Height-aware BFS와 Movement
- LOS
- Map Generation
- Isometric Tilemap Presentation
- Height를 왜곡하지 않는 `TraversalLink` 계열 연결: 계단, 점프, 일방통행, 사다리, 특수 이동
