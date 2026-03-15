use tauri::{
    menu::{Menu, MenuItem},
    tray::{TrayIconBuilder},
    Manager, Runtime, WebviewWindow,
};

#[cfg(target_os = "windows")]
use windows::Win32::UI::WindowsAndMessaging::{
    GetWindowLongW, SetWindowLongW, GWL_EXSTYLE, WS_EX_LAYERED, WS_EX_TRANSPARENT,
};

#[cfg(target_os = "windows")]
fn setup_overlay_window<R: Runtime>(window: WebviewWindow<R>) {
    let hwnd = window.hwnd().unwrap().0;
    unsafe {
        let ex_style = GetWindowLongW(windows::Win32::Foundation::HWND(hwnd as _), GWL_EXSTYLE);
        let new_style = (ex_style | WS_EX_LAYERED.0 as i32) & !WS_EX_TRANSPARENT.0 as i32;
        let _ = SetWindowLongW(
            windows::Win32::Foundation::HWND(hwnd as _),
            GWL_EXSTYLE,
            new_style,
        );
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_autostart::init(tauri_plugin_autostart::MacosLauncher::LaunchAgent, Some(vec!["--hidden"])))
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            let _ = app.get_webview_window("main").map(|w| w.show().and_then(|_| w.set_focus()));
        }))
        .setup(|app| {
            // Create Tray Menu
            let quit_i = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;
            let show_i = MenuItem::with_id(app, "show", "Open Dashboard", true, None::<&str>)?;
            let menu = Menu::with_items(app, &[&show_i, &quit_i])?;

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
                    _ => {}
                })
                .build(app)?;

            // Setup Main Window hide on close
            if let Some(window) = app.get_webview_window("main") {
                let window_ = window.clone();
                window.on_window_event(move |event| {
                    if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                        api.prevent_close();
                        let _ = window_.hide();
                    }
                });
            }

            // Setup Overlay Window
            if let Some(overlay) = app.get_webview_window("overlay") {
                #[cfg(target_os = "windows")]
                setup_overlay_window(overlay);
            }

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
