# Authentication System Setup Guide

## Overview
This guide explains how to set up and use the authentication system for the Attrak attendance management system.

## Database Setup

### 1. Create the User Table
Run the SQL script in `user_table.sql` to create the User table:

```sql
-- Execute the contents of user_table.sql in your MySQL database
```

### 2. Sample Data
Use the sample data in `sample_user_data.sql` to create test users. **Note**: The sample data uses simple base64 encoding for demonstration. For production, use the `PasswordHashUtility` to generate proper hashes.

## API Endpoints

### Login Endpoint
**POST** `/api/auth/login`

**Request Body:**
```json
{
    "username": "admin",
    "password": "admin123"
}
```

**Success Response (200):**
```json
{
    "success": true,
    "message": "Login successful",
    "token": null,
    "user": {
        "userId": "uuid-here",
        "username": "admin",
        "email": "admin@attrak.com",
        "userType": 1,
        "teacherId": null,
        "studentId": null,
        "lastLoginAt": "2024-01-01T10:00:00Z"
    }
}
```

**Error Response (401):**
```json
{
    "success": false,
    "message": "Invalid username or password",
    "token": null,
    "user": null
}
```

### Validate User Endpoint
**POST** `/api/auth/validate`

**Request Body:**
```json
{
    "username": "admin",
    "password": "admin123"
}
```

**Response:** `true` or `false`

## User Types
- **1** = Admin
- **2** = Teacher  
- **3** = Student

## Password Hashing

### For Development/Testing
Use the `PasswordHashUtility` class to generate proper password hashes:

```csharp
var (hash, salt) = PasswordHashUtility.GenerateHashAndSalt("your_password");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine($"Salt: {salt}");
```

### For Production
The `AuthService` automatically handles password hashing using SHA256 with salt.

## Database Schema

### User Table Structure
```sql
CREATE TABLE User (
    UserId VARCHAR(36) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Salt VARCHAR(255) NOT NULL,
    UserType ENUM('Admin', 'Teacher', 'Student') NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    LastLoginAt TIMESTAMP NULL,
    TeacherId VARCHAR(36) NULL,
    StudentId VARCHAR(36) NULL,
    FOREIGN KEY (TeacherId) REFERENCES Teacher(TeacherId),
    FOREIGN KEY (StudentId) REFERENCES Student(StudentId)
);
```

## Security Features

1. **Password Hashing**: Uses SHA256 with random salt
2. **Account Status**: Users can be deactivated
3. **Last Login Tracking**: Records when users last logged in
4. **Input Validation**: Validates email format and password requirements
5. **Error Handling**: Secure error messages that don't reveal system details

## Testing the API

### Using Swagger UI
1. Run the application
2. Navigate to `/swagger` in your browser
3. Test the `/api/auth/login` endpoint

### Using curl
```bash
curl -X POST "https://localhost:7000/api/auth/login" \
     -H "Content-Type: application/json" \
     -d '{
       "username": "admin",
       "password": "admin123"
     }'
```

## Next Steps

1. **JWT Token Implementation**: Add JWT token generation for stateless authentication
2. **Role-based Authorization**: Implement authorization based on UserType
3. **Password Reset**: Add password reset functionality
4. **Account Management**: Add user registration and profile management endpoints
5. **Audit Logging**: Add comprehensive logging for security events

## Troubleshooting

### Common Issues

1. **Connection String**: Ensure your database connection string in `appsettings.json` is correct
2. **MySQL Driver**: Make sure MySql.Data package is properly installed
3. **Table Creation**: Verify the User table was created successfully
4. **Sample Data**: Check that sample users were inserted correctly

### Logs
Check the application logs for detailed error information. The `AuthService` logs all authentication attempts and errors.
