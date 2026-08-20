# 목표 아키텍처

## 1. 아키텍처 요약

서버는 하나의 ASP.NET Core 프로세스와 하나의 프로덕션 `.csproj`로 구성된 모듈형 모노리스입니다. 외부 게임 기능은 `POST /rpc`의 JSON-RPC 메서드로 제공하고, 상태 확인만 `GET /health` HTTP 엔드포인트로 분리합니다.

```text
게임 클라이언트
      │ HTTP + JSON-RPC 2.0
      ▼
┌───────────────────────────────────────────────────┐
│ SummerProject.Server                              │
│  Rpc Parsing → Validation → Dispatch              │
│                         │                         │
│                    Controllers                    │
│                         │                         │
│                      Services                     │
│          ┌──────────────┼──────────────┐           │
│       Models        GameData      Infrastructure  │
│                                      │            │
│                                   SQLite          │
└───────────────────────────────────────────────────┘
```

결정 근거는 [ADR-0001](adr/0001-modular-monolith.md), [ADR-0002](adr/0002-json-rpc-over-http.md), [ADR-0003](adr/0003-sqlite-dapper.md)을 참조합니다.

## 2. 프로젝트 경계

```text
src/SummerProject.Server/SummerProject.Server.csproj
tests/SummerProject.Server.Tests/SummerProject.Server.Tests.csproj
```

- 프로덕션 프로젝트 분리는 금지합니다.
- 테스트 프로젝트는 배포 산출물이 아니므로 별도로 둡니다.
- 기능 경계는 폴더, namespace, 내부 접근 제한, 코드 리뷰 규칙으로 유지합니다.
- 기존 `SummerLoginServer`, `SummerGameServer`, `Persistence`는 새 구현 완료 후 제거 대상입니다.

## 3. 목표 폴더

```text
src/SummerProject.Server
├─ Program.cs
├─ Bootstrap
│  ├─ ServiceRegistration.cs
│  └─ EndpointRegistration.cs
├─ Common
├─ Controllers
├─ Extensions
├─ GameData
│  └─ Catalogs
│     ├─ Maps
│     └─ Stages
├─ Infrastructure
│  ├─ Database
│  │  └─ Migrations
│  ├─ Logging
│  └─ Security
├─ Models
│  ├─ Datas
│  ├─ DTOs
│  └─ GameData
├─ Properties
├─ Rpc
│  ├─ Contracts
│  ├─ Dispatching
│  ├─ Serialization
│  └─ Validation
└─ Services
```

인증, 캐릭터, 재화, 스테이지, 방 구분이 필요하면 `Controllers`, `Services`, `Models` 아래에 `Auth`, `Characters`, `Currencies`, `Stages`, `Rooms` 하위 폴더를 같은 이름으로 만듭니다. 빈 계층을 형식적으로 채우지 않습니다.

`Controllers`는 ASP.NET MVC Controller 집합이 아닙니다. 외부 HTTP 경로는 계속 `/rpc` 하나이며, 이 폴더에는 `IRpcMethodHandler<TRequest, TResponse>`를 구현하는 JSON-RPC Handler를 둡니다.

## 4. 책임

### Bootstrap

- 설정 바인딩과 시작 시 검증
- DI 서비스 등록
- 미들웨어와 `/rpc`, `/health` 연결
- DB 마이그레이션과 정적 카탈로그 적재 순서 제어

업무 로직을 포함하지 않습니다.

### Rpc

- 단일/배치 JSON 문서 구분
- Request Object 구조 검증
- `id` 존재 여부와 원본 타입 보존
- 메서드 등록·탐색
- params 역직렬화와 검증
- 알림 응답 억제
- 성공·오류 Object 직렬화
- 예외를 오류 카탈로그로 변환

업무 타입이나 SQLite를 직접 참조하지 않습니다.

권장 핵심 타입:

```text
JsonRpcRequest
JsonRpcResponse<T>
JsonRpcErrorPacket
JsonRpcIdProto
JsonRpcDispatcher
JsonRpcMethodRegistry
JsonRpcRequestParser
JsonRpcExceptionMapper
IRpcMethodHandler<TRequest, TResponse>
```

### Controllers

- JSON-RPC 메서드별 Handler 구현
- DTO 입력을 Service 호출로 연결
- Service 결과 또는 업무 오류를 RPC 응답 계약으로 변환
- 인증된 호출자 문맥 전달

업무 규칙, SQL, SQLite 연결, 정적 카탈로그 파싱은 포함하지 않습니다.

### Services

- Auth: Google 로그인 조정, 사용자 생성 경쟁, 리프레시 토큰 회전·폐기
- Characters: 캐릭터 지연 생성, 조회, 경험치 성장
- Currencies: 재화 지연 생성, 조회, 원자적 증가·차감
- Stages: 정적 조회, 입장·포기·완료 상태 전이, 보상 트랜잭션
- Rooms: 맵·함정 검증, 사용자 방 저장·조회
- 기능별 Repository와 명시적인 트랜잭션 조정

