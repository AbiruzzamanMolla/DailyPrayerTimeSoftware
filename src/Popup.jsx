import { useState, useEffect } from "react";
import { listen } from "@tauri-apps/api/event";

function Popup() {
  const [data, setData] = useState(null);

  useEffect(() => {
    let unlistenFn;
    const setup = async () => {
      unlistenFn = await listen("prayer-update", (event) => {
        setData(event.payload);
      });
    };
    setup();
    return () => { if (unlistenFn) unlistenFn(); };
  }, []);

  const displayData = data || {
    current: "Loading...",
    countdown: "--:--",
    location: "Waiting for data...",
    times: { Fajr: "--:--", Sunrise: "--:--", Dhuhr: "--:--", Asr: "--:--", Maghrib: "--:--", Isha: "--:--" }
  };

  const prayerNames = ["Fajr", "Sunrise", "Dhuhr", "Asr", "Maghrib", "Isha"];

  return (
    <div style={{
      width: '100vw',
      height: '100vh',
      background: 'rgba(15, 12, 35, 0.85)',
      backdropFilter: 'blur(12px)',
      border: '1px solid rgba(255, 255, 255, 0.1)',
      borderRadius: '8px',
      color: 'white',
      fontFamily: "'Segoe UI', sans-serif",
      display: 'flex',
      flexDirection: 'column',
      padding: '10px 12px',
      boxSizing: 'border-box',
      overflow: 'hidden',
      boxShadow: '0 10px 30px rgba(0,0,0,0.5)',
    }}>
      <div style={{ marginBottom: '8px', borderBottom: '1px solid rgba(255,255,255,0.1)', paddingBottom: '4px' }}>
        <div style={{ fontSize: '9px', opacity: 0.6, textTransform: 'uppercase' }}>Next Prayer</div>
        <div style={{ fontSize: '14px', fontWeight: 'bold', color: '#00ff00' }}>
          {displayData.current}: {displayData.countdown}
        </div>
        <div style={{ fontSize: '9px', opacity: 0.4, marginTop: '2px', whiteSpace: 'nowrap', textOverflow: 'ellipsis', overflow: 'hidden' }}>{displayData.location}</div>
      </div>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
        {prayerNames.map((name) => {
          const isActive = displayData.current === name || (name === "Sunrise" && displayData.current === "Doha");
          return (
            <div key={name} style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              padding: '4px 8px',
              borderRadius: '6px',
              background: isActive ? 'rgba(0, 255, 0, 0.15)' : 'transparent',
              border: isActive ? '1px solid rgba(0, 255, 0, 0.3)' : '1px solid transparent',
              transition: 'all 0.2s ease'
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                <span style={{ fontSize: '12px', fontWeight: isActive ? 'bold' : 'normal' }}>{name}</span>
              </div>
              <span style={{ 
                fontSize: '11px', 
                fontFamily: 'monospace',
                color: isActive ? '#00ff00' : 'rgba(255,255,255,0.8)'
              }}>
                {displayData.times[name]}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default Popup;
