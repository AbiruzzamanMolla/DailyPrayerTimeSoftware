use tauri::{
    menu::{Menu, MenuItem},
    tray::TrayIconBuilder,
    Emitter, Manager, Runtime, WebviewWindow,
};

use windows::Win32::Foundation::HWND;
#[cfg(target_os = "windows")]
use windows::Win32::UI::WindowsAndMessaging::{
    GetWindowLongW, SetWindowLongW, SetWindowPos, GWL_EXSTYLE, HWND_TOPMOST, SWP_NOMOVE,
    SWP_NOSIZE, SWP_SHOWWINDOW, WS_EX_LAYERED, WS_EX_NOACTIVATE, WS_EX_TOPMOST,
    SetParent, FindWindowA,
};
use windows::core::PCSTR;

#[cfg(target_os = "windows")]
fn setup_native_window<R: Runtime>(window: WebviewWindow<R>) {
    let hwnd = window.hwnd().unwrap().0;
    let hwnd_struct = HWND(hwnd as _);

    unsafe {
        let ex_style = GetWindowLongW(hwnd_struct, GWL_EXSTYLE);

        // WS_EX_TOPMOST: Reinforce for always-on-top nature
        // WS_EX_NOACTIVATE: prevents the window from being activated when clicked (non-stealing focus)
        let _ = SetWindowLongW(
            hwnd_struct,
            GWL_EXSTYLE,
            ex_style | WS_EX_LAYERED.0 as i32 | WS_EX_TOPMOST.0 as i32 | WS_EX_NOACTIVATE.0 as i32,
        );

        let _ = SetWindowPos(
            hwnd_struct,
            HWND_TOPMOST,
            0,
            0,
            0,
            0,
            SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW,
        );

        // Taskbar Injection: Make the overlay a child of the Windows Taskbar
        // This ensures the Windows 11 Taskbar will NEVER cover our overlay because they share the same Z-band
        let taskbar_hwnd = FindWindowA(windows::core::PCSTR(b"Shell_TrayWnd\0".as_ptr()), windows::core::PCSTR::null());
        if taskbar_hwnd.0 != 0 {
            // Un-comment the line below if you want strict child injection, 
            // but setting the taskbar as OWNER is usually safer for dragging:
            // let _ = SetParent(hwnd_struct, taskbar_hwnd);
            
            // Actually, to fully inject into the taskbar hierarchy (what the user asked for), SetParent is correct
            let _ = SetParent(hwnd_struct, taskbar_hwnd);
        }
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_autostart::init(
            tauri_plugin_autostart::MacosLauncher::LaunchAgent,
            Some(vec!["--hidden"]),
        ))
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            let _ = app
                .get_webview_window("main")
                .map(|w| w.show().and_then(|_| w.set_focus()));
        }))
        .setup(|app| {
            // Create Tray Menu
            let quit_i = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;
            let show_i = MenuItem::with_id(app, "show", "Open Dashboard", true, None::<&str>)?;
            let toggle_overlay_i =
                MenuItem::with_id(app, "toggle_overlay", "Toggle Overlay", true, None::<&str>)?;
            let menu = Menu::with_items(app, &[&show_i, &toggle_overlay_i, &quit_i])?;

            let _tray = TrayIconBuilder::new()
                .icon(app.default_window_icon().unwrap().clone())
                .menu(&menu)
                .on_menu_event(|app, event| match event.id.as_ref() {
                    "quit" => {
                        app.exit(0);
                    }
                    "show" => {
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.show();
                            let _ = window.set_focus();
                        }
                    }
                    "toggle_overlay" => {
                        let _ = app.emit("toggle-overlay-request", ());
                    }
                    _ => {}
                })
                .build(app)?;

            // Setup Window Events
            if let Some(window) = app.get_webview_window("main") {
                let window_ = window.clone();
                window.on_window_event(move |event| {
                    if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                        api.prevent_close();
                        let _ = window_.hide();
                    }
                });
            }

            // Setup Native Styles for Overlay
            if let Some(overlay) = app.get_webview_window("overlay") {
                #[cfg(target_os = "windows")]
                setup_native_window(overlay);
            }

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
