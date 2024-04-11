global using Debug = FamiStudio.AndroidDebug;

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Widget;
using Xamarin.Essentials;

namespace FamiStudio
{
    public static class Platform
    {
        public delegate void AudioDeviceChangedDelegate();
        public static event AudioDeviceChangedDelegate AudioDeviceChanged;
        
        public static bool IsCommandLine => false;
        public static bool CanExportToVideo => true;

        private static Toast    lastToast;
        private static DateTime lastToastTime = DateTime.MinValue;
        private static string   lastToastText;
        private static int      glThreadId;
        
        private static byte[] internalClipboardData;

        private const int ToastShortDuration = 2000;
        private const int ToastLongDuration  = 3500;

        public static bool Initialize(bool commandLine)
        {
            HackForThaiCalendar();
            return true;
        }

        public static void Shutdown()
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

        public static IAudioStream CreateAudioStream(int rate, bool stereo, int bufferSizeMs)
        {
            return AndroidAudioStream.Create(rate, stereo, bufferSizeMs);
        }

        public static IVideoEncoder CreateVideoEncoder()
        {
            return new VideoEncoderAndroid();
        }

        public static string UserProjectsDirectory => Path.Combine(Application.Context.FilesDir.AbsolutePath, "Projects");
        public static string SettingsDirectory     => System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        public static string ApplicationVersion    => Assembly.GetExecutingAssembly().GetName().Version.ToString();
        
        public const string DllPrefix = "lib";
        public const string DllExtension = ".so";

        public const  bool IsPortableMode = false;
        public static bool ThreadOwnsGLContext => glThreadId == Thread.CurrentThread.ManagedThreadId;

        public static int GetOutputAudioSampleSampleRate()
        {
            // In order to get the LowLatency flag accepted on android, the app audio output
            // needs to match the device output sample rate.    
            AudioManager am = (AudioManager)Application.Context.GetSystemService(Context.AudioService);
            return int.Parse(am.GetProperty(AudioManager.PropertyOutputSampleRate), CultureInfo.InvariantCulture);
        }

        public static int GetPixelDensity()
        {
            var metrics = Xamarin.Essentials.Platform.AppContext.Resources.DisplayMetrics;
            return (int)Math.Min(metrics.Xdpi, metrics.Ydpi);
        }

        public static Size GetScreenResolution()
        {
            var displayInfo = DeviceDisplay.MainDisplayInfo;
            return new Size((int)displayInfo.Width, (int)displayInfo.Height);
        }

        public static DialogResult MessageBox(FamiStudioWindow win, string text, string title, MessageBoxButtons buttons)
        {
            Debug.Assert(false); 
            return DialogResult.None;
        }

        public static void MessageBoxAsync(FamiStudioWindow win, string text, string title, MessageBoxButtons buttons, Action<DialogResult> callback = null)
        {
            AlertDialog.Builder dialog = new AlertDialog.Builder(Xamarin.Essentials.Platform.CurrentActivity);
            AlertDialog alert = dialog.Create();
            
            alert.SetTitle(title);
            alert.SetMessage(text);

            if (buttons == MessageBoxButtons.YesNo ||
                buttons == MessageBoxButtons.YesNoCancel)
            {
                alert.SetButton("Yes", (c, ev) => { callback?.Invoke(DialogResult.Yes); });
                alert.SetButton2("No",  (c, ev) => { callback?.Invoke(DialogResult.No); });
                if (buttons == MessageBoxButtons.YesNoCancel)
                    alert.SetButton3("Cancel", (c, ev) => { callback?.Invoke(DialogResult.Cancel); });
            }
            else
            {
                alert.SetButton("OK", (c, ev) => { callback?.Invoke(DialogResult.OK); });
            }

            alert.Show();
        }

        public static void DelayedMessageBoxAsync(string text, string title)
        {
            FamiStudioWindow.Instance.QueueDelayedMessageBox(text, title);
        }

        public static void StartMobileLoadFileOperationAsync(string mimeType, Action<string> callback)
        {
            FamiStudioWindow.Instance.StartLoadFileActivityAsync(mimeType, callback);
        }

        public static void StartMobileSaveFileOperationAsync(string mimeType, string filename, Action<string> callback)
        {
            FamiStudioWindow.Instance.StartSaveFileActivityAsync(mimeType, filename, callback);
        }

