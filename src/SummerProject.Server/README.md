# 목표 서버 소스 루트

이 폴더는 JSON-RPC 2.0 기반 모노리스의 신규 구현 위치입니다. Phase 7까지 `POST /rpc` 프로토콜 코어, ZLogger 구조화 로그, 시작 설정 검증, SQLite 마이그레이션, 정적 카탈로그, 인증, 캐릭터·재화와 스테이지 기능을 구성했습니다. 현재 인증·캐릭터·재화 RPC와 `stage.get`, `stage.enter`, `stage.complete`를 제공하며 개발 환경과 명시 옵션이 모두 허용할 때만 `auth.login.development`를 제공합니다.

실행 시 `Jwt:SigningKey`는 32바이트 이상으로, `Google:ClientIds`는 하나 이상의 허용된 OAuth Client ID로 외부 구성해야 합니다. 저장소의 `appsettings.json`에는 비밀값과 실제 Client ID를 넣지 않습니다.

## 구조

| 폴더 | 책임 |
|---|---|
| `Bootstrap` | 설정, DI, 미들웨어, 시작 순서 |
| `Rpc/Contracts` | JSON-RPC 봉투와 오류 Packet |
| `Rpc/Dispatching` | 메서드 Registry와 Handler 호출 |
| `Rpc/Serialization` | id 보존과 result/error 직렬화 |
| `Rpc/Validation` | Request Object와 params 검증 |
| `Controllers` | 기능별 JSON-RPC Handler와 요청 진입 조정 |
| `Services` | 인증·캐릭터·재화·스테이지·방 업무 규칙과 흐름 조정 |
| `Repositories` | 기능별 Dapper SQL과 명시적인 트랜잭션 |
| `Helpers` | 기능별 Factory, Generator, Calculator와 명확한 보조 책임 |
| `Exceptions` | 예상 가능한 기능별 업무 실패 예외 |
| `Models/DTOs` | RPC Request·Response·Packet |
| `Models/Datas` | Dapper DB 행 매핑 Model |
| `Models/GameData` | 검증된 맵·스테이지·함정 Proto와 열거형 |
| `Models` | 공통 값 객체와 열거형 |
| `Infrastructure/Database` | SQLite, Dapper, 마이그레이션 |
| `Infrastructure/Logging` | ZLogger와 민감정보 필터 |
| `Infrastructure/Security` | JWT, 암호학, Google 검증 Adapter |
| `GameData/Catalogs/Maps` | 정적 맵 JSON과 Map Catalog |
| `GameData/Catalogs/Stages` | 정적 스테이지 JSON과 Stage Catalog |
| `Extensions` | 구체적인 등록·변환 확장 메서드 |
| `Common` | 두 개 이상의 기능에서 의미가 같은 최소 공통 타입 |
| `Properties` | 어셈블리·로컬 실행 설정 |

## 금지 사항

- 이 폴더 아래에 기능별 `.csproj`를 추가하지 않습니다.
- 기존 Controller, DbContext, Entity 파일을 복사하지 않습니다.
- `Controllers`에서 업무 규칙이나 SQL을 직접 실행하지 않습니다.
- `Features`, `Content` 폴더를 새 구현 위치로 만들지 않습니다.
- DB Model을 Response로 직접 반환하지 않습니다.
- `Common`을 미분류 코드의 임시 보관소로 사용하지 않습니다.

작업 전 루트 [AGENTS.md](../../AGENTS.md), [아키텍처](../../docs/architecture/ARCHITECTURE.md), [네이밍 규칙](../../docs/architecture/NAMING_CONVENTIONS.md)을 읽습니다.
