// ========================================
// BanjirWatch - Site JavaScript
// ========================================

// Mobile Navigation Toggle
document.addEventListener('DOMContentLoaded', function () {
    const navToggle = document.getElementById('navToggle');
    const navMenu = document.getElementById('navMenu');

    if (navToggle && navMenu) {
        navToggle.addEventListener('click', function () {
            navMenu.classList.toggle('active');
        });

        // Close menu when clicking outside
        document.addEventListener('click', function (e) {
            if (!navToggle.contains(e.target) && !navMenu.contains(e.target)) {
                navMenu.classList.remove('active');
            }
        });
    }

    // Dropdown toggle for mobile
    const dropdownToggles = document.querySelectorAll('.dropdown-toggle');
    dropdownToggles.forEach(toggle => {
        toggle.addEventListener('click', function (e) {
            e.preventDefault();
            const parent = this.closest('.nav-dropdown');
            parent.classList.toggle('active');
        });
    });

    // Auto-hide alerts after 5 seconds
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            alert.style.opacity = '0';
            setTimeout(() => alert.remove(), 300);
        }, 5000);
    });
});

// ========================================
// Like Functionality
// ========================================

async function toggleLike(postId) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    
    try {
        const response = await fetch(`/Posts/ToggleLike?postId=${postId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            }
        });

        if (response.ok) {
            const data = await response.json();
            updateLikeUI(postId, data.liked, data.likesCount);
        } else if (response.status === 401) {
            window.location.href = '/Auth/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
        }
    } catch (error) {
        console.error('Error toggling like:', error);
    }
}

function updateLikeUI(postId, liked, count) {
    const likeBtn = document.querySelector(`[data-post-id="${postId}"].like-btn`);
    if (likeBtn) {
        likeBtn.classList.toggle('liked', liked);
        const countSpan = likeBtn.querySelector('.like-count');
        if (countSpan) {
            countSpan.textContent = count;
        }
        
        const icon = likeBtn.querySelector('i');
        if (icon) {
            icon.className = liked ? 'fas fa-heart' : 'far fa-heart';
        }
    }
}

// ========================================
// Comment Functionality
// ========================================

async function addComment(postId, content) {
    if (!content.trim()) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

    try {
        const formData = new FormData();
        formData.append('postId', postId);
        formData.append('content', content);

        const response = await fetch('/Posts/AddComment', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });

        if (response.ok) {
            const data = await response.json();
            appendCommentToUI(postId, data);
            
            // Clear input
            const input = document.querySelector(`#comment-input-${postId}`);
            if (input) input.value = '';
        } else if (response.status === 401) {
            window.location.href = '/Auth/Login';
        }
    } catch (error) {
        console.error('Error adding comment:', error);
    }
}

function appendCommentToUI(postId, comment) {
    const commentsContainer = document.querySelector(`#comments-${postId}`);
    if (!commentsContainer) return;

    const commentHTML = `
        <div class="comment" data-comment-id="${comment.id}">
            <img src="${comment.avatarPath || '/images/default-avatar.svg'}" alt="" class="comment-avatar">
            <div class="comment-content">
                <div class="comment-header">
                    <span class="comment-username">${escapeHtml(comment.username)}</span>
                    <span class="comment-time">${comment.createdAt}</span>
                </div>
                <p class="comment-text">${escapeHtml(comment.content)}</p>
            </div>
        </div>
    `;

    commentsContainer.insertAdjacentHTML('beforeend', commentHTML);
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ========================================
// Image Preview
// ========================================

function previewImage(input, previewId) {
    const preview = document.getElementById(previewId);
    if (!preview) return;

    if (input.files && input.files[0]) {
        const reader = new FileReader();
        reader.onload = function (e) {
            preview.src = e.target.result;
            preview.style.display = 'block';
        };
        reader.readAsDataURL(input.files[0]);
    }
}

// ========================================
// Location Functions
// ========================================

function getCurrentPosition() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error('Geolocation is not supported'));
            return;
        }

        navigator.geolocation.getCurrentPosition(
            position => resolve({
                lat: position.coords.latitude,
                lng: position.coords.longitude
            }),
            error => reject(error),
            { enableHighAccuracy: true, timeout: 10000, maximumAge: 60000 }
        );
    });
}

