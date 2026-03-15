# Daily Prayer Timer 🕋

A beautiful, lightweight, and modern prayer time desktop application built with **Tauri 2.0**, **React**, and **Rust**. Keep track of your daily prayers with a stunning dashboard and a convenient taskbar overlay.

![App Dashboard Showcase](https://raw.githubusercontent.com/AbiruzzamanMolla/DailyPrayerTimerSoftware/main/public/screenshots/01.png)

## 🚀 Key Features

- **Dashboard View**: A full-featured window with beautiful gradients, countdown timers, and Hijri dates.
- **Taskbar Overlay**: A minimalist, semi-transparent timer that sits cleanly on your taskbar or anywhere on your screen. Completely draggable and positionable.
- **Tauri 2.0 Powered**: Ultra-fast performance and low resource usage compared to traditional desktop apps.
- **Global Location Support**: Search thousands of cities worldwide or enter manual coordinates for absolute precision.
- **Offline Calculations**: Uses the **Adhan-js** library to calculate timings locally. No internet required once configured.
- **Theme Engine**: Customize background colors, gradients, and text styles to make the app truly yours.
- **Windows Notifications**: Stay alerted with native system notifications for every prayer time.
- **Ramadan Mode**: Special tracking for Sehri and Iftar times with progress bars.
- **Prohibited Times Warning**: Real-time alerts for Sunrise, Zawal, and sunset periods where prayer is prohibited.

## 🛠️ Technology Stack

- **Backend**: Rust (Tauri 2.0)
- **Frontend**: React (Vite)
- **Logic**: Adhan-js for prayer time calculations
- **State Management**: LocalStorage for persistent settings
- **Styling**: Modern CSS with Glassmorphism and Dynamic Gradients

## 📸 Screenshots

|                                                        Dashboard                                                         |                                                    Theme Settings                                                     |                                                   Location Discovery                                                    |
| :----------------------------------------------------------------------------------------------------------------------: | :-------------------------------------------------------------------------------------------------------------------: | :---------------------------------------------------------------------------------------------------------------------: |
| ![Dashboard](https://raw.githubusercontent.com/AbiruzzamanMolla/DailyPrayerTimerSoftware/main/public/screenshots/01.png) | ![Themes](https://raw.githubusercontent.com/AbiruzzamanMolla/DailyPrayerTimerSoftware/main/public/screenshots/02.png) | ![Location](https://raw.githubusercontent.com/AbiruzzamanMolla/DailyPrayerTimerSoftware/main/public/screenshots/03.png) |

## ⚙️ Development & Build

### Prerequisites

- Rust & Cargo installed ([rust-lang.org](https://www.rust-lang.org/))
- Node.js ([nodejs.org](https://nodejs.org/))

### Installation

1.  Clone the repository:
    ```bash
    git clone https://github.com/AbiruzzamanMolla/DailyPrayerTimerSoftware.git
    cd DailyPrayerTimerSoftware
    ```
2.  Install dependencies:
    ```bash
    npm install
    ```

### Run Locally

```bash
npm run tauri dev
```

### Build for Windows

```bash
npm run tauri build
```

## 📜 License

This project is licensed under the MIT License.

---
