# 오류 카탈로그

## 1. 오류 Object

```json
{
  "code": 1401,
  "message": "존재하지 않는 스테이지입니다.",
  "data": {
    "key": "STAGE_NOT_FOUND",
    "traceId": "00-abcd"
  }
}
```

클라이언트는 `message`가 아니라 `code` 또는 `data.key`로 분기합니다. `message`는 사람이 읽는 짧은 설명이며 문구가 개선될 수 있습니다.

## 2. JSON-RPC 표준 오류

| 코드 | key | message | 조건 |
|---:|---|---|---|
| -32700 | `RPC_PARSE_ERROR` | `Parse error` | JSON 자체를 파싱할 수 없음 |
| -32600 | `RPC_INVALID_REQUEST` | `Invalid Request` | JSON-RPC Request Object가 아님 |
| -32601 | `RPC_METHOD_NOT_FOUND` | `Method not found` | 메서드가 등록되지 않음 |
| -32602 | `RPC_INVALID_PARAMS` | `Invalid params` | params 구조, 타입, 필수값, 범위 오류 |
| -32603 | `RPC_INTERNAL_ERROR` | `Internal error` | 처리되지 않은 내부 오류 |

## 3. 서버 공통 오류

`-32000`부터 `-32099`까지는 구현 정의 서버 오류로 사용합니다.

| 코드 | key | 기본 message | 조건 |
|---:|---|---|---|
| -32001 | `AUTH_UNAUTHENTICATED` | 인증이 필요합니다. | 보호된 메서드에 유효한 호출자가 없음 |
| -32003 | `AUTH_FORBIDDEN` | 이 작업을 수행할 권한이 없습니다. | 인증되었지만 대상 소유권 또는 권한이 없음 |
| -32009 | `RPC_CONFLICT` | 요청이 현재 상태와 충돌합니다. | 별도 업무 코드가 없는 동시성 충돌 |
| -32010 | `DATABASE_UNAVAILABLE` | 데이터 저장소를 사용할 수 없습니다. | SQLite 연결 또는 마이그레이션 문제 |
| -32029 | `RPC_RATE_LIMITED` | 요청이 너무 많습니다. | RPC 디스패치 이후의 메서드 제한 초과 |

HTTP 미디어 타입, 본문 크기, 메서드, 디스패치 전 속도 제한 실패는 JSON-RPC 오류가 아니라 415, 413, 405, 429 전송 계층 응답입니다.

## 4. 인증 업무 오류

| 코드 | key | 기본 message | 조건 |
|---:|---|---|---|
| 1001 | `AUTH_INVALID_GOOGLE_TOKEN` | Google 인증 정보가 유효하지 않습니다. | ID 토큰 검증 실패 |
| 1002 | `AUTH_INVALID_REFRESH_TOKEN` | 리프레시 토큰이 유효하지 않거나 만료되었습니다. | 해시 없음, 만료, 폐기 |
| 1003 | `AUTH_REFRESH_TOKEN_REUSED` | 토큰 재사용이 감지되어 세션을 폐기했습니다. | 사용된 토큰 재제출 또는 회전 경쟁 패배 |
| 1004 | `AUTH_DEVELOPMENT_USER_NOT_FOUND` | 개발 사용자를 찾을 수 없습니다. | 개발 로그인 계정 미존재 |

## 5. 사용자와 캐릭터 오류

| 코드 | key | 기본 message | 조건 |
|---:|---|---|---|
| 1101 | `USER_NOT_FOUND` | 사용자를 찾을 수 없습니다. | 인증 ID에 해당하는 사용자 미존재 |
| 1201 | `CHARACTER_NOT_FOUND` | 캐릭터를 찾을 수 없습니다. | 사용자 존재 확인 후에도 캐릭터 초기화 실패 |
| 1202 | `CHARACTER_INVALID_EXPERIENCE` | 지급 경험치가 유효하지 않습니다. | 0 이하 또는 범위 위반 경험치 |

