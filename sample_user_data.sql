-- Sample user data for testing
-- WARNING: Passwords are stored in plain text - NOT RECOMMENDED for production!

-- Sample Admin User
-- Username: admin, Password: admin123
INSERT INTO User (UserId, Username, Email, Password, UserType, IsActive, CreatedAt, UpdatedAt)
VALUES (
    UUID(),
    'admin',
    'admin@attrak.com',
    'admin123', -- Plain text password
    1, -- Admin
    TRUE,
    NOW(),
    NOW()
);

-- Sample Teacher User
-- Username: teacher1, Password: teacher123
INSERT INTO User (UserId, Username, Email, Password, UserType, IsActive, CreatedAt, UpdatedAt, TeacherId)
VALUES (
    UUID(),
    'teacher1',
    'teacher1@attrak.com',
    'teacher123', -- Plain text password
    2, -- Teacher
    TRUE,
    NOW(),
    NOW(),
    (SELECT TeacherId FROM Teacher LIMIT 1) -- This will only work if you have teachers in your Teacher table
);

-- Sample Student User
-- Username: student1, Password: student123
INSERT INTO User (UserId, Username, Email, Password, UserType, IsActive, CreatedAt, UpdatedAt, StudentId)
VALUES (
    UUID(),
    'student1',
    'student1@attrak.com',
    'student123', -- Plain text password
    3, -- Student
    TRUE,
    NOW(),
    NOW(),
    (SELECT StudentId FROM Student LIMIT 1) -- This will only work if you have students in your Student table
);