async function updateLocationFields() {
    try {
        const position = await getCurrentPosition();
        const latInput = document.getElementById('Latitude');
        const lngInput = document.getElementById('Longitude');

        if (latInput) latInput.value = position.lat.toFixed(6);
        if (lngInput) lngInput.value = position.lng.toFixed(6);

        // Try to get location name using reverse geocoding
        const locationName = await reverseGeocode(position.lat, position.lng);
        const locationInput = document.getElementById('LocationName');
        if (locationInput && locationName) {
            locationInput.value = locationName;
        }
    } catch (error) {
        console.warn('Could not get location:', error);
    }
}

async function reverseGeocode(lat, lng) {
    try {
        const response = await fetch(`https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}`);
        const data = await response.json();
        return data.display_name || null;
    } catch (error) {
        console.warn('Reverse geocoding failed:', error);
        return null;
    }
}

// ========================================
// Infinite Scroll
// ========================================

function setupInfiniteScroll(containerSelector, loadMoreUrl) {
    let currentPage = 1;
    let isLoading = false;
    let hasMore = true;

    const container = document.querySelector(containerSelector);
    if (!container) return;

    const observer = new IntersectionObserver((entries) => {
        const lastEntry = entries[0];
        if (lastEntry.isIntersecting && !isLoading && hasMore) {
            loadMore();
        }
    });

    const sentinel = document.createElement('div');
    sentinel.className = 'scroll-sentinel';
    container.appendChild(sentinel);
    observer.observe(sentinel);

    async function loadMore() {
        isLoading = true;
        currentPage++;

        try {
            const response = await fetch(`${loadMoreUrl}?page=${currentPage}`, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (response.ok) {
                const html = await response.text();
                if (html.trim()) {
                    sentinel.insertAdjacentHTML('beforebegin', html);
                } else {
                    hasMore = false;
                    observer.disconnect();
                    sentinel.remove();
                }
            }
        } catch (error) {
            console.error('Error loading more:', error);
        } finally {
            isLoading = false;
        }
    }
}

// ========================================
// Utility Functions
// ========================================

function formatTimeAgo(dateString) {
    const date = new Date(dateString);
    const now = new Date();
    const seconds = Math.floor((now - date) / 1000);

    if (seconds < 60) return 'just now';
    if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
    if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
    if (seconds < 604800) return `${Math.floor(seconds / 86400)}d ago`;
    
    return date.toLocaleDateString();
}

// Update all time-ago elements
document.addEventListener('DOMContentLoaded', function () {
    const timeElements = document.querySelectorAll('.time-ago');
    timeElements.forEach(el => {
        const date = el.getAttribute('data-date');
        if (date) {
            el.textContent = formatTimeAgo(date);
        }
    });
});

// ========================================
// Map Initialization Helper
// ========================================

function initializeMap(containerId, options = {}) {
    const defaultOptions = {
        center: [-6.2088, 106.8456],
        zoom: 12
    };

    const config = { ...defaultOptions, ...options };
    
    const map = L.map(containerId).setView(config.center, config.zoom);

    // Add OpenStreetMap tiles
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        maxZoom: 19
    }).addTo(map);

    return map;
}

// ========================================
// Severity Badge Helper
// ========================================

function getSeverityClass(severity) {
    if (severity >= 75) return 'severity-severe';
    if (severity >= 50) return 'severity-high';
    if (severity >= 25) return 'severity-moderate';
    return 'severity-low';
}

function getSeverityLabel(severity) {
    if (severity >= 75) return 'Severe';
    if (severity >= 50) return 'High';
    if (severity >= 25) return 'Moderate';
    return 'Low';
}
