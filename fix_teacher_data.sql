-- Fix teacher data issue
-- This script will create teacher records for users who have TeacherId but no corresponding teacher record

-- First, let's see what users have TeacherId but no teacher record
SELECT 
    u.UserId,
    u.Username,
    u.Email,
    u.TeacherId,
    t.TeacherId as TeacherExists
FROM user u
LEFT JOIN teacher t ON u.TeacherId = t.TeacherId
WHERE u.UserType = 2 AND u.TeacherId IS NOT NULL;

-- Create teacher records for users who don't have them
INSERT INTO teacher (TeacherId, FullName, Email, SchoolId)
SELECT 
    u.TeacherId,
    u.Username as FullName,
    u.Email,
    (SELECT SchoolId FROM school LIMIT 1) as SchoolId  -- Use first available school
FROM user u
LEFT JOIN teacher t ON u.TeacherId = t.TeacherId
WHERE u.UserType = 2 
    AND u.TeacherId IS NOT NULL 
    AND t.TeacherId IS NULL;

-- Verify the fix
SELECT 
    u.UserId,
    u.Username,
    u.Email,
    u.TeacherId,
    t.FullName as TeacherFullName,
    t.SchoolId
FROM user u
INNER JOIN teacher t ON u.TeacherId = t.TeacherId
WHERE u.UserType = 2 AND u.TeacherId IS NOT NULL;
