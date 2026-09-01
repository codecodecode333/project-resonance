# Game Design

## 게임 방향

- 장르: Mobile Tactical Roguelike
- 전투: Grid 기반 턴제 전투
- 캐릭터: 서브컬쳐 캐릭터 중심

## 핵심 재미

### 1. Enemy Intent

적의 다음 행동을 플레이어가 미리 확인하고 대응한다.

### 2. Position / Height

위치, 사거리, LOS, 고도를 활용해 적의 다음 행동에 대응한다.

### 3. Run Build

전투 후 선택하는 강화에 따라 Run마다 캐릭터의 플레이 방식이 달라진다.

## 전투 기본 구상

- 약 8×10 크기의 Grid
- Height 0~2
- Player 약 3 Units
- Enemy 약 4~6 Units
- 짧은 Encounter
- AP 기반 행동
- Skill 기반 전투

## 로그라이크 기본 구조

Battle → Reward 선택 → Route 선택 → Battle / Event / Shop / Elite → Boss

현재는 전체 Run을 구현하지 않는다. 최우선 목표는 **전투 → Upgrade 선택 → 다음 전투** 흐름이 재미있는지 검증하는 것이다.
