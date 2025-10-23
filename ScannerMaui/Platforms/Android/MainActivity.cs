using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Core.OS;

namespace ScannerMaui
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            // Auto-setup permissions with better handling
            SetupPermissions();
        }

        private void SetupPermissions()
        {
            try
            {
                // Define all required permissions
                var permissions = new string[]
                {
                    "android.permission.CAMERA",
                    "android.permission.READ_EXTERNAL_STORAGE",
                    "android.permission.WRITE_EXTERNAL_STORAGE",
                    "android.permission.INTERNET",
                    "android.permission.ACCESS_NETWORK_STATE",
                    "android.permission.RECORD_AUDIO",
                    "android.permission.MODIFY_AUDIO_SETTINGS",
                    "android.permission.VIBRATE"
                };

                // Check Android version for different permission handling
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu) // Android 13+
                {
                    // For Android 13+, some permissions are automatically granted
                    RequestPermissionsForAndroid13Plus(permissions);
                }
                else
                {
                    // For older Android versions
                    RequestPermissionsLegacy(permissions);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting up permissions: {ex.Message}");
            }
        }

        private void RequestPermissionsForAndroid13Plus(string[] permissions)
        {
            var permissionsToRequest = new List<string>();
            
            foreach (var permission in permissions)
            {
                // Skip permissions that are automatically granted in Android 13+
                if (permission == "android.permission.READ_EXTERNAL_STORAGE" || 
                    permission == "android.permission.WRITE_EXTERNAL_STORAGE")
                {
                    continue; // These are handled by new media permissions
                }
                
                if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
                {
                    permissionsToRequest.Add(permission);
                }
            }

            if (permissionsToRequest.Count > 0)
            {
                // Request permissions with better user experience
                ActivityCompat.RequestPermissions(this, permissionsToRequest.ToArray(), 100);
            }
        }

        private void RequestPermissionsLegacy(string[] permissions)
        {
            var permissionsToRequest = new List<string>();
            
            foreach (var permission in permissions)
            {
                if (ContextCompat.CheckSelfPermission(this, permission) != Permission.Granted)
                {
                    permissionsToRequest.Add(permission);
                }
            }

            if (permissionsToRequest.Count > 0)
            {
                ActivityCompat.RequestPermissions(this, permissionsToRequest.ToArray(), 100);
            }
        }

        // Handle permission results automatically
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            
            if (requestCode == 100)
            {
                var allGranted = true;
                for (int i = 0; i < permissions.Length; i++)
                {
                    if (grantResults[i] != Permission.Granted)
                    {
                        allGranted = false;
                        System.Diagnostics.Debug.WriteLine($"Permission denied: {permissions[i]}");
                    }
                }
                
                if (allGranted)
                {
                    System.Diagnostics.Debug.WriteLine("All permissions granted successfully!");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Some permissions were denied. App may not function properly.");
                }
            }
        }
    }
}
