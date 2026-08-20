# 현행-목표 차이 분석

## 요약

새 구현은 기존 서버의 기능을 유지하되 배포 단위, 외부 프로토콜, DB, ORM, JSON, 로깅, 문서와 테스트 체계를 교체합니다. 기계적인 코드 이동보다 계약 중심 재작성에 해당합니다.

| 구분 | 현행 | 목표 | 주요 작업/위험 |
|---|---|---|---|
| 서버 구조 | 로그인/게임/Persistence 3 프로젝트 | 프로덕션 1 프로젝트 | 모델 소유권과 DI 통합 |
| API | REST Controller 여러 경로 | `POST /rpc` JSON-RPC | 클라이언트 전체 계약 전환 |
| DB | MySQL | SQLite | SQL 문법, 동시성, 날짜 저장 변환 |
| ORM | EF Core | Dapper | Change Tracker 제거, SQL·트랜잭션 명시 |
| 마이그레이션 | EF migrations 두 소유자 | SQL migrations 한 소유자 | 최종 스키마 기준 새 초기화 |
| JSON | STJ + Newtonsoft 혼용 | System.Text.Json | 카탈로그와 enum 계약 고정 |
| 로깅 | 기본 Logging/HTTP logging | ZLogger 구조화 로그 | 민감정보 필터와 공통 필드 |
| 인증 | JWT + Google | 동일 개념, 통합 Handler | JSON-RPC 오류와 Bearer 문맥 결합 |
| 배치 | 없음 | JSON-RPC batch | 격리, 알림, 제한, SQLite 쓰기 경쟁 |
| 테스트 | 추적된 프로젝트 없음 | 단위/계약/SQLite/동시성 | 재작성의 핵심 안전망 |
| 배포 | 서버별 두 workflow | 단일 publish/deploy | 서비스명, 경로, 롤백 재정의 |

## 1. 프로토콜 전환

현행 HTTP 상태와 익명 Object 오류를 JSON-RPC error code로 변환해야 합니다. HTTP 경로 파라미터는 params 필드가 되고, 인증은 Bearer 헤더를 유지합니다.

| 현행 | 목표 |
|---|---|
| `POST /api/account/login/google` | `auth.login.google` |
| `GET /api/account/test` | `auth.login.development` |
| `POST /api/account/refresh` | `auth.token.refresh` |
| `POST /api/account/logout` | `auth.logout` |
| `GET /api/character/me` | `character.getMine` |
| `GET /api/currency/me/{type}` | `currency.getMine` |
| `GET /api/currency/me` | `currency.listMine` |
| `GET /api/stage/{stageId}` | `stage.get` |
| `POST /api/stage/{stageId}/enter` | `stage.enter` |
| `POST /api/stage/runs/{runId}/complete` | `stage.complete` |
| `POST /api/user-room/upload` | `room.upsertMine` |
| `GET /api/user-room/me` | `room.getMine` |

클라이언트는 HTTP status 기반 분기에서 JSON-RPC `error.code` 또는 `data.key` 기반 분기로 바뀌어야 합니다.

## 2. 데이터 전환

MySQL 특화 항목:

- `INSERT IGNORE`
- `ON DUPLICATE KEY UPDATE`
- `FOR UPDATE`
- `GREATEST`
- MySQL JSON 타입
- EF Core Change Tracker와 `ExecuteUpdateAsync`

SQLite 목표 대체:

- `INSERT ... ON CONFLICT DO NOTHING`
- `INSERT ... ON CONFLICT(...) DO UPDATE`
- write transaction과 조건부 UPDATE
- `CASE` 또는 애플리케이션 검증
- TEXT JSON + `json_valid`
- Dapper `ExecuteAsync`, `Query*Async`와 영향 행 수 검사

기존 EF migration 파일을 변환하지 않고 목표 최종 스키마로 `0001_initial.sql`을 새로 만듭니다.

## 3. 동시성 차이

MySQL의 row-level lock과 `SELECT ... FOR UPDATE`를 SQLite에서 그대로 사용할 수 없습니다.

목표 방식:

- 가능한 규칙은 유일·체크 제약으로 이동
- 토큰 회전과 실행 완료는 상태 조건을 포함한 UPDATE 영향 행 수로 선점
- 진행 중 실행은 부분 유일 인덱스로 보강
- 쓰기 트랜잭션을 짧게 유지
- 배치 요소는 순차 처리
- SQLITE_BUSY 제한적 재시도와 관측

동시성 테스트 없이 전환을 완료 처리하지 않습니다.

## 4. 계약 모양 변경

현행 DTO의 dictionary와 중첩 Entity 모양을 그대로 복사하지 않습니다.

목표 변경:

- 재화 전체 응답은 코드 오름차순 `CurrencyPacket[]`
- 정적 데이터는 `MapPacket`, `StagePacket`, `TrapPacket`으로 계약 고정
- DB Model은 응답에 노출하지 않음
- 모든 업무 실패는 오류 카탈로그의 안정적 code/key 사용
- 날짜는 명시적 UTC ISO 8601

클라이언트가 기존 REST JSON과 호환되어야 한다면 별도 호환 Adapter 요구사항이 필요합니다. 현재 기준선은 신규 JSON-RPC 계약을 우선합니다.

## 5. 보안 개선

| 현행 위험 | 목표 대응 |
|---|---|
| 개발 로그인이 모든 환경에 등록될 수 있음 | Development에서만 Registry 등록 |
| Swagger가 모든 환경에 노출될 수 있음 | JSON-RPC 계약 문서와 테스트 사용, 운영 UI 미노출 |
| HTTP logging 확장 시 토큰 노출 가능 | ZLogger 허용 필드 방식, 헤더·본문 미기록 |
| 최소 시간만으로 스테이지 완료 검증 | 한계를 문서화하고 향후 서버 권위 검증 별도 요구 |
| 비밀값 설정 방식이 README에 없음 | 환경 변수/User Secrets/운영 secret store 문서화 |

## 6. 카탈로그 차이

현행 Loader는 파일 존재와 JSON 파싱 정도만 강하게 검증합니다. 목표 Loader는 다음을 시작 시 검증합니다.

- 파일별 ID와 중복
- width, height 양수
- tile 배열 길이 정책
- 함정 타입, 좌표, 중복, 회전
- 최소 클리어 시간과 보상 범위
- DB가 참조하는 카탈로그 ID 제거 호환성

현재 Map1/Stage1의 tile 배열 길이 정책은 미결정이므로 Phase 4 Loader는 `width × height` 일치를 강제하지 않으며 제품 확인이 필요합니다.

Phase 4의 초기 `Map1.json`, `Stage1.json`은 승인된 RPC 계약 예시와 현행 동작 인벤토리에서 확인된 ID·크기·보상·SawTrap 정보를 기준으로 구성했습니다. 원본 JSON 전체가 현재 저장소에 없으므로 실제 배포 전 타일 내용과 함정 위치·회전을 콘텐츠 원본과 대조해야 합니다.

## 7. 배포 전환

기존 두 GitHub Actions는 목표 서버가 준비될 때까지 유지합니다. 단일 서버 전환 시:

1. 새 workflow에서 restore/build/test/publish를 수행합니다.
2. 배포 직전 SQLite 백업을 실행합니다.
3. 하나의 원격 release 디렉터리와 서비스만 갱신합니다.
4. health와 대표 RPC smoke test 후 성공을 확정합니다.
5. 안정화 후 기존 두 workflow를 제거합니다.

## 8. 데이터 이관 선택지

### 선택 A: 새 SQLite로 시작

- 개발/프로토타입에 가장 단순합니다.
- 기존 사용자와 진행 데이터가 필요 없다는 제품 승인이 필요합니다.

### 선택 B: MySQL 데이터 1회 이전

- users, refresh tokens, characters, currencies, stage runs, rooms를 순서대로 변환합니다.
- 토큰 해시 BLOB, 시간, GUID, JSON, enum 코드를 검증해야 합니다.
- 이관 중 쓰기를 중단하거나 변경분 동기화 계획이 필요합니다.
- 행 수, FK, 합계, 샘플 계약 검증이 필요합니다.

현재 선택은 미결정입니다. 제품 승인 없이 마이그레이션 도구를 구현하거나 운영 데이터를 이동하지 않습니다.
