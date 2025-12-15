//// Home Page JavaScript

//// Initialize when document is ready
//document.addEventListener('DOMContentLoaded', function () {
//    console.log('Home page loaded');

//    // Initialize animations
//    initAnimations();

//    // Initialize counters
//    initCounters();

//    // Initialize search functionality
//    initSearch();

//    // Initialize testimonials carousel
//    initTestimonials();

//    // Load dynamic content
//    loadDynamicContent();
//});

//// Initialize animations
//function initAnimations() {
//    // Animate elements on scroll
//    const observerOptions = {
//        threshold: 0.1,
//        rootMargin: '0px 0px -50px 0px'
//    };

//    const observer = new IntersectionObserver((entries) => {
//        entries.forEach(entry => {
//            if (entry.isIntersecting) {
//                entry.target.classList.add('animate__animated', 'animate__fadeInUp');
//            }
//        });
//    }, observerOptions);

//    // Observe all cards and sections
//    document.querySelectorAll('.feature-card, .course-card, .instructor-card, .testimonial-card').forEach(card => {
//        observer.observe(card);
//    });

//    // Add hover animations
//    document.querySelectorAll('.course-card, .instructor-card').forEach(card => {
//        card.addEventListener('mouseenter', function () {
//            this.style.zIndex = '10';
//        });

//        card.addEventListener('mouseleave', function () {
//            this.style.zIndex = '1';
//        });
//    });
//}

//// Animate counters
//function initCounters() {
//    const counterElements = document.querySelectorAll('.counter');

//    const observer = new IntersectionObserver((entries) => {
//        entries.forEach(entry => {
//            if (entry.isIntersecting) {
//                const element = entry.target;
//                const target = parseInt(element.getAttribute('data-count'));
//                const duration = 2000; // 2 seconds
//                const step = Math.ceil(target / (duration / 16)); // 60fps
//                let current = 0;

//                const timer = setInterval(() => {
//                    current += step;
//                    if (current >= target) {
//                        current = target;
//                        clearInterval(timer);
//                    }
//                    element.textContent = current.toLocaleString();
//                }, 16);

//                observer.unobserve(element);
//            }
//        });
//    }, { threshold: 0.5 });

//    counterElements.forEach(element => {
//        observer.observe(element);
//    });
//}

//// Initialize search functionality
//function initSearch() {
//    const searchInput = document.getElementById('homeSearch');
//    const searchButton = document.getElementById('searchButton');

//    if (searchInput && searchButton) {
//        // Search on button click
//        searchButton.addEventListener('click', function () {
//            performSearch();
//        });

//        // Search on Enter key
//        searchInput.addEventListener('keypress', function (e) {
//            if (e.key === 'Enter') {
//                performSearch();
//            }
//        });

//        // Show search suggestions
//        searchInput.addEventListener('input', function () {
//            showSearchSuggestions(this.value);
//        });
//    }
//}

//function performSearch() {
//    const query = document.getElementById('homeSearch').value.trim();
//    if (query) {
//        // Redirect to courses page with search query
//        window.location.href = `/Student/BrowseCourses?search=${encodeURIComponent(query)}`;
//    } else {
//        // Show error message
//        showToast('Please enter a search term', 'warning');
//    }
//}

//function showSearchSuggestions(query) {
//    if (query.length < 2) return;

//    // In a real application, you would make an AJAX call here
//    // For now, we'll use static suggestions
//    const suggestions = [
//        'Web Development',
//        'Data Science',
//        'Mobile Development',
//        'UI/UX Design',
//        'Digital Marketing',
//        'Business Analytics',
//        'Machine Learning',
//        'Cloud Computing'
//    ];

//    // Filter suggestions based on query
//    const filtered = suggestions.filter(s =>
//        s.toLowerCase().includes(query.toLowerCase())
//    );

//    // Show suggestions (implement dropdown in real app)
//}

//// Initialize testimonials
//function initTestimonials() {
//    const testimonialCards = document.querySelectorAll('.testimonial-card');

//    if (testimonialCards.length > 0) {
//        // Add click to expand functionality
//        testimonialCards.forEach(card => {
//            const textElement = card.querySelector('.testimonial-text');
//            const originalText = textElement.textContent;

//            card.addEventListener('click', function () {
//                if (textElement.classList.contains('expanded')) {
//                    textElement.textContent = originalText.length > 120 ?
//                        originalText.substring(0, 120) + '...' : originalText;
//                    textElement.classList.remove('expanded');
//                } else {
//                    textElement.textContent = originalText;
//                    textElement.classList.add('expanded');
//                }
//            });
//        });
//    }
//}

//// Load dynamic content
//function loadDynamicContent() {
//    // Load categories
//    loadCategories();

//    // Load recent courses
//    loadRecentCourses();

//    // Load featured instructors
//    loadFeaturedInstructors();

//    // Update live stats
//    updateLiveStats();
//}

