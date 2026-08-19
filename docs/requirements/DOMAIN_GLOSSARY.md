# 도메인 용어집

## 목적

기존 클래스명과 폴더명 대신 제품 개념을 기준으로 대화하고 구현하기 위한 용어 정의입니다. 문서와 코드에서 같은 개념에 다른 이름을 만들지 않습니다.

| 용어 | 영문/코드 표현 | 정의 |
|---|---|---|
| 사용자 | User | 외부 인증 제공자 계정과 연결된 서버 계정 |
| 플레이어 | Player | 게임 기능을 사용하는 인증된 사용자 역할. 현재 별도 DB 개체는 아님 |
| 인증 제공자 | LoginProvider | 사용자 신원을 증명하는 외부 시스템. 현재 Google이 확인됨 |
| 제공자 사용자 ID | ProviderUserId | 인증 제공자가 계정에 부여한 안정적인 식별자 |
| 액세스 토큰 | AccessToken | 보호된 RPC 호출에 사용하는 단기 JWT |
| 리프레시 토큰 | RefreshToken | 액세스 토큰을 재발급하기 위한 장기 무작위 비밀값 |
| 토큰 패밀리 | RefreshTokenFamily | 최초 발급과 이후 회전으로 연결된 리프레시 토큰 집합 |
| 토큰 회전 | TokenRotation | 기존 토큰을 사용 처리하고 새 토큰을 발급하는 과정 |
| 토큰 재사용 | TokenReuse | 이미 사용 처리된 리프레시 토큰을 다시 제출하는 행위 |
| 캐릭터 | Character | 사용자당 하나인 레벨과 경험치 성장 상태 |
| 재화 | Currency | 종류와 0 이상의 잔액으로 구성되는 게임 자원 |
| Gold | Gold | 스테이지 보상으로 확인된 기본 재화 |
| 정적 카탈로그 | StaticCatalog | 배포 파일에서 읽고 서버 시작 시 검증하는 맵·스테이지 정의 집합 |
| 맵 | Map | 방의 너비, 높이, 타일 배치를 정의하는 정적 데이터 |
| 스테이지 | Stage | 플레이 가능한 타일·함정과 최소 시간·보상을 정의하는 정적 데이터 |
| 스테이지 실행 | StageRun | 사용자의 스테이지 입장부터 완료 또는 포기까지의 기록 |
| 진행 중 | InProgress | 아직 완료 또는 포기되지 않은 실행 상태 |
| 완료 | Completed | 검증을 통과하고 보상이 한 번 지급된 실행 상태 |
| 포기 | Abandoned | 새 실행 시작 등으로 더 이상 완료할 수 없는 실행 상태 |
| 사용자 방 | UserRoom | 사용자가 선택한 맵과 함정 배치의 최신 저장본 |
| 함정 | Trap | 종류, 정수 좌표, 회전값으로 구성되는 방 또는 스테이지 요소 |
| 위치 | Position | 맵 격자 기준 정수 x, y, z 좌표. 현재 z는 0만 허용 |
| 회전 | Rotation | x, y, z, w 성분을 갖는 정규화 quaternion |
| RPC 메서드 | RpcMethod | JSON-RPC 요청의 `method`로 선택하는 서버 기능 |
| 알림 | Notification | `id` 속성이 없어 서버가 응답하지 않는 JSON-RPC 요청 |
| 배치 | Batch | 하나 이상의 JSON-RPC 요청을 담은 배열 |
| 프로토콜 오류 | ProtocolError | 파싱, 요청 형식, 메서드, params 등 JSON-RPC 처리 자체의 실패 |
| 업무 오류 | ApplicationError | 유효한 RPC 호출이 업무 조건을 만족하지 못한 실패 |
| 값 객체 | Proto | 값과 불변 조건으로 의미가 정해지는 도메인 개체. 이 프로젝트의 `Proto`는 Protobuf가 아님 |
| DB 모델/DAO 데이터 | Model | 요구사항의 DAO 범주에 해당하며 Dapper가 SQLite 행에 매핑하는 영속성 전용 개체 |
| 요청 DTO | Request | RPC 메서드의 params를 표현하는 외부 계약 타입 |
| 응답 DTO | Response | RPC 메서드의 result를 표현하는 외부 계약 타입 |
| 패킷 | Packet | Request 또는 Response 안에서 재사용하는 구성 타입 |

## 이름을 분리해야 하는 개념

- `UserModel`은 DB 행이고, `UserProto`가 필요하다면 도메인 불변값이어야 합니다.
- `StageProto`는 정적 카탈로그의 값이며 `StageModel`로 DB에 저장하지 않습니다.
- `StageRunModel`은 플레이 기록이고 `StagePacket`은 외부 응답 데이터입니다.
- `CurrencyModel`은 한 종류의 잔액 행이고 `CurrencyPacket`은 클라이언트에 전달하는 값입니다.
- `JsonRpcRequest`와 각 업무 `...Request`는 계층이 다릅니다. 전자는 RPC 봉투, 후자는 params입니다.

## 금지되는 모호한 용어

새 타입 이름에 `Data`, `Info`, `Manager`, `Helper`, `Util`, `CommonModel`, `BaseDto`를 사용하지 않습니다. 역할을 `Catalog`, `Repository`, `Handler`, `Factory`, `Validator`, `Packet`처럼 구체적으로 표현합니다.
