# 운영 Runbook

## 1. 사전 요구사항

- .NET 10 Runtime 또는 self-contained publish 산출물
- 서버 프로세스가 쓸 수 있는 전용 데이터 디렉터리
- 안전하게 주입된 JWT 서명 키와 Google Client ID
- HTTPS를 종료하는 reverse proxy 또는 직접 HTTPS 설정
- SQLite 백업을 저장할 별도 디스크 또는 원격 저장소

## 2. 빌드와 게시

```powershell
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet publish src/SummerProject.Server `
  --configuration Release `
  --no-build `
  --output artifacts/publish
```

배포 전 확인:

- 테스트 전체 성공
- `Content/Catalogs`가 publish에 포함됨
- `appsettings.json`에 비밀값이 없음
- `.db`, WAL, SHM, 로그가 산출물에 포함되지 않음
- 변경된 DB 마이그레이션과 체크섬 검토

## 3. 최초 시작

1. 데이터 디렉터리를 생성하고 서비스 계정에 최소 권한을 부여합니다.
2. 환경 변수 또는 서비스 비밀 저장소를 구성합니다.
3. 서버를 시작합니다.
4. 시작 로그에서 설정 검증, DB 마이그레이션, 카탈로그 적재 완료를 확인합니다.
5. `GET /health`가 성공인지 확인합니다.
6. 공개 `stage.get`과 개발/운영에 맞는 인증 smoke test를 실행합니다.

서버 시작 중 마이그레이션 또는 카탈로그 검증이 실패하면 트래픽을 받지 않아야 합니다.

적용된 SQL 마이그레이션 파일은 수정하지 않습니다. 기존 버전의 이름 또는 체크섬이 달라지면 서버는 시작을 거부하므로 변경은 항상 다음 번호의 새 파일로 추가합니다.

## 4. 상태 확인

```powershell
Invoke-RestMethod -Method Get -Uri "https://localhost:5001/health"
```

확인할 항목:

- 프로세스 응답
- SQLite 연결과 `SELECT 1`
- 적용된 마이그레이션 체크섬
- 카탈로그 적재 완료 상태

상태 응답에 DB 전체 경로, 환경 변수, 예외 스택을 노출하지 않습니다.

Phase 3 health check는 `SELECT 1`과 적용된 모든 마이그레이션의 이름·체크섬 일치를 함께 검사합니다. 실패 응답은 상태만 공개하므로 상세 원인은 시작 로그와 `schema_migrations`를 별도로 점검합니다.

## 5. 대표 RPC 확인

```powershell
$body = @{
    jsonrpc = "2.0"
    method = "stage.get"
    params = @{ stageId = 1 }
    id = "smoke-stage-1"
} | ConvertTo-Json -Depth 10

Invoke-RestMethod `
  -Method Post `
  -Uri "https://localhost:5001/rpc" `
  -ContentType "application/json" `
  -Body $body
```

응답에서 `jsonrpc`, `result`, 요청과 같은 `id`를 확인합니다.

## 6. SQLite 백업

WAL 모드에서 DB 파일 하나만 단순 복사하지 않습니다. `sqlite3` CLI가 있으면 온라인 backup API를 사용합니다.

```powershell
sqlite3 "data/summer-project.db" ".backup 'backups/summer-project-20260819.db'"
sqlite3 "backups/summer-project-20260819.db" "PRAGMA integrity_check;"
```

백업 절차:

1. 백업 파일명을 UTC 시각과 배포 버전으로 구분합니다.
2. `.backup`을 실행합니다.
3. 백업 DB에서 `PRAGMA integrity_check` 결과가 `ok`인지 확인합니다.
4. 별도 디스크 또는 원격 저장소에 복제합니다.
5. 보존 정책에 따라 오래된 백업을 정리합니다.

RPO와 보존 기간은 아직 미결정이므로 운영 전 확정해야 합니다.

## 7. 복구

1. 쓰기 트래픽과 서버 프로세스를 중지합니다.
2. 손상 DB, WAL, SHM을 같은 사건 디렉터리로 이동해 보존합니다.
3. 복구할 백업에서 `PRAGMA integrity_check`를 실행합니다.
4. 백업을 설정된 DB 경로에 배치합니다.
5. 서비스 계정 권한을 복원합니다.
6. 서버를 시작하고 마이그레이션, health, 대표 RPC를 확인합니다.
7. 마지막 백업 이후 유실 가능 범위를 기록합니다.

파일 삭제나 덮어쓰기 전에 원본 경로와 복구본 경로를 두 번 확인합니다.

## 8. 주요 장애

### 서버가 시작하지 않음

확인 순서:

1. 필수 Options 검증 오류
2. DB 디렉터리 권한과 디스크 용량
3. 마이그레이션 체크섬 불일치
4. 카탈로그 JSON 파싱·ID 중복·범위 오류
5. 포트 충돌

설정이나 카탈로그 검증을 우회해 서버를 강제로 시작하지 않습니다.

### SQLITE_BUSY 증가

확인 순서:

1. 장시간 열린 트랜잭션
2. 배치 크기와 동시 쓰기 요청 증가
3. 백업 또는 외부 도구의 잠금
4. busy timeout과 재시도 로그

대응:

- 트랜잭션 범위를 줄이고 순차 배치 처리를 확인합니다.
- 무제한 재시도를 추가하지 않습니다.
- 지속되면 쓰기 부하와 SQLite 적합성을 ADR 재검토 조건으로 기록합니다.

### 토큰 재사용 탐지 증가

확인 순서:

1. 동일 클라이언트의 병렬 refresh 버그
2. 네트워크 재시도에서 이전 토큰 재전송
3. 실제 토큰 탈취 가능성

대응:

- 패밀리 폐기는 유지합니다.
- 토큰 원문을 로그에 추가하지 않습니다.
- userId, 축약 family 식별자, 클라이언트 버전, 발생 시각으로 조사합니다.

### 스테이지 중복 보상 의심

확인 순서:

1. 실행 ID별 완료 상태와 보상 스냅샷
2. 재화와 캐릭터 변경의 동일 트랜잭션 여부
3. 조건부 UPDATE 영향 행 수
4. 동시 완료 테스트 최근 결과

데이터를 수동 수정하기 전에 DB 백업과 감사 기록을 남깁니다.

## 9. 배포와 롤백

- 배포 전 SQLite 백업을 생성합니다.
- 코드와 카탈로그를 같은 release로 배포합니다.
- DB 마이그레이션은 하위 호환 여부를 검토합니다.
- 롤백 코드가 새 스키마를 읽을 수 없는 경우 파일만 되돌리지 않습니다.
- 파괴적 마이그레이션은 expand/migrate/contract 단계로 분리합니다.
- 배포 후 health, `stage.get`, 인증, 읽기 기능, 한 개의 비파괴 쓰기 흐름을 확인합니다.

## 10. 로그 보존

- 구조화 JSON 로그를 사용합니다.
- 파일 로그라면 크기 또는 날짜 기반 회전을 적용합니다.
- 토큰과 params 원문이 없는지 정기적으로 샘플 검사합니다.
- 사용자 식별자와 보안 이벤트의 보존 기간은 개인정보 정책과 함께 확정합니다.