//function loadCategories() {
//    fetch('/Home/GetCourseCategories')
//        .then(response => response.json())
//        .then(data => {
//            if (data.success && data.subjects.length > 0) {
//                const container = document.getElementById('categoriesContainer');
//                if (container) {
//                    container.innerHTML = '';

//                    data.subjects.forEach(category => {
//                        const categoryCard = createCategoryCard(category);
//                        container.appendChild(categoryCard);
//                    });
//                }
//            }
//        })
//        .catch(error => {
//            console.error('Error loading categories:', error);
//        });
//}

//function createCategoryCard(category) {
//    const col = document.createElement('div');
//    col.className = 'col-6 col-md-4 col-lg-3';

//    col.innerHTML = `
//        <a href="/Student/BrowseCourses?subjectId=${category.Id}" class="category-card">
//            <div class="category-icon">
//                <img src="${category.ImageUrl || '/images/default-subject.jpg'}" 
//                     alt="${category.Name}"
//                     onerror="this.src='/images/default-subject.jpg'">
//            </div>
//            <div class="category-content">
//                <h5>${category.Name}</h5>
//                <p>${category.Description || 'Explore courses in this category'}</p>
//                <span class="badge bg-primary">${category.CourseCount} Courses</span>
//            </div>
//        </a>
//    `;

//    return col;
//}

//function loadRecentCourses() {
//    // This would be an AJAX call to get recent courses
//    // For now, we'll use the data already loaded in the view
//}

//function loadFeaturedInstructors() {
//    // This would be an AJAX call to get featured instructors
//    // For now, we'll use the data already loaded in the view
//}

//function updateLiveStats() {
//    // Update live stats periodically
//    setInterval(() => {
//        // In a real app, this would be an AJAX call to get updated stats
//        const stats = document.querySelectorAll('.stat-box h3');

//        // Just for demo - add random small numbers
//        stats.forEach(stat => {
//            const current = parseInt(stat.textContent.replace(/,/g, ''));
//            const increment = Math.floor(Math.random() * 10); // Random 0-9
//            const newValue = current + increment;
//            stat.textContent = newValue.toLocaleString();
//        });
//    }, 60000); // Update every minute
//}

//// Global toast function
//function showToast(message, type = 'info') {
//    // Check if toast container exists
//    let container = document.querySelector('.toast-container');
//    if (!container) {
//        container = document.createElement('div');
//        container.className = 'toast-container position-fixed top-0 end-0 p-3';
//        document.body.appendChild(container);
//    }

//    // Create toast
//    const toast = document.createElement('div');
//    toast.className = `toast align-items-center text-white bg-${type} border-0`;
//    toast.setAttribute('role', 'alert');
//    toast.setAttribute('aria-live', 'assertive');
//    toast.setAttribute('aria-atomic', 'true');

//    toast.innerHTML = `
//        <div class="d-flex">
//            <div class="toast-body">
//                <i class="fas fa-${getToastIcon(type)} me-2"></i>
//                ${message}
//            </div>
//            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
//        </div>
//    `;

//    container.appendChild(toast);
//    const bsToast = new bootstrap.Toast(toast);
//    bsToast.show();

//    // Remove toast after it hides
//    toast.addEventListener('hidden.bs.toast', function () {
//        toast.remove();
//    });
//}

//function getToastIcon(type) {
//    switch (type) {
//        case 'success': return 'check-circle';
//        case 'danger': return 'exclamation-circle';
//        case 'warning': return 'exclamation-triangle';
//        default: return 'info-circle';
//    }
//}

//// Newsletter subscription
//function subscribeNewsletter(email) {
//    if (!email || !validateEmail(email)) {
//        showToast('Please enter a valid email address', 'warning');
//        return;
//    }

//    // In a real app, this would be an AJAX call
//    showToast('Thank you for subscribing!', 'success');

//    // Reset form
//    document.querySelector('.newsletter-form input[type="email"]').value = '';
//}

//function validateEmail(email) {
//    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
//    return re.test(email);
//}

//// Initialize newsletter form if exists
//const newsletterForm = document.querySelector('.newsletter-form');
//if (newsletterForm) {
//    newsletterForm.addEventListener('submit', function (e) {
//        e.preventDefault();
//        const email = this.querySelector('input[type="email"]').value;
//        subscribeNewsletter(email);
//    });
//}

//// Track user engagement
//function trackEngagement(action, data = {}) {
//    // In a real app, this would send data to analytics
//    console.log(`User engagement: ${action}`, data);
//}

//// Track clicks on course cards
//document.querySelectorAll('.course-card').forEach(card => {
//    card.addEventListener('click', function () {
//        const courseId = this.dataset.courseId;
//        const courseTitle = this.querySelector('.course-title').textContent;
//        trackEngagement('course_click', { courseId, courseTitle });
//    });
//});

//// Track instructor clicks
//document.querySelectorAll('.instructor-card').forEach(card => {
//    card.addEventListener('click', function () {
//        const instructorId = this.dataset.instructorId;
//        const instructorName = this.querySelector('.instructor-name').textContent;
//        trackEngagement('instructor_click', { instructorId, instructorName });
//    });
//});