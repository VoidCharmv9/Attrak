// Attrak API Client-side JavaScript

// API Base URL
const API_BASE_URL = window.location.origin + '/api';

// Utility functions
const api = {
    // Generic API call function
    async call(endpoint, options = {}) {
        const url = `${API_BASE_URL}${endpoint}`;
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
            },
        };
        
        const config = { ...defaultOptions, ...options };
        
        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('API call failed:', error);
            throw error;
        }
    },
    
    // GET request
    async get(endpoint) {
        return this.call(endpoint, { method: 'GET' });
    },
    
    // POST request
    async post(endpoint, data) {
        return this.call(endpoint, {
            method: 'POST',
            body: JSON.stringify(data),
        });
    },
    
    // PUT request
    async put(endpoint, data) {
        return this.call(endpoint, {
            method: 'PUT',
            body: JSON.stringify(data),
        });
    },
    
    // DELETE request
    async delete(endpoint) {
        return this.call(endpoint, { method: 'DELETE' });
    }
};

// Authentication functions
const auth = {
    // Login
    async login(credentials) {
        return api.post('/auth/login', credentials);
    },
    
    // Register
    async register(userData) {
        return api.post('/auth/register', userData);
    },
    
    // Logout
    async logout() {
        return api.post('/auth/logout');
    }
};

// Attendance functions
const attendance = {
    // Get attendance records
    async getRecords() {
        return api.get('/attendance');
    },
    
    // Mark attendance
    async markAttendance(data) {
        return api.post('/attendance', data);
    },
    
    // Get attendance by student
    async getByStudent(studentId) {
        return api.get(`/attendance/student/${studentId}`);
    }
};

// School functions
const school = {
    // Get all students
    async getStudents() {
        return api.get('/school/students');
    },
    
    // Get student by ID
    async getStudent(id) {
        return api.get(`/school/students/${id}`);
    },
    
    // Get subjects
    async getSubjects() {
        return api.get('/school/subjects');
    }
};

// Teacher functions
const teacher = {
    // Get teacher subjects
    async getSubjects() {
        return api.get('/teacher/subjects');
    },
    
    // Get subject load
    async getSubjectLoad() {
        return api.get('/teacher/subject-load');
    }
};

// Utility functions
const utils = {
    // Format date
    formatDate(date) {
        return new Date(date).toLocaleDateString('en-PH', {
            year: 'numeric',
            month: 'long',
            day: 'numeric'
        });
    },
    
    // Format time
    formatTime(date) {
        return new Date(date).toLocaleTimeString('en-PH', {
            hour: '2-digit',
            minute: '2-digit'
        });
    },
    
    // Show notification
    showNotification(message, type = 'info') {
        // Simple notification system
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        
        // Add styles
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 15px 20px;
            border-radius: 5px;
            color: white;
            font-weight: 500;
            z-index: 1000;
            animation: slideIn 0.3s ease;
        `;
        
        // Set background color based on type
        const colors = {
            success: '#28a745',
            error: '#dc3545',
            warning: '#ffc107',
            info: '#17a2b8'
        };
        notification.style.backgroundColor = colors[type] || colors.info;
        
        document.body.appendChild(notification);
        
        // Remove after 3 seconds
        setTimeout(() => {
            notification.remove();
        }, 3000);
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('Attrak API client loaded');
    
    // Add CSS for notifications
    const style = document.createElement('style');
    style.textContent = `
        @keyframes slideIn {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }
    `;
    document.head.appendChild(style);
});

// Export for use in other scripts
window.AttrakAPI = {
    api,
    auth,
    attendance,
    school,
    teacher,
    utils
};
