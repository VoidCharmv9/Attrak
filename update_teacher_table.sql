-- Update teacher table to add Grade Level, Section, and Strand columns
-- Run this script to update your database schema

-- Add Gradelvl column (INT)
ALTER TABLE teacher 
ADD COLUMN Gradelvl INT DEFAULT 0;

-- Add Section column (VARCHAR)
ALTER TABLE teacher 
ADD COLUMN Section VARCHAR(50) DEFAULT '';

-- Add Strand column (VARCHAR, nullable)
ALTER TABLE teacher 
ADD COLUMN Strand VARCHAR(100) NULL;

-- Update existing records with default values if needed
UPDATE teacher 
SET Gradelvl = 0, Section = 'Default' 
WHERE Gradelvl IS NULL OR Section IS NULL;

-- Add comments for documentation
ALTER TABLE teacher 
MODIFY COLUMN Gradelvl INT DEFAULT 0 COMMENT 'Grade level (7-12)';

ALTER TABLE teacher 
MODIFY COLUMN Section VARCHAR(50) DEFAULT '' COMMENT 'Class section';

ALTER TABLE teacher 
MODIFY COLUMN Strand VARCHAR(100) NULL COMMENT 'Strand for Grade 11-12 (STEM, ABM, HUMSS, GAS, etc.)';

-- Verify the changes
DESCRIBE teacher;
