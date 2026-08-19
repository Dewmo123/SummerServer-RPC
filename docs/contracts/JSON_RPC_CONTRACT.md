# JSON-RPC 2.0 계약

## 1. 기준

외부 RPC 계약은 [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)을 따릅니다. 이 문서는 규격이 정의하지 않는 HTTP 전송 방식과 서버의 구체적인 제한을 추가로 결정합니다.

## 2. HTTP 전송

| 항목 | 결정 |
|---|---|
| 경로 | `POST /rpc` |
| 요청 Content-Type | `application/json` |
| 문자 인코딩 | UTF-8 |
| 일반 응답 | `200 OK`, JSON-RPC Object 또는 Array |
| 알림/전체 알림 배치 | `204 No Content`, 본문 없음 |
| 지원하지 않는 미디어 타입 | `415 Unsupported Media Type`, 전송 계층 오류 |
| 요청 크기 초과 | `413 Content Too Large`, 전송 계층 오류 |
| 메서드 불일치 | `405 Method Not Allowed`, 전송 계층 오류 |
| 전송 계층 속도 제한 | `429 Too Many Requests`, `Retry-After` 포함 가능 |

잘못된 JSON, 잘못된 JSON-RPC Object, 알 수 없는 메서드, 잘못된 params는 HTTP 상태가 아니라 JSON-RPC 오류로 표현하며 HTTP 200을 사용합니다.

`GET /health`는 운영 상태 확인용 HTTP 엔드포인트이며 JSON-RPC 계약에 포함하지 않습니다.

## 3. 요청 Object

```json
{
  "jsonrpc": "2.0",
  "method": "stage.enter",
  "params": {
    "stageId": 1
  },
  "id": "request-42"
}
```

| 멤버 | 필수 | 규칙 |
|---|---|---|
| `jsonrpc` | 예 | String이며 정확히 `2.0` |
| `method` | 예 | 비어 있지 않은 String, 대소문자 구분, `rpc.` 접두사 금지 |
| `params` | 아니요 | 생략하거나 Object 또는 Array |
| `id` | 아니요 | String, Number 또는 Null. Boolean, Object, Array 금지 |

`id` Number의 소수 부분은 규격에서 권장하지 않지만 금지하지 않으므로 서버는 값을 보존해 응답합니다. 내부에서 `double`로 변환하지 않고 JSON 값으로 보존합니다.

- 최상위 값이 Object가 아니거나 `jsonrpc`, `method`, `id` 규칙을 위반하면 `Invalid Request`이며 응답 ID는 null입니다.
- 유효한 Request Object의 `params`가 Object 또는 Array가 아니면 `Invalid params`이며, 일반 요청은 원래 ID를 보존합니다.

## 4. 알림

`id` 멤버가 완전히 생략된 요청만 알림입니다.

```json
{
  "jsonrpc": "2.0",
  "method": "currency.listMine"
}
```

- `"id": null`은 알림이 아니며 `id: null`인 응답을 생성합니다.
- 알림은 일반 요청과 동일하게 메서드를 실행하지만 성공 또는 실패 응답을 만들지 않습니다.
- 클라이언트는 토큰 발급과 같이 결과가 필요한 메서드를 알림으로 호출해서는 안 됩니다.
- 서버는 알림 실패를 구조화 로그와 메트릭에 남기되 클라이언트에 응답하지 않습니다.

## 5. params 바인딩

- 각 업무 메서드는 이름 기반 Object params를 표준으로 사용합니다.
- 서버는 JSON-RPC 규격을 위해 위치 기반 Array도 지원할 수 있지만, 각 위치는 메서드 카탈로그의 필드 순서와 정확히 일치해야 합니다.
- 신규 클라이언트는 반드시 Object params를 사용합니다.
- 이름 기반 필드는 대소문자를 구분하며 알 수 없는 필드는 기본적으로 `Invalid params`입니다.
- 필수 필드 누락, 타입 불일치, 범위 위반은 `-32602 Invalid params`입니다.
- 업무상 존재 여부나 상태 충돌은 `Invalid params`가 아니라 업무 오류입니다.

## 6. 성공 응답

```json
{
  "jsonrpc": "2.0",
  "result": {
    "runId": 17,
    "stage": {}
  },
  "id": "request-42"
}
```

