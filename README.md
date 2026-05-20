# 한무 반사 (Endless Reflection)

> **Unity 6.3 LTS** 기반 1인 개발 탑뷰 탱크 멀티플레이 슈터  
> WebGL (itch.io) 배포

---

## 게임 플레이

| 항목 | 내용 |
|------|------|
| 인원 | 2~4인 멀티플레이 |
| 맵 | 2종 (Map_Spaceship_Warehouse / Map_Spaceship_Arena) |
| 탱크 | 4종 (외형 차별화, 동일 스탯) |
| 포탄 | 무한 반사, 포탄끼리 충돌 시 소멸, 자기 포탄에 피격 가능 |
| 체력 | 2회 피격 시 게임오버 |
| 매칭 | 방 만들기 / 코드 참가 / 자동 매칭 |

---

## 기술 스택

| 분류 | 사용 기술 |
|------|----------|
| 엔진 | Unity 6.3 LTS (6000.3.10f1) |
| 네트워크 | Unity Netcode for GameObjects (NGO), Unity Transport (UTP) |
| 백엔드 | Unity Gaming Services — Relay, Lobby |
| 플랫폼 | WebGL (wss), exe (wss), Editor (udp) 크로스 플랫폼 |
| UI | 커스텀 UI 프레임워크 (BrmnModules.UI) |
| 오디오 | 커스텀 AudioManager (ScriptableObject 기반) |

