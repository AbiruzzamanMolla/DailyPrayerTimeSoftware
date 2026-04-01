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
        padding: '0 10px',
        height: '100vh',
        width: '100vw',
        color: 'white',
        fontFamily: "'Segoe UI', Roboto, Helvetica, Arial, sans-serif",
        fontSize: '12px',
        fontWeight: '600',
        textShadow: '0 1px 2px rgba(0,0,0,0.5)',
        overflow: 'hidden',
        userSelect: 'none',
        cursor: 'move',
        background: 'rgba(23, 9, 57, 0.75)',
        border: '1px solid rgba(255,255,255,0.15)',
        borderRadius: '8px',
        backdropFilter: 'blur(8px)',
        boxShadow: '0 4px 15px rgba(0,0,0,0.3)',
      }}
    >
      <div style={{
        width: '3px',
        height: '14px',
        background: '#00ff00',
        borderRadius: '2px',
        marginRight: '8px',
        boxShadow: '0 0 5px rgba(0,255,0,0.5)'
      }} />
      <div style={{ display: 'flex', alignItems: 'baseline', gap: '4px' }}>
        <span style={{ opacity: 0.8, fontSize: '10px', textTransform: 'uppercase', letterSpacing: '0.5px' }}>
          {prayerData.name || '---'}:
        </span>
        <span style={{ color: '#00ff00', fontFamily: 'monospace', fontSize: '13px' }}>
          {prayerData.time || '--:--'}
        </span>
      </div>
    </div>
  );
}

export default Overlay;
