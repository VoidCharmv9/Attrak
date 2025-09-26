-- Subject data for Grade 7-12 with strands for Grade 11-12
-- ABM = Accountancy, Business and Management
-- HUMSS = Humanities and Social Sciences
-- STEM = Science, Technology, Engineering and Mathematics
-- GAS = General Academic Strand

-- Grade 7 Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'English 7', 7, NULL, '07:30:00', '08:30:00'),
(UUID(), 'Filipino 7', 7, NULL, '08:30:00', '09:30:00'),
(UUID(), 'Mathematics 7', 7, NULL, '09:30:00', '10:30:00'),
(UUID(), 'Science 7', 7, NULL, '10:30:00', '11:30:00'),
(UUID(), 'Araling Panlipunan 7', 7, NULL, '11:30:00', '12:30:00'),
(UUID(), 'Edukasyon sa Pagpapakatao 7', 7, NULL, '13:30:00', '14:30:00'),
(UUID(), 'Music 7', 7, NULL, '14:30:00', '15:30:00'),
(UUID(), 'Arts 7', 7, NULL, '15:30:00', '16:30:00'),
(UUID(), 'Physical Education 7', 7, NULL, '16:30:00', '17:30:00'),
(UUID(), 'Health 7', 7, NULL, '17:30:00', '18:30:00'),
(UUID(), 'Technology and Livelihood Education 7', 7, NULL, '18:30:00', '19:30:00');

-- Grade 8 Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'English 8', 8, NULL, '07:30:00', '08:30:00'),
(UUID(), 'Filipino 8', 8, NULL, '08:30:00', '09:30:00'),
(UUID(), 'Mathematics 8', 8, NULL, '09:30:00', '10:30:00'),
(UUID(), 'Science 8', 8, NULL, '10:30:00', '11:30:00'),
(UUID(), 'Araling Panlipunan 8', 8, NULL, '11:30:00', '12:30:00'),
(UUID(), 'Edukasyon sa Pagpapakatao 8', 8, NULL, '13:30:00', '14:30:00'),
(UUID(), 'Music 8', 8, NULL, '14:30:00', '15:30:00'),
(UUID(), 'Arts 8', 8, NULL, '15:30:00', '16:30:00'),
(UUID(), 'Physical Education 8', 8, NULL, '16:30:00', '17:30:00'),
(UUID(), 'Health 8', 8, NULL, '17:30:00', '18:30:00'),
(UUID(), 'Technology and Livelihood Education 8', 8, NULL, '18:30:00', '19:30:00');

-- Grade 9 Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'English 9', 9, NULL, '07:30:00', '08:30:00'),
(UUID(), 'Filipino 9', 9, NULL, '08:30:00', '09:30:00'),
(UUID(), 'Mathematics 9', 9, NULL, '09:30:00', '10:30:00'),
(UUID(), 'Science 9', 9, NULL, '10:30:00', '11:30:00'),
(UUID(), 'Araling Panlipunan 9', 9, NULL, '11:30:00', '12:30:00'),
(UUID(), 'Edukasyon sa Pagpapakatao 9', 9, NULL, '13:30:00', '14:30:00'),
(UUID(), 'Music 9', 9, NULL, '14:30:00', '15:30:00'),
(UUID(), 'Arts 9', 9, NULL, '15:30:00', '16:30:00'),
(UUID(), 'Physical Education 9', 9, NULL, '16:30:00', '17:30:00'),
(UUID(), 'Health 9', 9, NULL, '17:30:00', '18:30:00'),
(UUID(), 'Technology and Livelihood Education 9', 9, NULL, '18:30:00', '19:30:00');

