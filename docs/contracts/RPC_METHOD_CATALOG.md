# RPC 메서드 카탈로그

## 1. 공통 규칙

- 모든 메서드명과 params 필드명은 대소문자를 구분합니다.
- 아래 예시는 JSON-RPC 봉투의 `params`와 `result` 부분만 표시합니다.
- C# 외부 계약 타입은 메서드별 `...Request`, `...Response`를 사용합니다.
- 중첩 외부 계약 타입은 `...Packet`을 사용합니다.
- 인증 `필수` 메서드는 HTTP Bearer JWT가 필요합니다.

## 2. 요약

| 메서드 | 인증 | Request | Response | 요구사항 |
|---|---|---|---|---|
| `auth.login.google` | 없음 | `GoogleLoginRequest` | `GoogleLoginResponse` | FR-AUTH-001 |
| `auth.login.development` | 없음/개발 전용 | `DevelopmentLoginRequest` | `DevelopmentLoginResponse` | FR-AUTH-002 |
| `auth.token.refresh` | 없음 | `RefreshTokenRequest` | `RefreshTokenResponse` | FR-AUTH-004 |
| `auth.logout` | 없음 | `LogoutRequest` | `LogoutResponse` | FR-AUTH-005 |
| `character.getMine` | 필수 | `GetMyCharacterRequest` | `GetMyCharacterResponse` | FR-CHAR-001 |
| `currency.getMine` | 필수 | `GetMyCurrencyRequest` | `GetMyCurrencyResponse` | FR-CURRENCY-001 |
| `currency.listMine` | 필수 | `ListMyCurrenciesRequest` | `ListMyCurrenciesResponse` | FR-CURRENCY-002 |
| `stage.get` | 없음 | `GetStageRequest` | `GetStageResponse` | FR-STAGE-001 |
| `stage.enter` | 필수 | `EnterStageRequest` | `EnterStageResponse` | FR-STAGE-002 |
| `stage.complete` | 필수 | `CompleteStageRequest` | `CompleteStageResponse` | FR-STAGE-003 |
| `room.upsertMine` | 필수 | `UpsertMyRoomRequest` | `UpsertMyRoomResponse` | FR-ROOM-001 |
| `room.getMine` | 필수 | `GetMyRoomRequest` | `GetMyRoomResponse` | FR-ROOM-002 |

## 3. 공통 Packet

### TokenPairPacket

```json
{
  "accessToken": "eyJ...",
  "accessTokenExpiresAt": "2026-08-19T10:30:00Z",
  "refreshToken": "random-base64url",
  "refreshTokenExpiresAt": "2026-09-18T09:30:00Z"
}
```

### CharacterPacket

```json
{
  "level": 1,
  "exp": 10,
  "expToNextLevel": 100
}
```

### CurrencyPacket

```json
{
  "type": 1,
  "amount": 100
}
```

재화 코드: Gold=1, Gem=2, StageTicket=3, EventToken=4.

### PositionPacket / RotationPacket / TrapPacket

```json
{
  "type": 0,
  "position": { "x": 3, "y": 0, "z": 0 },
  "rotation": { "x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0 }
}
```

함정 코드: SawTrap=0.

### MapPacket

```json
{
  "mapId": 1,
  "width": 16,
  "height": 8,
  "tiles": [true, false]
}
```

### StagePacket

```json
{
  "stageId": 1,
  "width": 16,
  "height": 8,
  "tiles": [true, false],
  "traps": [],
  "minimumClearSeconds": 1,
  "rewardExp": 10,
  "rewardGold": 100
}
```

## 4. 인증 메서드

### auth.login.google

params 필드 순서: `idToken`

```json
{ "idToken": "google-id-token" }
```

result:

```json
{
  "userId": 1,
  "username": "google_ab12cd34",
  "tokens": {
    "accessToken": "eyJ...",
    "accessTokenExpiresAt": "2026-08-19T10:30:00Z",
    "refreshToken": "random-base64url",
    "refreshTokenExpiresAt": "2026-09-18T09:30:00Z"
  }
}
```

오류: `AUTH_INVALID_GOOGLE_TOKEN`, `RPC_INVALID_PARAMS`, `RPC_INTERNAL_ERROR`.

### auth.login.development

params는 생략하거나 빈 Object입니다.

result는 `auth.login.google`과 동일한 모양입니다.

오류: `AUTH_DEVELOPMENT_USER_NOT_FOUND`, `RPC_METHOD_NOT_FOUND`(비개발 환경).

### auth.token.refresh

params 필드 순서: `refreshToken`

```json
{ "refreshToken": "current-refresh-token" }
```

result:

```json
{
  "tokens": {
    "accessToken": "eyJ...",
    "accessTokenExpiresAt": "2026-08-19T10:30:00Z",
    "refreshToken": "next-refresh-token",
    "refreshTokenExpiresAt": "2026-09-18T09:30:00Z"
  }
}
```

오류: `AUTH_INVALID_REFRESH_TOKEN`, `AUTH_REFRESH_TOKEN_REUSED`, `RPC_INVALID_PARAMS`.

