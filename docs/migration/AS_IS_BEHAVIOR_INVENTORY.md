# 현행 동작 인벤토리

## 목적과 범위

2026-08-19 기준 추적된 소스, 설정, 마이그레이션, 정적 게임 데이터와 배포 워크플로에서 관찰된 동작을 기록합니다. 이 문서는 기존 구조를 새 구조로 복사하기 위한 설계가 아니라, 재구현 중 기능 누락을 막기 위한 입력 자료입니다.

## 현행 시스템 형태

| 영역 | 관찰 내용 |
|---|---|
| 런타임 | .NET 10 ASP.NET Core |
| 서버 | 로그인 서버와 게임 서버가 별도 Web 프로젝트 |
| 공용 코드 | 인증 옵션과 User/Character/RefreshToken 엔티티를 별도 Persistence 프로젝트에서 공유 |
| 외부 API | REST Controller 기반 |
| DB | MySQL 8.0.41, EF Core/Pomelo |
| JSON | ASP.NET Core 기본 JSON과 일부 Newtonsoft.Json |
| 로깅 | 기본 ASP.NET Core Logging, 로그인 서버 HTTP logging |
| 배포 | 로그인과 게임 서버를 별도 GitHub Actions로 publish·전송·재시작 |
| 테스트 | 추적된 자동화 테스트 프로젝트 없음 |

## 인증 동작

### Google 로그인

현행 경로: `POST /api/account/login/google`

- 설정된 Google Client ID 목록으로 ID 토큰을 검증합니다.
- subject가 없는 토큰은 거부합니다.
- Google provider와 subject 조합으로 사용자를 찾습니다.
- 최초 로그인은 `google_` 접두사와 subject SHA-256 일부를 사용한 초기 사용자명을 만듭니다.
- 동시 최초 로그인은 DB 유일 인덱스 충돌 후 생성된 사용자를 다시 조회합니다.
- JWT 액세스 토큰과 리프레시 토큰을 함께 반환합니다.

### 개발 로그인

현행 경로: `GET /api/account/test`

- 사용자명이 `Developer`인 기존 사용자를 찾습니다.
- Google 로그인과 같은 토큰 응답을 발급합니다.
- 현행 코드에서는 Development 환경 조건이 주석 처리되어 모든 환경에 Route가 등록됩니다.

### JWT

- claim: sub, jti, username, provider
- 기본 액세스 토큰 수명: 60분
- 검증: issuer, audience, lifetime, signing key
- clock skew: 30초

### 리프레시 토큰

현행 경로:

- `POST /api/account/refresh`
- `POST /api/account/logout`

관찰 동작:

- 32바이트 난수를 Base64Url 문자열로 만듭니다.
- SHA-256 해시만 DB에 저장합니다.
- 기본 수명은 30일입니다.
- 회전 시 패밀리 ID와 기존 절대 만료를 유지합니다.
- 기존 토큰을 사용 처리하고 다음 토큰 ID를 연결합니다.
- 이미 사용된 토큰 또는 회전 경쟁 패배는 패밀리 전체를 폐기합니다.
- 로그아웃은 토큰이 없어도 성공하며 패밀리를 폐기합니다.

## 캐릭터 동작

현행 경로: `GET /api/character/me`

- 인증 사용자에게 캐릭터가 없으면 레벨 1, 경험치 0으로 지연 생성합니다.
- 레벨당 필요 경험치는 `100 × level`입니다.
- 경험치 지급은 여러 레벨을 연속으로 올리고 남은 경험치를 보존합니다.
- 경험치 지급은 외부 API가 아니라 스테이지 완료 흐름에서 호출됩니다.

## 재화 동작

현행 경로:

- `GET /api/currency/me/{type}`
- `GET /api/currency/me`

지원 코드:

- Gold=1
- Gem=2
- StageTicket=3
- EventToken=4

관찰 동작:

- 누락된 재화 행은 0으로 지연 생성합니다.
- 전체 조회는 지원되는 모든 종류를 생성한 뒤 dictionary 형태로 반환합니다.
- 내부 서비스는 양수 증가와 차감을 제공합니다.
- SQL 조건으로 음수 잔액과 Int64 overflow를 방지합니다.

