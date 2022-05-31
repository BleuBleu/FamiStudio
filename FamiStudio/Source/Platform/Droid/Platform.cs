using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Widget;
using Xamarin.Essentials;

using Debug = System.Diagnostics.Debug;

namespace FamiStudio
{
    public static class PlatformUtils
    {
        private static Toast    lastToast;
        private static DateTime lastToastTime = DateTime.MinValue;
        private static string   lastToastText;
        
        private static byte[] internalClipboardData;

        private const int ToastShortDuration = 2000;

        public static void Initialize()
        {
        }

        public static bool IsInMainThread()
        {
            return MainThread.IsMainThread;
        }

        public static float GetDesktopScaling()
        {
            return 1.0f;
        }

        public static string UserProjectsDirectory => Path.Combine(Application.Context.FilesDir.AbsolutePath, "Projects");
        public static string SettingsDirectory     => System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        public static string ApplicationVersion    => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public static int GetOutputAudioSampleSampleRate()
        {
            // In order to get the LowLatency flag accepted on android, the app audio output
            // needs to match the device output sample rate.    
            AudioManager am = (AudioManager)Application.Context.GetSystemService(Context.AudioService);
            return int.Parse(am.GetProperty(AudioManager.PropertyOutputSampleRate), CultureInfo.InvariantCulture);
        }

        public static int GetPixelDensity()
        {
            var metrics = Platform.AppContext.Resources.DisplayMetrics;
            return (int)Math.Min(metrics.Xdpi, metrics.Ydpi);
        }

        public static Size GetScreenResolution()
        {
            var displayInfo = DeviceDisplay.MainDisplayInfo;
            return new Size((int)displayInfo.Width, (int)displayInfo.Height);
        }

        public static System.Windows.Forms.DialogResult MessageBox(string text, string title, System.Windows.Forms.MessageBoxButtons buttons)
        {
            Debug.Assert(false); 
            return System.Windows.Forms.DialogResult.None;
        }

        public static void MessageBoxAsync(string text, string title, System.Windows.Forms.MessageBoxButtons buttons, Action<System.Windows.Forms.DialogResult> callback = null)
        {
            AlertDialog.Builder dialog = new AlertDialog.Builder(Platform.CurrentActivity);
            AlertDialog alert = dialog.Create();
            
            alert.SetTitle(title);
            alert.SetMessage(text);

            if (buttons == System.Windows.Forms.MessageBoxButtons.YesNo ||
                buttons == System.Windows.Forms.MessageBoxButtons.YesNoCancel)
            {
                alert.SetButton("Yes", (c, ev) => { callback?.Invoke(System.Windows.Forms.DialogResult.Yes); });
                alert.SetButton2("No",  (c, ev) => { callback?.Invoke(System.Windows.Forms.DialogResult.No); });
                if (buttons == System.Windows.Forms.MessageBoxButtons.YesNoCancel)
                    alert.SetButton3("Cancel", (c, ev) => { callback?.Invoke(System.Windows.Forms.DialogResult.Cancel); });
            }
            else
            {
                alert.SetButton("OK", (c, ev) => { callback?.Invoke(System.Windows.Forms.DialogResult.OK); });
            }

            alert.Show();
        }

        public static void DelayedMessageBoxAsync(string text, string title)
        {
            FamiStudioForm.Instance.QueueDelayedMessageBox(text, title);
        }

        public static void StartMobileLoadFileOperationAsync(string mimeType, Action<string> callback)
        {
            FamiStudioForm.Instance.StartLoadFileActivityAsync(mimeType, callback);
        }

        public static void StartMobileSaveFileOperationAsync(string mimeType, string filename, Action<string> callback)
        {
            FamiStudioForm.Instance.StartSaveFileActivityAsync(mimeType, filename, callback);
        }

        public static void FinishMobileSaveFileOperationAsync(bool commit, Action callback)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                FamiStudioForm.Instance.FinishSaveFileActivityAsync(commit, callback);
            });
        }

        public static void StartShareFileAsync(string filename, Action callback)
        {
            FamiStudioForm.Instance.StartFileSharingActivity(filename, callback);
        }

        public static string GetShareFilename(string filename)
        {
            var shareDir = Path.Combine(Application.Context.CacheDir.AbsolutePath, "Sharing");
            Directory.CreateDirectory(shareDir);
            return Path.Combine(shareDir, filename);
        }

        public static string ShowSaveFileDialog(string title, string extensions, ref string defaultPath)
        {
            Debug.Assert(false);
            return null;
        }

        public static string[] ShowOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect, object parentWindow = null)
        {
            Debug.Assert(false);
            return null;
        }

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath, object parentWindow = null)
        {
            Debug.Assert(false);
            return null;
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
            Debug.Assert(false);
            return null;
        }

        public static string KeyCodeToString(int keyval)
        {
            Debug.Assert(false);
            return string.Empty;
        }

        public static Android.Graphics.Bitmap LoadBitmapFromResource(string name, bool premultiplied = false)
        {
            return Android.Graphics.BitmapFactory.DecodeStream(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(name), null,
                new Android.Graphics.BitmapFactory.Options() { InPremultiplied = premultiplied });
        }

        public static void VibrateTick()
        {
            if (Settings.AllowVibration)
            {
                var v = (Vibrator)Application.Context.GetSystemService(Context.VibratorService);
                var fx = Build.VERSION.SdkInt >= BuildVersionCodes.Q ?
                    VibrationEffect.CreatePredefined(VibrationEffect.EffectTick) :
                    VibrationEffect.CreateOneShot(20, 64);
                v.Vibrate(fx);
            }
        }

        public static void VibrateClick()
        {
            if (Settings.AllowVibration)
            {
                Vibrator v = (Vibrator)Application.Context.GetSystemService(Context.VibratorService);
                var fx = Build.VERSION.SdkInt >= BuildVersionCodes.Q ?
                    VibrationEffect.CreatePredefined(VibrationEffect.EffectHeavyClick) :
                    VibrationEffect.CreateOneShot(20, 128);
                v.Vibrate(fx);
            }
        }

        public static void ShowToast(string message)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                var now = DateTime.Now;

                if (lastToast != null)
                {
                    if (lastToastText != message || (now - lastToastTime).TotalMilliseconds > ToastShortDuration)
                    {
                        lastToast.Cancel();
                        lastToast = null;
                    }
                    else
                    {
                        return;
                    }
                }

                lastToast = Toast.MakeText(Application.Context, message, ToastLength.Short);
                lastToast.Show();
                lastToastText = message;
                lastToastTime = now;
            });
        }

        public static void Beep()
        {
        }

        public static void OpenUrl(string url)
        {
            FamiStudioForm.Instance.StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse(url)));
        }

        public static void SetClipboardData(byte[] data)
        {
            internalClipboardData = data;
        }

        public static byte[] GetClipboardData(int maxSize)
        {
            return internalClipboardData;
        }

        public static string GetClipboardString()
        {
            return null;
        }

        public static void ClearClipboardString()
        {
        }

        public const bool IsMobile  = true;
        public const bool IsAndroid = true;
        public const bool IsDesktop = false;
        public const bool IsWindows = false;
        public const bool IsLinux   = false;
        public const bool IsMacOS   = false;
        public const bool IsGTK     = false;
    }
}