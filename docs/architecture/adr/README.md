# Architecture Decision Records

ADR은 구현 방법 자체보다 중요한 설계 선택, 대안, 결과, 재검토 조건을 기록합니다.

## 현재 결정

| ADR | 상태 | 결정 |
|---|---|---|
| [0001](0001-modular-monolith.md) | 승인 | 단일 프로덕션 프로젝트의 모듈형 모노리스 |
| [0002](0002-json-rpc-over-http.md) | 승인 | `POST /rpc` JSON-RPC 2.0 over HTTP |
| [0003](0003-sqlite-dapper.md) | 승인 | SQLite와 Dapper 기반 영속성 |
| [0004](0004-static-catalogs.md) | 승인 | 맵과 스테이지 정적 JSON 카탈로그 |

## 작성 규칙

- 파일명: `NNNN-kebab-case-title.md`
- 상태: 제안, 승인, 폐기, 대체
- 승인된 ADR의 내용을 조용히 수정하지 않습니다.
- 결정이 바뀌면 새 ADR을 만들고 이전 ADR을 `대체` 상태로 바꿉니다.
- 패키지, 프로토콜, 데이터 보존, 호환성처럼 되돌리기 어려운 선택을 기록합니다.

새 ADR은 [ADR 템플릿](../../templates/ADR_TEMPLATE.md)을 사용합니다.
