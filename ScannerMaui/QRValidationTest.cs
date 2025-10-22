using ScannerMaui.Services;
using AttrackSharedClass.Models;

namespace ScannerMaui
{
    /// <summary>
    /// Test class to demonstrate QR validation functionality
    /// This shows how the system validates QR codes against teacher information
    /// </summary>
    public class QRValidationTest
    {
        public static void DemonstrateQRValidation()
        {
            Console.WriteLine("=== QR Code Validation Test ===");
            Console.WriteLine();

            // Simulate a teacher from MEWOA High School, Grade 10, Section MEWOA
            var teacher = new TeacherInfo
            {
                TeacherId = "T001",
                FullName = "John Teacher",
                Email = "john.teacher@mewoa.edu",
                SchoolName = "MEWOA High School",
                SchoolId = "SCH001",
                GradeLevel = 10,
                Section = "MEWOA",
                Strand = null
            };

            Console.WriteLine($"Teacher Info:");
            Console.WriteLine($"- Name: {teacher.FullName}");
            Console.WriteLine($"- School: {teacher.SchoolName} (ID: {teacher.SchoolId})");
            Console.WriteLine($"- Grade: {teacher.GradeLevel}");
            Console.WriteLine($"- Section: {teacher.Section}");
            Console.WriteLine();

            // Test cases for QR code validation
            var testCases = new[]
            {
                new
                {
                    Description = "Valid student from same school, grade, and section",
                    QRCode = "ST001|Juan Dela Cruz|10|MEWOA|SCH001",
                    ExpectedResult = "Valid"
                },
                new
                {
                    Description = "Student from different school",
                    QRCode = "ST002|Maria Santos|10|MEWOA|SCH002",
                    ExpectedResult = "Invalid - Different school"
                },
                new
                {
                    Description = "Student from different grade level",
                    QRCode = "ST003|Pedro Garcia|11|MEWOA|SCH001",
                    ExpectedResult = "Invalid - Different grade"
                },
                new
                {
                    Description = "Student from different section",
                    QRCode = "ST004|Ana Lopez|10|STEM|SCH001",
                    ExpectedResult = "Invalid - Different section"
                },
                new
                {
                    Description = "Invalid QR code format",
                    QRCode = "InvalidQRCode",
                    ExpectedResult = "Invalid - Bad format"
                }
            };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"Test: {testCase.Description}");
                Console.WriteLine($"QR Code: {testCase.QRCode}");
                Console.WriteLine($"Expected: {testCase.ExpectedResult}");
                Console.WriteLine("---");
            }

            Console.WriteLine();
            Console.WriteLine("=== Validation Logic ===");
            Console.WriteLine("1. Parse QR code to extract student information");
            Console.WriteLine("2. Check if student's school matches teacher's school");
            Console.WriteLine("3. Check if student's grade level matches teacher's grade level");
            Console.WriteLine("4. Check if student's section matches teacher's section");
            Console.WriteLine("5. If all match, QR code is valid for attendance");
            Console.WriteLine("6. If any mismatch, show specific error message");
        }
    }
}
