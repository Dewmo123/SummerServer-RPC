# 테스트 전략

## 1. 목표

- JSON-RPC 2.0 규격 위반을 기능 구현보다 먼저 발견합니다.
- SQLite와 Dapper의 실제 SQL, 제약, 트랜잭션 동작을 검증합니다.
- 동시 요청으로 토큰, 보상, 재화, 방 데이터가 중복되지 않음을 증명합니다.
- 외부 계약 변경을 JSON fixture와 직렬화 테스트로 감지합니다.

## 2. 테스트 계층

| 계층 | 대상 | 외부 의존성 |
|---|---|---|
| 단위 테스트 | 값 검증, 성장 계산, 오류 매핑, 카탈로그 검증 | 없음 |
| 프로토콜 계약 테스트 | JSON-RPC 파싱·디스패치·직렬화 | TestServer, 가짜 Handler |
| Repository 통합 테스트 | Dapper SQL, 제약, 마이그레이션 | 실제 임시 SQLite |
| 기능 통합 테스트 | RPC부터 DB 결과까지 | TestServer + 임시 SQLite |
| 동시성 테스트 | 조건부 갱신, 유일 제약, 트랜잭션 | 별도 연결을 연 실제 SQLite |
| 보안 테스트 | JWT, 토큰, 권한, 로그 유출 | TestServer + 로그 수집기 |
| Smoke 테스트 | publish 산출물 실행, health, 대표 RPC | 실제 프로세스 |

In-memory 대체 DB는 사용하지 않습니다. SQLite 자체의 잠금과 제약을 검증해야 하므로 테스트마다 임시 파일 DB를 사용합니다.

테스트 배치는 프로덕션 구조를 따라 `Controllers`에는 Handler 연결 테스트, `Services`에는 업무 규칙 테스트, `Repositories`에는 SQL·트랜잭션·동시성 테스트, `GameData/Catalogs`에는 정적 JSON과 시작 검증 테스트를 둡니다. 공용 TestServer Factory와 데이터 생성기는 루트 `Fixtures`, 프로토콜 적합성은 `Rpc`, 설정·DB·로그는 `Infrastructure`에 둡니다. 기존 `Features/*` 빈 폴더에는 새 테스트를 추가하지 않습니다.

## 3. JSON-RPC 적합성 매트릭스

### Request

- 정확한 `jsonrpc: "2.0"`
- `jsonrpc` 누락, 숫자, `1.0`, 다른 문자열
- method 누락, 비문자열, 빈 문자열
- `rpc.` 예약 접두사
- params 생략, Object, Array
- params Primitive 또는 null
- id 생략, null, String, 정수, 소수 Number
- id Boolean, Object, Array
- 필드명 대소문자 불일치

### Response

- 성공에 result만 존재
- 실패에 error만 존재
- 요청 id의 값과 타입 보존
- 파싱할 수 없는 요청의 id null
- 오류 Object code 정수, message 문자열
- null result의 명시적 직렬화

### Notification

- id 없음과 id null 구분
- 성공 알림 응답 없음
- 업무 오류 알림 응답 없음
- 내부 예외 알림 응답 없음과 로그 기록

### Batch

- 정상 혼합 배치
- 빈 배열
- 비Object 요소
- 정상 요청과 잘못된 요소 혼합
- 알림 혼합 시 응답 제외
- 전체 알림 시 빈 본문
- 결과 순서를 id로 대응 가능
- 최대 배치 크기 경계와 초과

## 4. 기능별 최소 테스트

### 인증

- Google 토큰 유효/무효/audience 불일치/subject 없음
- 기존 사용자 로그인과 최초 사용자 생성
- 동시 최초 로그인에서 사용자 한 행
- JWT 필수 claim, 발급자, 대상, 만료, 서명
- 리프레시 토큰 원문이 DB와 로그에 없음
- 유효/만료/폐기/없는 리프레시 토큰
- 동시 회전에서 하나만 성공
- 사용 토큰 재제출 시 패밀리 폐기
- 로그아웃 멱등성
- Production 개발 로그인 미등록