        public static void FinishMobileSaveFileOperationAsync(bool commit, Action callback)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                FamiStudioWindow.Instance.FinishSaveFileActivityAsync(commit, callback);
            });
        }

        public static void StartShareFileAsync(string filename, Action callback)
        {
            FamiStudioWindow.Instance.StartFileSharingActivity(filename, callback);
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

        public static string[] ShowOpenFileDialog(string title, string extensions, ref string defaultPath, bool multiselect)
        {
            Debug.Assert(false);
            return null;
        }

        public static string ShowOpenFileDialog(string title, string extensions, ref string defaultPath)
        {
            Debug.Assert(false);
            return null;
        }

        public static string ShowBrowseFolderDialog(string title, ref string defaultPath)
        {
            Debug.Assert(false);
            return null;
        }

        public static int GetKeyScancode(Keys key)
        {
            Debug.Assert(false);
            return -1;
        }

        public static string KeyToString(Keys key)
        {
            Debug.Assert(false);
            return string.Empty;
        }

        public static string ScancodeToString(int scancode)
        {
            Debug.Assert(false);
            return string.Empty;
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

        public static void ForceScreenOn(bool on)
        {
            FamiStudioWindow.Instance.ForceScreenOn(on);
        }

        public static void ShowToast(FamiStudioWindow win, string message, bool longDuration = false, Action click = null)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                message  = message.Replace('\n', ' ').Trim();

                var duration = longDuration ? ToastLongDuration : ToastShortDuration;
                var now = DateTime.Now;

                if (lastToast != null)
                {
                    if (lastToastText != message || (now - lastToastTime).TotalMilliseconds > duration)
                    {
                        lastToast.Cancel();
                        lastToast = null;
                    }
                    else
                    {
                        return;
                    }
                }

                lastToast = Toast.MakeText(Application.Context, message, duration == ToastLongDuration ? ToastLength.Long : ToastLength.Short);
                lastToast.Show();
                lastToastText = message;
                lastToastTime = now;
            });
        }

        public static double TimeSeconds()
        {
            return SystemClock.ElapsedRealtimeNanos() / 1.0e9;
        }

        public static void Beep()
        {
        }

        public static void OpenUrl(string url)
        {
            FamiStudioWindow.Instance.StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse(url)));
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
            return "";
        }

        public static void SetClipboardString(string s)
        {
        }

        public static void ClearClipboardString()
        {
        }

        public static void AcquireGLContext()
        {
            glThreadId = Thread.CurrentThread.ManagedThreadId;
        }

        private static void HackForThaiCalendar()
        {
            // These classes won't be linked away because of the code,
            // but we also won't have to construct unnecessarily either,
            // hence the if statement with (hopefully) impossible
            // runtime condition.
            //
            // This is to resolve crash at CultureInfo.CurrentCulture
            // when language is set to Thai. See
            // https://github.com/xamarin/Xamarin.Forms/issues/4037
            if (Android.OS.Environment.DirectoryDocuments == "\\\\**_never_POSSIBLE_**\\\\")
            {
                new System.Globalization.ChineseLunisolarCalendar();
                new System.Globalization.HebrewCalendar();
                new System.Globalization.HijriCalendar();
                new System.Globalization.JapaneseCalendar();
                new System.Globalization.JapaneseLunisolarCalendar();
                new System.Globalization.KoreanCalendar();
                new System.Globalization.KoreanLunisolarCalendar();
                new System.Globalization.PersianCalendar();
                new System.Globalization.TaiwanCalendar();
                new System.Globalization.TaiwanLunisolarCalendar();
                new System.Globalization.ThaiBuddhistCalendar();
                new System.Globalization.UmAlQuraCalendar();
            }
        }

        public const bool IsMobile  = true;
        public const bool IsAndroid = true;
        public const bool IsDesktop = false;
        public const bool IsWindows = false;
        public const bool IsLinux   = false;
        public const bool IsMacOS   = false;
    }

    // By default Debug.Assert() doesnt break in the debugger on Android. Workaround.
    public class AndroidDebug
    {
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Write(string message)
        {
            Console.Write(message);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert(bool condition, [CallerArgumentExpression("condition")] string message = null)
        {
            if (!condition)
            {
                Console.WriteLine($"ASSERTION FAILED! {message}");
                global::System.Diagnostics.Debugger.Break(); // This doesnt even work 1/2 the time... Ugh.
            }
        }
    }
}