-- Grade 10 Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'English 10', 10, NULL, '07:30:00', '08:30:00'),
(UUID(), 'Filipino 10', 10, NULL, '08:30:00', '09:30:00'),
(UUID(), 'Mathematics 10', 10, NULL, '09:30:00', '10:30:00'),
(UUID(), 'Science 10', 10, NULL, '10:30:00', '11:30:00'),
(UUID(), 'Araling Panlipunan 10', 10, NULL, '11:30:00', '12:30:00'),
(UUID(), 'Edukasyon sa Pagpapakatao 10', 10, NULL, '13:30:00', '14:30:00'),
(UUID(), 'Music 10', 10, NULL, '14:30:00', '15:30:00'),
(UUID(), 'Arts 10', 10, NULL, '15:30:00', '16:30:00'),
(UUID(), 'Physical Education 10', 10, NULL, '16:30:00', '17:30:00'),
(UUID(), 'Health 10', 10, NULL, '17:30:00', '18:30:00'),
(UUID(), 'Technology and Livelihood Education 10', 10, NULL, '18:30:00', '19:30:00');

-- Grade 11 ABM Strand Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'Oral Communication', 11, 'ABM', '07:30:00', '08:30:00'),
(UUID(), 'Komunikasyon at Pananaliksik sa Wika at Kulturang Pilipino', 11, 'ABM', '08:30:00', '09:30:00'),
(UUID(), 'General Mathematics', 11, 'ABM', '09:30:00', '10:30:00'),
(UUID(), 'Earth and Life Science', 11, 'ABM', '10:30:00', '11:30:00'),
(UUID(), 'Personal Development', 11, 'ABM', '11:30:00', '12:30:00'),
(UUID(), 'Understanding Culture, Society and Politics', 11, 'ABM', '13:30:00', '14:30:00'),
(UUID(), 'Physical Education and Health', 11, 'ABM', '14:30:00', '15:30:00'),
(UUID(), 'Fundamentals of Accountancy, Business and Management 1', 11, 'ABM', '15:30:00', '16:30:00'),
(UUID(), 'Business Mathematics', 11, 'ABM', '16:30:00', '17:30:00'),
(UUID(), 'Organization and Management', 11, 'ABM', '17:30:00', '18:30:00'),
(UUID(), 'Principles of Marketing', 11, 'ABM', '18:30:00', '19:30:00');

-- Grade 11 HUMSS Strand Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'Oral Communication', 11, 'HUMSS', '07:30:00', '08:30:00'),
(UUID(), 'Komunikasyon at Pananaliksik sa Wika at Kulturang Pilipino', 11, 'HUMSS', '08:30:00', '09:30:00'),
(UUID(), 'General Mathematics', 11, 'HUMSS', '09:30:00', '10:30:00'),
(UUID(), 'Earth and Life Science', 11, 'HUMSS', '10:30:00', '11:30:00'),
(UUID(), 'Personal Development', 11, 'HUMSS', '11:30:00', '12:30:00'),
(UUID(), 'Understanding Culture, Society and Politics', 11, 'HUMSS', '13:30:00', '14:30:00'),
(UUID(), 'Physical Education and Health', 11, 'HUMSS', '14:30:00', '15:30:00'),
(UUID(), 'Introduction to World Religions and Belief Systems', 11, 'HUMSS', '15:30:00', '16:30:00'),
(UUID(), 'Creative Writing', 11, 'HUMSS', '16:30:00', '17:30:00'),
(UUID(), 'Disciplines and Ideas in the Social Sciences', 11, 'HUMSS', '17:30:00', '18:30:00'),
(UUID(), 'Philippine Politics and Governance', 11, 'HUMSS', '18:30:00', '19:30:00');

