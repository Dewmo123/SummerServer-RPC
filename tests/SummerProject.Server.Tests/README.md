# 목표 서버 테스트 루트

이 폴더는 신규 모노리스 서버의 자동화 테스트 위치입니다. 테스트 프로젝트는 프로덕션 서버 분리에 포함되지 않으므로 별도 `.csproj`를 사용합니다.

## 구조

| 폴더 | 대상 |
|---|---|
| `Rpc` | JSON-RPC 단일·알림·배치 적합성 |
| `Controllers` | JSON-RPC Handler의 요청·응답과 서비스 연결 |
| `Services` | 인증·캐릭터·재화·스테이지·방 업무 규칙과 Repository |
| `GameData/Catalogs` | 정적 JSON 적재, 불변 변환, 범위 검증과 시작 실패 |
| `Fixtures` | 여러 테스트 영역에서 공유하는 명시적 테스트 fixture |
| `Infrastructure/Database` | 마이그레이션, 제약, SQLite 연결 |
| `Infrastructure/Configuration` | Options 바인딩과 시작 검증 |
| `Infrastructure/Logging` | ZLogger 등록, 구조화 필드, 민감정보 제외 |

테스트 정책은 [TEST_STRATEGY.md](../../docs/engineering/TEST_STRATEGY.md)를 따릅니다. DB 통합 테스트는 실제 임시 SQLite 파일과 별도 연결을 사용합니다.

현재 `Features/*`의 빈 디렉터리는 이전 목표 구조의 자리표시자입니다. 새 테스트는 추가하지 않고 후속 정리 시 제거하며, 앞으로의 기능 테스트는 `Controllers` 또는 `Services`에 배치합니다.
