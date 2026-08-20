# 프로젝트 AI 작업 지침

## 목표

이 저장소는 비동기 멀티플레이 게임 서버의 기능을 JSON-RPC 2.0 기반 모노리스로 재구현한다.
기존 `SummerLoginServer`, `SummerGameServer`, `Persistence` 코드는 현행 동작을 파악하기 위한 참고 자료이며 목표 구조의 설계 기준이 아니다.
새 구현은 승인된 요구사항, RPC 계약, ADR을 기준으로 작성한다.

## 작업 시작 순서

1. `docs/README.md`에서 문서 지도를 확인한다.
2. 작업과 관련된 요구사항 ID와 RPC 메서드를 찾는다.
3. 관련 ADR과 데이터 규칙을 확인한다.
4. 구현 전에 변경 범위와 테스트 항목을 정리한다.
5. 구현, 테스트, 문서 추적성 갱신을 한 작업으로 처리한다.

## 기준 문서 우선순위

문서나 코드가 충돌하면 다음 순서로 판단한다.

1. [JSON-RPC 2.0 공식 스펙](https://www.jsonrpc.org/specification)
2. `docs/requirements/APP_REQUIREMENTS.md`
3. `docs/contracts/RPC_METHOD_CATALOG.md`
4. 승인된 `docs/architecture/adr/*.md`
5. `docs/architecture/*.md`
6. 목표 구조에 새로 작성된 코드
7. 기존 서버 코드

충돌을 임의로 해석하지 않는다. 확정할 수 없는 내용은 관련 문서의 `미결정 사항`에 기록한다.

## 목표 기술 제약

- .NET 10과 ASP.NET Core를 사용한다.
- 프로덕션 프로젝트는 `src/SummerProject.Server` 하나만 유지한다.
- 외부 RPC 진입점은 `POST /rpc` 하나로 통합한다.
- JSON 직렬화는 `System.Text.Json`을 사용한다.
- DB는 SQLite, 데이터 접근은 Dapper를 사용한다.
- 로깅은 ZLogger와 `Microsoft.Extensions.Logging` 추상화를 사용한다.
- EF Core, MySQL, Newtonsoft.Json을 새 구현에 사용하지 않는다.
- 패키지 추가나 버전 변경은 근거와 영향을 먼저 문서화한다.

## 목표 폴더 규칙

- `Rpc`: JSON-RPC 봉투, 파싱, 검증, 디스패치, 직렬화만 담당한다.
- `Controllers`: JSON-RPC 메서드 Handler와 요청 진입 조정만 담당하며 업무 규칙이나 SQL을 두지 않는다.
- `Services`: 인증, 캐릭터, 재화, 스테이지, 방의 업무 규칙과 Repository를 기능별로 구성한다.
- `Models/DTOs`: RPC `Request`, `Response`, `Packet` 타입을 보관한다.
- `Models/Datas`: Dapper가 DB 행에 매핑하는 `Model` 타입을 보관한다.
- `Models/GameData`: 검증된 맵, 스테이지, 함정 `Proto`와 정적 열거형을 보관한다.
- `Models`: 공통 값 객체와 열거형을 보관하되 DTO와 DB Model의 경계를 유지한다.
- `Infrastructure`: SQLite, Dapper, 보안, 로깅과 같은 외부 기술을 담당한다.
- `GameData/Catalogs`: 맵과 스테이지 정적 JSON 및 읽기 전용 Catalog를 보관한다.
- `Extensions`: 구체적인 대상이 드러나는 등록·변환 확장 메서드만 보관한다.
- `Common`: 둘 이상의 영역에서 의미와 규칙이 완전히 같은 최소 공통 타입만 보관한다.
- 기능 간 호출은 공개된 애플리케이션 서비스 또는 명시적인 인터페이스를 사용한다.
- 범용 `Manager`, `Helper`, `Util`, `Info` 폴더나 타입을 만들지 않는다.
- `Models/Datas`는 현재 구조의 고정 폴더명이며, 역할이 불명확한 `Data` 타입을 새로 만드는 근거로 사용하지 않는다.

## 클래스 접미사

- 값 객체(VO): `Proto`
- DAO 범주의 DB 행 매핑 객체: `Model`
- 요청 DTO: `Request`
- 응답 DTO: `Response`
- DTO 내부 구성 객체: `Packet`

요구사항의 DAO는 DB에 저장되는 데이터 표현 범주로 해석해 `Model`을 사용하고, SQL 실행 책임은 `Repository`로 분리한다.
`Proto`는 이 저장소에서 값 객체를 의미하며 Protocol Buffers를 의미하지 않는다.
접미사는 역할이 실제로 일치할 때만 사용한다. Repository, Handler, Service, Factory, Options는 해당 역할명을 사용한다.

## JSON-RPC 필수 규칙

- `jsonrpc`는 정확히 `"2.0"`이어야 한다.
- `id` 속성이 없는 요청만 알림이다. `"id": null`은 알림이 아니다.
- 알림에는 성공과 실패 모두 응답하지 않는다.
- `params`는 생략하거나 Object 또는 Array여야 한다.
- 성공 응답은 `result`, 실패 응답은 `error`만 가진다.
- 요청의 `id` 타입과 값을 응답에 보존한다.
- 메서드명과 이름 기반 파라미터는 대소문자를 구분한다.
- `rpc.` 접두사는 애플리케이션 메서드에 사용하지 않는다.
- 빈 배치와 배치 내 잘못된 요소를 스펙대로 구분한다.

## 데이터 및 보안 규칙

- SQL은 반드시 매개변수화한다. 사용자 입력을 SQL 문자열에 결합하지 않는다.
- SQLite 연결마다 외래 키를 활성화한다.
- 업무 상태를 여러 테이블에 변경할 때 명시적인 트랜잭션을 사용한다.
- 액세스 토큰, 리프레시 토큰, Google ID 토큰, 서명 키, 원문 요청 본문을 로그에 남기지 않는다.
- 리프레시 토큰은 SHA-256 해시만 저장하고 회전과 재사용 탐지를 유지한다.
- 개발 로그인 기능은 Development 환경 밖에서 노출하지 않는다.

## 코드와 한국어 주석

- 새 주석과 수정한 주석은 한국어로 작성한다.
- 주석은 코드의 동작을 번역하지 않고 이유, 업무 규칙, 동시성 제약, 보안 불변 조건을 설명한다.
- 공개 API에는 계약 이해에 필요한 경우 한국어 XML 문서 주석을 작성한다.
- 확인되지 않은 의도를 추측해 주석으로 만들지 않는다.
- `AI가 작성함` 같은 생성 도구 표시는 코드와 문서에 남기지 않는다.

## 테스트 및 완료 조건

- 모든 요구사항은 정상, 경계, 실패 경로 테스트를 가진다.
- JSON-RPC 적합성 테스트를 기능 테스트보다 먼저 통과시킨다.
- SQLite 통합 테스트는 실제 임시 DB를 사용하고 테스트 후 격리한다.
- 완료 전 `dotnet build`, `dotnet test`, 필요 시 `dotnet format --verify-no-changes`를 실행한다.
- 관련 요구사항, RPC 카탈로그, 오류 카탈로그, 추적성 표를 함께 갱신한다.
- 비밀값, `.db`, `.db-wal`, `.db-shm`, 로그, 빌드 산출물을 커밋하지 않는다.

## 커밋 메시지

스테이징된 diff만 근거로 Conventional Commits 형식의 한국어 메시지를 작성한다.

예시:

```text
feat(stage): 스테이지 완료 보상 트랜잭션 구현

- 완료 상태 선점 후 재화와 경험치 지급
- 중복 완료와 최소 클리어 시간 테스트 추가

Refs: FR-STAGE-003
```
