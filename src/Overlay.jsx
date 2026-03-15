import { useState, useEffect } from "react";
import { listen } from "@tauri-apps/api/event";
import { getCurrentWindow } from "@tauri-apps/api/window";

function Overlay() {
  const [prayerData, setPrayerData] = useState({ name: "---", time: "--:--" });

  useEffect(() => {
    console.log("📡 Overlay Window: Initializing listener for 'prayer-update'");
    let unlistenFn;
    
    const setup = async () => {
      unlistenFn = await listen("prayer-update", (event) => {
        console.log("📥 Overlay Window: Received payload", event.payload);
        setPrayerData(event.payload);
      });
    };

    setup();

    return () => {
      if (unlistenFn) {
        console.log("📡 Overlay Window: Unsubscribing");
        unlistenFn();
      }
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
        padding: '0 8px',
        height: '100vh',
        width: '100vw',
        color: 'white',
        fontFamily: 'Segoe UI, Tahoma, Geneva, Verdana, sans-serif',
        fontSize: '11px',
        fontWeight: '700',
        textShadow: '0 1px 2px rgba(0,0,0,0.8)',
        overflow: 'hidden',
        userSelect: 'none',
        cursor: 'move',
        background: 'rgba(23, 9, 57, 0.7)', // Dark semi-transparent background
        border: '1px solid rgba(255,255,255,0.2)',
        borderRadius: '6px',
        backdropFilter: 'blur(4px)',
      }}
    >
      <div style={{
        width: '4px',
        height: '16px',
        background: 'rgba(255,255,255,0.3)',
        borderRadius: '2px',
        marginRight: '8px'
      }} />
      <span style={{ marginRight: '4px', opacity: 0.9 }}>{prayerData.name}:</span>
      <span style={{ color: '#00ff00' }}>{prayerData.time}</span>
    </div>
  );
}

export default Overlay;
