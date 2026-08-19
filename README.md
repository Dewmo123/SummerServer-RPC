# SummerServer-RPC

.NET 10, JSON-RPC 2.0, SQLite와 Dapper 기반의 단일 ASP.NET Core 게임 서버 재구현 저장소입니다.

신규 서버는 [`src/SummerProject.Server`](src/SummerProject.Server)에 있으며 업무 코드는 `Controllers`, `Services`, `Models` 구조를 사용합니다. 정적 맵·스테이지는 `GameData/Catalogs`, 공통 외부 기술은 `Infrastructure`, JSON-RPC 프로토콜 코어는 `Rpc`에 둡니다.

문서 읽기 순서와 기준선은 [문서 지도](docs/README.md), 상세 폴더 책임은 [목표 아키텍처](docs/architecture/ARCHITECTURE.md)를 참조합니다.