Service는 HTTP 타입이나 JSON-RPC 봉투를 직접 참조하지 않습니다. Repository는 Response DTO가 아니라 `Models/Datas`의 Model 또는 내부 결과를 반환합니다.

### Models

- `Models/DTOs`: 외부 RPC의 Request, Response, Packet
- `Models/Datas`: Dapper가 SQLite 행에 매핑하는 Model
- `Models/GameData`: 검증된 Map, Stage, Trap Proto와 정적 열거형
- `Models`: 값 객체 `Proto`, 열거형과 두 모델 계층에서 공유하는 명시적 계약

DTO는 DB Model을 참조하지 않으며, DB Model을 JSON-RPC 응답으로 직접 직렬화하지 않습니다.

### GameData

- `GameData/Catalogs/Maps`: 정적 맵 JSON과 읽기 전용 Map Catalog
- `GameData/Catalogs/Stages`: 정적 스테이지 JSON과 읽기 전용 Stage Catalog
- 시작 시 전체 파일 검증과 메모리 적재

### Extensions

- 대상과 역할이 이름에 드러나는 DI 등록 또는 변환 확장 메서드
- 업무 규칙이나 상태를 보관하지 않는 얇은 연결 코드

### Common

- 둘 이상의 영역에서 의미와 불변 조건이 완전히 같은 최소 공통 타입
- 미분류 코드나 범용 Helper의 임시 보관 금지

### Infrastructure/Database

- SQLite 연결 Factory
- Dapper 실행
- SQL 마이그레이션과 체크섬
- 트랜잭션 생성
- health check

업무 SQL과 Repository는 `Services`의 기능별 하위 폴더에 두고 연결·마이그레이션 공통 기반만 `Infrastructure/Database`에 둡니다.

Phase 3 구현은 SQL 파일을 프로덕션 어셈블리에 포함하고 시작 시 버전 순서대로 적용합니다. 적용 이력의 이름과 SHA-256 체크섬이 현재 파일과 다르면 요청을 받기 전에 시작을 실패시킵니다.

### Infrastructure/Security

- JWT 옵션, 발급, 검증
- 암호학적 난수와 토큰 해시
- 외부 Google 토큰 검증 Adapter

### Infrastructure/Logging

- ZLogger 등록과 JSON 형식
- 공통 로그 속성명
- 민감정보 필터링

## 5. 의존 방향

```text
Bootstrap ───────► Rpc
    │              │
    ├─────────────► Controllers ─────► Services
    │                                      │
    ├─────────────► GameData ◄─────────────┤
    └─────────────► Infrastructure ◄───────┘

Controllers ─────► Models/DTOs
Services ────────► Models, Models/Datas
GameData ────────► Models/GameData
Models/DTOs ─────► Models/GameData
```

- RPC는 `Controllers`의 Handler를 인터페이스로 호출합니다.
- Controller Handler는 HTTP 타입에 의존하지 않고 Service를 호출합니다.
- Controller Handler는 `ClaimsPrincipal` 대신 정규화된 `CallerProto`를 받습니다.
- `Models/DTOs`는 `Models/Datas`의 DB Model을 참조하지 않습니다.
- 정적 JSON 검증과 카탈로그는 `Models/GameData`의 불변 Proto만 생성합니다.
- Repository는 Response를 반환하지 않고 Model 또는 도메인 결과를 반환합니다.
- 기능 간 직접 Repository 호출은 피하고 공개된 Service를 사용합니다.

## 6. 요청 처리 흐름

1. ASP.NET Core가 요청 크기, Content-Type, 속도 제한을 확인합니다.
2. 인증 Handler가 Bearer 토큰을 검증하고 호출자 문맥을 구성합니다.
3. JSON-RPC Parser가 단일/배치와 JSON 유효성을 확인합니다.
4. Validator가 Request Object와 id, method, params 구조를 검증합니다.
5. Registry가 대소문자를 구분해 메서드를 찾습니다.
6. Dispatcher가 params를 `Models/DTOs`의 Request 타입으로 바인딩하고 Controller Handler를 호출합니다.
7. Controller Handler가 Service에 위임하고 Service가 업무 규칙과 트랜잭션을 수행합니다.
8. 결과 또는 오류를 Response Object로 변환합니다.
9. 알림이면 직렬화를 생략하고, 일반 요청이면 `id`를 보존해 응답합니다.
10. ZLogger가 메서드, 요청 ID의 안전한 표현, 소요 시간, 결과 코드를 기록합니다.

## 7. 서비스 수명

