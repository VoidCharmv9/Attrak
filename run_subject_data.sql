-- Run this script to populate the Subject table with sample data
-- Make sure to run this in your MySQL database

-- First, let's check if the Subject table exists and has data
SELECT COUNT(*) as SubjectCount FROM Subject;

-- If the count is 0, then run the subject_data.sql script
-- The subject_data.sql file contains 132 subjects for Grade 7-12 with strands
