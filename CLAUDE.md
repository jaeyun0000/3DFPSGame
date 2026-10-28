# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity **6000.3.22f1**(Unity 6) + URP 기반 3D FPS 학습용 프로젝트. 게임 씬은 `Assets/01.Scenes/GameScene.unity` 하나뿐이다.
게임 코드는 전부 `Assets/02.Scripts/` 아래에 있고, 어셈블리 정의(asmdef) 없이 기본 `Assembly-CSharp`로 컴파일된다.

## 명령어

빌드, 실행, 테스트는 Unity 에디터에서만 할 수 있다. CLI에서 쓸 수 있는 명령은 포맷 검사뿐이다.

```bash
# 최초 1회: 로컬 도구(Husky.Net) 복원과 git hook 설치
dotnet tool restore
dotnet husky install

# 포맷/네이밍 검사 (pre-commit hook과 동일)
dotnet format 3DFPSGame.slnx --verify-no-changes --severity warn --include Assets/02.Scripts/

# 자동 수정
dotnet format 3DFPSGame.slnx --severity warn --include Assets/02.Scripts/
```

- `3DFPSGame.slnx`와 `*.csproj`는 Unity가 생성하는 파일이다. 스크립트를 추가하거나 삭제한 뒤에는 Unity 에디터가 갱신해야 `dotnet format`에 반영된다.
- Unity Test Framework 패키지는 설치되어 있지만 테스트 어셈블리는 없다. `Assets/02.Scripts/Test/`는 단위 테스트가 아니라 C# 문법 학습용 MonoBehaviour다.

## Git 규칙 (hook으로 강제됨)

- **커밋 메시지**: `.husky/commit-msg`가 `type: 설명` 또는 `type(scope): 설명` 형식을 검사한다.
  허용 type은 `asset | build | chore | ci | docs | feat | fix | perf | refactor | revert | style | test`이고, 설명은 한국어로 작성한다.
- **pre-commit**: 위의 `dotnet format --verify-no-changes`를 실행하므로, 경고가 하나라도 있으면 커밋이 실패한다.
- **CI**: `main`에 `Assets/02.Scripts/**/*.cs` 변경을 푸시하면 `.github/workflows/gemini-commit-review.yml`이 Gemini로 코드 리뷰를 하고 커밋 코멘트를 남긴다.
  리뷰 기준은 `Docs/CodeReviewGuide.md`이다. 코드 리뷰를 요청받으면 이 문서의 중요도 기준(Critical/High/Medium/Low)과 결과 형식(총평, 발견된 문제, 설계 개선 제안, 잘 작성된 점)을 따른다.

## 코딩 컨벤션 (`.editorconfig`, `Assets/02.Scripts/**.cs`에만 적용)

- 들여쓰기는 공백 4칸이고, 여는 중괄호는 Allman 스타일로 항상 새 줄에 둔다.
- 한 줄짜리 조기 리턴(`if (...) return;`)은 중괄호를 생략해도 되고, 여러 줄이면 중괄호가 필수다.
- private 필드는 `_camelCase`, `private const`와 `private static readonly`는 `PascalCase`, 타입명은 언더스코어 없는 `PascalCase`, 지역 변수는 `camelCase`로 쓴다.
  - 예외: UI 스크립트는 `UI_` 접두사를 쓴다(`UI_HitEffect`, `UI_Minimap`). 기존 패턴이므로 유지한다.
- Inspector 노출 값은 `[SerializeField] private`와 `[Header("Stat")]` 또는 `[Header("스탯")]`로 묶는다. 외부에는 읽기 전용 프로퍼티(`public float Health => _health;`)로만 공개한다.
- 코루틴 메서드 이름은 `Xxx_Coroutine`으로 짓는다.
- 주석은 한국어로 쓴다.

## 아키텍처

### 게임 상태 게이트
`GameManager`는 정적 `Instance`를 가진 싱글턴으로, `GameState`(`Ready`, `Playing`, `GameOver`)를 관리한다.
시작 시퀀스 코루틴("준비" → "시작")이 끝나야 `Playing`이 된다. **입력이나 AI를 처리하는 모든 `Update()`는 첫 줄에서 `GameManager.Instance.State != GameState.Playing`이면 리턴한다.** 새 게임플레이 스크립트도 이 패턴을 따라야 한다.

