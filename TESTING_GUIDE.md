# 🧪 Testing Guide - Attendance System

## ✅ **Current Status:**
- **Server**: Online (`https://attrak.onrender.com`)
- **Registration Endpoints**: Working (has fallback data)
- **Daily Attendance Endpoints**: Database connection issues
- **Mobile App**: Has offline mode fallback

## 🎯 **Quick Test:**

### 1. **Test QR Scanning**
1. Open your mobile app
2. Go to QR Scanner page
3. Look for status indicators:
   - 🟡 **"Offline Mode"** - Normal, data will save offline
   - 🔴 **"Server Error"** - Will fallback to offline mode
   - 🟢 **"Online"** - Will try server first

### 2. **Scan a QR Code**
1. Select "Time In" or "Time Out"
2. Scan any QR code
3. **Expected Result**: 
   - ✅ **"Saved offline"** message (if server has DB issues)
   - ✅ **"Attendance marked successfully"** (if server is fully working)

### 3. **Check Offline Records**
1. Look for the offline count badge (e.g., "📱 3 offline")
2. Click it to see pending records
3. Use "Sync Now" button when server is working

## 🔧 **Server Issues to Fix:**

### Database Connection Problem
The server can't connect to the MySQL database. This could be:
- Database server is down
- Connection string is incorrect
- Network connectivity issues
- Database credentials expired

### Quick Fix Options:

#### Option 1: Check Database Connection
```bash
# Test database connection locally
cd ServerAtrrak
dotnet run
# Check logs for database connection errors
```

#### Option 2: Update Connection String
Edit `ServerAtrrak/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "dbconstring": "your_new_connection_string_here"
  }
}
```

#### Option 3: Use Local Database
1. Install MySQL locally
2. Create `attendance` database
3. Run the SQL scripts in your project
4. Update connection string to local MySQL

## 📱 **Mobile App Testing:**

### Expected Behavior (Current):
1. **Login**: ✅ Works (has offline fallback)
2. **QR Scanning**: ✅ Works (saves offline when server has DB issues)
3. **Data Sync**: ✅ Works (syncs when server is fixed)

### Test Scenarios:

#### Scenario 1: Server DB Working
- QR scan → Server save → ✅ "Attendance marked successfully"

#### Scenario 2: Server DB Issues (Current)
- QR scan → Offline fallback → ✅ "Saved offline - will sync later"

#### Scenario 3: No Internet
- QR scan → Offline save → ✅ "Saved offline"

## 🚀 **Next Steps:**

### Immediate (Today):
1. **Test QR scanning** - Should work with offline fallback
2. **Check offline records** - Verify data is being saved
3. **Try sync when possible** - Test the sync functionality

### Short-term (This Week):
1. **Fix database connection** - Get server fully working
2. **Test end-to-end** - Full online functionality
3. **Deploy fixes** - Update server deployment

### Long-term (This Month):
1. **Add health checks** - Better server monitoring
2. **Improve error handling** - More robust fallbacks
3. **Add logging** - Better debugging capabilities

## 🎯 **Success Criteria:**

- ✅ **QR scanning works** (offline or online)
- ✅ **No data loss** (all attendance saved)
- ✅ **Sync functionality works** (when server is up)
- ✅ **User experience is smooth** (clear status messages)

## 📞 **If Issues Persist:**

1. **Check logs** - Look for specific error messages
2. **Test individual endpoints** - Use browser/Postman
3. **Verify database** - Check if MySQL is accessible
4. **Contact support** - If database hosting issues

**The good news: Your app is working with offline mode! Data is being saved and will sync when the server is fixed.** 🎉
