using System;
using System.IO;
using System.Reflection;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using Xamarin.Essentials;

using Debug = System.Diagnostics.Debug;

namespace FamiStudio
{
    public static class PlatformUtils
    {
        public static void Initialize()
        {
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

        public static void StartMobileSaveOperationAsync(string mimeType, string filename, Action<string> callback)
        {
            FamiStudioForm.Instance.StartSaveFileActivityAsync(mimeType, filename, callback);
        }

        public static void FinishMobileSaveOperationAsync(bool commit, Action callback)
        {
            MainThread.InvokeOnMainThreadAsync(() =>
            {
                FamiStudioForm.Instance.FinishSaveFileActivityAsync(commit, callback);
            });
        }

        public static void ShareFileAsync(string filename, Action callback)
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

        public static string ApplicationVersion => "9.9.9"; // DROIDTODO

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
            Vibrator v = (Vibrator)Application.Context.GetSystemService(Context.VibratorService);
            v.Vibrate(VibrationEffect.CreatePredefined(VibrationEffect.EffectTick));
        }

        public static void VibrateClick()
        {
            Vibrator v = (Vibrator)Application.Context.GetSystemService(Context.VibratorService);
            v.Vibrate(VibrationEffect.CreatePredefined(VibrationEffect.EffectClick));
        }

        public static void ShowToast(string message)
        {
            MainThread.InvokeOnMainThreadAsync(() => 
            {
                Toast.MakeText(Application.Context, message, ToastLength.Short).Show(); 
            });
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