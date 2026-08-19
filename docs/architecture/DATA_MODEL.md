# SQLite 데이터 모델

## 1. 저장 원칙

- 하나의 SQLite DB 파일을 사용합니다.
- 모든 연결에서 외래 키를 활성화합니다.
- SQL은 Dapper 매개변수로 실행합니다.
- 정적 맵과 스테이지 정의는 DB가 아니라 `Content/Catalogs` JSON 파일에 저장합니다.
- 시간은 UTC Unix milliseconds를 SQLite `INTEGER`로 저장하고 외부에서는 ISO 8601로 변환합니다.
- 내부 숫자 ID는 C# `long`, SQLite `INTEGER`를 사용합니다.
- GUID는 정규화한 소문자 문자열 `TEXT`, 토큰 해시는 32바이트 `BLOB`으로 저장합니다.

## 2. 관계 개요

```text
users 1 ─── 0..1 characters
  │
  ├──── 0..* currencies
  ├──── 0..* refresh_tokens
  ├──── 0..* stage_runs
  └──── 0..1 user_rooms

maps, stages, traps ── 정적 JSON 카탈로그
```

## 3. schema_migrations

| 컬럼 | 타입 | 규칙 |
|---|---|---|
| `version` | INTEGER | PK, 증가하는 마이그레이션 번호 |
| `name` | TEXT | NOT NULL |
| `checksum` | TEXT | NOT NULL, 적용한 SQL SHA-256 |
| `applied_at_utc_ms` | INTEGER | NOT NULL |

서버 시작 시 적용된 버전의 체크섬이 파일과 다르면 시작을 실패시킵니다.

## 4. users

| 컬럼 | 타입 | 규칙 |
|---|---|---|
| `id` | INTEGER | PK AUTOINCREMENT |
| `username` | TEXT | NOT NULL, 길이 1..50 |
| `provider` | INTEGER | NOT NULL |
| `provider_user_id` | TEXT | NOT NULL, 길이 1..255 |
| `created_at_utc_ms` | INTEGER | NOT NULL |

제약과 인덱스:

- `ux_users_username` 유일
- `ux_users_provider_provider_user_id` 유일
- provider 코드: Google=1, Facebook=2, Guest=999. 기준선에서는 Google만 외부 메서드로 지원합니다.

## 5. refresh_tokens

| 컬럼 | 타입 | 규칙 |
|---|---|---|
| `id` | TEXT | PK, UUID |
| `user_id` | INTEGER | NOT NULL, users FK, CASCADE |
| `family_id` | TEXT | NOT NULL, UUID |
| `token_hash` | BLOB | NOT NULL, 32 bytes |
| `created_at_utc_ms` | INTEGER | NOT NULL |
| `expires_at_utc_ms` | INTEGER | NOT NULL |
| `used_at_utc_ms` | INTEGER | NULL |
| `revoked_at_utc_ms` | INTEGER | NULL |
| `revoke_reason` | TEXT | NULL, 최대 64자 |
| `replaced_by_token_id` | TEXT | NULL, 자기 참조 ID |

인덱스:

- `ux_refresh_tokens_token_hash` 유일
- `ix_refresh_tokens_family_id`
- `ix_refresh_tokens_user_id_revoked_at`
- `ix_refresh_tokens_expires_at`

회전 선점은 다음 의미의 조건부 UPDATE가 정확히 한 행에 적용될 때만 성공합니다.

```sql
UPDATE refresh_tokens
SET used_at_utc_ms = @Now,
    replaced_by_token_id = @NextTokenId
WHERE id = @CurrentTokenId
  AND used_at_utc_ms IS NULL
  AND revoked_at_utc_ms IS NULL
  AND expires_at_utc_ms > @Now;
```

## 6. characters

| 컬럼 | 타입 | 규칙 |
|---|---|---|
| `user_id` | INTEGER | PK, users FK, CASCADE |
| `level` | INTEGER | NOT NULL, `>= 1` |
| `exp` | INTEGER | NOT NULL, `>= 0` |

별도 ID 없이 사용자 ID를 기본 키로 사용해 사용자당 하나를 보장합니다.

## 7. currencies

| 컬럼 | 타입 | 규칙 |
|---|---|---|
| `user_id` | INTEGER | users FK, CASCADE |
| `type` | INTEGER | 지원 재화 코드 |
| `amount` | INTEGER | NOT NULL, `>= 0` |

