# AutoKart 🏎🤖

A simple self-driving go-kart project capable of navigating a cone track.  
Includes a Unity-based simulator with a custom physics engine and real-time pathfinding.

![Project Demo](./media/demo.gif) <!-- GIF placeholder -->

## Overview
AutoKart combines embedded hardware, computer vision, and custom simulation tools to create a simple yet functional self-driving go-kart.  
The go-kart is equipped with **3 core sensors**:
- Wheel speed sensor
- IMU (accelerometer + gyroscope)
- Stereo RPI cameras

---

## Simulation Backend
The `AutoKartSim` folder contains a Unity-based simulator:
- **Custom Physics Engine** — models motor forces, tire grip, and other dynamics.
- **Simulated Sensors** — mirrors real-world IMU, wheel speed, and stereo camera data.
- **Real-Time Visualization** — displays pathfinding in various scenarios.
- Originally written in C#, converted to Python for embedded hardware.

![Simulation Screenshot](./media/sim_screenshot.png) <!-- Image placeholder -->

---

## Self-Driving Pipeline

[IMU + Wheel Speed] ---v ---------------------v  
[Stereo Cameras] -> [Cone Tracker] -> [Path Planner] -> [Path Follower]

---

## Hardware Overview (Add Links here)
- Premade go-kart frame
- QS138 70H V3 motor
- FarDriver ND72300 Motor Controller
- 10x Nissan Leaf Gen 1 battery modules (76V nominal)
- JBD 20S 100A BMS
- NVIDIA Jetson Orin Nano
- 2x Raspberry Pi cameras
- IMU sensor
- Worm gear motor (for steering)

![kart static pic](./media/hardware.png) <!-- Image placeholder -->

---

Media

    Demo Video <!-- placeholder -->

    Simulation Showcase <!-- placeholder -->