### 대미지 시스템
- `IDamageable.TakeDamage(DamageInfo)` 인터페이스 하나로 모든 피격을 처리한다. `DamageInfo` 구조체에는 `Value`, `HitPoint`, `HitNormal`이 들어 있다.
- 구현체는 `Player`, `Zombie`, `Drum`(폭발 드럼통), `Box`이다.
- 공격하는 쪽은 구체 타입을 모른 채 `collider.GetComponent<IDamageable>()?.TakeDamage(info)`로 호출한다.
  - 총(`PlayerGunFire`)은 `Camera.main` 기준 Raycast를 쓴다.
  - 범위 공격(`Bomb`, `Drum`)은 `Physics.OverlapSphere`에 `LayerMask.GetMask("Drum", "Zombie")`를 쓴다. 새 피격 대상이 범위 공격을 받으려면 **레이어를 지정하고 이 마스크에 추가해야 한다.**
- 드럼통 폭발은 주변 드럼통에 대미지를 주므로 연쇄 폭발이 일어난다.

### 플레이어
- 기능별로 컴포넌트가 나뉘어 있다: `PlayerMove`(CharacterController 이동, 2단 점프, 스태미나), `PlayerRotate`, `PlayerGunFire`(연사, 탄창, R키 재장전), `PlayerBombFire`(우클릭을 누르는 시간만큼 던지는 힘이 커짐), `Player`(체력, 피격).
- `Player`가 죽으면 `GameManager.Instance.GameOver()`를 호출한다.
- 카메라 회전은 `Camera/CameraRotate`, 위치 추적은 `Camera/CameraFollow`가 담당한다.

### 좀비 AI
`Zombie`는 `switch` 기반 FSM이다(`ZombieState`: Idle, Trace, Comeback, Attack, Hit, Death).
- `Hit`와 `Death`는 `Update`의 switch에서 처리하지 않고 `TakeDamage`가 코루틴으로 전이시킨다. `TakeDamage`가 `StopAllCoroutines()`를 호출한다는 점에 주의한다.
- 플레이어는 `Start()`에서 `FindGameObjectWithTag("Player")`로 찾는다.
- 이동은 NavMesh가 아니라 `CharacterController.SimpleMove`로 한다. `ai.navigation` 패키지는 설치만 되어 있다.

### UI와 입력
- UI는 매 프레임 모델 값을 읽어 표시하는 폴링 방식이다(`UI_PlayerStats`, `ZombieHealthBar`). 이벤트 기반 구조는 아직 없다.
- `ZombieHealthBar`는 좀비 루트 오브젝트에 붙어 있다. 빌보드 회전은 반드시 `_slider.transform`에 적용해야 한다(커밋 8797f74 버그 참고).
- 입력은 구 Input Manager(`Input.GetAxisRaw`, `GetButton("Fire1"/"Fire2"/"Jump"/"Sprint")`, `GetKeyDown`)를 사용한다.
  Player Settings가 Both(`activeInputHandler: 2`)라 Input System 패키지와 `InputSystem_Actions.inputactions`도 있지만, 코드에서는 쓰지 않는다.
- **커서 잠금**: `CursorManager`는 Left Alt를 누르고 있는 동안 커서 잠금을 푼다. `PlayerGunFire`는 `Cursor.visible`이면 발사하지 않는다.
- **미니맵**: `MinimapCamera`가 플레이어 머리 위에서 내려다보는 직교 카메라로 `MinimapRenderTexture`에 렌더링한다. 9/0 키로 줌을 조절한다. 리소스는 `Assets/Minimap_Resource/`에 있다.

### 로깅
`GameLogger.Log`는 `[Conditional("ENABLE_LOG")]`가 붙어 있어서, 심볼이 없는 빌드에서는 호출 자체가 제거된다. 현재 Standalone 빌드에는 `ENABLE_LOG`가 정의되어 있다.
`LogWarning`과 `LogError`는 항상 동작한다. 기존 코드에는 `Debug.Log`를 직접 호출하는 곳도 남아 있다.

## Unity 작업 시 주의

- `.unity`, `.prefab`, `.asset` 같은 YAML 에셋은 직접 편집하지 않는다. 씬 배치와 Inspector 참조 연결은 사용자에게 에디터 작업으로 안내한다.
- 스크립트를 새로 만들면 Unity가 `.meta`를 생성하므로 `.meta` 파일도 함께 커밋해야 한다.
- `Assets/Workshop/`, `Assets/BloodDecalsAndEffects/`, `Assets/TextMesh Pro/`, `Assets/TutorialInfo/`는 외부 에셋이나 템플릿이다. 수정 대상이 아니다.