기본 키는 `(user_id, type)`입니다.

증가 시 `amount <= Int64.MaxValue - @Amount`, 차감 시 `amount >= @Amount` 조건을 UPDATE에 포함해 조회와 갱신 사이 경쟁을 제거합니다.

## 8. stage_runs

| 컬럼 | 타입 | 규칙 |
|---|---|---|
| `id` | INTEGER | PK AUTOINCREMENT |
| `user_id` | INTEGER | NOT NULL, users FK, CASCADE |
| `stage_id` | INTEGER | NOT NULL, 정적 카탈로그 참조 |
| `status` | INTEGER | NOT NULL, InProgress=0, Completed=1, Abandoned=2 |
| `started_at_utc_ms` | INTEGER | NOT NULL |
| `completed_at_utc_ms` | INTEGER | NULL |
| `exp_gained` | INTEGER | NOT NULL, 기본 0, `>= 0` |
| `currencies_gained_json` | TEXT | NULL, 완료 시 보상 스냅샷 |

인덱스:

- `ix_stage_runs_user_id_status`
- `ux_stage_runs_user_in_progress` 부분 유일 인덱스: `user_id WHERE status = 0`

완료 선점은 `id`, `user_id`, `status = 0` 조건의 UPDATE가 한 행에 적용될 때만 성공합니다.

`currencies_gained_json`은 응답 복원과 감사용 스냅샷이며 현재 `CurrencyPacket` 배열 형태를 저장합니다. 쓰기 전 애플리케이션에서 직렬화하고 읽을 때 스키마를 검증합니다.

## 9. user_rooms

| 컬럼 | 타입 | 규칙 |
|---|---|---|
| `user_id` | INTEGER | PK, users FK, CASCADE |
| `map_id` | INTEGER | NOT NULL, 정적 카탈로그 참조 |
| `traps_json` | TEXT | NOT NULL, JSON Array |
| `updated_at_utc_ms` | INTEGER | NOT NULL |

별도 ID 없이 사용자 ID를 기본 키로 사용합니다. Upsert는 전체 맵과 함정 스냅샷을 교체합니다.

가능한 SQLite 빌드에서는 `json_valid(traps_json)` 체크 제약을 사용합니다. 애플리케이션 검증은 DB 체크를 대체하지 않고 함께 유지합니다.

## 10. 연결 설정

연결 생성 직후 다음 정책을 적용합니다.

```sql
PRAGMA foreign_keys = ON;
PRAGMA busy_timeout = 5000;
```

서버 시작 시 DB 초기화 단계에서 WAL 모드를 설정합니다.

```sql
PRAGMA journal_mode = WAL;
```

- 연결은 작업 단위로 짧게 유지합니다.
- 하나의 트랜잭션에 참여하는 Repository는 같은 `DbConnection`과 `DbTransaction`을 전달받습니다.
- 배치 요소끼리 연결이나 트랜잭션을 공유하지 않습니다.
- `SQLITE_BUSY`는 짧은 제한적 재시도 후 `DATABASE_UNAVAILABLE`로 변환합니다.

## 11. 마이그레이션

목표 위치:

```text
Infrastructure/Database/Migrations/
├─ 0001_initial.sql
├─ 0002_seed_development_user.sql
└─ ...
```

- 운영 스키마는 시작 시 순서대로 적용합니다.
- 한 파일은 하나의 트랜잭션으로 적용합니다.
- 실패하면 해당 파일을 롤백하고 서버 시작을 실패시킵니다.
- 운영 데이터 seed는 마이그레이션에 넣지 않습니다.
- 개발 사용자 seed는 Development 환경 전용으로 분리합니다.
- 기존 MySQL 마이그레이션을 기계적으로 변환하지 않고 이 문서의 최종 스키마를 기준으로 새 초기 마이그레이션을 작성합니다.

## 12. 데이터 보존과 삭제

- 사용자 삭제 시 관련 캐릭터, 재화, 리프레시 토큰, 실행, 방은 외래 키 CASCADE로 삭제됩니다.
- 토큰 정리 작업은 만료·폐기 후 보존 기간을 운영 정책으로 확정한 뒤 추가합니다.
- 스테이지 실행 기록의 보존 기간은 미결정입니다.
- SQLite 백업과 복구는 [운영 Runbook](../operations/RUNBOOK.md)을 따릅니다.
