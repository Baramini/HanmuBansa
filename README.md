# 한무 반사 (Endless Reflection)

> **Unity 6.3 LTS** 기반 1인 개발 탑뷰 탱크 멀티플레이 슈터  
> WebGL (itch.io) 배포 · Android 이식 예정

---

## 기술 스택

| 분류 | 사용 기술 |
|------|----------|
| 엔진 | Unity 6.3 LTS (6000.3.10f1) |
| 네트워크 | Unity Netcode for GameObjects (NGO), Unity Transport (UTP) |
| 백엔드 | Unity Gaming Services — Relay, Lobby |
| 플랫폼 | WebGL (wss), Android (dtls), Editor (udp) 멀티 플랫폼 |
| UI | 커스텀 UI 프레임워크 (BrmnModules.UI) |
| 오디오 | 커스텀 AudioManager (ScriptableObject 기반) |

---

## 주요 구현

### 1. 커스텀 UI 프레임워크 (BrmnModules.UI)

반복 개발을 피하기 위해 직접 설계한 UI 아키텍처입니다.

**상속 구조**
```
BaseUI
├── PersistentUI  — 항상 화면에 유지되는 UI (HUD 등)
└── PopupUI       — Fade In/Out 애니메이션, 스택 기반 관리
```

**UIManager의 제네릭 API**
```csharp
// 데이터 설정과 표시를 한 번의 호출로 처리
UIManager.Instance.ShowPopup<ResultPanel>(panel =>
    panel.SetResult(winnerName));

// 팝업 참조 획득
UIManager.Instance.GetPersistent<HUDPanel>()?.SetTimer(remaining);
```

- 팝업 스택 관리로 중복 표시 방지
- 제네릭 기반으로 타입 안전성 보장
- `onBeforeShow` 콜백으로 표시 전 데이터 주입

---

### 2. StatusPanel — 자동 닫힘 + 코루틴 교체 설계

매칭 중 상태 메시지를 연속으로 갱신하는 UX를 위해 설계했습니다.

```
"방 탐색 중..." → "방 생성 중..." → "Room Code: ABCD12" (3초 후 자동 닫힘)
```

- 새 메시지가 들어오면 기존 코루틴을 취소하고 새 코루틴 즉시 적용
- `WaitForSecondsRealtime` 사용으로 `TimeScale = 0` 상황에서도 동작
- 유저가 닫을 수 없는 정보성 팝업 vs 유저가 닫아야 하는 에러 팝업 분리 설계

---

### 3. 멀티 플랫폼 Relay 연결 (WebGL / Android / Editor)

WebGL 배포 시 wss 프로토콜이 필요한 문제를 런타임 체크로 해결했습니다.

```csharp
private void SetupTransport()
{
    var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    transport.UseWebSockets = Application.platform == RuntimePlatform.WebGLPlayer;
}

private string GetConnectionType()
{
    if (Application.isEditor) return "udp";
    if (Application.platform == RuntimePlatform.WebGLPlayer) return "wss";
    return "dtls";
}
```

- `#if UNITY_WEBGL` 컴파일 분기를 지양하고 런타임 체크로 단일 코드베이스 유지
- `AllocationUtils.ToRelayServerData(allocation, connectionType)` 사용으로 프로토콜별 RelayServerData 올바르게 생성
- Editor, WebGL, Android 세 플랫폼을 하나의 코드로 처리

---

### 4. MatchManager — MonoBehaviour 기반 네트워크 씬 전환

NGO의 `NetworkBehaviour` 라이프사이클이 씬 전환과 충돌하는 문제를 해결했습니다.

**문제**: `NetworkBehaviour`로 구현 시 `OnNetworkDespawn`에서 씬 로드 이벤트 구독이 해제되어 씬 전환 로직이 누락됨

**해결**: `MonoBehaviour(DontDestroyOnLoad)` + 별도 `NetworkBridge(NetworkBehaviour)` 분리

