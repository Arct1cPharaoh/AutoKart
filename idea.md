# Project

Create a self dring go kart
interface with vcu using udp

## 🏎️ AutoKart Simulation Design
### Simulation Goals
- Simulate a go-kart in Unity using physics and C#
- Interface with external driving logic over UDP
- Match real-world control input/output format
- Use visual/environmental elements like cones for navigation

---

### 🎮 Assets

- **Kart model**: basic 3D kart with wheels (Placeholder for now)
- **Cones/obstacles**: placeable prefab for path-following & navigation
- **Ground plane**: large flat surface (rough asphalt texture)

---

### 🧠 Code Components

#### 1. CarController.cs
- Inputs: throttle, brake, steering
- Outputs: velocity, steering angle, GPS-like pose, optionally IMU
- Mimics a **VCU**: converts commands into rigidbody physics

#### 2. UDPManager.cs
- Listens for control packets from code
- Sends telemetry back to AI
- Lightweight, non-blocking UDP using Unity’s `UdpClient`

#### 3. SensorSimulator.cs *(optional but powerful)*
- Fake sensors, camera feeds (to be added later)
- Export Unity camera frames if needed via socket or files


---


### 🔌 Network Protocol (simplified idea)

**Incoming (from Jetson):**
```json
{ "steering": 0.1, "throttle": 0.8 }
```
**outgoing (to jetson)**
```json
{ "x": 1.2, "y": 4.5, "heading": 90, "speed": 2.3 }
```


