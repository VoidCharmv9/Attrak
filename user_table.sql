-- USER TABLE
CREATE TABLE user (
    UserId VARCHAR(36) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE NOT NULL,
    Email VARCHAR(100) UNIQUE NOT NULL,
    Password VARCHAR(100) NOT NULL,
    UserType ENUM('Admin', 'Teacher', 'Student') NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    LastLoginAt TIMESTAMP NULL,
    -- Foreign key references based on user type
    TeacherId VARCHAR(36) NULL,
    StudentId VARCHAR(36) NULL,
    FOREIGN KEY (TeacherId) REFERENCES teacher(TeacherId),
    FOREIGN KEY (StudentId) REFERENCES student(StudentId)
);

-- Create indexes for better performance
CREATE INDEX idx_user_username ON user(Username);
CREATE INDEX idx_user_email ON user(Email);
CREATE INDEX idx_user_type ON user(UserType);
CREATE INDEX idx_user_active ON user(IsActive);
