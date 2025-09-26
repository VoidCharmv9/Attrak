-- SCHOOL TABLE
-- This table stores school information for the attendance system

CREATE TABLE IF NOT EXISTS school (
    SchoolId VARCHAR(36) PRIMARY KEY,
    SchoolName VARCHAR(255) NOT NULL,
    Region VARCHAR(100) NOT NULL,
    Division VARCHAR(100) NOT NULL,
    District VARCHAR(100) NULL,
    SchoolAddress TEXT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    IsActive BOOLEAN DEFAULT TRUE
);

-- Create indexes for better performance
CREATE INDEX idx_school_name ON school(SchoolName);
CREATE INDEX idx_school_region ON school(Region);
CREATE INDEX idx_school_division ON school(Division);
CREATE INDEX idx_school_district ON school(District);
CREATE INDEX idx_school_active ON school(IsActive);

-- Insert sample schools with different regions for testing
INSERT IGNORE INTO school (SchoolId, SchoolName, Region, Division, District, SchoolAddress) VALUES
(UUID(), 'Manila High School', 'National Capital Region (NCR)', 'Manila', 'Manila District I', 'Manila, Philippines'),
(UUID(), 'Quezon City High School', 'National Capital Region (NCR)', 'Quezon City', 'Quezon City District I', 'Quezon City, Philippines'),
(UUID(), 'Makati High School', 'National Capital Region (NCR)', 'Makati', 'Makati District', 'Makati, Philippines'),
(UUID(), 'Bulacan High School', 'Region III - Central Luzon', 'Bulacan', 'Bulacan District I', 'Bulacan, Philippines'),
(UUID(), 'Pampanga High School', 'Region III - Central Luzon', 'Pampanga', 'Pampanga District', 'Pampanga, Philippines'),
(UUID(), 'Cebu High School', 'Region VII - Central Visayas', 'Cebu', 'Cebu District I', 'Cebu, Philippines'),
(UUID(), 'Davao High School', 'Region XI - Davao Region', 'Davao', 'Davao District', 'Davao, Philippines'),
(UUID(), 'Baguio High School', 'Cordillera Administrative Region (CAR)', 'Baguio', 'Baguio District', 'Baguio, Philippines'),
(UUID(), 'Iloilo High School', 'Region VI - Western Visayas', 'Iloilo', 'Iloilo District', 'Iloilo, Philippines'),
(UUID(), 'Cagayan de Oro High School', 'Region X - Northern Mindanao', 'Cagayan de Oro', 'Cagayan de Oro District', 'Cagayan de Oro, Philippines');
