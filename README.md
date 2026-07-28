# ⚡ AmbilightHA - Ultra-High Performance Gaming Ambilight for Home Assistant

**AmbilightHA** is a lightweight, ultra-high performance Windows desktop application built with **C# / .NET 9 (WPF)**. It captures your screen in real time with minimal latency, performs intelligent color extraction, and synchronizes the ambient lighting across classic Home Assistant smart bulbs via native WebSockets with built-in mesh network rate limiting.

---

## ✨ Features

* **⚡ Zero-Overhead DXGI Desktop Duplication**:
  * Powered by `Vortice.DXGI` & Direct3D 11 staging textures.
  * Direct VRAM-to-RAM DMA transfers with frame latency **< 2ms** and CPU usage **< 0.2%** (perfect for high-framerate gaming).

* **🎨 Advanced Color Extraction Engine**:
  * **Vibrant Accent Mode (Chroma-Weighted Sampling)**: Filters out gray UI elements, asphalt, concrete walls, and dark backgrounds to isolate vivid spell/explosion/laser accent colors.
  * **Standard Average Mode**: Classic arithmetic mean across the target screen regions.
  * Adjustments for **Saturation Boost**, **Brightness Scaling**, **Gamma Correction**, and **Minimum Brightness Floor** (prevents bulbs from shutting off completely in dark gaming scenes).

* **💡 Smart Home Assistant Synchronization**:
  * Direct **WebSocket API connection** (`ws://` / `wss://`) using Long-Lived Access Tokens.
  * **Bounded Channel Rate Limiting (`DropOldest`)**: Caps send rate (e.g., 5–10 updates/sec per light) to protect Zigbee/Wi-Fi mesh networks from traffic saturation.
  * **Temporal LERP Smoothing**: Blends color transitions seamlessly with Home Assistant's `transition` attribute.
  * **🔄 Automatic Capture & Restore**: Snapshots initial bulb states and colors upon starting, and restores them smoothly when stopped!

* **🖥️ Sleek Modern User Interface**:
  * Modern Dark Theme UI with dynamic tabbed configuration.
  * **Live Color Swatches**: Real-time preview for every assigned light entity.
  * **👁️ Interactive Visual Screen Zone Overlay**: Transparent overlay showing captured screen zone boundaries (Top, Bottom, Left, Right, Corners, Global Screen).
  * **🔔 System Tray Minimization**: Minimizes silently to the notification area with a custom RGB glowing bulb icon and context menu.

---

## 🚀 Quick Start

### Prerequisites
* **Windows 10 / 11 (64-bit)**
* **[.NET 9.0 SDK or Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)**
* **Home Assistant** instance with a Long-Lived Access Token.

### Build & Run
```bash
git clone https://github.com/Pixelzeus/AmbilightHA.git
cd AmbilightHA
dotnet build
dotnet run
```

---

## 🛠️ Configuration

1. Open the application.
2. In the **🎛️ Connexion & Réglages Image** tab:
   * Enter your Home Assistant URL (e.g. `http://192.168.1.50:8123`) and Long-Lived Access Token.
   * Adjust Saturation, Brightness, Gamma, Target FPS, and Rate Limit sliders.
3. In the **💡 Ampoules & Capteurs** tab:
   * Add your Home Assistant light entities (e.g., `light.living_room_left`, `light.tv_backlight`).
   * Assign a screen zone for each light entity (*Écran Global*, *Haut*, *Bas*, *Gauche*, *Droite*, *Coins*).
4. Click **▶ DÉMARRER**!

---

## 📜 License

This project is licensed under the [MIT License](LICENSE).
