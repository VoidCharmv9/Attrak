-- Attendance table for tracking student attendance
-- This works with your existing TeacherSubject, Subject, and Teacher tables
CREATE TABLE IF NOT EXISTS attendance (
    AttendanceId VARCHAR(36) PRIMARY KEY,
    StudentId VARCHAR(36) NOT NULL,
    SubjectId VARCHAR(36) NOT NULL,
    TeacherId VARCHAR(36) NOT NULL,
    Timestamp DATETIME NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'Present',
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Foreign key constraints (optional, for data integrity)
    FOREIGN KEY (StudentId) REFERENCES student(StudentId) ON DELETE CASCADE,
    FOREIGN KEY (SubjectId) REFERENCES subject(SubjectId) ON DELETE CASCADE,
    FOREIGN KEY (TeacherId) REFERENCES teacher(TeacherId) ON DELETE CASCADE,
    
    INDEX idx_student_subject (StudentId, SubjectId),
    INDEX idx_timestamp (Timestamp),
    INDEX idx_subject_date (SubjectId, DATE(Timestamp)),
    INDEX idx_teacher_date (TeacherId, DATE(Timestamp))
);

-- Sample attendance data for testing
-- Insert some sample attendance records
INSERT IGNORE INTO attendance (AttendanceId, StudentId, SubjectId, TeacherId, Timestamp, Status)
SELECT 
    UUID() as AttendanceId,
    'STU001' as StudentId,
    s.SubjectId,
    ts.TeacherId,
    NOW() as Timestamp,
    'Present' as Status
FROM subject s
INNER JOIN teachersubject ts ON s.SubjectId = ts.SubjectId
LIMIT 3;