- `jsonrpc`, `result`, `id`가 존재합니다.
- `error`는 존재하지 않습니다.
- `result`가 개념적으로 비어 있어도 `null` 또는 명시적인 Response Object를 포함합니다.
- 요청의 `id` 값과 JSON 타입을 응답에 보존합니다.

## 7. 오류 응답

```json
{
  "jsonrpc": "2.0",
  "error": {
    "code": 1401,
    "message": "존재하지 않는 스테이지입니다.",
    "data": {
      "key": "STAGE_NOT_FOUND",
      "traceId": "00-abcd"
    }
  },
  "id": "request-42"
}
```

- `error`는 `code`, `message`, 선택적인 `data`를 갖습니다.
- `result`는 존재하지 않습니다.
- 요청 ID를 확인할 수 없는 Parse error와 Invalid Request는 `id: null`입니다.
- `data.key`는 클라이언트 분기용 안정적인 문자열입니다.
- `traceId`는 운영 추적용이며 토큰, SQL, 스택 추적을 포함하지 않습니다.
- 표준 오류의 영문 message는 JSON-RPC 스펙 문구를 사용합니다.

상세 코드는 [오류 카탈로그](ERROR_CATALOG.md)를 따릅니다.

## 8. 배치

```json
[
  { "jsonrpc": "2.0", "method": "character.getMine", "id": 1 },
  { "jsonrpc": "2.0", "method": "currency.listMine", "id": 2 }
]
```

- Array는 하나 이상의 요소를 가져야 합니다.
- 최대 요소 수 기본값은 50이며 설정으로 낮출 수 있습니다.
- 최대 요소 수를 초과한 배치는 Array가 아닌 단일 `Invalid Request` 오류 Object를 반환합니다.
- 초기 구현은 SQLite 쓰기 경쟁을 피하기 위해 요청 순서대로 처리합니다.
- 응답 순서에 의존해서는 안 되며 클라이언트는 `id`로 대응시킵니다.
- 알림은 응답 Array에서 제외합니다.
- 잘못된 요소는 해당 요소의 `Invalid Request` 응답을 생성합니다.
- 빈 Array는 Array가 아닌 단일 `Invalid Request` 오류 Object를 반환합니다.
- 모든 요소가 알림이면 204와 빈 본문을 반환합니다.
- 한 요소의 실패가 다른 요소를 롤백하지 않습니다. 배치 전체 트랜잭션은 제공하지 않습니다.
- 동일 HTTP 요청 안의 모든 메서드는 같은 Authorization 헤더와 호출자 문맥을 공유합니다.

## 9. 직렬화 규칙

| 대상 | 형식 |
|---|---|
| JSON 속성 | lower camelCase |
| 시간 | UTC ISO 8601 문자열, 예: `2026-08-19T09:30:00Z` |
| 식별자 | JSON Number 또는 String. 메서드별 계약 참조 |
| 열거형 | 계약에 정의된 정수 코드 |
| 64비트 재화·경험치 | JSON Number |
| null | 계약에서 명시적으로 허용한 필드만 사용 |
| 알 수 없는 필드 | params에서는 거부, 응답에서는 클라이언트가 무시 가능 |

정적 카탈로그와 RPC 계약 모두 `System.Text.Json`을 사용합니다.

## 10. 인증

- 보호된 메서드는 HTTP `Authorization: Bearer <access-token>` 헤더를 사용합니다.
- 배치 요소별로 다른 사용자를 지정할 수 없습니다.
- 토큰이 없거나 유효하지 않은 보호 메서드는 JSON-RPC `AUTH_UNAUTHENTICATED` 오류입니다.
- 인증이 필요 없는 메서드는 `auth.login.google`, `auth.login.development`, `auth.token.refresh`, `auth.logout`, `stage.get`입니다.
- `auth.login.development`는 Development 환경에서만 등록합니다.

## 11. 요청 제한

- 전체 HTTP 본문 기본 최대 크기: 64 KiB
- 배치 기본 최대 요소 수: 50
- JSON 최대 깊이 기본값: 32
- 로그인·토큰 메서드: IP별 분당 10회 기본값
- 일반 메서드: 인증 사용자 또는 원격 IP별 초당 120회 기본값

제한값은 운영 설정으로 낮출 수 있으며 높일 때 부하 테스트와 ADR이 필요합니다.

JSON 최대 깊이를 초과해 문서 파싱을 완료할 수 없으면 `Parse error`를 반환합니다.