| 구성 요소 | 권장 수명 | 이유 |
|---|---|---|
| 정적 카탈로그 | Singleton | 시작 시 불변 데이터로 적재 |
| JSON 옵션/메서드 Registry | Singleton | 불변 설정과 Handler 메타데이터 |
| SQLite Connection Factory | Singleton | 연결 문자열과 생성 정책만 보유 |
| Controller Handler | Scoped 또는 Transient | 요청 문맥과 상태 격리 |
| Service/Repository | Scoped 또는 Transient | 업무 상태 격리와 명시적인 연결·트랜잭션 사용 |
| JWT 발급기 | Singleton | 검증된 불변 옵션과 자격 증명 보유 |

SQLite 연결 자체를 Singleton으로 공유하지 않습니다. 작업 또는 트랜잭션 단위로 열고 즉시 닫습니다.

## 8. 트랜잭션 경계

- 로그인 사용자 생성: 유일 제약 충돌을 정상 경쟁 결과로 처리합니다.
- 리프레시 토큰 회전: 새 토큰 삽입, 기존 토큰 조건부 사용 처리, 재사용 대응을 명시적인 트랜잭션으로 처리합니다.
- 스테이지 입장: 기존 진행 실행 포기와 새 실행 생성이 한 트랜잭션입니다.
- 스테이지 완료: 실행 완료 선점, 보상 기록, 재화, 경험치가 한 트랜잭션입니다.
- 배치 전체를 하나의 트랜잭션으로 묶지 않습니다.

## 9. 정적 카탈로그

- 맵과 스테이지는 DB가 아니라 버전 관리되는 JSON 파일입니다.
- 파일 위치는 `GameData/Catalogs/Maps`, `GameData/Catalogs/Stages`입니다.
- 시작 시 모든 파일을 읽고 ID 중복, 양수 크기, 보상 범위, 좌표, 회전값을 검증합니다.
- 하나라도 잘못되면 서버 시작을 실패시켜 손상된 콘텐츠 제공을 막습니다.
- Handler는 읽기 전용 Catalog 인터페이스만 사용합니다.

## 10. 관측성

구조화 로그 공통 필드:

| 필드 | 설명 |
|---|---|
| `traceId` | ASP.NET Core 분산 추적 ID |
| `rpcId` | 안전하게 축약한 JSON-RPC ID. 민감 가능 문자열은 해시 또는 길이 제한 |
| `rpcMethod` | 호출 메서드 |
| `userId` | 인증된 내부 사용자 ID, 없으면 생략 |
| `durationMs` | 처리 시간 |
| `outcome` | `success`, `error`, `notification` |
| `errorCode` | JSON-RPC 또는 업무 오류 코드 |

params, 토큰, Authorization 헤더, SQL 매개변수 원문은 기록하지 않습니다.

## 11. 아키텍처 금지 사항

- 기능별 프로덕션 `.csproj` 추가
- 새 `Features`, `Content` 폴더 추가
- Controller에서 업무 로직 수행
- 정적 Service Locator
- DB Model을 JSON-RPC result로 직접 직렬화
- Controller Handler에서 SQL 문자열 조립
- 배치 전체 공유 트랜잭션
- 시작 시 검증하지 않은 카탈로그 파일의 지연 파싱
- 오류 응답에 예외 메시지, SQL, 스택 추적 노출

## 12. 패키지 기준선

패키지 버전은 루트 `Directory.Packages.props`에서 중앙 관리하고 각 프로젝트의 `packages.lock.json`으로 전이 의존성까지 고정합니다. 버전을 변경할 때는 중앙 버전과 모든 잠금 파일의 차이를 함께 검토합니다.

| 패키지 | 버전 | 선택 근거와 영향 |
|---|---:|---|
| Dapper | 2.1.79 | ADR-0003의 명시적 SQL·트랜잭션 원칙에 따라 Phase 3 마이그레이션 이력과 상태 쿼리에 사용합니다. |
| Microsoft.Data.Sqlite | 10.0.11 | `net10.0`과 같은 제품군의 SQLite 공급자로, Phase 3 연결 Factory와 실제 임시 DB 통합 테스트에 사용합니다. |
| ZLogger | 2.5.10 | NFR-OBSERVABILITY-001에 따라 Phase 2에서 UTC JSON 콘솔 출력과 구조화 RPC 요약 로그를 구성했습니다. |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.11 | `net10.0` 서버와 같은 제품군의 TestServer 기반 통합 테스트를 제공합니다. |
| Microsoft.NET.Test.Sdk | 18.8.1 | xUnit 테스트 검색과 실행에 사용합니다. TestHost의 `net8.0` 자산에서 Newtonsoft.Json 전이 의존성을 제거한 버전으로 올렸으며, 잠금 파일 갱신 후 테스트 검색과 실행을 다시 검증해야 합니다. |
| xunit | 2.9.3 | 테스트 프레임워크 기준선입니다. |
| xunit.runner.visualstudio | 3.1.4 | `dotnet test`와 IDE의 xUnit 검색을 연결하며 프로덕션 산출물에는 포함하지 않습니다. |
| coverlet.collector | 6.0.4 | 추후 품질 게이트에서 커버리지 수집에 사용하며 프로덕션 산출물에는 포함하지 않습니다. |
