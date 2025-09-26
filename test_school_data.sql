-- Test data for School table to populate regions dropdown
-- Run this SQL to add sample schools with different regions

-- Sample schools with different regions
INSERT INTO School (SchoolId, SchoolName, Region, Division, District, SchoolAddress) VALUES
(UUID(), 'Manila High School', 'National Capital Region (NCR)', 'Manila', 'Manila District I', 'Manila, Philippines'),
(UUID(), 'Quezon City High School', 'National Capital Region (NCR)', 'Quezon City', 'Quezon City District I', 'Quezon City, Philippines'),
(UUID(), 'Makati High School', 'National Capital Region (NCR)', 'Makati', 'Makati District', 'Makati, Philippines'),
(UUID(), 'Bulacan High School', 'Region III - Central Luzon', 'Bulacan', 'Bulacan District I', 'Bulacan, Philippines'),
(UUID(), 'Pampanga High School', 'Region III - Central Luzon', 'Pampanga', 'Pampanga District', 'Pampanga, Philippines'),
(UUID(), 'Cebu High School', 'Region VII - Central Visayas', 'Cebu', 'Cebu District I', 'Cebu, Philippines'),
(UUID(), 'Davao High School', 'Region XI - Davao Region', 'Davao', 'Davao District', 'Davao, Philippines');

-- Check what regions are in the database
SELECT DISTINCT Region FROM School ORDER BY Region;

-- Check total count of schools
SELECT COUNT(*) as TotalSchools FROM School;
