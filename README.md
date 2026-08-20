# SummerServer-RPC

비동기 멀티플레이 게임 서버를 **.NET 10 + JSON-RPC 2.0 + SQLite** 기반의 단일 서버로 재구현하는 프로젝트입니다.

> **현재 상태**
>
> 서버 기반인 **Phase 0~4까지 완료**했습니다.
>
> JSON-RPC 처리, SQLite 마이그레이션, 구조화 로그, 정적 맵·스테이지 적재는 동작합니다.
>
> 로그인·캐릭터·재화·스테이지 진행·사용자 방 RPC는 아직 구현 전입니다.

[빠른 실행](#빠른-실행) · [현재 진행 상황](#현재-진행-상황) · [코드 구조](#코드-구조) · [AI 활용 방식](#ai-활용-방식) · [문서 안내](#문서-안내)

---

## 한눈에 보기

| 항목 | 내용 |
|---|---|
| 목표 | 기존 REST 기반 분리 서버를 JSON-RPC 모듈형 모노리스로 재구현 |
| 서버 프로젝트 | [`src/SummerProject.Server`](src/SummerProject.Server) 하나 |
| 외부 진입점 | 게임 기능은 `POST /rpc`, 상태 확인은 `GET /health` |
| 데이터베이스 | SQLite + Dapper |
| 정적 데이터 | JSON 파일을 시작 시 검증한 뒤 메모리에 적재 |
| 현재 완료 | 저장소 기반, JSON-RPC 코어, 설정·로그, SQLite, 정적 카탈로그 |
| 다음 단계 | Phase 5 인증 기능 |
| 최신 상태 기준 | [요구사항 추적성 표](docs/migration/TRACEABILITY.md) |

### 지금 사용할 수 있는 기능

- `POST /rpc`
  - JSON-RPC 단일 요청
  - 알림과 배치 요청
  - Object·Array params 바인딩
  - 요청 ID 타입과 값 보존
  - 표준 오류 응답
  - 요청 크기·JSON 깊이·배치 크기 제한
- `GET /health`
  - SQLite 연결 확인
  - `SELECT 1` 확인
  - 적용된 마이그레이션 이름과 체크섬 확인

> **중요:** 현재 프로덕션 코드에는 게임 업무 Handler가 등록되어 있지 않습니다.
>
> 따라서 `auth.*`, `character.*`, `currency.*`, `stage.*`, `room.*` 호출은 아직 `Method not found`를 반환합니다.

---

## 프로젝트가 해결하려는 문제

기존 서버는 로그인 서버, 게임 서버, 공용 영속성 프로젝트로 나뉘어 있고 REST, MySQL, EF Core를 사용합니다. 새 서버는 기능을 단순히 옮기지 않고, 승인된 요구사항과 공개 계약을 기준으로 다시 구현합니다.

핵심 목표는 다음과 같습니다.

- 인증과 게임 기능을 하나의 프로세스와 배포 단위로 통합합니다.
- 모든 게임 기능을 하나의 JSON-RPC 엔드포인트로 제공합니다.
- SQL과 트랜잭션 경계를 코드에 명확하게 드러냅니다.
- 중복 로그인, 토큰 재사용, 중복 보상, 잘못된 방 배치로 인한 데이터 훼손을 방지합니다.
- 요구사항 → 계약 → 코드 → 테스트의 연결을 문서로 추적합니다.

### 제품 범위

구현 대상으로 확정된 기능:

- Google 로그인과 개발 로그인
- JWT 액세스 토큰
- 리프레시 토큰 회전·재사용 탐지·로그아웃
- 캐릭터 레벨과 경험치
- Gold를 포함한 재화
- 정적 스테이지 조회·입장·완료·보상
- 개인 방의 맵과 함정 배치

현재 범위 밖:

- 매칭, 랭킹, 리더보드
- 장비, 스킬, 인벤토리
- 결제와 상점
- 다른 플레이어 방의 실제 약탈 결과 처리
- 강한 치트 방지와 서버 권위 시뮬레이션
- 다중 서버 인스턴스가 같은 SQLite DB에 쓰는 구조

---

## 기술 구성

| 영역 | 기술 |
|---|---|
| Runtime | .NET SDK 10.0.400, ASP.NET Core |
| RPC | JSON-RPC 2.0 over HTTP |
| JSON | System.Text.Json |
| DB | SQLite, Microsoft.Data.Sqlite 10.0.11 |
| 데이터 접근 | Dapper 2.1.79 |
| 로그 | ZLogger 2.5.10, Microsoft.Extensions.Logging |
| 테스트 | xUnit, TestServer, 실제 임시 SQLite |
| 패키지 관리 | 중앙 버전 관리 + packages.lock.json |

신규 서버에는 **EF Core, MySQL, Newtonsoft.Json을 사용하지 않습니다.**

### 요청 처리 흐름

```text
게임 클라이언트
    │
    ▼
POST /rpc
    │  Content-Type·본문 크기 확인
    ▼
JSON-RPC 파싱과 구조 검증
    │  단일·알림·배치 / id / params
    ▼
Method Registry와 params 바인딩
    ▼
Controllers의 RPC Handler
    ▼
Services의 업무 규칙과 Repository
    ▼
SQLite / 정적 Catalog
    │
    ▼
result 또는 error 응답 + 구조화 로그
```

### 반드시 지키는 JSON-RPC 규칙

- `jsonrpc`는 정확히 `"2.0"`이어야 합니다.
- `id` 속성이 없는 요청만 알림입니다.
- `"id": null`은 알림이 아니므로 응답해야 합니다.
- 요청 ID의 String·Number·Null 타입과 값을 응답에 보존합니다.
- 메서드명과 이름 기반 params 필드명은 대소문자를 구분합니다.
- 성공 응답은 `result`, 실패 응답은 `error`만 가집니다.
- 알림은 실행하지만 성공·실패 모두 응답하지 않습니다.
- 배치 요소는 서로 독립적이며 배치 전체 트랜잭션을 만들지 않습니다.

전체 규칙은 [JSON-RPC 계약](docs/contracts/JSON_RPC_CONTRACT.md)을 따릅니다.

---

## 코드 구조

```text
SummerServer-RPC
├─ src/SummerProject.Server
│  ├─ Program.cs
│  ├─ Bootstrap                 설정, DI, 시작 순서, 엔드포인트
│  ├─ Rpc                       파싱, 검증, 디스패치, 직렬화
│  ├─ Infrastructure
│  │  ├─ Database               SQLite 연결, 마이그레이션, health check
│  │  ├─ Logging                ZLogger와 민감정보 필터
│  │  └─ Security               인증 설정과 후속 보안 구현 위치
│  ├─ GameData/Catalogs         맵·스테이지 JSON과 읽기 전용 Catalog
│  ├─ Models
│  │  ├─ DTOs                   RPC Request, Response, Packet
│  │  ├─ Datas                  Dapper DB 행 Model
│  │  └─ GameData               검증된 정적 Proto
│  ├─ Controllers               기능별 JSON-RPC Handler
│  └─ Services                  업무 규칙, Repository, 트랜잭션
│
├─ tests/SummerProject.Server.Tests
├─ docs
├─ AGENTS.md
├─ CONTRIBUTING.md
└─ CHANGELOG.md
```

### 폴더를 나누는 기준

- **Rpc**는 JSON-RPC 규격만 처리합니다. 게임 업무와 SQL을 알지 못합니다.
- **Controllers**는 DTO를 Service 호출로 연결합니다. 업무 규칙과 SQL을 넣지 않습니다.
- **Services**는 인증·캐릭터·재화·스테이지·방 규칙과 Repository를 가집니다.
- **Models/DTOs**는 외부 계약, **Models/Datas**는 SQLite 행을 표현합니다.
- **GameData/Catalogs**는 배포된 정적 JSON을 검증하고 읽기 전용으로 제공합니다.
- **Infrastructure**는 DB, 로깅, 보안처럼 외부 기술과 맞닿는 코드를 담당합니다.

`Manager`, `Helper`, `Util`, `Info`, `Data` 같은 모호한 타입명과 새 `Features`, `Content` 폴더는 사용하지 않습니다.

### 현재 데이터 기반

SQLite 초기 마이그레이션은 다음 테이블을 만듭니다.

- `users`: 외부 인증 계정과 연결된 사용자
- `refresh_tokens`: 원문이 아닌 SHA-256 해시와 토큰 패밀리 상태
- `characters`: 사용자별 레벨과 경험치
- `currencies`: 사용자·재화 종류별 잔액
- `stage_runs`: 스테이지 입장·완료·포기 기록
- `user_rooms`: 사용자의 최신 맵과 함정 배치
- `schema_migrations`: 적용된 SQL 이름과 체크섬

연결마다 외래 키와 busy timeout을 활성화하고, 서버 시작 시 WAL 모드를 설정합니다. 자세한 제약과 인덱스는 [데이터 모델](docs/architecture/DATA_MODEL.md)에서 확인할 수 있습니다.

### 현재 정적 데이터

- [Map1.json](src/SummerProject.Server/GameData/Catalogs/Maps/Map1.json): ID 1, 16×8
- [Stage1.json](src/SummerProject.Server/GameData/Catalogs/Stages/Stage1.json): ID 1, 최소 1초, 경험치 10, Gold 100, SawTrap 1개

서버는 시작할 때 ID, 크기, 필수 배열, 함정 종류, 좌표, 중복 위치, quaternion, 보상을 검사합니다. 검증에 실패하면 요청을 받기 전에 시작을 중단합니다.

---

## 현재 진행 상황

| Phase | 내용 | 상태 |
|---:|---|:---:|
| 0 | .NET 10, 단일 프로젝트, 중앙 패키지, CI | ✅ 완료 |
| 1 | JSON-RPC 단일·알림·배치 처리 | ✅ 완료 |
| 2 | 설정 검증, ZLogger, 민감정보 차단, health | ✅ 완료 |
| 3 | SQLite 연결, WAL, SQL 마이그레이션, 체크섬 | ✅ 완료 |
| 4 | 맵·스테이지 JSON Loader와 Catalog | ✅ 완료 |
| 5 | Google·개발 로그인, JWT, 리프레시 토큰 | ⏳ 예정 |
| 6 | 캐릭터와 재화 | ⏳ 예정 |
| 7 | 스테이지 조회·입장·완료·보상 | ⏳ 예정 |
| 8 | 사용자 방 저장·조회 | ⏳ 예정 |
| 9 | 통합, publish, 백업·복구, 배포 전환 | ⏳ 예정 |
| 10 | 클라이언트 전환 후 레거시 제거 | 대기 |

Phase 완료와 기능 요구사항 완료는 다를 수 있습니다. 예를 들어 Phase 4 카탈로그는 완료됐지만 `stage.get` Handler가 아직 없어 FR-STAGE-001은 **진행 중**입니다.

<details>
<summary><strong>계약이 확정된 RPC 목록 보기</strong></summary>

| 영역 | RPC | 상태 |
|---|---|---|
| 인증 | `auth.login.google` | 구현 전 |
| 인증 | `auth.login.development` | 구현 전 |
| 인증 | `auth.token.refresh` | 구현 전 |
| 인증 | `auth.logout` | 구현 전 |
| 캐릭터 | `character.getMine` | 구현 전 |
| 재화 | `currency.getMine` | 구현 전 |
| 재화 | `currency.listMine` | 구현 전 |
| 스테이지 | `stage.get` | 카탈로그 완료, Handler 구현 전 |
| 스테이지 | `stage.enter` | 구현 전 |
| 스테이지 | `stage.complete` | 구현 전 |
| 사용자 방 | `room.upsertMine` | 구현 전 |
| 사용자 방 | `room.getMine` | 구현 전 |

params, result, 인증, 오류는 [RPC 메서드 카탈로그](docs/contracts/RPC_METHOD_CATALOG.md)를 기준으로 합니다.

</details>

---

## 빠른 실행

### 1. 준비

- .NET SDK 10.0.400
- PowerShell
- 쓰기 가능한 로컬 데이터 디렉터리

### 2. 복원하고 실행

JWT 서명 키는 저장소에 없으므로 실행 세션에서 임시 개발 키를 만듭니다.

```powershell
dotnet restore --locked-mode

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Jwt__SigningKey = [Convert]::ToBase64String(
    [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
)

dotnet run --project src/SummerProject.Server
```

실행 주소는 [launchSettings.json](src/SummerProject.Server/Properties/launchSettings.json) 또는 콘솔 출력을 확인합니다.

### 3. 상태 확인

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:<port>/health"
```

현재는 업무 RPC가 등록되기 전이므로 `/health`가 대표 smoke test입니다.

### 전체 검증

```powershell
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet format --verify-no-changes --no-restore
```

테스트는 JSON-RPC 적합성, 요청 제한, 시작 설정, 민감정보 로그 차단, 실제 SQLite 제약·마이그레이션, 정적 카탈로그 검증을 다룹니다.

---

## AI 활용 방식

이 프로젝트에서 AI는 **문서에 없는 결정을 내리는 주체가 아니라, 승인된 계약을 구현하고 검증하는 협업 도구**입니다.

### 사람과 AI의 역할

| 사람 | AI |
|---|---|
| 제품 범위와 미결정 사항 승인 | 관련 문서와 기존 동작 조사 |
| 공개 계약과 설계 결정 승인 | 요구사항에 맞는 코드·테스트 작성 |
| 운영 데이터 변경과 배포 승인 | 빌드·테스트·문서 정합성 검증 |
| 최종 리뷰와 병합 | 스테이징 diff 기반 커밋 메시지 제안 |

### AI 작업 순서

1. [문서 지도](docs/README.md)에서 읽을 문서를 찾습니다.
2. 작업할 FR/NFR ID와 RPC 메서드를 확인합니다.
3. 관련 ADR, 데이터 규칙, 오류 계약을 읽습니다.
4. 구현 범위와 정상·경계·실패·동시성 테스트를 먼저 정리합니다.
5. 코드, 테스트, 추적성 문서를 함께 수정합니다.
6. restore, build, test, format을 실행합니다.
7. 스테이징된 diff만 보고 한국어 커밋 메시지를 제안합니다.

### AI에게 전달하는 명령 예시

```text
FR-STAGE-003 스테이지 완료를 구현하라.

- 먼저 AGENTS.md와 관련 요구사항, RPC 계약, 오류 카탈로그,
  DATA_MODEL.md, 관련 ADR을 읽어라.
- 허용 범위는 Controllers/Stages, Services/Stages,
  관련 Models와 테스트다.
- 완료 선점과 Gold·경험치 지급을 같은 트랜잭션에서 처리하라.
- 정상, 소유권 실패, 최소 시간 미충족, 동시 완료,
  보상 실패 롤백을 테스트하라.
- 미결정 사항은 추측하지 말고 문서에 기록하라.
- build와 test를 실행하고 TRACEABILITY.md를 갱신하라.
```

작업 요청에는 가능한 한 다음 내용을 명시합니다.

- 요구사항 ID와 RPC 이름
- 수정해도 되는 폴더
- 하지 말아야 할 범위
- 반드시 필요한 테스트
- 문서 갱신과 완료 조건

AI는 미결정 요구사항 확정, 운영 데이터 이동·삭제, 공개 계약 변경, 패키지 추가, push·배포 권한을 자동으로 갖지 않습니다.

---

## 문서 안내

### 처음 읽는다면

목적에 따라 아래 문서부터 보면 됩니다.

| 알고 싶은 내용 | 먼저 읽을 문서 |
|---|---|
| 제품이 무엇을 해야 하는가 | [앱 요구사항](docs/requirements/APP_REQUIREMENTS.md) |
| 어떤 RPC가 있는가 | [RPC 메서드 카탈로그](docs/contracts/RPC_METHOD_CATALOG.md) |
| JSON-RPC를 어떻게 처리하는가 | [JSON-RPC 계약](docs/contracts/JSON_RPC_CONTRACT.md) |
| 코드가 왜 이 구조인가 | [목표 아키텍처](docs/architecture/ARCHITECTURE.md) |
| DB가 어떻게 생겼는가 | [데이터 모델](docs/architecture/DATA_MODEL.md) |
| 지금 어디까지 구현됐는가 | [요구사항 추적성](docs/migration/TRACEABILITY.md) |
| 다음에는 무엇을 구현하는가 | [구현 계획](docs/engineering/IMPLEMENTATION_PLAN.md) |
| 실행·백업·장애 대응 방법 | [운영 Runbook](docs/operations/RUNBOOK.md) |
| AI에게 작업을 요청하는 방법 | [AI 작업 템플릿](docs/templates/AI_TASK_TEMPLATE.md) |

### 문서 우선순위

문서와 코드가 충돌하면 다음 순서로 판단합니다.

1. [JSON-RPC 2.0 공식 스펙](https://www.jsonrpc.org/specification)
2. [앱 요구사항](docs/requirements/APP_REQUIREMENTS.md)
3. [RPC 메서드 카탈로그](docs/contracts/RPC_METHOD_CATALOG.md)
4. 승인된 [ADR](docs/architecture/adr)
5. 그 밖의 아키텍처 문서
6. 신규 목표 코드
7. 기존 서버 코드

확정할 수 없는 내용은 임의로 구현하지 않고 관련 문서의 **미결정 사항**에 기록합니다.

<details>
<summary><strong>전체 Markdown 문서 34개 보기</strong></summary>

#### 저장소 진입 문서

- [README.md](README.md): 프로젝트 전체 소개와 빠른 시작
- [AGENTS.md](AGENTS.md): AI가 반드시 지켜야 하는 저장소 규칙
- [CONTRIBUTING.md](CONTRIBUTING.md): 기여, 리뷰, 검증, 커밋 절차
- [CHANGELOG.md](CHANGELOG.md): 기능·계약 중심 변경 기록
- [docs/README.md](docs/README.md): 상세 문서 지도와 읽는 순서

#### 요구사항

- [APP_REQUIREMENTS.md](docs/requirements/APP_REQUIREMENTS.md): FR, NFR, 업무 규칙, 범위와 미결정 사항
- [DOMAIN_GLOSSARY.md](docs/requirements/DOMAIN_GLOSSARY.md): 프로젝트 공통 용어
- [USE_CASES.md](docs/requirements/USE_CASES.md): 주요 정상·대안 흐름

#### 외부 계약

- [JSON_RPC_CONTRACT.md](docs/contracts/JSON_RPC_CONTRACT.md): JSON-RPC와 HTTP 결합 규칙
- [RPC_METHOD_CATALOG.md](docs/contracts/RPC_METHOD_CATALOG.md): 메서드별 params, result, 인증, 오류
- [ERROR_CATALOG.md](docs/contracts/ERROR_CATALOG.md): 표준·서버·업무 오류 코드

#### 아키텍처와 보안

- [ARCHITECTURE.md](docs/architecture/ARCHITECTURE.md): 구조, 책임, 의존 방향, 요청 흐름
- [NAMING_CONVENTIONS.md](docs/architecture/NAMING_CONVENTIONS.md): 타입 접미사와 금지 이름
- [DATA_MODEL.md](docs/architecture/DATA_MODEL.md): SQLite 스키마와 트랜잭션 정책
- [SECURITY.md](docs/architecture/SECURITY.md): 인증, 토큰, 권한, 로그 보안
- [adr/README.md](docs/architecture/adr/README.md): ADR 목록과 관리 규칙
- [ADR-0001](docs/architecture/adr/0001-modular-monolith.md): 모듈형 모노리스
- [ADR-0002](docs/architecture/adr/0002-json-rpc-over-http.md): JSON-RPC over HTTP
- [ADR-0003](docs/architecture/adr/0003-sqlite-dapper.md): SQLite와 Dapper
- [ADR-0004](docs/architecture/adr/0004-static-catalogs.md): 정적 JSON 카탈로그

#### 개발과 테스트

- [IMPLEMENTATION_PLAN.md](docs/engineering/IMPLEMENTATION_PLAN.md): Phase 0~10 계획
- [TEST_STRATEGY.md](docs/engineering/TEST_STRATEGY.md): 테스트 계층과 품질 게이트
- [COMMENT_GUIDE.md](docs/engineering/COMMENT_GUIDE.md): 한국어 주석 규칙
- [COMMIT_GUIDE.md](docs/engineering/COMMIT_GUIDE.md): 한국어 Conventional Commit 규칙

#### 운영과 재구현 추적

- [CONFIGURATION.md](docs/operations/CONFIGURATION.md): 설정 키와 비밀값 주입
- [RUNBOOK.md](docs/operations/RUNBOOK.md): 실행, 상태 확인, 백업, 복구, 장애 대응
- [AS_IS_BEHAVIOR_INVENTORY.md](docs/migration/AS_IS_BEHAVIOR_INVENTORY.md): 기존 서버에서 관찰한 동작
- [GAP_ANALYSIS.md](docs/migration/GAP_ANALYSIS.md): 기존 구조와 목표 구조의 차이
- [TRACEABILITY.md](docs/migration/TRACEABILITY.md): 요구사항, 구현, 테스트 상태 연결

#### 템플릿과 하위 안내

- [REQUIREMENT_TEMPLATE.md](docs/templates/REQUIREMENT_TEMPLATE.md): 새 요구사항 작성 형식
- [ADR_TEMPLATE.md](docs/templates/ADR_TEMPLATE.md): 새 설계 결정 작성 형식
- [AI_TASK_TEMPLATE.md](docs/templates/AI_TASK_TEMPLATE.md): AI 구현 요청 형식
- [서버 소스 README](src/SummerProject.Server/README.md): 서버 폴더 책임
- [테스트 README](tests/SummerProject.Server.Tests/README.md): 테스트 폴더와 DB 테스트 원칙

</details>

### 문서를 함께 바꾸는 규칙

| 코드 변경 | 함께 확인할 문서 |
|---|---|
| 외부 동작·RPC 변경 | 요구사항, RPC 카탈로그, 오류 카탈로그, 테스트, 추적성 |
| DB 변경 | 데이터 모델, 새 SQL 마이그레이션, 테스트, 필요 시 ADR |
| 보안 변경 | 보안 문서, 설정 문서, 위협 관련 테스트 |
| 패키지 변경 | 선택 근거, 아키텍처 또는 ADR, 중앙 버전, 잠금 파일 |
| 구현 완료 | 추적성 표와 CHANGELOG |

승인된 ADR과 이미 적용된 SQL 마이그레이션은 조용히 수정하지 않습니다. 결정이 바뀌면 새 ADR을, 스키마가 바뀌면 다음 번호의 마이그레이션을 추가합니다.

### 이름과 주석 규칙

| 역할 | 접미사 | 예시 |
|---|---|---|
| 값 객체 | `Proto` | `StageProto` |
| Dapper DB 행 | `Model` | `UserModel` |
| RPC params | `Request` | `EnterStageRequest` |
| RPC result | `Response` | `EnterStageResponse` |
| DTO 내부 구성 | `Packet` | `StagePacket` |

코드 식별자는 영어로 작성하고, 새 주석과 수정한 주석은 한국어로 작성합니다. 주석은 코드 동작을 번역하지 않고 업무 이유, JSON-RPC 불변 조건, 동시성, 트랜잭션, 보안 제약을 설명합니다.

---

## 아직 결정되지 않은 것

- 기존 MySQL 운영 데이터를 SQLite로 이전할지 새 DB로 시작할지
- 최초 배포에 Guest 로그인을 포함할지
- 운영 RPO, RTO, 최대 동시 사용자, 최대 DB 크기
- 데이터와 로그의 보존 기간
- `tiles.Length == width × height`를 강제할지
- Map1·Stage1의 최종 타일과 함정 콘텐츠

현재 스테이지 완료 검증 목표는 서버 시작 시각과 최소 클리어 시간 확인까지입니다. 이것을 강한 치트 방지로 표현하지 않습니다.

---

## 기여하기

1. [문서 지도](docs/README.md)에서 관련 문서를 찾습니다.
2. [추적성 표](docs/migration/TRACEABILITY.md)에서 현재 상태를 확인합니다.
3. 구현 전에 범위와 테스트 항목을 정리합니다.
4. 코드, 테스트, 문서 추적성을 함께 변경합니다.
5. build, test, format을 실행합니다.
6. 스테이징된 diff를 기준으로 한국어 커밋 메시지를 작성합니다.

자세한 절차는 [CONTRIBUTING.md](CONTRIBUTING.md), AI 작업 규칙은 [AGENTS.md](AGENTS.md)를 기준으로 합니다.