-- Grade 11 STEM Strand Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'Oral Communication', 11, 'STEM', '07:30:00', '08:30:00'),
(UUID(), 'Komunikasyon at Pananaliksik sa Wika at Kulturang Pilipino', 11, 'STEM', '08:30:00', '09:30:00'),
(UUID(), 'General Mathematics', 11, 'STEM', '09:30:00', '10:30:00'),
(UUID(), 'Earth and Life Science', 11, 'STEM', '10:30:00', '11:30:00'),
(UUID(), 'Personal Development', 11, 'STEM', '11:30:00', '12:30:00'),
(UUID(), 'Understanding Culture, Society and Politics', 11, 'STEM', '13:30:00', '14:30:00'),
(UUID(), 'Physical Education and Health', 11, 'STEM', '14:30:00', '15:30:00'),
(UUID(), 'Pre-Calculus', 11, 'STEM', '15:30:00', '16:30:00'),
(UUID(), 'Basic Calculus', 11, 'STEM', '16:30:00', '17:30:00'),
(UUID(), 'General Physics 1', 11, 'STEM', '17:30:00', '18:30:00'),
(UUID(), 'General Chemistry 1', 11, 'STEM', '18:30:00', '19:30:00');

-- Grade 11 GAS Strand Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'Oral Communication', 11, 'GAS', '07:30:00', '08:30:00'),
(UUID(), 'Komunikasyon at Pananaliksik sa Wika at Kulturang Pilipino', 11, 'GAS', '08:30:00', '09:30:00'),
(UUID(), 'General Mathematics', 11, 'GAS', '09:30:00', '10:30:00'),
(UUID(), 'Earth and Life Science', 11, 'GAS', '10:30:00', '11:30:00'),
(UUID(), 'Personal Development', 11, 'GAS', '11:30:00', '12:30:00'),
(UUID(), 'Understanding Culture, Society and Politics', 11, 'GAS', '13:30:00', '14:30:00'),
(UUID(), 'Physical Education and Health', 11, 'GAS', '14:30:00', '15:30:00'),
(UUID(), 'Applied Economics', 11, 'GAS', '15:30:00', '16:30:00'),
(UUID(), 'Business Ethics and Social Responsibility', 11, 'GAS', '16:30:00', '17:30:00'),
(UUID(), 'Work Immersion', 11, 'GAS', '17:30:00', '18:30:00'),
(UUID(), 'Research in Daily Life 1', 11, 'GAS', '18:30:00', '19:30:00');

-- Grade 12 ABM Strand Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'Reading and Writing Skills', 12, 'ABM', '07:30:00', '08:30:00'),
(UUID(), 'Pagbasa at Pagsusuri ng Iba\'t Ibang Teksto Tungo sa Pananaliksik', 12, 'ABM', '08:30:00', '09:30:00'),
(UUID(), 'Statistics and Probability', 12, 'ABM', '09:30:00', '10:30:00'),
(UUID(), 'Physical Science', 12, 'ABM', '10:30:00', '11:30:00'),
(UUID(), 'Physical Education and Health', 12, 'ABM', '11:30:00', '12:30:00'),
(UUID(), 'Contemporary Philippine Arts from the Regions', 12, 'ABM', '13:30:00', '14:30:00'),
(UUID(), 'Media and Information Literacy', 12, 'ABM', '14:30:00', '15:30:00'),
(UUID(), 'Fundamentals of Accountancy, Business and Management 2', 12, 'ABM', '15:30:00', '16:30:00'),
(UUID(), 'Business Finance', 12, 'ABM', '16:30:00', '17:30:00'),
(UUID(), 'Applied Economics', 12, 'ABM', '17:30:00', '18:30:00'),
(UUID(), 'Business Ethics and Social Responsibility', 12, 'ABM', '18:30:00', '19:30:00');

