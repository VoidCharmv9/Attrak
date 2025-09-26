-- ALTER STUDENT TABLE - Add missing columns to existing Student table
-- Based on your existing table structure

-- Add Email column (needed for student registration)
ALTER TABLE student ADD COLUMN Email VARCHAR(100) UNIQUE NOT NULL AFTER FullName;

-- Modify QRImage column from LONGBLOB to TEXT for Base64 storage (optional - if you want to keep LONGBLOB, skip this)
-- ALTER TABLE student MODIFY COLUMN QRImage TEXT NULL;

-- Add IsActive column (needed for soft delete)
ALTER TABLE student ADD COLUMN IsActive BOOLEAN DEFAULT TRUE AFTER QRImage;

-- Add CreatedAt column (for audit trail)
ALTER TABLE student ADD COLUMN CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP AFTER IsActive;

-- Add UpdatedAt column (for audit trail)
ALTER TABLE student ADD COLUMN UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP AFTER CreatedAt;

-- Create indexes for better performance
CREATE INDEX idx_student_email ON student(Email);
CREATE INDEX idx_student_school ON student(SchoolId);
CREATE INDEX idx_student_grade ON student(GradeLevel);
CREATE INDEX idx_student_active ON student(IsActive);
