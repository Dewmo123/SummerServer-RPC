# 구현 계획

## 원칙

- 기존 코드는 동작 확인에만 사용하고 새 구현은 문서 기준선만 따라 작성합니다.
- 프로토콜 코어를 먼저 완성한 뒤 기능을 한 영역씩 수직 구현합니다.
- 각 단계는 코드, 테스트, 문서 추적성을 함께 완료해야 다음 단계로 넘어갑니다.
- 새 구현이 동작할 때까지 기존 서버를 삭제하거나 이동하지 않습니다.
- JSON-RPC Handler는 `Controllers`, 업무 Service와 Repository는 `Services`, DTO와 DB 행 Model은 각각 `Models/DTOs`, `Models/Datas`에 둡니다.
- 맵·스테이지 정적 파일과 Catalog는 `GameData/Catalogs`에 두며 새 `Features`, `Content` 폴더를 만들지 않습니다.

## Phase 0. 저장소 기반 정리

작업:

- `global.json`으로 .NET 10 SDK 정책 고정
- `Directory.Packages.props`로 패키지 버전 중앙 관리
- `packages.lock.json` 생성과 locked restore 적용
- `src/SummerProject.Server` Web 프로젝트 생성
- `tests/SummerProject.Server.Tests` xUnit 프로젝트 생성
- Dapper, Microsoft.Data.Sqlite, ZLogger 추가
- EF Core, MySQL, Newtonsoft.Json은 목표 프로젝트에 추가하지 않음
- `.db`, `.db-wal`, `.db-shm`, 로그 경로 gitignore 확인

완료 조건:

- 빈 목표 서버와 테스트 프로젝트가 `dotnet build`, `dotnet test`에 성공함
- 프로덕션 `.csproj`가 하나뿐임
- CI가 restore/build/test를 실행함

## Phase 1. JSON-RPC 프로토콜 코어

작업:

- `POST /rpc` 엔드포인트
- 단일/배치 JSON 파서
- `jsonrpc`, method, params, id 구조 검증
- id 속성 존재 여부와 JSON 타입 보존
- 메서드 Registry와 제네릭 Handler 계약
- Object/Array params 바인딩
- 성공/오류 응답 직렬화
- 알림 응답 억제
- 오류 매핑과 traceId
- 요청 크기, JSON 깊이, 배치 크기 제한

완료 조건:

- [JSON-RPC 계약](../contracts/JSON_RPC_CONTRACT.md)의 적합성 테스트 전체 통과
- `id` 누락과 `id: null`의 차이를 테스트로 보장
- 빈 배치, 잘못된 배치 요소, 전체 알림 배치를 테스트로 보장
- result와 error가 함께 직렬화될 수 없음

## Phase 2. 관측성과 설정

작업:

- ZLogger JSON 콘솔 또는 파일 출력 설정
- traceId, rpcId, method, userId, duration, outcome, errorCode 필드
- 민감정보 로그 필터
- `JsonRpcOptions`, `DatabaseOptions`, `JwtOptions`, `RefreshTokenOptions`, `GoogleAuthOptions` 바인딩과 시작 시 검증
- `/health` 기본 엔드포인트

완료 조건:

- 토큰 또는 Authorization 헤더가 로그에 포함되지 않는 자동 테스트
- 필수 설정 누락 시 서버가 명확한 한국어 운영 로그와 함께 시작 실패

## Phase 3. SQLite 기반

작업:

- `SqliteConnectionFactory`
- foreign keys, busy timeout, WAL 초기화
- SQL 마이그레이션 Runner와 체크섬
- `0001_initial.sql`
- DB health check
- 임시 SQLite 통합 테스트 fixture

완료 조건:

- 빈 DB에서 전체 스키마 생성
- 같은 마이그레이션 재시작 안전
- 체크섬 변경 탐지
- 모든 외래 키, 유일, 체크 제약 테스트

## Phase 4. 정적 카탈로그

작업:

- `GameData/Catalogs/Maps`, `GameData/Catalogs/Stages` 경로 사용
- Map/Stage JSON 스키마와 Packet/Proto 분리
- `System.Text.Json` 카탈로그 Loader
- ID 중복, 크기, 배열, 함정, 보상 검증
- 기존 Map1/Stage1 데이터를 목표 위치로 변환
- 읽기 전용 `MapCatalog`, `StageCatalog`

완료 조건:

- 올바른 카탈로그 시작 성공
- 손상·중복·범위 오류 카탈로그 시작 실패
- `stage.get` 구현 준비 완료

## Phase 5. 인증 수직 구현

순서:

1. `Models/Datas/Auth` 사용자 Model과 `Services/Auth` Repository
2. `Infrastructure/Security`의 JWT 발급·검증과 `Models/Auth/CallerProto`
3. `Controllers/Auth` Google 로그인 Handler
4. `Models/Datas/Auth` 리프레시 토큰 Model과 `Services/Auth` Repository
5. 토큰 생성·회전·재사용 탐지
6. 로그아웃
7. 개발 환경 로그인

완료 조건:

- FR-AUTH-001부터 FR-AUTH-005까지 추적성 상태 완료
- 동시 최초 로그인에 사용자 한 명
- 동시 토큰 회전에 한 요청만 성공
- 재사용 시 패밀리 전체 폐기
- Production에서 개발 로그인 Method not found

## Phase 6. 캐릭터와 재화

작업:

- 지연 생성 Repository
- 캐릭터 조회와 경험치 성장 서비스
- 재화 단건·전체 조회
- 재화 원자적 증가·차감
- JSON 응답의 재화 정렬

완료 조건:

- FR-CHAR-001, FR-CHAR-002, FR-CURRENCY-001~003 완료
- 동시 초기화에도 행 하나
- 경험치 다중 레벨 상승과 오버플로 테스트
- 잔액 음수와 오버플로 방지 테스트

## Phase 7. 스테이지

작업:

- 공개 정적 조회
- 입장 시 기존 진행 실행 포기와 새 실행 생성
- 완료 상태 선점
- 재화·경험치 보상 트랜잭션
- 완료 결과 스냅샷

완료 조건:

- FR-STAGE-001~003 완료
- 동시 입장 후 진행 실행 최대 하나
- 동시 완료 보상 한 번
- 최소 시간과 소유권 실패 시 데이터 무변경
- 보상 실패 시 전체 롤백

## Phase 8. 사용자 방

작업:

- 함정 Packet을 검증된 도메인 Proto로 변환
- 종류, 좌표, 중복, quaternion 검증
- 사용자별 방 Upsert와 조회
- 저장된 카탈로그 참조 무결성 오류

완료 조건:

- FR-ROOM-001~002 완료
- 잘못된 요청에서 기존 방 불변
- 동시 Upsert 후 사용자당 행 하나
- 최대 요청 크기와 최대 함정 수 테스트

## Phase 9. 통합과 운영 전환

작업:

- 전체 RPC `.http` 예제 또는 계약 fixture
- CI restore/build/test/format
- 단일 publish와 배포 워크플로
- SQLite 파일 디렉터리, 백업, 복구 스크립트 운영 검증
- README 빠른 시작 최종 갱신
- 기존 서버와 목표 서버의 기능 대조 테스트

완료 조건:

- 모든 추적성 행 완료
- Release build와 publish 성공
- 새 DB에서 smoke test 성공
- 백업 복구 리허설 성공
- 비밀값과 운영 데이터가 Git에 없음

## Phase 10. 레거시 제거

선행 조건:

- 신규 서버가 모든 확정 요구사항을 충족함
- 클라이언트가 JSON-RPC 계약으로 전환됨
- 운영 데이터 이전 또는 초기화 결정이 완료됨
- 롤백 기준과 보존 태그가 있음

작업:

- 기존 분리 서버와 Persistence 프로젝트 제거
- EF Core/MySQL/Newtonsoft 패키지 제거
- 기존 마이그레이션을 별도 보관 태그 또는 문서로 보존
- 이중 배포 워크플로를 단일 배포로 교체

## 작업 단위 템플릿

```markdown
### 작업: FR-STAGE-003 스테이지 완료

- 읽을 문서:
  - APP_REQUIREMENTS.md#fr-stage-003
  - RPC_METHOD_CATALOG.md#stagecomplete
  - DATA_MODEL.md#stage_runs
- 구현:
  - `Controllers/Stages/CompleteStageHandler`
  - `Services/Stages/StageRunRepository` 조건부 완료
  - 보상 트랜잭션 조정
- 테스트:
  - 정상 완료
  - 존재하지 않는 실행
  - 다른 사용자 소유
  - 최소 시간 미충족
  - 동시 완료
  - 보상 실패 롤백
- 문서:
  - TRACEABILITY 상태 갱신
  - 계약 변경이 있으면 예제 갱신
```