### 캐릭터

- 최초 조회 기본 생성
- 동시 최초 조회 한 행
- 존재하지 않는 사용자
- 경험치 경계 직전/정확히 요구량/여러 레벨
- 0, 음수, 64비트 오버플로

### 재화

- 지원 재화 모두 지연 생성
- 잘못된 재화 코드
- 양수 증가와 차감
- 잔액 정확히 전부 차감
- 부족 잔액, 0/음수 변경량, 오버플로
- 동시 차감에서 잔액 음수 방지

### 스테이지

- 카탈로그 조회 성공/없음
- 입장 시 기존 진행 실행 포기
- 동시 입장 후 진행 실행 하나
- 실행 소유권, 존재, 상태 검증
- 최소 클리어 시간 직전/정확히 경과
- 동시 완료에서 완료와 보상 한 번
- 보상 실패 강제 시 전체 롤백
- 응답의 획득량과 DB 스냅샷 일치

### 사용자 방

- 유효한 맵과 빈/여러 함정
- 없는 맵과 지원하지 않는 타입
- x/y 하한·상한, z 비0
- 중복 위치
- quaternion 크기 0.98, 1.02, 경계 밖
- 함정 100개와 101개
- 요청 크기 64 KiB 경계
- Upsert가 이전 구성을 전체 교체
- 저장된 맵이 카탈로그에서 사라진 경우

## 5. 카탈로그 테스트

- 파일명 순서와 무관한 ID 기반 적재
- 빈 디렉터리
- 잘못된 JSON
- 중복 ID
- 0 이하 width/height
- tile 배열 길이 정책
- 함정 좌표와 회전
- 음수 최소 시간과 보상
- 시작 실패 메시지에 대상 파일은 포함하되 비밀 경로는 최소화

## 6. DB 테스트 격리

- 각 테스트 클래스 또는 테스트마다 고유 임시 디렉터리와 DB 파일을 사용합니다.
- WAL, SHM 파일까지 테스트 종료 후 정리합니다.
- 동시성 테스트는 같은 DB 파일에 여러 연결을 엽니다.
- 마이그레이션 테스트는 빈 DB, 부분 적용 DB, 체크섬 변조 DB를 각각 만듭니다.
- 시간은 `TimeProvider`를 주입해 결정적으로 제어합니다.
- 무작위 토큰은 테스트용 deterministic generator 또는 검증 가능한 interface로 대체하되 운영 구현은 CSPRNG를 사용합니다.

## 7. 계약 fixture

`tests/SummerProject.Server.Tests/Fixtures/Rpc`에 요청과 기대 응답 JSON을 저장할 수 있습니다.

```text
Fixtures/Rpc/
├─ valid-single-request.json
├─ invalid-empty-batch.json
├─ mixed-batch-request.json
└─ mixed-batch-response.json
```

fixture는 property 순서보다 JSON 구조와 타입을 비교합니다. 단, id의 타입과 result/error 상호 배타성은 엄격히 확인합니다.

## 8. 커버리지와 품질 게이트

초기 품질 게이트:

- JSON-RPC 적합성 목록 100% 자동화
- 확정 기능 요구사항마다 정상 테스트 1개 이상
- 각 오류 카탈로그 코드마다 발생 또는 매핑 테스트
- 동시성 요구사항마다 경쟁 테스트
- 모든 SQL 마이그레이션 빈 DB 적용 테스트
- Release build 경고 0

라인 커버리지 숫자만 완료 기준으로 사용하지 않습니다. 위험 기반 시나리오가 누락되지 않았는지를 우선합니다.

## 9. CI 표준 명령

```powershell
dotnet restore --locked-mode
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet format --verify-no-changes --no-restore
```

테스트 실패 시 배포와 AI 자동 커밋을 중단합니다.
