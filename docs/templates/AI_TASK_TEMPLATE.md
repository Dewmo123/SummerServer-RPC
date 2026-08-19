# AI 구현 작업 템플릿

```markdown
## 목표

<완료되어야 할 관찰 가능한 결과>

## 범위

- 요구사항: <FR/NFR ID>
- RPC: <method 또는 없음>
- 허용된 폴더: <경로>
- 범위 밖: <하지 않을 일>

## 먼저 읽을 문서

1. AGENTS.md
2. docs/requirements/APP_REQUIREMENTS.md#...
3. docs/contracts/RPC_METHOD_CATALOG.md#...
4. 관련 ADR와 DATA_MODEL.md

## 구현 조건

- <기술·네이밍·보안 조건>

## 필수 테스트

- 정상:
- 실패:
- 경계:
- 동시성:

## 완료 조건

- dotnet build 성공
- dotnet test 성공
- 계약과 추적성 갱신
- 스테이징 diff 기반 한국어 커밋 메시지 제안
```

## AI에 전달할 공통 문장

```text
기존 SummerLoginServer, SummerGameServer, Persistence의 코드 구조와 이름을 복사하지 말고 승인된 문서 계약만 기준으로 구현하라. 미결정 사항을 추측하지 말고 문서에 기록하라. 변경된 주석은 COMMENT_GUIDE.md에 따라 한국어로 작성하라.
```