```
MatchManager (MonoBehaviour, DontDestroyOnLoad)
└── 씬 전환, 매칭, 로비 복귀 로직 담당

NetworkBridge (NetworkBehaviour)
└── ClientRpc 전용 — MatchManager를 대신해 클라이언트에 신호 전달
```

---

### 5. 씬 전환 안정성 — WaitUntil 기반 지연 초기화

씬 로드 완료 후 매니저 초기화 타이밍 문제를 코루틴으로 해결했습니다.

```csharp
// 씬 로드 완료 후 SpawnManager, GameManager가 준비될 때까지 대기
yield return new WaitUntil(() =>
    SpawnManager.Instance != null && GameManager.Instance != null);

SpawnManager.Instance.SpawnAllPlayers();
GameManager.Instance.StartGame();
```

---

### 6. 호스트 연결 끊김 처리

클라이언트가 호스트 퇴장을 감지하고 메인 메뉴로 복귀하는 흐름을 구현했습니다.

- `OnClientStopped` 콜백으로 비의도적 연결 끊김 감지
- `_isLeavingIntentionally` 플래그로 자발적 퇴장과 구분
- `LobbyServiceException` 무시 처리 (호스트가 먼저 Lobby 삭제하므로)
- ErrorPanel 2초 표시 후 씬 0으로 복귀

---

### 7. 과열(Overheat) 시스템

포탄 차지량에 비례해 열이 누적되고, MAX 시 5초간 완전 정지하는 시스템입니다.

- 클라이언트 로컬에서 열 계산 후 변화가 있을 때만 `ServerRpc`로 동기화 (`SyncHeatIfNeeded`)
- 불필요한 RPC 호출을 최소화해 네트워크 부하 감소
- HUD의 열 게이지는 색상(초록 → 노랑 → 빨강)으로 단계별 경고

---

### 8. 스프라이트 컨테이너 — Dict + List 이중 관리

Inspector에서 순서로 접근하고 코드에서 키로 접근하는 두 가지 요구를 모두 충족합니다.

```
SpriteContainer (추상 베이스, Dict + List 이중 관리)
├── TankSpriteContainer (싱글턴, DontDestroyOnLoad)
└── MapSpriteContainer
```

---

### 9. ScriptableObject 기반 오디오 시스템

```
AudioManager (MonoBehaviour, DontDestroyOnLoad)
├── BGMData (ScriptableObject) — BGMKey 상수로 접근
└── SFXData (ScriptableObject) — SFXKey 상수로 접근
```

- 볼륨 0~10 정수로 저장(PlayerPrefs), 내부적으로 `/10f` 변환
- `PlaySFXAtPosition(Vector3)` — 포탄 충돌 등 3D 위치 기반 SFX
- 포탄 발사, 피격, 파괴, 아이템, 과열 경보 등 ClientRpc 안에서 직접 호출

---

### 10. RecordManager — 전적 영구 저장

```csharp
// PlayerPrefs 기반 Win / Loss / Draw 영구 저장
RecordManager (MonoBehaviour, DontDestroyOnLoad)
```

게임 종료 시 결과에 따라 자동 기록, 다음 실행에도 유지됩니다.

---

## 게임 플레이

| 항목 | 내용 |
|------|------|
| 인원 | 2~4인 멀티플레이 |
| 맵 | 2종 (Map_Spaceship_Warehouse / Map_Spaceship_Arena) |
| 탱크 | 4종 (외형 차별화, 동일 스탯) |
| 포탄 | 무한 반사, 포탄끼리 충돌 시 소멸, 자기 포탄에 피격 가능 |
| 체력 | 2회 피격 시 게임오버 |
| 아이템 | 방패, 소형 로켓, 냉각수, 공습 포탄 (3분 특수) |
| 매칭 | 방 만들기 / 코드 참가 / 자동 매칭 |

---

## 출시 계획

```
1차 — itch.io WebGL 출시 (현재)
2차 — 싱글플레이 AI 봇, 파티클 이펙트, 탱크 스킨
3차 — Android 이식, Google Play, AdMob 연동
```
