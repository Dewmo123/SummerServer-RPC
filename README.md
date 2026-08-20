# SummerServer-RPC

기존 비동기 멀티플레이 게임 서버를 **.NET 10, ASP.NET Core, JSON-RPC 2.0, SQLite, Dapper** 기반의 모듈형 모노리스로 재구성한 프로젝트입니다.

이 문서는 기능 사용법보다 저장소의 구조, 계층별 책임, 요청 처리 흐름과 AI를 사용한 개발 과정을 설명합니다.

---

## 목차

1. [프로젝트 구성](#1-프로젝트-구성)
2. [아키텍처](#2-아키텍처)
3. [서버 소스 구조](#3-서버-소스-구조)
4. [요청 처리와 의존 방향](#4-요청-처리와-의존-방향)
5. [테스트와 문서 구조](#5-테스트와-문서-구조)
6. [이름과 주석 규칙](#6-이름과-주석-규칙)
7. [AI를 사용한 과정](#7-ai를-사용한-과정)
8. [AI 작업 지시 문서](#8-ai-작업-지시-문서)

---

## 1. 프로젝트 구성

저장소는 프로덕션 서버, 테스트, 문서 기준선으로 구분됩니다.

```text
SummerServer-RPC
├─ src/SummerProject.Server           프로덕션 서버
├─ tests/SummerProject.Server.Tests   자동화 테스트
├─ docs                               요구사항·계약·설계 문서
├─ AGENTS.md                          AI 작업 규칙
├─ CONTRIBUTING.md                    작업 절차
├─ Directory.Packages.props           패키지 버전 관리
├─ global.json                        .NET SDK 고정
└─ SummerProject.slnx                 solution 구성
```

프로덕션 프로젝트는 [`src/SummerProject.Server`](src/SummerProject.Server) 하나만 사용합니다. 인증, 게임 기능, 데이터 접근과 프로토콜이 같은 프로젝트에 있지만 폴더와 namespace로 책임을 분리합니다.

테스트 프로젝트는 별도 배포물이 아니므로 [`tests/SummerProject.Server.Tests`](tests/SummerProject.Server.Tests)에 둡니다. [`docs`](docs)는 부가 설명이 아니라 요구사항과 외부 계약, 설계 결정, 구현 상태를 관리하는 기준선입니다.

---

## 2. 아키텍처

```text
게임 클라이언트
      │ HTTP + JSON-RPC 2.0
      ▼
┌──────────────────────────────────────────────┐
│ Rpc                                          │
│  파싱 · 검증 · 메서드 탐색 · 직렬화         │
│                    │                         │
│                    ▼                         │
│ Controllers ──► Services ──► Repositories    │
│                    │              │          │
│                    ▼              ▼          │
│             Helpers/Models   Infrastructure  │
│                                   │          │
│                        SQLite / 외부 인증     │
│                                              │
│ GameData Catalog ──► 검증된 정적 데이터      │
└──────────────────────────────────────────────┘
```

[`Rpc`](src/SummerProject.Server/Rpc)는 JSON-RPC 규격만 처리하고 게임 업무를 알지 못합니다. [`Controllers`](src/SummerProject.Server/Controllers)는 외부 요청을 Service로 연결하고, [`Services`](src/SummerProject.Server/Services)는 업무 흐름을 조정합니다. SQL은 [`Repositories`](src/SummerProject.Server/Repositories), SQLite·JWT·로깅 같은 외부 기술은 [`Infrastructure`](src/SummerProject.Server/Infrastructure)에만 둡니다.

프로젝트를 기능별 `.csproj`로 나누는 대신 각 계층 아래에서 같은 기능 구분을 사용합니다.

```text
Controllers/Auth          Services/Auth          Repositories/Auth
Controllers/Characters    Services/Characters    Repositories/Characters
Controllers/Currencies    Services/Currencies    Repositories/Currencies
Controllers/Stages        Services/Stages        Repositories/Stages
Controllers/Rooms         Services/Rooms         Repositories/Rooms
```

이 구조로 배포 단위는 하나로 유지하면서 기능 간 경계를 코드에서 확인할 수 있습니다.

---

## 3. 서버 소스 구조

| 위치 | 책임 |
|---|---|
| [`Program.cs`](src/SummerProject.Server/Program.cs) | 애플리케이션 생성과 시작 |
| [`Bootstrap`](src/SummerProject.Server/Bootstrap) | 설정 검증, DI 등록, 초기화 순서, 엔드포인트 연결 |
| [`Rpc`](src/SummerProject.Server/Rpc) | JSON-RPC 요청·응답 처리 |
| [`Controllers`](src/SummerProject.Server/Controllers) | RPC 메서드별 Handler |
| [`Services`](src/SummerProject.Server/Services) | 기능별 업무 규칙과 처리 흐름 |
| [`Repositories`](src/SummerProject.Server/Repositories) | Dapper SQL, 조건부 갱신, 트랜잭션 |
| [`Helpers`](src/SummerProject.Server/Helpers) | 역할이 명확한 생성·계산·검증·직렬화 타입 |
| [`Exceptions`](src/SummerProject.Server/Exceptions) | 예상 가능한 기능별 업무 실패 |
| [`Models`](src/SummerProject.Server/Models) | DTO, DB 행, 값 객체와 열거형 |
| [`GameData/Catalogs`](src/SummerProject.Server/GameData/Catalogs) | 맵·스테이지 JSON 검증과 읽기 전용 조회 |
| [`Infrastructure`](src/SummerProject.Server/Infrastructure) | SQLite, 보안, 구조화 로그 |

### Bootstrap과 Rpc

[`Bootstrap`](src/SummerProject.Server/Bootstrap)은 서버가 어떤 순서로 준비되는지를 담당합니다. 설정과 정적 Catalog를 검증하고 SQLite 마이그레이션을 적용한 뒤 `/rpc`와 `/health`를 연결합니다. 업무 규칙은 포함하지 않습니다.

[`Rpc`](src/SummerProject.Server/Rpc)는 다음 네 영역으로 나뉩니다.

| 하위 폴더 | 역할 |
|---|---|
| [`Contracts`](src/SummerProject.Server/Rpc/Contracts) | JSON-RPC 봉투, ID 값 객체, 오류 Packet |
| [`Validation`](src/SummerProject.Server/Rpc/Validation) | JSON 구조 검증과 params 바인딩 |
| [`Dispatching`](src/SummerProject.Server/Rpc/Dispatching) | Registry 조회, Handler 호출, 예외 변환 |
| [`Serialization`](src/SummerProject.Server/Rpc/Serialization) | result 또는 error 응답 작성 |

이 계층은 `id` 생략과 `id: null`을 구분하고 요청 ID의 JSON 타입을 응답까지 보존합니다.

### Controllers, Services, Repositories

[`Controllers`](src/SummerProject.Server/Controllers)는 ASP.NET MVC Controller가 아니라 [`IRpcMethodHandler<TRequest, TResponse>`](src/SummerProject.Server/Rpc/Dispatching/IRpcMethodHandler.cs) 구현을 보관합니다. Handler는 DTO를 Service 호출로 연결하며 HTTP 객체, 업무 규칙과 SQL을 직접 다루지 않습니다.

[`Services`](src/SummerProject.Server/Services)는 여러 처리의 순서와 상태 규칙을 조정합니다. JSON-RPC 봉투를 알지 못하고 SQL도 보유하지 않습니다.

[`Repositories`](src/SummerProject.Server/Repositories)는 Dapper를 사용해 SQLite에 접근합니다. DTO 대신 [`Models/Datas`](src/SummerProject.Server/Models/Datas)의 Model을 사용하고 모든 값을 매개변수로 전달합니다. 여러 테이블을 함께 변경할 때는 같은 `DbConnection`과 `DbTransaction`을 공유합니다.

### Helpers, Exceptions, Models

[`Helpers`](src/SummerProject.Server/Helpers)에는 `Factory`, `Generator`, `Calculator`, `Validator`, `Serializer`처럼 이름과 책임이 분명한 타입만 둡니다. [`Exceptions`](src/SummerProject.Server/Exceptions)는 Service와 Repository에서 발생하는 업무 실패를 기능별로 구분하고, Rpc 계층이 이를 오류 code와 key로 변환합니다.

[`Models`](src/SummerProject.Server/Models)는 외부 계약과 DB 표현을 분리합니다.

| 위치 | 역할 | 접미사 |
|---|---|---|
| [`Models/DTOs`](src/SummerProject.Server/Models/DTOs) | RPC 입력 | `Request` |
| [`Models/DTOs`](src/SummerProject.Server/Models/DTOs) | RPC 출력 | `Response` |
| [`Models/DTOs`](src/SummerProject.Server/Models/DTOs) | DTO 구성 요소 | `Packet` |
| [`Models/Datas`](src/SummerProject.Server/Models/Datas) | Dapper DB 행 | `Model` |
| [`Models/GameData`](src/SummerProject.Server/Models/GameData) | 검증된 정적 값 | `Proto` |
| [`Models`](src/SummerProject.Server/Models) 아래 기능 폴더 | 내부 값 객체 | `Proto` |

DB Model을 JSON-RPC 응답으로 직접 반환하지 않고, DTO가 DB Model에 의존하지 않도록 유지합니다.

### GameData와 Infrastructure

[`GameData/Catalogs`](src/SummerProject.Server/GameData/Catalogs)는 배포된 맵·스테이지 JSON을 읽고 검증된 Proto로 변환합니다. 성공한 데이터는 ID 기반 읽기 전용 Catalog에 저장하며 검증 실패 시 서버 시작을 중단합니다.

[`Infrastructure/Database`](src/SummerProject.Server/Infrastructure/Database)는 SQLite 연결, 마이그레이션과 health check를 담당합니다. 실제 기능 SQL은 [`Repositories`](src/SummerProject.Server/Repositories)에 둡니다. [`Infrastructure/Security`](src/SummerProject.Server/Infrastructure/Security)는 JWT와 Google 인증, 호출자 문맥을 담당하고, [`Infrastructure/Logging`](src/SummerProject.Server/Infrastructure/Logging)은 ZLogger와 민감정보 필터를 구성합니다.

---

## 4. 요청 처리와 의존 방향

```text
HTTP Request
   ▼
JsonRpcEndpoint
   ▼
JsonRpcRequestParser
   ▼
JsonRpcRequestProcessor
   ▼
JsonRpcDispatcher
   ▼
IRpcMethodHandler
   ▼
Service
   ▼
Repository / Helper / Catalog
   ▼
JsonRpcResponseWriter
```

[`JsonRpcEndpoint`](src/SummerProject.Server/Bootstrap/JsonRpcEndpoint.cs)가 HTTP 규칙과 본문 크기를 확인하고, [`JsonRpcRequestParser`](src/SummerProject.Server/Rpc/Validation/JsonRpcRequestParser.cs)가 단일 요청·알림·배치를 구분합니다. [`JsonRpcRequestProcessor`](src/SummerProject.Server/Rpc/Dispatching/JsonRpcRequestProcessor.cs)는 배치 요소를 독립적으로 처리하고 [`JsonRpcDispatcher`](src/SummerProject.Server/Rpc/Dispatching/JsonRpcDispatcher.cs)는 대소문자를 구분하는 Registry에서 Handler를 찾습니다.

Handler 아래로는 HTTP와 JSON-RPC 봉투가 전달되지 않습니다. Service와 Repository의 처리 결과는 Handler에서 Response DTO로 바뀌고, Response Writer가 요청 ID를 포함한 최종 JSON을 작성합니다.

의존 방향은 위에서 아래로만 흐릅니다.

```text
Controllers ─► Services ─► Repositories
     │              │             │
     ▼              ▼             ▼
Models/DTOs   Helpers/Models   Models/Datas
                                  │
                                  ▼
                         Infrastructure/Database
```

Repository가 Response DTO를 만들거나 Service가 JSON-RPC Error Packet을 반환하는 역방향 의존은 허용하지 않습니다.

---

## 5. 테스트와 문서 구조

### 테스트

```text
tests/SummerProject.Server.Tests
├─ Auth / Characters / Currencies / Stages / Rooms
├─ Gameplay
├─ Rpc
├─ GameData/Catalogs
└─ Infrastructure
   ├─ Configuration
   ├─ Database
   └─ Logging
```

테스트의 [`Rpc`](tests/SummerProject.Server.Tests/Rpc)는 가짜 Handler로 프로토콜 자체를 검증하고, [`Auth`](tests/SummerProject.Server.Tests/Auth), [`Characters`](tests/SummerProject.Server.Tests/Characters), [`Currencies`](tests/SummerProject.Server.Tests/Currencies), [`Stages`](tests/SummerProject.Server.Tests/Stages), [`Rooms`](tests/SummerProject.Server.Tests/Rooms)는 TestServer에서 `POST /rpc`부터 SQLite까지 전체 경로를 확인합니다. DB 테스트는 in-memory 대체 DB가 아니라 테스트마다 별도의 임시 SQLite 파일을 사용합니다.

[`Gameplay`](tests/SummerProject.Server.Tests/Gameplay)은 여러 기능이 같은 트랜잭션을 공유하는 경계를 검증하고, [`GameData/Catalogs`](tests/SummerProject.Server.Tests/GameData/Catalogs)는 테스트용 JSON을 구성해 정상 적재와 시작 실패를 확인합니다.

### 문서

| 영역 | 내용 |
|---|---|
| [`requirements`](docs/requirements) | 기능·비기능 요구사항, 용어, 유스케이스 |
| [`contracts`](docs/contracts) | JSON-RPC, RPC 메서드, 오류 계약 |
| [`architecture`](docs/architecture) | 구조, 이름, 데이터 모델, 보안, ADR |
| [`engineering`](docs/engineering) | 구현 계획, 테스트, 주석, 커밋 규칙 |
| [`operations`](docs/operations) | 설정, 실행, 백업과 복구 |
| [`migration`](docs/migration) | 기존 동작, 차이 분석, 추적성 |
| [`templates`](docs/templates) | 요구사항, ADR, AI 작업 형식 |

[docs/README.md](docs/README.md)가 전체 문서의 읽기 순서와 변경 규칙을 안내합니다.

---

## 6. 이름과 주석 규칙

타입은 역할이 드러나는 이름을 사용합니다. Handler, Service, Repository, Factory, Validator, Options, Exception 같은 접미사는 실제 책임과 일치할 때만 사용합니다.

범용 `Manager`, `Helper`, `Util`, `Info`, `Data` 타입은 만들지 않습니다. [`Helpers`](src/SummerProject.Server/Helpers) 폴더 안에서도 구체적인 역할이 타입 이름에 나타나야 합니다.

코드 식별자는 영어, 설명 주석은 한국어를 사용합니다. 주석은 코드를 번역하지 않고 구조가 필요한 이유, JSON-RPC 불변 조건, 동시성, 트랜잭션과 보안 제약을 설명합니다.

---

## 7. AI를 사용한 과정

먼저 AI에게 기존 `SummerLoginServer`, `SummerGameServer`, `Persistence`의 Controller, Service, Entity, 마이그레이션, 설정과 배포 파일을 분석시켰습니다. 기존 구조를 복사하지 않고 외부에서 확인되는 동작과 데이터 규칙만 추출하게 했으며, 결과를 [AS_IS_BEHAVIOR_INVENTORY.md](docs/migration/AS_IS_BEHAVIOR_INVENTORY.md)와 [GAP_ANALYSIS.md](docs/migration/GAP_ANALYSIS.md)에 정리했습니다.

분석 결과를 바로 코드로 만들지 않고 요구사항, 용어집, 유스케이스, JSON-RPC 계약, RPC·오류 카탈로그를 먼저 작성하게 했습니다. 이후 목표 구조와 데이터 모델을 설계하고, 중요한 기술 선택은 ADR로 분리했습니다.

구현은 한 번에 맡기지 않고 저장소 기반, JSON-RPC, 설정·로그, SQLite, 정적 Catalog, 인증, 캐릭터·재화, 스테이지, 사용자 방 순서로 나누었습니다. 각 작업마다 관련 요구사항 ID, RPC, 수정 가능한 폴더, 범위 밖의 내용과 필수 테스트를 함께 전달했습니다.

테스트가 실패했을 때는 즉시 우회 코드를 작성하게 하지 않고 같은 조건을 재현한 뒤 실패한 계층과 원인을 확인하게 했습니다. 수정 후에는 관련 테스트뿐 아니라 프로토콜, SQLite 제약과 추적성 문서까지 다시 대조했습니다.

마지막으로 추가된 코드와 테스트를 요구사항 추적성 표에 연결하고, 주석은 한국어 주석 기준에 맞춰 정리했습니다. 커밋 메시지는 작업 대화가 아니라 실제 스테이징된 diff만 분석해 작성하도록 했습니다.

---

## 8. AI 작업 지시 문서

AI 작업 규칙은 대화에만 두지 않고 Markdown 문서로 관리했습니다.

| 문서 | AI에게 지시하는 내용 |
|---|---|
| [AGENTS.md](AGENTS.md) | 문서 우선순위, 기술 제약, 폴더 책임, 보안과 완료 조건 |
| [docs/README.md](docs/README.md) | 작업 전에 읽을 문서의 순서와 변경 규칙 |
| [AI_TASK_TEMPLATE.md](docs/templates/AI_TASK_TEMPLATE.md) | 목표, 범위, 요구사항 ID, 필수 테스트, 완료 조건 작성 형식 |
| [IMPLEMENTATION_PLAN.md](docs/engineering/IMPLEMENTATION_PLAN.md) | 구현 Phase와 단계별 선행 조건 |
| [TEST_STRATEGY.md](docs/engineering/TEST_STRATEGY.md) | 프로토콜, 기능, SQLite, 동시성 테스트 기준 |
| [COMMENT_GUIDE.md](docs/engineering/COMMENT_GUIDE.md) | 한국어 주석에 남길 이유와 불변 조건 |
| [COMMIT_GUIDE.md](docs/engineering/COMMIT_GUIDE.md) | 스테이징된 diff 기반 한국어 커밋 메시지 규칙 |
| [TRACEABILITY.md](docs/migration/TRACEABILITY.md) | 요구사항과 구현·테스트의 연결 및 완료 판단 |
| [CONTRIBUTING.md](CONTRIBUTING.md) | 문서 확인부터 구현, 검증, 추적성 갱신까지의 작업 절차 |

[AGENTS.md](AGENTS.md)가 저장소 전체 규칙을 제공하고, [docs/README.md](docs/README.md)가 읽을 문서를 안내합니다. 개별 작업은 [AI_TASK_TEMPLATE.md](docs/templates/AI_TASK_TEMPLATE.md) 형식으로 범위를 제한하며, 구현 후에는 [TEST_STRATEGY.md](docs/engineering/TEST_STRATEGY.md)와 [TRACEABILITY.md](docs/migration/TRACEABILITY.md)를 기준으로 완료 여부를 확인합니다. 주석과 커밋 메시지는 각각 별도 가이드를 적용해 매 작업에서 같은 기준을 반복하도록 구성했습니다.
