# Server Setup Guide - Attrak Attendance System

## 🚨 Current Issue
Ang server sa `https://attrak.onrender.com` ay hindi accessible. Ito ay maaaring dahil sa:
- Server ay down o hindi running
- Render.com deployment ay may problema  
- Server ay nasa sleeping mode (common sa free tier ng Render)

## 🔧 Solutions

### Option 1: Run Server Locally (Recommended for Testing)

1. **Start the Local Server:**
   ```bash
   cd ServerAtrrak
   dotnet run
   ```
   
2. **Server will run on:**
   - HTTP: `http://localhost:5000`
   - HTTPS: `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`

3. **Update Mobile App Configuration:**
   - Change the API URL from `https://attrak.onrender.com/` to `http://localhost:5000/` (for local testing)
   - Or use your computer's IP address: `http://192.168.1.xxx:5000/`

### Option 2: Fix Render.com Deployment

1. **Check Render Dashboard:**
   - Login to Render.com
   - Check if your service is running
   - Look for any error logs

2. **Common Render Issues:**
   - Free tier services sleep after 15 minutes of inactivity
   - Database connection issues
   - Build failures

3. **Wake Up the Service:**
   - Visit your Render service URL to wake it up
   - Or make an API call to activate it

### Option 3: Use Alternative Hosting

1. **Railway.app** - Similar to Render, easy deployment
2. **Heroku** - Popular platform
3. **Azure App Service** - Microsoft's cloud platform
4. **AWS EC2** - More control, requires more setup

## 📱 Mobile App Configuration

### Update API URL in ScannerMaui:

1. **File: `ScannerMaui/MauiProgram.cs`**
   ```csharp
   // Change this line:
   client.BaseAddress = new Uri("https://attrak.onrender.com/");
   
   // To this (for local testing):
   client.BaseAddress = new Uri("http://localhost:5000/");
   
   // Or to this (for your computer's IP):
   client.BaseAddress = new Uri("http://192.168.1.100:5000/");
   ```

2. **File: `ScannerMaui/Services/QRScannerService.cs`**
   ```csharp
   // Update line 142:
   var apiBaseUrl = "http://localhost:5000/"; // Change to your server URL
   ```

3. **File: `ScannerMaui/Components/Pages/QRScanner.razor`**
   ```csharp
   // Update lines 1559, 1611, 1665:
   var apiBaseUrl = "http://localhost:5000/"; // Change to your server URL
   ```

4. **File: `ScannerMaui/Services/OfflineDataService.cs`**
   ```csharp
   // Update line 639:
   httpClient.BaseAddress = new Uri("http://localhost:5000/");
   ```

## 🚀 Quick Start (Local Development)

1. **Start Server:**
   ```bash
   cd ServerAtrrak
   dotnet run
   ```

2. **Update Mobile App:**
   - Change all API URLs to `http://localhost:5000/`
   - Rebuild the mobile app

3. **Test Connection:**
   - Open browser: `http://localhost:5000/api/health`
   - Should return: `{"status":"Healthy"}`

4. **Run Mobile App:**
   - Connect your phone to the same WiFi network
   - Use your computer's IP address instead of localhost

## 🔍 Troubleshooting

### Server Won't Start:
- Check if port 5000 is available
- Run `dotnet restore` in ServerAtrrak folder
- Check database connection string in `appsettings.json`

### Mobile App Can't Connect:
- Make sure server is running
- Check firewall settings
- Use computer's IP address instead of localhost
- Make sure phone and computer are on same WiFi

### Database Issues:
- Check connection string in `ServerAtrrak/appsettings.json`
- Make sure MySQL database is accessible
- Verify database credentials

## 📞 Next Steps

1. **Immediate:** Run server locally for testing
2. **Short-term:** Fix Render deployment or use alternative hosting
3. **Long-term:** Set up proper production environment

Choose the option that works best for your situation!
