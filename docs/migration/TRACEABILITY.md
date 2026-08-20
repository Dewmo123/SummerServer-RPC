# 요구사항 추적성

## 상태 정의

- `문서화 완료`: 요구사항과 계약은 확정되었음
- `구현 전`: 목표 코드와 테스트가 아직 없음
- `진행 중`: 코드 또는 테스트 일부가 있음
- `완료`: 구현, 자동 테스트, 문서 검증이 모두 끝남

Phase 0 저장소 기반, Phase 1 JSON-RPC 프로토콜 코어, Phase 2 관측성과 설정, Phase 3 SQLite 기반, Phase 4 정적 카탈로그, Phase 5 인증 수직 기능을 구현했습니다. 후속 업무 Handler는 `Controllers`, 업무 Service는 `Services`, SQL 접근은 `Repositories`, 생성 보조 타입은 `Helpers`, 업무 예외는 `Exceptions`, 모델은 `Models`에 구현합니다.

## 기능 추적표

| 요구사항 | RPC/내부 기능 | 목표 구현 위치 | 필수 테스트 | 상태 |
|---|---|---|---|---|
| FR-AUTH-001 | `auth.login.google` | [GoogleLoginHandler](../../src/SummerProject.Server/Controllers/Auth/GoogleLoginHandler.cs), [GoogleLoginService](../../src/SummerProject.Server/Services/Auth/GoogleLoginService.cs) | [유효/무효 토큰, 기존·동시 최초 로그인](../../tests/SummerProject.Server.Tests/Auth/AuthenticationEndpointTests.cs), [공식 검증기 무효 토큰](../../tests/SummerProject.Server.Tests/Auth/GoogleIdTokenValidatorTests.cs) | 완료 |
| FR-AUTH-002 | `auth.login.development` | [DevelopmentLoginHandler](../../src/SummerProject.Server/Controllers/Auth/DevelopmentLoginHandler.cs), [DevelopmentLoginService](../../src/SummerProject.Server/Services/Auth/DevelopmentLoginService.cs) | [Development 성공·사용자 없음, Production/Staging 미등록 효과](../../tests/SummerProject.Server.Tests/Auth/DevelopmentLoginTests.cs) | 완료 |
| FR-AUTH-003 | JWT 발급·검증 | [JwtTokenService](../../src/SummerProject.Server/Infrastructure/Security/JwtTokenService.cs), [CallerContext](../../src/SummerProject.Server/Infrastructure/Security/CallerContext.cs) | [claim, issuer, audience, expiry, signature, 보호 메서드](../../tests/SummerProject.Server.Tests/Auth/JwtAuthenticationTests.cs) | 완료 |
| FR-AUTH-004 | `auth.token.refresh` | [RefreshTokenHandler](../../src/SummerProject.Server/Controllers/Auth/RefreshTokenHandler.cs), [RefreshTokenService](../../src/SummerProject.Server/Services/Auth/RefreshTokenService.cs) | [회전, 만료, 폐기, 동시 회전, 재사용과 패밀리 폐기](../../tests/SummerProject.Server.Tests/Auth/AuthenticationEndpointTests.cs) | 완료 |
| FR-AUTH-005 | `auth.logout` | [LogoutHandler](../../src/SummerProject.Server/Controllers/Auth/LogoutHandler.cs), [RefreshTokenService](../../src/SummerProject.Server/Services/Auth/RefreshTokenService.cs) | [정상, 없는 토큰, 반복 로그아웃](../../tests/SummerProject.Server.Tests/Auth/AuthenticationEndpointTests.cs) | 완료 |
| FR-CHAR-001 | `character.getMine` | [GetMyCharacterHandler](../../src/SummerProject.Server/Controllers/Characters/GetMyCharacterHandler.cs), [CharacterQueryService](../../src/SummerProject.Server/Services/Characters/CharacterQueryService.cs), [CharacterRepository](../../src/SummerProject.Server/Repositories/Characters/CharacterRepository.cs) | [지연 생성, 동시 생성, 사용자 없음](../../tests/SummerProject.Server.Tests/Characters/CharacterEndpointTests.cs) | 완료 |
| FR-CHAR-002 | 경험치 지급 | [CharacterProgressionService](../../src/SummerProject.Server/Services/Characters/CharacterProgressionService.cs), [CharacterProgressionCalculator](../../src/SummerProject.Server/Helpers/Characters/CharacterProgressionCalculator.cs) | [경계, 여러 레벨, 0/음수, overflow, 경쟁](../../tests/SummerProject.Server.Tests/Characters/CharacterProgressionServiceTests.cs), [보상 트랜잭션 공유](../../tests/SummerProject.Server.Tests/Gameplay/RewardTransactionCompositionTests.cs) | 완료 |
| FR-CURRENCY-001 | `currency.getMine` | [GetMyCurrencyHandler](../../src/SummerProject.Server/Controllers/Currencies/GetMyCurrencyHandler.cs), [CurrencyQueryService](../../src/SummerProject.Server/Services/Currencies/CurrencyQueryService.cs) | [지연 생성, 잘못된 코드, 사용자 없음](../../tests/SummerProject.Server.Tests/Currencies/CurrencyEndpointTests.cs) | 완료 |
| FR-CURRENCY-002 | `currency.listMine` | [ListMyCurrenciesHandler](../../src/SummerProject.Server/Controllers/Currencies/ListMyCurrenciesHandler.cs), [CurrencyRepository](../../src/SummerProject.Server/Repositories/Currencies/CurrencyRepository.cs) | [모든 코드, 정렬, 동시 생성](../../tests/SummerProject.Server.Tests/Currencies/CurrencyEndpointTests.cs) | 완료 |
| FR-CURRENCY-003 | 재화 변경 | [CurrencyBalanceService](../../src/SummerProject.Server/Services/Currencies/CurrencyBalanceService.cs), [CurrencyRepository](../../src/SummerProject.Server/Repositories/Currencies/CurrencyRepository.cs) | [증가, 차감, 부족, overflow, 경쟁](../../tests/SummerProject.Server.Tests/Currencies/CurrencyBalanceServiceTests.cs), [보상 트랜잭션 공유](../../tests/SummerProject.Server.Tests/Gameplay/RewardTransactionCompositionTests.cs) | 완료 |
| FR-STAGE-001 | `stage.get` | `Controllers/Stages/GetStageHandler`, `GameData/Catalogs/Stages` | 조회·없음 구현 전, [카탈로그 적재·검증](../../tests/SummerProject.Server.Tests/GameData/Catalogs/CatalogLoaderTests.cs), [시작 검증](../../tests/SummerProject.Server.Tests/GameData/Catalogs/CatalogStartupTests.cs) | 진행 중 |
| FR-STAGE-002 | `stage.enter` | `Controllers/Stages/EnterStageHandler`, `Services/Stages` | 기존 포기, 새 실행, 동시 입장 | 구현 전 |
| FR-STAGE-003 | `stage.complete` | `Controllers/Stages/CompleteStageHandler`, `Services/Stages` | 소유권, 시간, 중복, 보상 롤백 | 구현 전 |
| FR-ROOM-001 | `room.upsertMine` | `Controllers/Rooms/UpsertMyRoomHandler`, `Services/Rooms` | 종류, 좌표, 중복, 회전, 크기, upsert | 구현 전 |
| FR-ROOM-002 | `room.getMine` | `Controllers/Rooms/GetMyRoomHandler`, `Services/Rooms` | 정상, 방 없음, 카탈로그 불일치 | 구현 전 |
| FR-SYSTEM-001 | `GET /health` | ASP.NET Core Health Checks, `DatabaseHealthCheck` | [프로세스·SQLite 정상, DB 불가·정보 비노출](../../tests/SummerProject.Server.Tests/HealthEndpointTests.cs) | 완료 |

