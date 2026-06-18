using System;
using System.Runtime.InteropServices;

namespace DailyPrayerTime.Native.Helpers
{
    public static class VolumeHelper
    {
        [ComImport]
        [Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumerator
        {
        }

        [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            int NotImpl1();
            [PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppDevice);
        }

        [Guid("D66606E4-827E-49E1-A106-407D40403454")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int dwClsContext, IntPtr pActivationParams, out IAudioEndpointVolume ppInterface);
        }

        [Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioEndpointVolume
        {
            int NotImpl1();
            int NotImpl2();
            int NotImpl3();
            int NotImpl4();
            int NotImpl5();
            int NotImpl6();
            [PreserveSig]
            int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
            [PreserveSig]
            int GetMute(out bool pbMute);
        }

        private static IAudioEndpointVolume? GetVolumeInterface()
        {
            try
            {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                // dataFlow: 0 = Render (output), role: 1 = Multimedia
                int hr = enumerator.GetDefaultAudioEndpoint(0, 1, out IMMDevice device);
                if (hr != 0) return null;

                var iid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
                hr = device.Activate(ref iid, 1, IntPtr.Zero, out IAudioEndpointVolume volume);
                if (hr != 0) return null;

                return volume;
            }
            catch
            {
                return null;
            }
        }

        public static bool IsMuted()
        {
            var volume = GetVolumeInterface();
            if (volume == null) return false;
            try
            {
                volume.GetMute(out bool muted);
                Marshal.ReleaseComObject(volume);
                return muted;
            }
            catch
            {
                return false;
            }
        }

        public static void SetMute(bool mute)
        {
            var volume = GetVolumeInterface();
            if (volume == null) return;
            try
            {
                var context = Guid.Empty;
                volume.SetMute(mute, ref context);
                Marshal.ReleaseComObject(volume);
            }
            catch
            {
            }
        }
    }
}
