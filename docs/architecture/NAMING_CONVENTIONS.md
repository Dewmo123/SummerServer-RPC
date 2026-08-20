# 네이밍 규칙

## 1. 기본 언어

- 코드 식별자는 영어를 사용합니다.
- 설명, XML 문서 주석, 일반 주석은 한국어를 사용합니다.
- JSON 속성은 lower camelCase, C# 공개 멤버는 PascalCase입니다.
- 약어를 단어처럼 다룹니다: `JsonRpc`, `Jwt`, `Sqlite`, `GoogleIdToken`.

## 2. 폴더 배치

| 폴더 | 허용 역할 |
|---|---|
| `Controllers/<기능>` | `IRpcMethodHandler`를 구현하는 JSON-RPC Handler |
| `Services/<기능>` | 업무 Service, Repository, Validator, Factory |
| `Models/DTOs/<기능>` | Request, Response, Packet |
| `Models/Datas/<기능>` | Dapper DB 행 매핑 Model |
| `Models/GameData` | 검증된 Map, Stage, Trap Proto와 정적 열거형 |
| `Models/<기능>` | Proto, 열거형과 내부 불변값 |
| `GameData/Catalogs/Maps` | 맵 JSON, Map Catalog와 검증 타입 |
| `GameData/Catalogs/Stages` | 스테이지 JSON, Stage Catalog와 검증 타입 |
| `Infrastructure` | Database, Logging, Security 외부 기술 구현 |
| `Rpc` | JSON-RPC 규격 처리 전용 타입 |
| `Extensions` | 대상이 명확한 등록·변환 확장 메서드 |
| `Common` | 둘 이상의 영역에서 의미가 완전히 같은 최소 공통 타입 |

업무 기능 하위 폴더는 `Auth`, `Characters`, `Currencies`, `Stages`, `Rooms`를 사용하고 정적 카탈로그 값은 `Models/GameData`에 둡니다. `Models/Datas`는 현재 디렉터리 이름일 뿐이며 타입 접미사는 계속 `Model`을 사용합니다.

## 3. 필수 접미사

| 역할 | 접미사 | 예시 | 금지 예시 |
|---|---|---|---|
| 값 객체 | `Proto` | `UserIdProto`, `CallerProto` | `UserIdVO`, `CallerData` |
| DAO 범주의 DB 행 매핑 | `Model` | `UserModel`, `StageRunModel` | `UserEntity`, `UserDAO` |
| RPC params | `Request` | `EnterStageRequest` | `EnterStageDto` |
| RPC result | `Response` | `EnterStageResponse` | `EnterStageResultDto` |
| DTO 구성 요소 | `Packet` | `StagePacket`, `TrapPacket` | `StageInfo`, `TrapData` |

C# 식별자에는 하이픈을 사용할 수 없으므로 요구사항의 `-Proto`는 `UserIdProto`처럼 접미사로 적용합니다.

`Proto`는 이 프로젝트에서 Value Object를 뜻합니다. Protocol Buffers 생성 타입과 혼동할 수 있으므로 Protobuf를 도입할 경우 생성 타입 namespace를 완전히 분리해야 합니다.

요구사항의 `DAO: -Model`은 DB에 저장되는 데이터 표현 타입을 `Model`로 명명한다는 뜻으로 적용합니다. SQL 실행 책임까지 `Model`에 넣지 않고 `Repository`에 둡니다. 즉 `UserModel`은 행 데이터이고 `UserRepository`가 조회·저장을 수행합니다.

## 4. 역할별 이름

| 역할 | 형식 | 예시 |
|---|---|---|
| JSON-RPC Handler | `<동사><대상>Handler` | `EnterStageHandler` |
| Repository | `<집합>Repository` | `StageRunRepository` |
| 정적 카탈로그 | `<대상>Catalog` | `StageCatalog` |
| 업무 서비스 | 명확한 업무명 + `Service` | `RefreshTokenService` |
| 생성 책임 | `<대상>Factory` | `SqliteConnectionFactory` |
| 검증 | `<대상>Validator` | `RoomLayoutValidator` |
| 설정 | `<영역>Options` | `JsonRpcOptions`, `JwtOptions` |
| 예외 | `<원인>Exception` | `CatalogValidationException` |