## 비기능 추적표

| 요구사항 | 구현 위치 | 검증 | 상태 |
|---|---|---|---|
| NFR-PROTOCOL-001 | `Rpc` | [요청·응답](../../tests/SummerProject.Server.Tests/Rpc/JsonRpcRequestTests.cs), [알림·배치](../../tests/SummerProject.Server.Tests/Rpc/JsonRpcNotificationAndBatchTests.cs), [HTTP 전송](../../tests/SummerProject.Server.Tests/Rpc/JsonRpcTransportTests.cs) | 완료 |
| NFR-PROTOCOL-002 | `JsonRpcMethodRegistry`, params binder | [메서드·필드 대소문자 불일치](../../tests/SummerProject.Server.Tests/Rpc/JsonRpcRequestTests.cs) | 완료 |
| NFR-SECURITY-001 | `Infrastructure/Security`, `Infrastructure/Logging`, Options | [민감 필드 차단·요청 로그 비노출](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/SensitiveLogFilterTests.cs), [RPC 로그 비노출](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/StructuredLoggingTests.cs), [JWT·해시 저장·인증 로그 비노출](../../tests/SummerProject.Server.Tests/Auth/) | 완료 |
| NFR-SECURITY-002 | Migration Runner, `Repositories` | [마이그레이션 이력 매개변수화·DB 제약](../../tests/SummerProject.Server.Tests/Infrastructure/Database/DatabaseConstraintTests.cs), [인증 Repository 매개변수·제약 통합 검증](../../tests/SummerProject.Server.Tests/Auth/AuthenticationEndpointTests.cs), 후속 기능별 injection 테스트 | 진행 중 |
| NFR-RELIABILITY-001 | `Repositories/Auth`, `Repositories/Stages` | [인증 최초 생성·토큰 회전 동시성](../../tests/SummerProject.Server.Tests/Auth/AuthenticationEndpointTests.cs), 후속 스테이지 동시성 | 진행 중 |
| NFR-RELIABILITY-002 | `SqliteMigrationRunner` | [빈 DB·반복 적용·체크섬 변조](../../tests/SummerProject.Server.Tests/Infrastructure/Database/DatabaseMigrationTests.cs) | 완료 |
| NFR-OBSERVABILITY-001 | ZLogger console, RPC dispatcher/processor | [공급자·구조화 필드](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/StructuredLoggingTests.cs) | 완료 |
| NFR-OBSERVABILITY-002 | `SensitiveLogFilter` | [Authorization·토큰·params·본문 차단](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/SensitiveLogFilterTests.cs), [요청 로그 비노출](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/StructuredLoggingTests.cs) | 완료 |
| NFR-MAINTAINABILITY-001 | `Controllers`, `Services`, `Repositories`, `Helpers`, `Exceptions`, `Models`, `GameData`, `Infrastructure`, `Rpc`, solution/csproj, [CI](../../.github/workflows/ci.yml) | 프로덕션 프로젝트 수와 금지 패키지 검증 | 완료 |
| NFR-MAINTAINABILITY-002 | 전체 코드 | analyzer, 리뷰 체크리스트 | 구현 전 |
| NFR-TEST-001 | test project/[CI](../../.github/workflows/ci.yml) | Release test 실행, [시작 설정 검증](../../tests/SummerProject.Server.Tests/Infrastructure/Configuration/OptionsValidationTests.cs), [실제 임시 SQLite 연결](../../tests/SummerProject.Server.Tests/Infrastructure/Database/SqliteConnectionPolicyTests.cs), [스키마 제약](../../tests/SummerProject.Server.Tests/Infrastructure/Database/DatabaseConstraintTests.cs), [정적 카탈로그](../../tests/SummerProject.Server.Tests/GameData/Catalogs/CatalogLoaderTests.cs) | 진행 중 |
| NFR-PERFORMANCE-001 | HTTP/RPC options | [64 KiB·JSON 깊이](../../tests/SummerProject.Server.Tests/Rpc/JsonRpcTransportTests.cs), [배치 50개 제한](../../tests/SummerProject.Server.Tests/Rpc/JsonRpcNotificationAndBatchTests.cs) | 완료 |
| NFR-COMPAT-001 | TimeProvider/serializer | [인증 UTC 밀리초 저장·ISO 8601 만료 응답](../../tests/SummerProject.Server.Tests/Auth/AuthenticationEndpointTests.cs), 후속 기능 시간 호환성 | 진행 중 |

## 단계별 완료 갱신 규칙

1. 구현 파일이 추가되면 상태를 `진행 중`으로 바꿉니다.
2. 관련 테스트 이름 또는 테스트 파일 링크를 표에 추가합니다.
3. 모든 인수 조건과 오류 코드를 테스트한 뒤에만 `완료`로 바꿉니다.
4. 수동 확인만으로 `완료`로 표시하지 않습니다.
5. 계약이 변경되면 요구사항, RPC 카탈로그, 오류 카탈로그를 먼저 갱신합니다.
