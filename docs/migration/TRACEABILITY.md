# 요구사항 추적성

## 상태 정의

- `문서화 완료`: 요구사항과 계약은 확정되었음
- `구현 전`: 목표 코드와 테스트가 아직 없음
- `진행 중`: 코드 또는 테스트 일부가 있음
- `완료`: 구현, 자동 테스트, 문서 검증이 모두 끝남

Phase 0 저장소 기반, Phase 1 JSON-RPC 프로토콜 코어, Phase 2 관측성과 설정, Phase 3 SQLite 기반을 구현했습니다. 후속 업무 Handler는 `Controllers`, Service와 Repository는 `Services`, 모델은 `Models`, 정적 카탈로그는 `GameData`에 구현합니다.

## 기능 추적표

| 요구사항 | RPC/내부 기능 | 목표 구현 위치 | 필수 테스트 | 상태 |
|---|---|---|---|---|
| FR-AUTH-001 | `auth.login.google` | `Controllers/Auth/GoogleLoginHandler`, `Services/Auth` | 유효/무효 토큰, 동시 최초 로그인 | 구현 전 |
| FR-AUTH-002 | `auth.login.development` | `Controllers/Auth/DevelopmentLoginHandler`, `Services/Auth` | Development 성공, Production 미등록 | 구현 전 |
| FR-AUTH-003 | JWT 발급·검증 | `Infrastructure/Security/JwtTokenService` | claim, issuer, audience, expiry, signature | 구현 전 |
| FR-AUTH-004 | `auth.token.refresh` | `Controllers/Auth/RefreshTokenHandler`, `Services/Auth/RefreshTokenService` | 회전, 만료, 폐기, 동시 회전, 재사용 | 구현 전 |
| FR-AUTH-005 | `auth.logout` | `Controllers/Auth/LogoutHandler`, `Services/Auth` | 정상, 없는 토큰, 반복 로그아웃 | 구현 전 |
| FR-CHAR-001 | `character.getMine` | `Controllers/Characters/GetMyCharacterHandler`, `Services/Characters` | 지연 생성, 동시 생성, 사용자 없음 | 구현 전 |
| FR-CHAR-002 | 경험치 지급 | `Services/Characters/CharacterProgressionService` | 경계, 여러 레벨, 0/음수, overflow | 구현 전 |
| FR-CURRENCY-001 | `currency.getMine` | `Controllers/Currencies/GetMyCurrencyHandler`, `Services/Currencies` | 지연 생성, 잘못된 코드 | 구현 전 |
| FR-CURRENCY-002 | `currency.listMine` | `Controllers/Currencies/ListMyCurrenciesHandler`, `Services/Currencies` | 모든 코드, 정렬, 동시 생성 | 구현 전 |
| FR-CURRENCY-003 | 재화 변경 | `Services/Currencies/CurrencyBalanceService` | 증가, 차감, 부족, overflow, 경쟁 | 구현 전 |
| FR-STAGE-001 | `stage.get` | `Controllers/Stages/GetStageHandler`, `GameData/Catalogs/Stages` | 조회, 없음, 카탈로그 검증 | 구현 전 |
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
| NFR-SECURITY-001 | `Infrastructure/Security`, `Infrastructure/Logging`, Options | [민감 필드 차단·요청 로그 비노출](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/SensitiveLogFilterTests.cs), [RPC 로그 비노출](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/StructuredLoggingTests.cs) | 진행 중 |
| NFR-SECURITY-002 | Migration Runner, 후속 `Services/*Repository` | [마이그레이션 이력 매개변수화·DB 제약](../../tests/SummerProject.Server.Tests/Infrastructure/Database/DatabaseConstraintTests.cs), 기능별 injection 테스트 | 진행 중 |
| NFR-RELIABILITY-001 | `Services/Auth`, `Services/Stages` Repository | 동시성 통합 테스트 | 구현 전 |
| NFR-RELIABILITY-002 | `SqliteMigrationRunner` | [빈 DB·반복 적용·체크섬 변조](../../tests/SummerProject.Server.Tests/Infrastructure/Database/DatabaseMigrationTests.cs) | 완료 |
| NFR-OBSERVABILITY-001 | ZLogger console, RPC dispatcher/processor | [공급자·구조화 필드](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/StructuredLoggingTests.cs) | 완료 |
| NFR-OBSERVABILITY-002 | `SensitiveLogFilter` | [Authorization·토큰·params·본문 차단](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/SensitiveLogFilterTests.cs), [요청 로그 비노출](../../tests/SummerProject.Server.Tests/Infrastructure/Logging/StructuredLoggingTests.cs) | 완료 |
| NFR-MAINTAINABILITY-001 | `Controllers`, `Services`, `Models`, `GameData`, `Infrastructure`, `Rpc`, solution/csproj, [CI](../../.github/workflows/ci.yml) | 프로덕션 프로젝트 수와 금지 패키지 검증 | 완료 |
| NFR-MAINTAINABILITY-002 | 전체 코드 | analyzer, 리뷰 체크리스트 | 구현 전 |
| NFR-TEST-001 | test project/[CI](../../.github/workflows/ci.yml) | Release test 실행, [시작 설정 검증](../../tests/SummerProject.Server.Tests/Infrastructure/Configuration/OptionsValidationTests.cs), [실제 임시 SQLite 연결](../../tests/SummerProject.Server.Tests/Infrastructure/Database/SqliteConnectionPolicyTests.cs), [스키마 제약](../../tests/SummerProject.Server.Tests/Infrastructure/Database/DatabaseConstraintTests.cs) | 진행 중 |
| NFR-PERFORMANCE-001 | HTTP/RPC options | [64 KiB·JSON 깊이](../../tests/SummerProject.Server.Tests/Rpc/JsonRpcTransportTests.cs), [배치 50개 제한](../../tests/SummerProject.Server.Tests/Rpc/JsonRpcNotificationAndBatchTests.cs) | 완료 |
| NFR-COMPAT-001 | TimeProvider/serializer | UTC 저장·ISO 응답 테스트 | 구현 전 |

## 단계별 완료 갱신 규칙

1. 구현 파일이 추가되면 상태를 `진행 중`으로 바꿉니다.
2. 관련 테스트 이름 또는 테스트 파일 링크를 표에 추가합니다.
3. 모든 인수 조건과 오류 코드를 테스트한 뒤에만 `완료`로 바꿉니다.
4. 수동 확인만으로 `완료`로 표시하지 않습니다.
5. 계약이 변경되면 요구사항, RPC 카탈로그, 오류 카탈로그를 먼저 갱신합니다.
