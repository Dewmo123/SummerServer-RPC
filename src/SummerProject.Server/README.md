# 목표 서버 소스 루트

이 폴더는 JSON-RPC 2.0 기반 모노리스의 신규 구현 위치입니다. 현재는 문서 기준선과 폴더만 생성되어 있으며 `.csproj`와 코드는 [구현 계획](../../docs/engineering/IMPLEMENTATION_PLAN.md)의 Phase 0부터 추가합니다.

## 구조

| 폴더 | 책임 |
|---|---|
| `Bootstrap` | 설정, DI, 미들웨어, 시작 순서 |
| `Rpc/Contracts` | JSON-RPC 봉투와 오류 Packet |
| `Rpc/Dispatching` | 메서드 Registry와 Handler 호출 |
| `Rpc/Serialization` | id 보존과 result/error 직렬화 |
| `Rpc/Validation` | Request Object와 params 검증 |
| `Features/Auth` | Google/JWT/리프레시 토큰/로그아웃 |
| `Features/Characters` | 캐릭터 조회와 성장 |
| `Features/Currencies` | 재화 조회와 원자적 변경 |
| `Features/Stages` | 카탈로그 조회, 입장, 완료, 보상 |
| `Features/Rooms` | 방 배치 검증과 저장 |
| `Infrastructure/Database` | SQLite, Dapper, 마이그레이션 |
| `Infrastructure/Logging` | ZLogger와 민감정보 필터 |
| `Infrastructure/Security` | JWT, 암호학, Google 검증 Adapter |
| `Content/Catalogs` | 정적 맵과 스테이지 JSON |
| `Common` | 두 개 이상의 기능에서 의미가 같은 최소 공통 타입 |

## 금지 사항

- 이 폴더 아래에 기능별 `.csproj`를 추가하지 않습니다.
- 기존 Controller, DbContext, Entity 파일을 복사하지 않습니다.
- DB Model을 Response로 직접 반환하지 않습니다.
- `Common`을 미분류 코드의 임시 보관소로 사용하지 않습니다.

작업 전 루트 [AGENTS.md](../../AGENTS.md), [아키텍처](../../docs/architecture/ARCHITECTURE.md), [네이밍 규칙](../../docs/architecture/NAMING_CONVENTIONS.md)을 읽습니다.