### auth.logout

params 필드 순서: `refreshToken`

```json
{ "refreshToken": "refresh-token" }
```

result:

```json
{ "completed": true }
```

토큰 존재 여부와 관계없이 같은 성공 결과를 반환합니다.

## 5. 캐릭터와 재화 메서드

### character.getMine

params는 생략하거나 빈 Object입니다.

result:

```json
{
  "character": {
    "level": 1,
    "exp": 0,
    "expToNextLevel": 100
  }
}
```

오류: `AUTH_UNAUTHENTICATED`, `USER_NOT_FOUND`, `CHARACTER_NOT_FOUND`.

### currency.getMine

params 필드 순서: `type`

```json
{ "type": 1 }
```

result:

```json
{ "currency": { "type": 1, "amount": 0 } }
```

오류: `AUTH_UNAUTHENTICATED`, `USER_NOT_FOUND`, `CURRENCY_INVALID_TYPE`.

### currency.listMine

params는 생략하거나 빈 Object입니다.

result:

```json
{
  "currencies": [
    { "type": 1, "amount": 0 },
    { "type": 2, "amount": 0 },
    { "type": 3, "amount": 0 },
    { "type": 4, "amount": 0 }
  ]
}
```

배열은 재화 코드 오름차순으로 정렬합니다.

오류: `AUTH_UNAUTHENTICATED`, `USER_NOT_FOUND`.

## 6. 스테이지 메서드

### stage.get

params 필드 순서: `stageId`

```json
{ "stageId": 1 }
```

result:

```json
{ "stage": { "stageId": 1, "width": 16, "height": 8, "tiles": [], "traps": [], "minimumClearSeconds": 1, "rewardExp": 10, "rewardGold": 100 } }
```

오류: `STAGE_NOT_FOUND`, `RPC_INVALID_PARAMS`.

### stage.enter

params 필드 순서: `stageId`

```json
{ "stageId": 1 }
```

result:

```json
{
  "runId": 17,
  "stage": { "stageId": 1, "width": 16, "height": 8, "tiles": [], "traps": [], "minimumClearSeconds": 1, "rewardExp": 10, "rewardGold": 100 }
}
```

오류: `AUTH_UNAUTHENTICATED`, `USER_NOT_FOUND`, `STAGE_NOT_FOUND`.

### stage.complete

params 필드 순서: `runId`

```json
{ "runId": 17 }
```

result:

```json
{
  "stageId": 1,
  "expGained": 10,
  "character": { "level": 1, "exp": 10, "expToNextLevel": 100 },
  "gainedCurrencies": [{ "type": 1, "amount": 100 }],
  "allCurrencies": [
    { "type": 1, "amount": 100 },
    { "type": 2, "amount": 0 },
    { "type": 3, "amount": 0 },
    { "type": 4, "amount": 0 }
  ]
}
```

오류: `AUTH_UNAUTHENTICATED`, `STAGE_RUN_NOT_FOUND`, `STAGE_RUN_FORBIDDEN`, `STAGE_RUN_ALREADY_COMPLETED`, `STAGE_CLEAR_TOO_EARLY`, `STAGE_NOT_FOUND`, `STAGE_REWARD_FAILED`.

## 7. 사용자 방 메서드

### room.upsertMine

params 필드 순서: `mapId`, `traps`

```json
{
  "mapId": 1,
  "traps": [
    {
      "type": 0,
      "position": { "x": 3, "y": 0, "z": 0 },
      "rotation": { "x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0 }
    }
  ]
}
```

result:

```json
{
  "room": {
    "userId": 1,
    "map": { "mapId": 1, "width": 16, "height": 8, "tiles": [] },
    "traps": []
  }
}
```

오류: `AUTH_UNAUTHENTICATED`, `USER_NOT_FOUND`, `MAP_NOT_FOUND`, `TRAP_TYPE_UNSUPPORTED`, `TRAP_OUT_OF_BOUNDS`, `TRAP_POSITION_DUPLICATED`, `TRAP_ROTATION_INVALID`, `RPC_INVALID_PARAMS`.

### room.getMine

params는 생략하거나 빈 Object입니다.

result는 `room.upsertMine`의 result와 동일한 `RoomPacket` 구조입니다.

오류: `AUTH_UNAUTHENTICATED`, `ROOM_NOT_FOUND`, `ROOM_MAP_INVALID`.

## 8. 계약 변경 규칙

- 메서드명, 필드명, 필드 타입, 열거 코드, 오류 코드는 공개 계약입니다.
- 기존 필드 삭제나 의미 변경은 호환성 검토와 ADR 없이는 금지합니다.
- 선택 필드 추가는 클라이언트가 알 수 없는 응답 필드를 무시한다는 전제에서 허용할 수 있습니다.
- Request에 새 필수 필드를 추가하는 변경은 호환되지 않습니다.
- 모든 계약 변경은 JSON 예제와 계약 테스트를 동시에 갱신합니다.