`Service`도 기능 이름만 붙이지 않고 책임을 구체화합니다. 예를 들어 범용 `GameService`는 금지합니다.

## 5. 기능별 계약 이름

### 인증

```text
GoogleLoginRequest
GoogleLoginResponse
DevelopmentLoginRequest
DevelopmentLoginResponse
RefreshTokenRequest
RefreshTokenResponse
LogoutRequest
LogoutResponse
TokenPairPacket
CallerProto
UserModel
RefreshTokenModel
```

### 캐릭터

```text
GetMyCharacterRequest
GetMyCharacterResponse
CharacterPacket
CharacterModel
CharacterRepository
CharacterProgressionService
```

### 재화

```text
GetMyCurrencyRequest
GetMyCurrencyResponse
ListMyCurrenciesRequest
ListMyCurrenciesResponse
CurrencyPacket
CurrencyTypeProto
CurrencyModel
CurrencyRepository
```

### 스테이지

```text
GetStageRequest
GetStageResponse
EnterStageRequest
EnterStageResponse
CompleteStageRequest
CompleteStageResponse
StagePacket
StageRunStatusProto
StageRunModel
StageRunRepository
StageCatalog
```

### 사용자 방

```text
UpsertMyRoomRequest
UpsertMyRoomResponse
GetMyRoomRequest
GetMyRoomResponse
RoomPacket
MapPacket
TrapPacket
PositionPacket
RotationPacket
RoomLayoutProto
UserRoomModel
UserRoomRepository
RoomLayoutValidator
```

외부 위치와 회전은 DTO 부품이므로 `Packet`입니다. 검증이 끝난 내부 불변값이 별도로 필요하면 `GridPositionProto`, `NormalizedRotationProto`로 변환합니다.

## 6. JSON-RPC 코어 이름

```text
JsonRpcRequest
JsonRpcResponse<TResponse>
JsonRpcErrorPacket
JsonRpcIdProto
JsonRpcDispatcher
JsonRpcMethodRegistry
JsonRpcRequestParser
JsonRpcExceptionMapper
JsonRpcOptions
```

JSON 필드명 `error.data`의 C# 타입은 DTO 부품이므로 `JsonRpcErrorDataPacket`을 사용합니다.

## 7. DB 이름

- 테이블과 컬럼은 `snake_case` 복수형 테이블을 사용합니다.
- 기본 키는 `id`, 외래 키는 `<대상>_id`입니다.
- 시간 컬럼은 의미를 포함합니다: `created_at_utc_ms`, `expires_at_utc_ms`.
- 인덱스는 `ix_<table>_<columns>`, 유일 인덱스는 `ux_<table>_<columns>`입니다.
- 체크 제약은 `ck_<table>_<rule>`, 외래 키는 `fk_<table>_<target>`입니다.

Dapper SQL은 `user_id AS UserId`처럼 C# 프로퍼티에 명시적으로 alias를 부여합니다. 전역 underscore 매핑 설정에 암묵적으로 의존하지 않습니다.

## 8. 메서드명

JSON-RPC 메서드는 `<영역>.<동사 또는 동작>` 형태의 lower camelCase를 사용합니다.

```text
auth.login.google
auth.token.refresh
character.getMine
currency.listMine
stage.complete
room.upsertMine
```

- 이미 공개된 메서드명을 코드 타입 이름에 맞추려고 변경하지 않습니다.
- `rpc.` 접두사는 예약되어 있으므로 사용하지 않습니다.
- 메서드명에 버전을 넣지 않습니다. 호환되지 않는 변경은 별도의 버전 정책 ADR에서 다룹니다.

## 9. 금지 이름

다음 이름은 역할이 불명확하므로 사용하지 않습니다.

```text
Data
Info
Manager
Helper
Util
CommonService
BaseDto
GenericRepository
RequestModel
ResponseModel
```

`Models/Datas`는 현재 폴더 구조를 유지하기 위한 예외입니다. 새 타입 이름으로 `Data`를 사용해서는 안 됩니다. 외부 라이브러리나 규격에서 고정한 이름은 예외이며 래퍼에서 프로젝트 규칙으로 변환합니다.