## 스테이지 동작

현행 경로:

- `GET /api/stage/{stageId}`: 익명 허용
- `POST /api/stage/{stageId}/enter`: 인증 필요
- `POST /api/stage/runs/{runId}/complete`: 인증 필요

입장:

- 정적 카탈로그에서 스테이지를 찾습니다.
- 사용자가 존재하는지 잠금 조회합니다.
- 같은 사용자의 진행 중 실행을 포기 처리합니다.
- 새 진행 실행을 생성합니다.

완료:

- 실행 존재, 소유권, 진행 상태를 확인합니다.
- 서버 시작 시각과 최소 클리어 시간을 비교합니다.
- 조건부 갱신으로 완료 상태를 한 번만 선점합니다.
- 획득 Gold와 경험치를 실행 기록에 저장합니다.
- Gold와 경험치를 같은 DB 트랜잭션에서 지급합니다.
- 갱신된 캐릭터와 전체 재화, 획득 재화를 반환합니다.

현재 정적 Stage1:

- stageId 1
- width 16, height 8
- 최소 클리어 1초
- 경험치 10, Gold 100
- SawTrap 한 개

## 사용자 방 동작

현행 경로:

- `POST /api/user-room/upload`
- `GET /api/user-room/me`

저장 검증:

- 요청 최대 64 KiB
- MapId는 양수이며 카탈로그에 존재
- 함정 최대 100개
- 정의된 함정 종류
- x와 y는 맵 경계 안, z는 0
- 같은 좌표 중복 금지
- quaternion 크기 제곱 0.98..1.02
- 사용자별 방 하나를 insert 또는 전체 갱신

조회:

- 저장된 MapId로 카탈로그 맵을 결합합니다.
- 방이 없거나 저장된 맵이 카탈로그에 없으면 서로 다른 오류를 반환합니다.

현재 정적 Map1은 mapId 1, width 16, height 8입니다.

## 공통 운영 동작

- 두 서버 모두 `/health`와 루트 상태 문자열을 제공합니다.
- OpenAPI와 Swagger UI의 환경 조건이 주석 처리되어 모든 환경에 노출됩니다.
- 로그인 정책은 IP별 1분 10회이며 Google login, test login, refresh에 적용됩니다.
- 게임 서버 전역 정책은 사용자 sub 또는 IP별 1초 120회입니다.
- 두 서버 모두 HTTPS redirect, HSTS, ProblemDetails를 사용합니다.

## 현행 DB 개념

- Users: 사용자명, provider, provider user ID, 생성 시각
- RefreshTokens: 사용자, 패밀리, 해시, 생성·만료·사용·폐기, 교체 토큰
- Characters: 사용자별 레벨과 경험치
- Currencies: 사용자와 재화 종류별 잔액
- StageRuns: 사용자, 스테이지, 상태, 시작·완료, 획득 경험치·재화
- UserRooms: 사용자별 맵과 JSON 함정 배치

## 확인된 불명확성 및 위험

- Map1과 Stage1의 tile 배열 원소 수가 선언된 `width × height`와 일치하지 않지만 현재 Loader는 검증하지 않습니다.
- 실제 약탈, 상대 방 조회, 방어 결과, 보상 로직은 확인되지 않습니다.
- 클라이언트 플레이 결과는 최소 시간 외에 검증하지 않습니다.
- 자동화 테스트가 없어 동시성과 외부 계약 회귀를 빌드만으로 검증할 수 없습니다.
- 개발 로그인과 Swagger가 운영 환경에도 노출될 수 있습니다.
- 기존 두 서버가 같은 MySQL 스키마의 마이그레이션 소유권을 나눠 관리합니다.
- 로그인 서버와 게임 서버의 패키지·런타임 버전 조합이 중앙 관리되지 않습니다.

이 항목은 [GAP_ANALYSIS.md](GAP_ANALYSIS.md)와 목표 요구사항의 미결정 사항으로 연결합니다.