## 6. 재화 오류

| 코드 | key | 기본 message | 조건 |
|---:|---|---|---|
| 1301 | `CURRENCY_INVALID_TYPE` | 지원하지 않는 재화 종류입니다. | 정의되지 않은 재화 코드 |
| 1302 | `CURRENCY_INSUFFICIENT` | 재화가 부족합니다. | 차감할 잔액 부족 |
| 1303 | `CURRENCY_INVALID_AMOUNT` | 재화 변경량이 유효하지 않습니다. | 0 이하 변경량 |
| 1304 | `CURRENCY_OVERFLOW` | 재화 한도를 초과합니다. | 64비트 정수 범위 초과 |

## 7. 스테이지 오류

| 코드 | key | 기본 message | 조건 |
|---:|---|---|---|
| 1401 | `STAGE_NOT_FOUND` | 존재하지 않는 스테이지입니다. | 카탈로그에 스테이지 없음 |
| 1402 | `STAGE_RUN_NOT_FOUND` | 스테이지 실행 기록을 찾을 수 없습니다. | 실행 ID 없음 |
| 1403 | `STAGE_RUN_FORBIDDEN` | 다른 사용자의 실행을 완료할 수 없습니다. | 실행 소유자 불일치 |
| 1404 | `STAGE_RUN_ALREADY_COMPLETED` | 이미 처리된 스테이지 실행입니다. | 상태가 InProgress가 아니거나 선점 실패 |
| 1405 | `STAGE_CLEAR_TOO_EARLY` | 최소 클리어 시간이 지나지 않았습니다. | 서버 시간 기준 최소 시간 미충족 |
| 1406 | `STAGE_REWARD_FAILED` | 보상 지급에 실패했습니다. | 재화·경험치·결과 조회 실패 및 롤백 |

## 8. 사용자 방 오류

| 코드 | key | 기본 message | 조건 |
|---:|---|---|---|
| 1501 | `MAP_NOT_FOUND` | 존재하지 않는 맵입니다. | 카탈로그에 맵 없음 |
| 1502 | `ROOM_NOT_FOUND` | 저장된 방이 없습니다. | 사용자 방 미존재 |
| 1503 | `ROOM_MAP_INVALID` | 저장된 방의 맵 정보가 유효하지 않습니다. | DB와 카탈로그 간 무결성 오류 |
| 1504 | `TRAP_TYPE_UNSUPPORTED` | 지원하지 않는 함정 종류입니다. | 정의되지 않은 함정 코드 |
| 1505 | `TRAP_OUT_OF_BOUNDS` | 함정 좌표가 맵 범위를 벗어났습니다. | x, y, z 경계 위반 |
| 1506 | `TRAP_POSITION_DUPLICATED` | 같은 위치에 함정을 중복 배치할 수 없습니다. | 중복 좌표 |
| 1507 | `TRAP_ROTATION_INVALID` | 함정 회전값이 정규화되어 있지 않습니다. | quaternion 크기 허용 범위 위반 |

## 9. 예외 매핑 규칙

- 입력 JSON 및 바인딩 오류는 `-32600` 또는 `-32602`로 변환합니다.
- 예상 가능한 업무 실패는 위의 양수 코드를 사용합니다.
- 소유권 불일치는 정보 노출 가능성을 검토한 뒤 `-32003` 또는 구체 업무 오류를 사용합니다.
- SQLite busy/locked가 재시도 후 지속되면 `DATABASE_UNAVAILABLE`입니다.
- 취소 토큰 취소는 서버 오류로 기록하지 않으며 연결 종료 상황에 맞게 처리를 중단합니다.
- 예상하지 못한 예외는 `-32603`으로 변환하고 외부에 스택 추적을 노출하지 않습니다.
- 알림 처리 중 오류는 동일하게 분류하지만 응답은 생성하지 않습니다.
