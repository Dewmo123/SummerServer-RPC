# ADR-0003: SQLite와 Dapper 기반 영속성

- 상태: 승인
- 날짜: 2026-08-19

## 배경

현행 구현은 MySQL과 EF Core를 사용하지만 목표 기술 스택은 SQLite와 Dapper입니다. EF 마이그레이션과 Change Tracker에 의존한 동작을 명시적인 SQL과 트랜잭션으로 재정의해야 합니다.

## 결정

- 단일 SQLite DB 파일을 사용합니다.
- Dapper와 `Microsoft.Data.Sqlite`를 사용합니다.
- SQL 파일 기반 마이그레이션 Runner를 서버 내부에 구현합니다.
- 연결마다 외래 키를 활성화하고 busy timeout을 설정합니다.
- WAL 모드를 사용합니다.
- 업무 트랜잭션은 `DbConnection`과 `DbTransaction`을 명시적으로 전달합니다.
- 동시성 선점은 유일 제약과 조건부 UPDATE의 영향 행 수로 판정합니다.

## 결과

장점:

- 배포와 로컬 실행에 외부 DB 서버가 필요하지 않습니다.
- SQL과 트랜잭션 경계가 코드에 명시적으로 드러납니다.
- 작은 단일 인스턴스 게임 서버 운영이 단순해집니다.

비용:

- EF Core의 모델 검증과 자동 마이그레이션 생성 기능을 잃습니다.
- SQLite는 쓰기를 직렬화하므로 높은 동시 쓰기 부하에 한계가 있습니다.
- MySQL 전용 SQL을 직접 변환할 수 없고 새 SQL을 작성해야 합니다.

## 재검토 조건

- 지속적인 write contention이 목표 부하를 만족하지 못함
- 다중 인스턴스가 같은 DB에 써야 함
- DB 크기, 백업 시간, 운영 가용성이 SQLite 한계를 초과함
