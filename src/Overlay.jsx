import { useState, useEffect } from "react";
import { listen } from "@tauri-apps/api/event";
import { getCurrentWindow } from "@tauri-apps/api/window";

function Overlay() {
  const [prayerData, setPrayerData] = useState({ name: "---", time: "--:--" });

  useEffect(() => {
    console.log("📡 Overlay Window: Initializing listener for 'prayer-update'");
    let unsubscribeData;
    let unsubscribeToggle;

    const setup = async () => {
      unsubscribeData = await listen("prayer-update", (event) => {
        setPrayerData(event.payload);
      });

      // Listen to visibility instructions from the main App state (which listens to tray & UI)
      const win = getCurrentWindow();
      unsubscribeToggle = await listen("set-visibility", async (event) => {
        const shouldShow = event.payload;
        if (shouldShow) {
          await win.show();
        } else {
          await win.hide();
        }
      });

      // Aggressively maintain z-order against Windows 11 Taskbar
      const zOrderInterval = setInterval(() => {
        win.setAlwaysOnTop(true);
      }, 1500);

      // Save interval ID for cleanup
      globalThis._zOrderInterval = zOrderInterval;
    };

    setup();
    return () => {
      if (unsubscribeData) unsubscribeData();
      if (unsubscribeToggle) unsubscribeToggle();
      if (window._zOrderInterval) clearInterval(window._zOrderInterval);
    };
  }, []);

  const handleMouseDown = async () => {
    console.log("🖱️ Overlay: Mouse down, starting drag...");
    try {
      await getCurrentWindow().startDragging();
    } catch (err) {
      console.error("❌ Overlay: Drag error:", err);
    }
  };

  return (
    <div 
      onMouseDown={handleMouseDown}
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '0 12px',
        height: '100vh',
        width: '100vw',
        color: 'white',
        fontFamily: "'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
        fontSize: '12px',
        fontWeight: '700',
        textShadow: '0 1px 2px rgba(0,0,0,0.5)',
        overflow: 'hidden',
        userSelect: 'none',
        cursor: 'default',
        background: 'rgba(23, 9, 57, 0.65)',
        border: '1px solid rgba(255,255,255,0.15)',
        borderRadius: '6px',
        backdropFilter: 'blur(10px)',
        boxShadow: '0 2px 10px rgba(0,0,0,0.2)',
      }}
    >
      <div style={{
        width: '2px',
        height: '12px',
        background: '#00ff00',
        borderRadius: '1px',
        marginRight: '10px',
        boxShadow: '0 0 4px rgba(0,255,0,0.6)'
      }} />
      <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
        <span style={{ opacity: 0.6, fontSize: '11px', textTransform: 'uppercase', letterSpacing: '0.4px' }}>
          {prayerData.name || '---'}:
        </span>
        <span style={{ color: '#00ff00', fontFamily: 'monospace', fontSize: '14px' }}>
          {prayerData.time || '--:--'}
        </span>
      </div>
    </div>
  );
}

export default Overlay;
