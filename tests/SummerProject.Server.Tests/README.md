# 목표 서버 테스트 루트

이 폴더는 신규 모노리스 서버의 자동화 테스트 위치입니다. 테스트 프로젝트는 프로덕션 서버 분리에 포함되지 않으므로 별도 `.csproj`를 사용합니다.

## 구조

| 폴더 | 대상 |
|---|---|
| `Rpc` | JSON-RPC 단일·알림·배치 적합성 |
| `Features/Auth` | Google/JWT/리프레시 토큰/로그아웃 |
| `Features/Characters` | 지연 생성과 경험치 성장 |
| `Features/Currencies` | 재화 무결성과 동시성 |
| `Features/Stages` | 입장, 완료 선점, 보상 롤백 |
| `Features/Rooms` | 맵·함정 검증과 Upsert |
| `Infrastructure/Database` | 마이그레이션, 제약, SQLite 연결 |

테스트 정책은 [TEST_STRATEGY.md](../../docs/engineering/TEST_STRATEGY.md)를 따릅니다. DB 통합 테스트는 실제 임시 SQLite 파일과 별도 연결을 사용합니다.