-- Grade 12 HUMSS Strand Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'Reading and Writing Skills', 12, 'HUMSS', '07:30:00', '08:30:00'),
(UUID(), 'Pagbasa at Pagsusuri ng Iba\'t Ibang Teksto Tungo sa Pananaliksik', 12, 'HUMSS', '08:30:00', '09:30:00'),
(UUID(), 'Statistics and Probability', 12, 'HUMSS', '09:30:00', '10:30:00'),
(UUID(), 'Physical Science', 12, 'HUMSS', '10:30:00', '11:30:00'),
(UUID(), 'Physical Education and Health', 12, 'HUMSS', '11:30:00', '12:30:00'),
(UUID(), 'Contemporary Philippine Arts from the Regions', 12, 'HUMSS', '13:30:00', '14:30:00'),
(UUID(), 'Media and Information Literacy', 12, 'HUMSS', '14:30:00', '15:30:00'),
(UUID(), 'Creative Nonfiction', 12, 'HUMSS', '15:30:00', '16:30:00'),
(UUID(), 'Trends, Networks, and Critical Thinking in the 21st Century', 12, 'HUMSS', '16:30:00', '17:30:00'),
(UUID(), 'Community Engagement, Solidarity, and Citizenship', 12, 'HUMSS', '17:30:00', '18:30:00'),
(UUID(), 'Disciplines and Ideas in Applied Social Sciences', 12, 'HUMSS', '18:30:00', '19:30:00');

-- Grade 12 STEM Strand Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'Reading and Writing Skills', 12, 'STEM', '07:30:00', '08:30:00'),
(UUID(), 'Pagbasa at Pagsusuri ng Iba\'t Ibang Teksto Tungo sa Pananaliksik', 12, 'STEM', '08:30:00', '09:30:00'),
(UUID(), 'Statistics and Probability', 12, 'STEM', '09:30:00', '10:30:00'),
(UUID(), 'Physical Science', 12, 'STEM', '10:30:00', '11:30:00'),
(UUID(), 'Physical Education and Health', 12, 'STEM', '11:30:00', '12:30:00'),
(UUID(), 'Contemporary Philippine Arts from the Regions', 12, 'STEM', '13:30:00', '14:30:00'),
(UUID(), 'Media and Information Literacy', 12, 'STEM', '14:30:00', '15:30:00'),
(UUID(), 'Basic Calculus', 12, 'STEM', '15:30:00', '16:30:00'),
(UUID(), 'General Physics 2', 12, 'STEM', '16:30:00', '17:30:00'),
(UUID(), 'General Chemistry 2', 12, 'STEM', '17:30:00', '18:30:00'),
(UUID(), 'General Biology 2', 12, 'STEM', '18:30:00', '19:30:00');

-- Grade 12 GAS Strand Subjects
INSERT INTO Subject (SubjectId, SubjectName, GradeLevel, Strand, ScheduleStart, ScheduleEnd) VALUES
(UUID(), 'Reading and Writing Skills', 12, 'GAS', '07:30:00', '08:30:00'),
(UUID(), 'Pagbasa at Pagsusuri ng Iba\'t Ibang Teksto Tungo sa Pananaliksik', 12, 'GAS', '08:30:00', '09:30:00'),
(UUID(), 'Statistics and Probability', 12, 'GAS', '09:30:00', '10:30:00'),
(UUID(), 'Physical Science', 12, 'GAS', '10:30:00', '11:30:00'),
(UUID(), 'Physical Education and Health', 12, 'GAS', '11:30:00', '12:30:00'),
(UUID(), 'Contemporary Philippine Arts from the Regions', 12, 'GAS', '13:30:00', '14:30:00'),
(UUID(), 'Media and Information Literacy', 12, 'GAS', '14:30:00', '15:30:00'),
(UUID(), 'Applied Economics', 12, 'GAS', '15:30:00', '16:30:00'),
(UUID(), 'Business Ethics and Social Responsibility', 12, 'GAS', '16:30:00', '17:30:00'),
(UUID(), 'Work Immersion', 12, 'GAS', '17:30:00', '18:30:00'),
(UUID(), 'Research in Daily Life 2', 12, 'GAS', '18:30:00', '19:30:00');

-- Check total subjects inserted
SELECT 
    GradeLevel,
    Strand,
    COUNT(*) as SubjectCount
FROM Subject 
GROUP BY GradeLevel, Strand 
ORDER BY GradeLevel, Strand;