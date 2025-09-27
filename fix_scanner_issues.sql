-- Fix ScannerMaui App Issues
-- This script addresses the following problems:
-- 1. Teacher records missing from teacher table
-- 2. No subject assignments for teachers
-- 3. Missing school data

-- Step 1: Check current state
SELECT 'Current User State' as Status;
SELECT 
    u.UserId,
    u.Username,
    u.Email,
    u.UserType,
    u.TeacherId,
    t.TeacherId as TeacherExists,
    t.FullName as TeacherName,
    s.SchoolName
FROM user u
LEFT JOIN teacher t ON u.TeacherId = t.TeacherId
LEFT JOIN school s ON t.SchoolId = s.SchoolId
WHERE u.UserType = 2; -- Teacher users

-- Step 2: Check if we have any schools
SELECT 'School Data' as Status;
SELECT SchoolId, SchoolName FROM school LIMIT 5;

-- Step 3: Create teacher records for users who have TeacherId but no teacher record
SELECT 'Creating missing teacher records...' as Status;

-- First, let's create a default school if none exists
INSERT IGNORE INTO school (SchoolId, SchoolName, Address, ContactNumber, Email, IsActive, CreatedAt)
VALUES ('SCHOOL001', 'Default School', '123 Main St', '555-0123', 'admin@defaultschool.com', TRUE, NOW());

-- Now create teacher records for users who don't have them
INSERT INTO teacher (TeacherId, FullName, Email, SchoolId, IsActive, CreatedAt)
SELECT 
    u.TeacherId,
    u.Username as FullName,
    u.Email,
    (SELECT SchoolId FROM school LIMIT 1) as SchoolId,  -- Use first available school
    TRUE,
    NOW()
FROM user u
LEFT JOIN teacher t ON u.TeacherId = t.TeacherId
WHERE u.UserType = 2  -- Teacher users
    AND u.TeacherId IS NOT NULL 
    AND t.TeacherId IS NULL;

-- Step 4: Check if we have subjects available
SELECT 'Subject Data' as Status;
SELECT COUNT(*) as SubjectCount FROM subject;

-- Step 5: Initialize sample subjects if table is empty
INSERT IGNORE INTO subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd)
SELECT * FROM (
    SELECT 'SUBJ001' as SubjectId, 'Mathematics 7' as SubjectName, 7 as GradeLevel, NULL as Strand, '08:00:00' as ScheduleStart, '09:00:00' as ScheduleEnd
    UNION ALL SELECT 'SUBJ002', 'English 7', 7, NULL, '09:00:00', '10:00:00'
    UNION ALL SELECT 'SUBJ003', 'Science 7', 7, NULL, '10:00:00', '11:00:00'
    UNION ALL SELECT 'SUBJ004', 'Mathematics 8', 8, NULL, '08:00:00', '09:00:00'
    UNION ALL SELECT 'SUBJ005', 'English 8', 8, NULL, '09:00:00', '10:00:00'
    UNION ALL SELECT 'SUBJ006', 'Science 8', 8, NULL, '10:00:00', '11:00:00'
    UNION ALL SELECT 'SUBJ007', 'Mathematics 9', 9, NULL, '08:00:00', '09:00:00'
    UNION ALL SELECT 'SUBJ008', 'English 9', 9, NULL, '09:00:00', '10:00:00'
    UNION ALL SELECT 'SUBJ009', 'Science 9', 9, NULL, '10:00:00', '11:00:00'
    UNION ALL SELECT 'SUBJ010', 'Mathematics 10', 10, NULL, '08:00:00', '09:00:00'
    UNION ALL SELECT 'SUBJ011', 'English 10', 10, NULL, '09:00:00', '10:00:00'
    UNION ALL SELECT 'SUBJ012', 'Science 10', 10, NULL, '10:00:00', '11:00:00'
    UNION ALL SELECT 'SUBJ013', 'General Mathematics', 11, 'ABM', '08:00:00', '09:00:00'
    UNION ALL SELECT 'SUBJ014', 'Oral Communication', 11, 'ABM', '09:00:00', '10:00:00'
    UNION ALL SELECT 'SUBJ015', 'General Mathematics', 11, 'STEM', '08:00:00', '09:00:00'
    UNION ALL SELECT 'SUBJ016', 'Oral Communication', 11, 'STEM', '09:00:00', '10:00:00'
) as sample_subjects
WHERE NOT EXISTS (SELECT 1 FROM subject LIMIT 1);

-- Step 6: Assign some subjects to teachers
SELECT 'Assigning subjects to teachers...' as Status;

-- Get the first few teachers and assign them some subjects
INSERT IGNORE INTO TeacherSubject (TeacherSubjectId, TeacherId, SubjectId, Section)
SELECT 
    CONCAT('TS', UUID()) as TeacherSubjectId,
    t.TeacherId,
    s.SubjectId,
    'A' as Section
FROM teacher t
CROSS JOIN subject s
WHERE t.TeacherId IN (SELECT TeacherId FROM user WHERE UserType = 2 LIMIT 3) -- First 3 teachers
    AND s.GradeLevel IN (7, 8, 9, 10) -- Basic subjects
    AND NOT EXISTS (
        SELECT 1 FROM TeacherSubject ts 
        WHERE ts.TeacherId = t.TeacherId AND ts.SubjectId = s.SubjectId
    )
LIMIT 15; -- Assign up to 15 subjects

-- Step 7: Verify the fixes
SELECT 'Verification - Updated State' as Status;
SELECT 
    u.UserId,
    u.Username,
    u.Email,
    u.UserType,
    u.TeacherId,
    t.TeacherId as TeacherExists,
    t.FullName as TeacherName,
    s.SchoolName,
    COUNT(ts.TeacherSubjectId) as SubjectCount
FROM user u
LEFT JOIN teacher t ON u.TeacherId = t.TeacherId
LEFT JOIN school s ON t.SchoolId = s.SchoolId
LEFT JOIN TeacherSubject ts ON t.TeacherId = ts.TeacherId
WHERE u.UserType = 2
GROUP BY u.UserId, u.Username, u.Email, u.UserType, u.TeacherId, t.TeacherId, t.FullName, s.SchoolName;

-- Step 8: Show sample teacher assignments
SELECT 'Sample Teacher Assignments' as Status;
SELECT 
    t.FullName as TeacherName,
    s.SubjectName,
    s.GradeLevel,
    ts.Section
FROM teacher t
INNER JOIN TeacherSubject ts ON t.TeacherId = ts.TeacherId
INNER JOIN subject s ON ts.SubjectId = s.SubjectId
LIMIT 10;

SELECT 'Fix completed successfully!' as Status;
