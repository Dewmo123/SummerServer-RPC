# 설정 가이드

## 1. 원칙

- 안전한 기본값은 `appsettings.json`에 둡니다.
- 비밀값은 환경 변수, .NET User Secrets 또는 운영 비밀 저장소로 주입합니다.
- 모든 Options는 시작 시 검증하고 잘못된 설정에서 서버 시작을 실패시킵니다.
- 환경 변수는 ASP.NET Core의 `__` 구분자를 사용합니다.

## 2. 권장 설정 구조

```json
{
  "JsonRpc": {
    "Path": "/rpc",
    "MaxRequestBodyBytes": 65536,
    "MaxBatchSize": 50,
    "MaxJsonDepth": 32
  },
  "Database": {
    "Path": "data/summer-project.db",
    "BusyTimeoutMilliseconds": 5000,
    "UseWriteAheadLogging": true
  },
  "Jwt": {
    "Issuer": "summer-project-server",
    "Audience": "summer-project-client",
    "AccessTokenMinutes": 60,
    "ClockSkewSeconds": 30
  },
  "RefreshToken": {
    "LifetimeDays": 30
  },
  "Google": {
    "ClientIds": []
  },
  "DevelopmentLogin": {
    "Enabled": false,
    "Username": "Developer"
  },
  "Catalog": {
    "RootPath": "Content/Catalogs"
  },
  "RateLimits": {
    "LoginPermitLimit": 10,
    "LoginWindowSeconds": 60,
    "GeneralPermitLimit": 120,
    "GeneralWindowSeconds": 1
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

`Jwt:SigningKey`는 예시 파일에도 값을 넣지 않습니다.

현재 서버는 `Jwt:SigningKey`가 없거나 UTF-8 기준 32바이트보다 짧으면 한국어 검증 오류를 기록하고 시작을 실패시킵니다. 테스트는 운영 비밀이 아닌 실행 중 생성한 가짜 키를 별도 구성 공급자로 주입합니다.

## 3. 설정 키

| 키 | 필수 | 기본값 | 규칙 |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | 예 | Production | Development/Staging/Production |
| `JsonRpc:Path` | 예 | `/rpc` | `/`로 시작하는 고정 경로 |
| `JsonRpc:MaxRequestBodyBytes` | 예 | 65536 | 1 이상, 증가는 보안 검토 필요 |
| `JsonRpc:MaxBatchSize` | 예 | 50 | 1 이상 |
| `JsonRpc:MaxJsonDepth` | 예 | 32 | 1 이상 |
| `Database:Path` | 예 | `data/summer-project.db` | 서비스 계정 쓰기 가능 경로 |
| `Database:BusyTimeoutMilliseconds` | 예 | 5000 | 0 이상 |
| `Database:UseWriteAheadLogging` | 예 | true | 운영 변경 시 부하·백업 검토 |
| `Jwt:Issuer` | 예 | 없음 | 빈 문자열 금지 |
| `Jwt:Audience` | 예 | 없음 | 빈 문자열 금지 |
| `Jwt:SigningKey` | 예 | 없음 | 비밀값, 최소 32바이트 엔트로피 |
| `Jwt:AccessTokenMinutes` | 예 | 60 | 1..1440 |
| `Jwt:ClockSkewSeconds` | 예 | 30 | 0 이상 |
| `RefreshToken:LifetimeDays` | 예 | 30 | 1..365 |
| `Google:ClientIds` | 운영 Google 로그인 시 | 빈 배열 | 하나 이상의 허용 audience |
| `DevelopmentLogin:Enabled` | 아니요 | false | Development 환경에서만 효력 |
| `DevelopmentLogin:Username` | 개발 로그인 시 | Developer | DB seed와 일치 |
| `Catalog:RootPath` | 예 | `Content/Catalogs` | publish 결과에 포함 |
| `RateLimits:*` | 예 | 표 참조 | 양수 |

## 4. 환경 변수 예시

PowerShell 개발 세션 예시:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Jwt__SigningKey = "replace-with-a-long-random-development-key"
$env:Jwt__Issuer = "summer-project-server"
$env:Jwt__Audience = "summer-project-client"
$env:Google__ClientIds__0 = "replace-with-google-client-id"
$env:DevelopmentLogin__Enabled = "true"
dotnet run --project src/SummerProject.Server
```

실제 키를 문서, 셸 기록 공유본, 커밋에 남기지 않습니다.

## 5. User Secrets

목표 프로젝트가 생성된 후 로컬 비밀값은 다음 형태로 설정합니다.

```powershell
dotnet user-secrets init --project src/SummerProject.Server
dotnet user-secrets set "Jwt:SigningKey" "replace-with-local-secret" --project src/SummerProject.Server
dotnet user-secrets set "Google:ClientIds:0" "replace-with-client-id" --project src/SummerProject.Server
```

Google Client ID 자체는 일반적으로 비밀이 아니지만 환경별 설정이므로 코드와 분리합니다. Google Client Secret은 현재 ID 토큰 검증 흐름에 필요하지 않으며 임의로 추가하지 않습니다.

## 6. 경로

- DB 상대 경로는 실행 작업 디렉터리가 아니라 Content Root 기준 절대 경로로 정규화합니다.
- 절대 DB 경로는 그대로 정규화해 사용하며 서버 시작 시 상위 디렉터리가 없으면 생성합니다.
- 허용 데이터 디렉터리의 별도 경계 설정은 목표 배포 경로가 확정된 뒤 추가합니다. 현재 운영자는 서비스 계정 전용 경로와 최소 OS 권한을 사용해야 합니다.
- 카탈로그 경로는 읽기 전용이며 publish 산출물에 포함합니다.
- 로그 파일을 사용할 경우 DB와 다른 운영 디렉터리에 두고 회전 정책을 적용합니다.

## 7. 로그 환경별 권장값

| 환경 | 기본 레벨 | 형식 | 비고 |
|---|---|---|---|
| Development | Debug 또는 Information | 읽기 쉬운 콘솔 또는 JSON | 토큰·params 마스킹은 동일 적용 |
| Staging | Information | JSON | 운영과 같은 필드 검증 |
| Production | Information | JSON | 파일 회전 또는 표준 출력 수집 |

`Microsoft.AspNetCore`의 기본 레벨은 Warning으로 두고 health check의 반복 성공 로그는 필요하면 별도 필터링합니다.

현재 서버는 ZLogger JSON 콘솔 출력을 사용하며 타임스탬프는 UTC입니다. JSON-RPC 처리 요약에는 `traceId`, 안전하게 축약한 `rpcId`, `rpcMethod`, `durationMs`, `outcome`, 실패 시 `errorCode`를 기록합니다. 인증 문맥이 구현된 뒤에는 호출자가 확인된 요청에만 `userId`를 추가합니다. Authorization 헤더, 토큰, 전체 params와 원문 요청 본문은 기록하지 않습니다.

Phase 3 서버는 시작 시 SQLite 외래 키, busy timeout, WAL과 마이그레이션 체크섬을 검증합니다. `/health`는 연결과 `SELECT 1`, 적용된 마이그레이션 상태를 확인하지만 DB 전체 경로나 내부 오류는 응답하지 않습니다.

## 8. 설정 변경 관리

- 외부 계약이나 보안 수준에 영향을 주는 기본값 변경은 CHANGELOG와 관련 문서를 갱신합니다.
- JWT 서명 키 변경은 기존 액세스 토큰을 즉시 무효화할 수 있으므로 배포 계획이 필요합니다.
- DB 경로 변경 전 백업과 권한을 검증합니다.
- 배치·요청 제한 상향은 부하 테스트 결과를 첨부합니다.
