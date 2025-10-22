using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using ZXing.Net.Maui.Controls;

namespace ScannerMaui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseBarcodeReader()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            // Configure HttpClient with base URL
            builder.Services.AddHttpClient("AttrakAPI", client =>
            {
                // Replace with your actual API base URL
                client.BaseAddress = new Uri("https://attrak.onrender.com/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            
            // Also add a default HttpClient
            
            // Register services
            builder.Services.AddSingleton<ScannerMaui.Services.OfflineDataService>();
            builder.Services.AddSingleton<ScannerMaui.Services.AuthService>();
            builder.Services.AddSingleton<ScannerMaui.Services.ConnectionStatusService>();
            builder.Services.AddSingleton<ScannerMaui.Services.HybridQRValidationService>();
            builder.Services.AddSingleton<ScannerMaui.Services.QRScannerService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
