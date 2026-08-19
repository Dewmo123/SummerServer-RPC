# 기여 가이드

## 기본 원칙

- 새 구현은 `src/SummerProject.Server`에만 작성한다.
- 기존 분리 서버 코드는 기능 확인용이며 새 코드의 구조나 이름을 복사하지 않는다.
- 한 변경은 하나 이상의 요구사항 ID와 연결한다.
- 계약, 구현, 테스트, 추적성 문서를 함께 변경한다.

## 작업 절차

1. `docs/README.md`에서 관련 문서를 찾는다.
2. `docs/migration/TRACEABILITY.md`에서 구현 상태를 확인한다.
3. 요구사항과 RPC 계약에서 정상·실패·경계 조건을 추출한다.
4. 구현 전에 테스트 목록을 작성한다.
5. 최소 변경으로 구현한다.
6. 빌드, 테스트, 포맷 검증을 실행한다.
7. 추적성 표와 변경 기록을 갱신한다.
8. 스테이징된 diff를 기준으로 AI가 커밋 메시지를 생성하게 한다.

## 브랜치와 커밋

브랜치 예시:

```text
feat/json-rpc-dispatcher
feat/google-login
fix/refresh-token-reuse
docs/stage-contract
```

커밋 메시지는 Conventional Commits 형식의 한국어 문장을 사용합니다.

```text
feat(auth): Google 로그인 RPC 구현
fix(room): 중복 함정 좌표 검증 보완
test(rpc): 전체 알림 배치 회귀 테스트 추가
docs(database): SQLite 인덱스 정책 명시
```

자세한 규칙은 [커밋 가이드](docs/engineering/COMMIT_GUIDE.md)를 따릅니다.

## 코드 리뷰 체크리스트

- 요구사항 밖의 기능을 임의로 추가하지 않았는가?
- JSON-RPC 알림, id, batch 규칙을 훼손하지 않았는가?
- DTO와 DB 모델이 분리되어 있는가?
- SQL이 매개변수화되어 있는가?
- 동시성 변경에 트랜잭션 또는 원자적 조건절이 있는가?
- 토큰과 개인정보가 로그에 포함되지 않는가?
- 한국어 주석이 이유와 제약을 설명하는가?
- 정상·실패·경계 테스트가 있는가?
- README가 아닌 상세 문서에 구현 규칙이 위치하는가?

## 로컬 검증

```powershell
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
```

구현 초기에는 마지막 명령이 설정되지 않았을 수 있습니다. 이 경우 포맷 설정 작업과 함께 활성화하고, 검증하지 않은 상태를 커밋 메시지에 숨기지 않습니다.
