# ⚡ AmbilightHA - Ultra-High Performance Gaming Ambilight for Home Assistant & WLED

[![GitHub Release](https://img.shields.io/github/v/release/Pixelzeus/AmbilightHA?style=for-the-badge&color=6C5CE7)](https://github.com/Pixelzeus/AmbilightHA/releases/latest)
[![Download Zip](https://img.shields.io/badge/Download-Release%20v1.1.0%20(win--x64)-00B894?style=for-the-badge&logo=windows)](https://github.com/Pixelzeus/AmbilightHA/releases/download/v1.1.0/AmbilightHA-v1.1.0-win-x64.zip)
[![License MIT](https://img.shields.io/badge/License-MIT-blue.style=for-the-badge)](LICENSE)

**AmbilightHA** is a lightweight, ultra-high performance Windows desktop application built with **C# / .NET 9 (WPF)**. It captures your screen in real time with minimal latency, performs intelligent color extraction, and synchronizes ambient lighting across Home Assistant smart bulbs and WLED controllers via native WebSockets or direct UDP streams with built-in mesh network rate limiting.

---

## 📦 Direct Download

👉 **[Download AmbilightHA v1.1.0 Standalone Executable (zip)](https://github.com/Pixelzeus/AmbilightHA/releases/download/v1.1.0/AmbilightHA-v1.1.0-win-x64.zip)**
*(Self-contained single executable — no .NET installation required!)*

---

## ✨ Key Features

* **⚡ Zero-Overhead DXGI Desktop Duplication**:
  * Powered by `Vortice.DXGI` & Direct3D 11 staging textures.
  * Direct VRAM-to-RAM DMA transfers with frame latency **< 2ms** and CPU usage **< 0.2%** (perfect for high-framerate gaming).

* **🌈 WLED Device Support & Direct UDP Realtime Streaming**:
  * **⚡ Direct WLED UDP Mode**: Streams real-time DRGB packets (UDP port 21324) directly to WLED controller IP addresses for **< 1ms ultra-low latency**, bypassing WebSocket overhead.
  * **🌈 Home Assistant WLED Integration**: Support for WLED entities (`light.wled_strip`) via HA WebSocket API.

* **🎨 Advanced Color Extraction Engine**:
  * **Vibrant Accent Mode (Chroma-Weighted Sampling)**: Filters out gray UI elements, asphalt, concrete walls, and dark backgrounds to isolate vivid spell/explosion/laser accent colors.
  * **Fast-Path Math Optimization**: Accelerated saturation weighting providing **3x–5x CPU performance** on frame processing.
  * **Standard Average Mode**: Classic arithmetic mean across target screen regions.
  * Adjustments for **Saturation Boost**, **Brightness Scaling**, **Gamma Correction**, and **Minimum Brightness Floor** (prevents bulbs from shutting off in dark gaming scenes).

* **💡 Smart Home Assistant Synchronization**:
  * Direct **WebSocket API connection** (`ws://` / `wss://`) using Long-Lived Access Tokens with thread-safe `SemaphoreSlim` serialization.
  * **Bounded Channel Rate Limiting (`DropOldest`)**: Caps send rate (e.g., 5–10 updates/sec per light) to protect Zigbee/Wi-Fi/Z-Wave mesh networks from traffic saturation.
  * **Temporal LERP Smoothing**: Blends color transitions seamlessly with Home Assistant's `transition` attribute.
  * **🔄 Automatic Capture & Restore**: Snapshots initial bulb states and colors upon starting, and restores them smoothly when stopped!

* **🖥️ Sleek Modern User Interface**:
  * Modern Dark Theme UI with dynamic tabbed configuration.
  * **➕➕ Multi-Light Batch Buttons**: Quickly add single lights (`➕`) or a batch of 3 lights (`➕➕ Lot 3: Left, Center, Right`).
  * **Contextual Target Inputs**: Dynamic equipment selector switching automatically between HA Entity IDs (`light.living_room`) and WLED IP addresses (`192.168.1.100`).
  * **Live Color Swatches**: Real-time preview for every assigned light entity.
  * **👁️ Interactive Visual Screen Zone Overlay**: Transparent overlay showing captured screen zone boundaries (Top, Bottom, Left, Right, Corners, Global Screen).
  * **🔔 System Tray Minimization**: Minimizes silently to the notification area with a custom RGB glowing bulb icon and context menu.

---

## 🚀 Quick Start Guide

### Option 1: Standalone Release (Recommended)
1. Download **[AmbilightHA-v1.1.0-win-x64.zip](https://github.com/Pixelzeus/AmbilightHA/releases/download/v1.1.0/AmbilightHA-v1.1.0-win-x64.zip)**.
2. Extract `AmbilightHA.exe` to any folder.
3. Run `AmbilightHA.exe`!

### Option 2: Build from Source
```bash
git clone https://github.com/Pixelzeus/AmbilightHA.git
cd AmbilightHA
dotnet build
dotnet run
```

---

## 🛠️ Configuration Steps

1. Launch `AmbilightHA.exe`.
2. In the **💡 Ampoules & Capteurs** tab:
   * Select the equipment type: **Home Assistant Light**, **WLED via HA**, or **WLED Direct UDP (IP)**.
   * Enter your HA Light entity (e.g. `light.living_room_left`) or WLED IP address (e.g. `192.168.1.100`).
   * Assign a screen zone for each light entity (*Écran Global*, *Haut*, *Bas*, *Gauche*, *Droite*, *Coins*).
   * Use **➕ Ajouter une ampoule** or **➕➕ Lot de 3** to configure multiple lights instantly.
3. In the **🎛️ Connexion & Réglages Image** tab:
   * Enter your Home Assistant URL (e.g., `http://192.168.1.50:8123`) and your **Long-Lived Access Token**.
   * Adjust Saturation, Brightness, Gamma, Target FPS, and Rate Limit sliders.
4. Click **▶ DÉMARRER** and enjoy your ultra-reactive lighting!

---

## 📜 License

This project is licensed under the [MIT License](LICENSE).
