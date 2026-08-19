# 프로젝트 문서 지도

이 디렉터리는 기존 소스의 구조가 아니라 제품 개념과 외부 계약을 기준으로 신규 서버를 구현하기 위한 단일 문서 기준선입니다.

## 읽는 순서

새 작업을 시작하는 사람과 AI는 다음 순서로 읽습니다.

1. [앱 요구사항](requirements/APP_REQUIREMENTS.md)
2. [도메인 용어집](requirements/DOMAIN_GLOSSARY.md)
3. [JSON-RPC 계약](contracts/JSON_RPC_CONTRACT.md)
4. 작업 대상이 포함된 [RPC 메서드 카탈로그](contracts/RPC_METHOD_CATALOG.md)
5. [아키텍처](architecture/ARCHITECTURE.md)와 관련 ADR
6. [데이터 모델](architecture/DATA_MODEL.md)
7. [구현 계획](engineering/IMPLEMENTATION_PLAN.md)과 [테스트 전략](engineering/TEST_STRATEGY.md)
8. [추적성 표](migration/TRACEABILITY.md)

## 문서 목록

### 요구사항

| 문서 | 역할 |
|---|---|
| [APP_REQUIREMENTS.md](requirements/APP_REQUIREMENTS.md) | 제품 범위, 기능·비기능 요구사항, 업무 규칙 |
| [DOMAIN_GLOSSARY.md](requirements/DOMAIN_GLOSSARY.md) | 팀이 공통으로 사용할 도메인 용어와 의미 |
| [USE_CASES.md](requirements/USE_CASES.md) | 주요 사용자 흐름과 예외 흐름 |

### 외부 계약

| 문서 | 역할 |
|---|---|
| [JSON_RPC_CONTRACT.md](contracts/JSON_RPC_CONTRACT.md) | JSON-RPC 2.0 및 HTTP 결합 규칙 |
| [RPC_METHOD_CATALOG.md](contracts/RPC_METHOD_CATALOG.md) | 메서드별 권한, params, result, 오류 |
| [ERROR_CATALOG.md](contracts/ERROR_CATALOG.md) | 프로토콜·서버·업무 오류 코드 |

### 아키텍처

| 문서 | 역할 |
|---|---|
| [ARCHITECTURE.md](architecture/ARCHITECTURE.md) | 모노리스 구조, 책임, 의존 방향 |
| [NAMING_CONVENTIONS.md](architecture/NAMING_CONVENTIONS.md) | `Proto`, `Model`, `Request`, `Response`, `Packet` 규칙 |
| [DATA_MODEL.md](architecture/DATA_MODEL.md) | SQLite 테이블, 제약, 트랜잭션 |
| [SECURITY.md](architecture/SECURITY.md) | 인증, 토큰, 비밀값, 로깅 보안 |
| [adr](architecture/adr/) | 중요한 설계 결정과 근거 |

### 개발 및 운영

| 문서 | 역할 |
|---|---|
| [IMPLEMENTATION_PLAN.md](engineering/IMPLEMENTATION_PLAN.md) | 구현 단계, 선행 조건, 완료 기준 |
| [TEST_STRATEGY.md](engineering/TEST_STRATEGY.md) | 프로토콜·업무·DB·보안 테스트 정책 |
| [COMMENT_GUIDE.md](engineering/COMMENT_GUIDE.md) | AI를 포함한 한국어 주석 작성 기준 |
| [COMMIT_GUIDE.md](engineering/COMMIT_GUIDE.md) | AI 커밋 메시지 생성 규칙 |
| [CONFIGURATION.md](operations/CONFIGURATION.md) | 설정 키, 기본값, 비밀값 주입 |
| [RUNBOOK.md](operations/RUNBOOK.md) | 실행, 상태 확인, 장애 대응, 백업 |

### 재구현 추적

| 문서 | 역할 |
|---|---|
| [AS_IS_BEHAVIOR_INVENTORY.md](migration/AS_IS_BEHAVIOR_INVENTORY.md) | 기존 서버에서 관찰된 동작 |
| [GAP_ANALYSIS.md](migration/GAP_ANALYSIS.md) | 기존 기술과 목표 기술의 차이 |
| [TRACEABILITY.md](migration/TRACEABILITY.md) | 요구사항에서 구현·테스트까지의 연결 |

### 템플릿

| 문서 | 역할 |
|---|---|
| [REQUIREMENT_TEMPLATE.md](templates/REQUIREMENT_TEMPLATE.md) | 새 기능 요구사항 작성 형식 |
| [ADR_TEMPLATE.md](templates/ADR_TEMPLATE.md) | 되돌리기 어려운 설계 결정 기록 형식 |
| [AI_TASK_TEMPLATE.md](templates/AI_TASK_TEMPLATE.md) | AI 구현 작업에 전달할 범위와 완료 조건 |

## 변경 규칙

- 외부 동작 변경: 요구사항, RPC 카탈로그, 오류 카탈로그, 테스트, 추적성 표를 갱신합니다.
- DB 변경: 데이터 모델, SQL 마이그레이션, ADR 또는 변경 근거를 갱신합니다.
- 보안 변경: 보안 문서와 위협 관련 테스트를 갱신합니다.
- 새 의존성: 아키텍처 문서와 ADR에 추가 이유와 대안을 기록합니다.
- 문서에서 확정되지 않은 사실을 구현 코드가 새로운 기준으로 만들지 않습니다.

## 상태 표기

- `확정`: 사용자 요구사항 또는 기존 동작에서 분명하게 확인됨
- `목표 결정`: 재구현을 위해 이 문서 집합에서 선택한 설계
- `미결정`: 제품 또는 운영 결정을 추가로 받아야 함
- `구현 전`: 계약은 확정되었으나 목표 코드가 없음
- `완료`: 코드와 테스트가 계약을 충족함

문서 기준선 날짜는 2026-08-19입니다.
