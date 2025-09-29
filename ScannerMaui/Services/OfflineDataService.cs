using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Data;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ScannerMaui.Services
{
    public class OfflineDataService
    {
        private readonly string _databasePath;
        private readonly string _connectionString;

        public OfflineDataService()
        {
            // Use AppDataDirectory (guaranteed to work, no permissions needed)
            _databasePath = Path.Combine(FileSystem.AppDataDirectory, "attrak_offline.db");
            _connectionString = $"Data Source={_databasePath}";
            InitializeDatabase();
            
            // Log the database path for debugging
            System.Diagnostics.Debug.WriteLine($"SQLite Database Path: {_databasePath}");
            System.Diagnostics.Debug.WriteLine($"App Data Directory: {FileSystem.AppDataDirectory}");
        }

        private void InitializeDatabase()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                // Create offline attendance table
                var createAttendanceTable = @"
                    CREATE TABLE IF NOT EXISTS offline_attendance (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        student_id TEXT NOT NULL,
                        attendance_type TEXT NOT NULL,
                        scan_time DATETIME NOT NULL,
                        device_id TEXT,
                        is_synced INTEGER DEFAULT 0,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";

                // Create offline users table for authentication
                var createUsersTable = @"
                    CREATE TABLE IF NOT EXISTS offline_users (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        username TEXT UNIQUE NOT NULL,
                        password_hash TEXT NOT NULL,
                        user_type TEXT NOT NULL,
                        full_name TEXT,
                        is_active INTEGER DEFAULT 1,
                        last_login DATETIME,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";

                // Create sync log table
                var createSyncLogTable = @"
                    CREATE TABLE IF NOT EXISTS sync_log (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        sync_type TEXT NOT NULL,
                        record_count INTEGER,
                        sync_time DATETIME DEFAULT CURRENT_TIMESTAMP,
                        status TEXT NOT NULL,
                        error_message TEXT
                    )";

                var command1 = new SqliteCommand(createAttendanceTable, connection);
                command1.ExecuteNonQuery();

                var command2 = new SqliteCommand(createUsersTable, connection);
                command2.ExecuteNonQuery();

                var command3 = new SqliteCommand(createSyncLogTable, connection);
                command3.ExecuteNonQuery();

                System.Diagnostics.Debug.WriteLine("Offline database initialized successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing offline database: {ex.Message}");
            }
        }

        // Offline Authentication Methods
        public async Task<bool> AuthenticateUserOfflineAsync(string username, string password)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "SELECT password_hash, user_type, full_name FROM offline_users WHERE username = @username AND is_active = 1",
                    connection);
                command.Parameters.AddWithValue("@username", username);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var storedHash = reader.GetString("password_hash");
                    var userType = reader.GetString("user_type");
                    var fullName = reader.GetString("full_name");

                    // Simple password verification (in production, use proper hashing)
                    if (storedHash == password) // This should be proper hash comparison
                    {
                        // Update last login
                        await UpdateLastLoginAsync(username);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in offline authentication: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddOfflineUserAsync(string username, string password, string userType, string fullName)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "INSERT INTO offline_users (username, password_hash, user_type, full_name) VALUES (@username, @password, @userType, @fullName)",
                    connection);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@password", password); // Should be hashed
                command.Parameters.AddWithValue("@userType", userType);
                command.Parameters.AddWithValue("@fullName", fullName);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding offline user: {ex.Message}");
                return false;
            }
        }

        private async Task UpdateLastLoginAsync(string username)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "UPDATE offline_users SET last_login = CURRENT_TIMESTAMP WHERE username = @username",
                    connection);
                command.Parameters.AddWithValue("@username", username);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating last login: {ex.Message}");
            }
        }

        // Offline Attendance Methods
        public async Task<bool> SaveOfflineAttendanceAsync(string studentId, string attendanceType, string? deviceId = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Attempting to save offline attendance: StudentId={studentId}, Type={attendanceType}, DeviceId={deviceId}");
                System.Diagnostics.Debug.WriteLine($"Database path: {_databasePath}");
                System.Diagnostics.Debug.WriteLine($"Database exists: {File.Exists(_databasePath)}");

                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();
                System.Diagnostics.Debug.WriteLine("Database connection opened successfully");

                var command = new SqliteCommand(
                    "INSERT INTO offline_attendance (student_id, attendance_type, scan_time, device_id) VALUES (@studentId, @attendanceType, @scanTime, @deviceId)",
                    connection);
                command.Parameters.AddWithValue("@studentId", studentId);
                command.Parameters.AddWithValue("@attendanceType", attendanceType);
                command.Parameters.AddWithValue("@scanTime", DateTime.Now);
                command.Parameters.AddWithValue("@deviceId", deviceId ?? GetDeviceId());

                var result = await command.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"Offline attendance saved successfully. Rows affected: {result}");
                return result > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving offline attendance: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<List<OfflineAttendanceRecord>> GetUnsyncedAttendanceAsync()
        {
            var records = new List<OfflineAttendanceRecord>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "SELECT * FROM offline_attendance WHERE is_synced = 0 ORDER BY created_at",
                    connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    records.Add(new OfflineAttendanceRecord
                    {
                        Id = reader.GetInt32("id"),
                        StudentId = reader.GetString("student_id"),
                        AttendanceType = reader.GetString("attendance_type"),
                        ScanTime = reader.GetDateTime("scan_time"),
                        DeviceId = reader.GetString("device_id"),
                        IsSynced = reader.GetInt32("is_synced") == 1,
                        CreatedAt = reader.GetDateTime("created_at")
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unsynced attendance: {ex.Message}");
            }

            return records;
        }

        public async Task<bool> MarkAttendanceAsSyncedAsync(int recordId)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "UPDATE offline_attendance SET is_synced = 1 WHERE id = @id",
                    connection);
                command.Parameters.AddWithValue("@id", recordId);

                var result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking attendance as synced: {ex.Message}");
                return false;
            }
        }

        public async Task<int> GetUnsyncedCountAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "SELECT COUNT(DISTINCT student_id) FROM offline_attendance WHERE is_synced = 0",
                    connection);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unsynced count: {ex.Message}");
                return 0;
            }
        }

        public async Task<List<PendingStudent>> GetPendingStudentsAsync()
        {
            var pendingStudents = new List<PendingStudent>();
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    @"SELECT DISTINCT 
                        oa.student_id,
                        oa.attendance_type,
                        oa.scan_time,
                        oa.device_id,
                        COUNT(*) as record_count
                      FROM offline_attendance oa 
                      WHERE oa.is_synced = 0 
                      GROUP BY oa.student_id, oa.attendance_type, DATE(oa.scan_time)
                      ORDER BY oa.scan_time DESC",
                    connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var studentId = reader.GetString("student_id");
                    var attendanceType = reader.GetString("attendance_type");
                    var scanTime = reader.GetDateTime("scan_time");
                    var deviceId = reader.IsDBNull("device_id") ? null : reader.GetString("device_id");
                    var recordCount = reader.GetInt32("record_count");

                    // Try to get student name from offline users table
                    var studentName = await GetStudentNameAsync(studentId);

                    pendingStudents.Add(new PendingStudent
                    {
                        StudentId = studentId,
                        StudentName = studentName ?? $"Student {studentId}",
                        AttendanceType = attendanceType,
                        ScanTime = scanTime,
                        DeviceId = deviceId,
                        RecordCount = recordCount
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting pending students: {ex.Message}");
            }
            return pendingStudents;
        }

        private async Task<string?> GetStudentNameAsync(string studentId)
        {
            try
            {
                // First try to get from local cache
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "SELECT student_name FROM student_names_cache WHERE student_id = @studentId",
                    connection);
                command.Parameters.AddWithValue("@studentId", studentId);

                var cachedName = await command.ExecuteScalarAsync();
                if (cachedName != null)
                {
                    return cachedName.ToString();
                }

                // If not in cache, try to fetch from server
                return await FetchStudentNameFromServerAsync(studentId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting student name: {ex.Message}");
                return null;
            }
        }

        // Public method to get student name (for UI)
        public async Task<string> GetStudentNameForDisplayAsync(string studentId)
        {
            var name = await GetStudentNameAsync(studentId);
            return name ?? $"Student {studentId}";
        }

        private async Task<string?> FetchStudentNameFromServerAsync(string studentId)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://attrak.onrender.com/");
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                // Try to get student info from your API
                var response = await httpClient.GetAsync($"api/student/{studentId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Student API response: {json}");
                    
                    // Parse the JSON to get student name
                    // Your API returns: {"studentId": "123", "fullName": "John Doe", ...}
                    var studentData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                    
                    if (studentData != null && studentData.ContainsKey("fullName"))
                    {
                        var studentName = studentData["fullName"].ToString();
                        
                        // Cache the name locally for future use
                        await CacheStudentNameAsync(studentId, studentName);
                        
                        return studentName;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Student API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching student name from server: {ex.Message}");
            }
            
            return null;
        }

        private async Task CacheStudentNameAsync(string studentId, string studentName)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Create cache table if it doesn't exist
                var createTableCommand = new SqliteCommand(
                    @"CREATE TABLE IF NOT EXISTS student_names_cache (
                        student_id TEXT PRIMARY KEY,
                        student_name TEXT NOT NULL,
                        cached_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    )", connection);
                await createTableCommand.ExecuteNonQueryAsync();

                // Insert or update the cached name
                var command = new SqliteCommand(
                    @"INSERT OR REPLACE INTO student_names_cache (student_id, student_name, cached_at) 
                      VALUES (@studentId, @studentName, @cachedAt)",
                    connection);
                command.Parameters.AddWithValue("@studentId", studentId);
                command.Parameters.AddWithValue("@studentName", studentName);
                command.Parameters.AddWithValue("@cachedAt", DateTime.Now);

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error caching student name: {ex.Message}");
            }
        }

        // Bulk download all students for a teacher when they log in
        public async Task<bool> DownloadAllStudentsForTeacherAsync(string teacherId, string apiBaseUrl)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Starting bulk download of students for teacher: {teacherId}");

                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(apiBaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for bulk download

                // Get all students assigned to this teacher
                var response = await httpClient.GetAsync($"api/teacher/{teacherId}/students");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Students API response: {json}");
                    
                    // Parse the JSON array of students
                    var students = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);
                    
                    if (students != null && students.Any())
                    {
                        // Clear existing cache and bulk insert
                        await ClearStudentCacheAsync();
                        await BulkCacheStudentsAsync(students);
                        
                        System.Diagnostics.Debug.WriteLine($"Successfully cached {students.Count} students for teacher {teacherId}");
                        return true;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Students API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error downloading students for teacher: {ex.Message}");
            }
            
            return false;
        }

        private async Task ClearStudentCacheAsync()
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand("DELETE FROM student_names_cache", connection);
                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing student cache: {ex.Message}");
            }
        }

        private async Task BulkCacheStudentsAsync(List<Dictionary<string, object>> students)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                // Create cache table if it doesn't exist
                var createTableCommand = new SqliteCommand(
                    @"CREATE TABLE IF NOT EXISTS student_names_cache (
                        student_id TEXT PRIMARY KEY,
                        student_name TEXT NOT NULL,
                        cached_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    )", connection);
                await createTableCommand.ExecuteNonQueryAsync();

                // Bulk insert all students
                using var transaction = connection.BeginTransaction();
                var command = new SqliteCommand(
                    @"INSERT OR REPLACE INTO student_names_cache (student_id, student_name, cached_at) 
                      VALUES (@studentId, @studentName, @cachedAt)",
                    connection, transaction);

                foreach (var student in students)
                {
                    if (student.ContainsKey("studentId") && student.ContainsKey("fullName"))
                    {
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@studentId", student["studentId"].ToString());
                        command.Parameters.AddWithValue("@studentName", student["fullName"].ToString());
                        command.Parameters.AddWithValue("@cachedAt", DateTime.Now);
                        await command.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
                System.Diagnostics.Debug.WriteLine($"Bulk cached {students.Count} students successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error bulk caching students: {ex.Message}");
            }
        }

        public async Task<bool> SyncIndividualStudentAsync(string studentId, string apiBaseUrl, string teacherId)
        {
            try
            {
                var studentRecords = await GetUnsyncedAttendanceAsync();
                var studentSpecificRecords = studentRecords.Where(r => r.StudentId == studentId).ToList();
                
                if (!studentSpecificRecords.Any())
                {
                    return true; // No records to sync
                }

                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(apiBaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                int successCount = 0;
                int failCount = 0;

                foreach (var record in studentSpecificRecords)
                {
                    try
                    {
                        HttpResponseMessage response;
                        
                        if (record.AttendanceType == "TimeIn")
                        {
                            var request = new
                            {
                                StudentId = record.StudentId,
                                Date = record.ScanTime.Date,
                                TimeIn = record.ScanTime.TimeOfDay
                            };
                            response = await httpClient.PostAsJsonAsync("api/dailyattendance/daily-timein", request);
                        }
                        else
                        {
                            var request = new
                            {
                                StudentId = record.StudentId,
                                Date = record.ScanTime.Date,
                                TimeOut = record.ScanTime.TimeOfDay
                            };
                            response = await httpClient.PostAsJsonAsync("api/dailyattendance/daily-timeout", request);
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            // Mark record as synced
                            await MarkAttendanceAsSyncedAsync(record.Id);
                            successCount++;
                            System.Diagnostics.Debug.WriteLine($"Successfully synced record {record.Id} for student {record.StudentId}");
                        }
                        else
                        {
                            failCount++;
                            System.Diagnostics.Debug.WriteLine($"Failed to sync record {record.Id}: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        System.Diagnostics.Debug.WriteLine($"Error syncing record {record.Id}: {ex.Message}");
                    }
                }

                // Log sync results
                await LogSyncAsync("IndividualSync", studentSpecificRecords.Count, 
                    failCount == 0 ? "Success" : "Partial", 
                    failCount > 0 ? $"{failCount} records failed to sync" : null);

                System.Diagnostics.Debug.WriteLine($"Individual sync completed for {studentId}: {successCount} successful, {failCount} failed");
                return failCount == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in individual sync: {ex.Message}");
                await LogSyncAsync("IndividualSync", 0, "Error", ex.Message);
                return false;
            }
        }

        // Export Methods
        public async Task<string> ExportAttendanceDataAsync()
        {
            try
            {
                var records = await GetUnsyncedAttendanceAsync();
                var csv = new System.Text.StringBuilder();
                
                // CSV Header
                csv.AppendLine("Student ID,Attendance Type,Scan Time,Device ID,Created At");

                // CSV Data
                foreach (var record in records)
                {
                    csv.AppendLine($"{record.StudentId},{record.AttendanceType},{record.ScanTime:yyyy-MM-dd HH:mm:ss},{record.DeviceId},{record.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                }

                return csv.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting attendance data: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<bool> SaveExportToFileAsync(string fileName = null)
        {
            try
            {
                var csvData = await ExportAttendanceDataAsync();
                if (string.IsNullOrEmpty(csvData))
                    return false;

                fileName ??= $"attendance_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                
                // Save to AppDataDirectory (guaranteed to work)
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                await File.WriteAllTextAsync(filePath, csvData);
                System.Diagnostics.Debug.WriteLine($"Data exported to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving export file: {ex.Message}");
                return false;
            }
        }

        // Utility Methods
        private string GetDeviceId()
        {
            // Generate a simple device ID (in production, use proper device identification)
            return $"DEV_{Environment.MachineName}_{DateTime.Now:yyyyMMdd}";
        }

        public async Task LogSyncAsync(string syncType, int recordCount, string status, string? errorMessage = null)
        {
            try
            {
                using var connection = new SqliteConnection(_connectionString);
                await connection.OpenAsync();

                var command = new SqliteCommand(
                    "INSERT INTO sync_log (sync_type, record_count, status, error_message) VALUES (@syncType, @recordCount, @status, @errorMessage)",
                    connection);
                command.Parameters.AddWithValue("@syncType", syncType);
                command.Parameters.AddWithValue("@recordCount", recordCount);
                command.Parameters.AddWithValue("@status", status);
                command.Parameters.AddWithValue("@errorMessage", errorMessage ?? "");

                await command.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error logging sync: {ex.Message}");
            }
        }

        // Method to get database path for display
        public string GetDatabasePath()
        {
            return _databasePath;
        }

        // Method to get app data directory
        public string GetAppDataDirectory()
        {
            return FileSystem.AppDataDirectory;
        }

        // Method to copy database to a more accessible location
        public async Task<string> CopyDatabaseToAccessibleLocationAsync()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    return "Database file does not exist yet. Scan some QR codes first.";
                }

                // Try multiple Downloads folder paths
                var downloadsPaths = new[]
                {
                    "/storage/emulated/0/Download",
                    "/storage/emulated/0/Downloads", 
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Download"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                    "/sdcard/Download",
                    "/sdcard/Downloads"
                };

                foreach (var downloadsPath in downloadsPaths)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Trying Downloads path: {downloadsPath}");
                        
                        if (Directory.Exists(downloadsPath))
                        {
                            var destinationPath = Path.Combine(downloadsPath, "attrak_database_copy.db");
                            File.Copy(_databasePath, destinationPath, true);
                            
                            System.Diagnostics.Debug.WriteLine($"Successfully copied database to: {destinationPath}");
                            return $"✅ Database copied to Downloads!\nPath: {destinationPath}\nCheck your Downloads folder in file manager.";
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Directory does not exist: {downloadsPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not copy to {downloadsPath}: {ex.Message}");
                    }
                }

                // If all Downloads paths fail, try Documents
                var documentsPaths = new[]
                {
                    "/storage/emulated/0/Documents",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
                    "/sdcard/Documents"
                };

                foreach (var documentsPath in documentsPaths)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Trying Documents path: {documentsPath}");
                        
                        if (Directory.Exists(documentsPath))
                        {
                            var destinationPath = Path.Combine(documentsPath, "attrak_database_copy.db");
                            File.Copy(_databasePath, destinationPath, true);
                            
                            System.Diagnostics.Debug.WriteLine($"Successfully copied database to: {destinationPath}");
                            return $"✅ Database copied to Documents!\nPath: {destinationPath}\nCheck your Documents folder in file manager.";
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not copy to {documentsPath}: {ex.Message}");
                    }
                }

                // Final fallback: Create a copy in AppDataDirectory
                try
                {
                    var copyPath = Path.Combine(FileSystem.AppDataDirectory, "attrak_database_copy.db");
                    File.Copy(_databasePath, copyPath, true);
                    return $"⚠️ Could not access Downloads/Documents folders.\nDatabase copied to: {copyPath}\nThis is in app's private directory (not visible in file manager).\nTry enabling 'All files access' permission in Android Settings.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Could not create copy: {ex.Message}");
                }

                return "❌ Could not copy database to any accessible location.\nDatabase is stored in app's private directory for security.\nTry enabling storage permissions in Android Settings > Apps > ScannerMaui > Permissions.";
            }
            catch (Exception ex)
            {
                return $"❌ Error copying database: {ex.Message}";
            }
        }

        // Export database using MAUI file sharing
        public async Task<string> ExportDatabaseViaSharingAsync()
        {
            try
            {
                if (!File.Exists(_databasePath))
                {
                    return "Database file does not exist yet. Scan some QR codes first.";
                }

                // Create a copy with timestamp in AppDataDirectory
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var exportPath = Path.Combine(FileSystem.AppDataDirectory, $"attrak_database_{timestamp}.db");
                File.Copy(_databasePath, exportPath, true);

                // Try to use MAUI's file sharing (if available)
                try
                {
                    // This would require implementing file sharing in the UI
                    // For now, just return the path
                    return $"Database exported to: {exportPath}\nUse 'Create Test File' to get more details about accessing this file.";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"File sharing error: {ex.Message}");
                    return $"Database exported to: {exportPath}\nNote: File is in app's private directory.";
                }
            }
            catch (Exception ex)
            {
                return $"Error exporting database: {ex.Message}";
            }
        }

        // Auto-sync method to send offline data to API when connection is restored
        public async Task<bool> AutoSyncOfflineDataAsync(string apiBaseUrl, string teacherId)
        {
            try
            {
                var unsyncedRecords = await GetUnsyncedAttendanceAsync();
                if (!unsyncedRecords.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No offline records to sync");
                    return true;
                }

                System.Diagnostics.Debug.WriteLine($"Starting auto-sync of {unsyncedRecords.Count} offline records");

                using var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(apiBaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                int successCount = 0;
                int failCount = 0;

                foreach (var record in unsyncedRecords)
                {
                    try
                    {
                        HttpResponseMessage response;
                        
                        if (record.AttendanceType == "TimeIn")
                        {
                            var request = new
                            {
                                StudentId = record.StudentId,
                                Date = record.ScanTime.Date,
                                TimeIn = record.ScanTime.TimeOfDay
                            };
                            response = await httpClient.PostAsJsonAsync("api/dailyattendance/daily-timein", request);
                        }
                        else
                        {
                            var request = new
                            {
                                StudentId = record.StudentId,
                                Date = record.ScanTime.Date,
                                TimeOut = record.ScanTime.TimeOfDay
                            };
                            response = await httpClient.PostAsJsonAsync("api/dailyattendance/daily-timeout", request);
                        }

                        if (response.IsSuccessStatusCode)
                        {
                            // Mark record as synced
                            await MarkAttendanceAsSyncedAsync(record.Id);
                            successCount++;
                            System.Diagnostics.Debug.WriteLine($"Successfully synced record {record.Id} for student {record.StudentId}");
                        }
                        else
                        {
                            failCount++;
                            System.Diagnostics.Debug.WriteLine($"Failed to sync record {record.Id}: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        System.Diagnostics.Debug.WriteLine($"Error syncing record {record.Id}: {ex.Message}");
                    }
                }

                // Log sync results
                await LogSyncAsync("AutoSync", unsyncedRecords.Count, 
                    failCount == 0 ? "Success" : "Partial", 
                    failCount > 0 ? $"{failCount} records failed to sync" : null);

                System.Diagnostics.Debug.WriteLine($"Auto-sync completed: {successCount} successful, {failCount} failed");
                return failCount == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in auto-sync: {ex.Message}");
                await LogSyncAsync("AutoSync", 0, "Error", ex.Message);
                return false;
            }
        }

        // Method to create a test database file
        public async Task<bool> CreateTestDatabaseFileAsync()
        {
            try
            {
                // Create test file in AppDataDirectory (guaranteed to work)
                var testFilePath = Path.Combine(FileSystem.AppDataDirectory, "test_database_location.txt");
                
                // Force database creation by adding a test record
                System.Diagnostics.Debug.WriteLine("Creating test attendance record...");
                var testRecordSaved = await SaveOfflineAttendanceAsync("TEST_STUDENT_001", "TimeIn", "TEST_DEVICE");
                System.Diagnostics.Debug.WriteLine($"Test record saved: {testRecordSaved}");

                // Create a simple text file that's easy to find
                var simpleTestFile = Path.Combine(FileSystem.AppDataDirectory, "SIMPLE_TEST.txt");
                await File.WriteAllTextAsync(simpleTestFile, $"Test file created at {DateTime.Now}");

                // Try multiple external storage locations
                var externalFileCreated = false;
                var externalPaths = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Download"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Pictures"),
                    "/storage/emulated/0/Download",
                    "/storage/emulated/0/Downloads",
                    "/storage/emulated/0/Documents"
                };

                foreach (var externalPath in externalPaths)
                {
                    try
                    {
                        if (Directory.Exists(externalPath))
                        {
                            var externalTestFile = Path.Combine(externalPath, "ATTRAK_TEST.txt");
                            await File.WriteAllTextAsync(externalTestFile, $"Attrak test file created at {DateTime.Now}\nDatabase: {_databasePath}\nExternal Path: {externalPath}");
                            externalFileCreated = true;
                            System.Diagnostics.Debug.WriteLine($"External file created successfully at: {externalTestFile}");
                            break; // Stop after first successful creation
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not create file in {externalPath}: {ex.Message}");
                    }
                }

                if (!externalFileCreated)
                {
                    System.Diagnostics.Debug.WriteLine("Could not create external file in any location - permissions may be restricted");
                }

                // Create the detailed test file with all information
                var testContent = $"Database Location Test\n" +
                                 $"Created: {DateTime.Now}\n" +
                                 $"Database Path: {_databasePath}\n" +
                                 $"App Data Path: {FileSystem.AppDataDirectory}\n" +
                                 $"Database Exists: {File.Exists(_databasePath)}\n" +
                                 $"Test File Path: {testFilePath}\n" +
                                 $"Test Record Saved: {testRecordSaved}\n" +
                                 $"External File Created: {externalFileCreated}\n" +
                                 $"Storage Permission: Check Android Settings > Apps > ScannerMaui > Permissions > Storage\n" +
                                 $"If no external file, try enabling 'All files access' permission";

                await File.WriteAllTextAsync(testFilePath, testContent);

                System.Diagnostics.Debug.WriteLine($"Test file created at: {testFilePath}");
                System.Diagnostics.Debug.WriteLine($"Simple test file created at: {simpleTestFile}");
                System.Diagnostics.Debug.WriteLine($"Database file exists: {File.Exists(_databasePath)}");
                System.Diagnostics.Debug.WriteLine($"Database path: {_databasePath}");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating test database file: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }

    public class OfflineAttendanceRecord
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string AttendanceType { get; set; } = string.Empty;
        public DateTime ScanTime { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public bool IsSynced { get; set; }
        public DateTime CreatedAt { get; set; }
    }


    public class PendingStudent
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AttendanceType { get; set; } = string.Empty;
        public DateTime ScanTime { get; set; }
        public string? DeviceId { get; set; }
        public int RecordCount { get; set; }
    }
}
