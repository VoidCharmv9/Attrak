using Microsoft.Maui.Authentication;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace ScannerMaui.Services
{
    public class PermissionService
    {
        public static async Task<bool> RequestCameraPermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                
                if (status == PermissionStatus.Granted)
                {
                    Debug.WriteLine("Camera permission already granted");
                    return true;
                }
                
                if (status == PermissionStatus.Denied)
                {
                    Debug.WriteLine("Camera permission denied");
                    return false;
                }
                
                // Request permission
                status = await Permissions.RequestAsync<Permissions.Camera>();
                
                if (status == PermissionStatus.Granted)
                {
                    Debug.WriteLine("Camera permission granted");
                    return true;
                }
                else
                {
                    Debug.WriteLine("Camera permission denied by user");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error requesting camera permission: {ex.Message}");
                return false;
            }
        }
        
        public static async Task<bool> RequestStoragePermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                
                if (status == PermissionStatus.Granted)
                {
                    Debug.WriteLine("Storage permission already granted");
                    return true;
                }
                
                if (status == PermissionStatus.Denied)
                {
                    Debug.WriteLine("Storage permission denied");
                    return false;
                }
                
                // Request permission
                status = await Permissions.RequestAsync<Permissions.StorageRead>();
                
                if (status == PermissionStatus.Granted)
                {
                    Debug.WriteLine("Storage permission granted");
                    return true;
                }
                else
                {
                    Debug.WriteLine("Storage permission denied by user");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error requesting storage permission: {ex.Message}");
                return false;
            }
        }
        
        public static async Task<bool> RequestAllRequiredPermissionsAsync()
        {
            try
            {
                Debug.WriteLine("Requesting all required permissions...");
                
                var cameraGranted = await RequestCameraPermissionAsync();
                var storageGranted = await RequestStoragePermissionAsync();
                
                var allGranted = cameraGranted && storageGranted;
                
                if (allGranted)
                {
                    Debug.WriteLine("All required permissions granted!");
                }
                else
                {
                    Debug.WriteLine("Some permissions were denied. App functionality may be limited.");
                }
                
                return allGranted;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error requesting permissions: {ex.Message}");
                return false;
            }
        }
        
        public static async Task<bool> CheckAllPermissionsAsync()
        {
            try
            {
                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                var storageStatus = await Permissions.CheckStatusAsync<Permissions.StorageRead>();
                
                var allGranted = cameraStatus == PermissionStatus.Granted && 
                                storageStatus == PermissionStatus.Granted;
                
                Debug.WriteLine($"Permission status - Camera: {cameraStatus}, Storage: {storageStatus}");
                
                return allGranted;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking permissions: {ex.Message}");
                return false;
            }
        }
    }
}
