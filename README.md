# 🚁 MediapipeDroning
> **Real-time Dual-Hand Gesture Controlled Drone Simulation in Unity using MediaPipe**

MediapipeDroning은 복잡한 하드웨어 컨트롤러 없이 **컴퓨터 비전과 인공지능 기반의 손짓 제스처**만으로 유니티(Unity) 3D 공간 속 드론을 직관적이고 세밀하게 조종할 수 있는 융합 프로젝트입니다.

---

## 🌟 Key Features

- **Dual-Hand Mode 2 Control System**  
  실제 드론 조종기(Mode 2) 방식에서 착안하여 양손의 역할을 분리한 고도화된 제어 체계
  - **Left Hand**: 상승(Ascent), 하강(Descent), 좌우 수평 이동(Roll)
  - **Right Hand**: 전진(Pitch Forward), 후진(Pitch Backward), 제자리 회전(Yaw)
- **Precise Landmark Tracking**  
  MediaPipe Tasks API (`HandLandmarker`)를 이용해 21개 3D 랜드마크의 마디 좌표를 추적, 팔 높이에 영향을 받지 않는 손가락 상대 벡터 기반 방향 판정
- **Context-Aware Safety Mechanism**  
  - 단일 손 주먹 쥐기: 해당 입력 축만 중립(`NONE`) 처리
  - 양손 동시 주먹 쥐기: 긴급 정지 및 호버링(`STOP`)
- **Responsive Dynamic Camera**  
  Cinemachine 3.x 기반으로 드론의 회전 및 이동 축을 딜레이 없이 실시간 추적하는 무손실 시점 탑재
- **Low-Latency Socket Communication**  
  Python(OpenCV + MediaPipe)과 Unity C# 엔진 간 TCP/IP 소켓 통신을 통한 실시간 데이터 스트리밍

---

## 🛠 Tech Stack

- **Engine & Graphics**: Unity 6, Cinemachine 3.x
- **Computer Vision & AI**: Python 3.x, OpenCV, MediaPipe Tasks API
- **Networking**: TCP/IP Socket (Local Server-Client Architecture)
- **Language**: C#, Python

---

## 🎮 Control Mapping

| 분류 | 제스처 (Hand Gesture) | 동작 (Drone Action) |
| :--- | :--- | :--- |
| **왼손 (Left)** | 검지 세우기 (Up) | 상승 (Ascend) |
| | 검지 내리기 (Down) | 하강 (Descend) |
| | 손 끝 좌/우 기울이기 | 좌/우 평행 이동 |
| | 주먹 쥐기 (Fist) | 왼손 입력 중립 (`NONE`) |
| **오른손 (Right)**| 검지 세우기 (Up) | 전진 (Forward) |
| | 검지 내리기 (Down) | 후진 (Backward) |
| | 손 끝 좌/우 기울이기 | 좌/우 회전 (Yaw) |
| | 주먹 쥐기 (Fist) | 오른손 입력 중립 (`NONE`) |
| **양손 (Both)** | **양손 동시 주먹 쥐기** | **긴급 정지 (EMERGENCY STOP)** |

---

## 🚀 Quick Start

1. **Python Server Execution**
   ```bash
   python drone_server.py