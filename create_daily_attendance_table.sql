-- Create daily_attendance table for single time-in per day
-- Run this script to create the table for student daily attendance management

CREATE TABLE IF NOT EXISTS daily_attendance (
    AttendanceId VARCHAR(36) PRIMARY KEY,
    StudentId VARCHAR(36) NOT NULL,
    Date DATE NOT NULL,
    TimeIn VARCHAR(10), -- Format: HH:MM
    Status ENUM('Present', 'Late', 'Absent') NOT NULL DEFAULT 'Present',
    Remarks TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Foreign key constraint
    FOREIGN KEY (StudentId) REFERENCES student(StudentId) ON DELETE CASCADE,
    
    -- Unique constraint to ensure only one record per student per day
    UNIQUE KEY unique_student_date (StudentId, Date),
    
    -- Indexes for better performance
    INDEX idx_student_date (StudentId, Date),
    INDEX idx_date (Date),
    INDEX idx_status (Status)
);

-- Add comments for documentation
ALTER TABLE daily_attendance 
COMMENT = 'Daily attendance records for students - single time-in per day';

-- Verify the table structure
DESCRIBE daily_attendance;
