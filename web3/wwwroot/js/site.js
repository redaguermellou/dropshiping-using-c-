// Modern E-commerce Interactions
class ECommerceUI {
    constructor() {
        this.init();
    }

    init() {
        this.initCart();
        this.initQuickView();
        this.initNotifications();
        this.initAnimations();
        this.initSearch();
        this.initWishlist();
    }

    // Cart Management
    initCart() {
        // Add to cart with animation
        document.addEventListener('click', (e) => {
            const addToCartBtn = e.target.closest('[data-add-to-cart]');
            if (addToCartBtn) {
                e.preventDefault();
                this.addToCartWithAnimation(addToCartBtn);
            }
        });

        // Update cart badge
        this.updateCartBadge();
    }

    async addToCartWithAnimation(button) {
        const productId = button.dataset.productId;
        const productName = button.dataset.productName || 'Produit';

        // Show loading state
        const originalHTML = button.innerHTML;
        button.innerHTML = '<span class="loading-spinner" style="width:16px; height:16px; border-width:2px;"></span>';
        button.disabled = true;

        try {
            // Real API call to CartController
            const formData = new FormData();
            formData.append('productId', productId);
            formData.append('quantity', 1);

            const response = await fetch('/Cart/AddToCart', {
                method: 'POST',
                body: formData,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (response.ok) {
                // Show success animation if we have product card
                const productCard = button.closest('.product-card');
                if (productCard) {
                    this.showProductFlyAnimation(productCard);
                }

                // Update cart badge from server
                await this.refreshCartBadge();

                // Show notification
                this.showNotification(`${productName} ajouté au panier !`, 'success');
            } else {
                throw new Error('Erreur');
            }
        } catch (error) {
            console.error('Error adding to cart:', error);
            this.showNotification('Échec de l\'ajout au panier', 'error');
        } finally {
            // Restore button
            button.innerHTML = originalHTML;
            button.disabled = false;
        }
    }

    showProductFlyAnimation(productCard) {
        const productImg = productCard.querySelector('img');
        const cartIcon = document.querySelector('.cart-icon');

        if (!productImg || !cartIcon) return;

        // Clone the image for animation
        const flyingImg = productImg.cloneNode();
        const rect = productImg.getBoundingClientRect();
        const cartRect = cartIcon.getBoundingClientRect();

        // Style the flying image
        flyingImg.style.position = 'fixed';
        flyingImg.style.width = '50px';
        flyingImg.style.height = '50px';
        flyingImg.style.objectFit = 'cover';
        flyingImg.style.borderRadius = '8px';
        flyingImg.style.zIndex = '9999';
        flyingImg.style.left = `${rect.left}px`;
        flyingImg.style.top = `${rect.top}px`;
        flyingImg.style.transition = 'all 0.6s cubic-bezier(0.4, 0, 0.2, 1)';

        document.body.appendChild(flyingImg);

        // Trigger animation
        requestAnimationFrame(() => {
            flyingImg.style.left = `${cartRect.left}px`;
            flyingImg.style.top = `${cartRect.top}px`;
            flyingImg.style.width = '20px';
            flyingImg.style.height = '20px';
            flyingImg.style.opacity = '0';
        });

        // Clean up
        setTimeout(() => flyingImg.remove(), 600);
    }

    incrementCartBadge() {
        const badge = document.querySelector('.cart-badge');
        if (!badge) return;

        const currentCount = parseInt(badge.textContent) || 0;
        badge.textContent = currentCount + 1;
        badge.classList.add('pulse-animation');

        setTimeout(() => {
            badge.classList.remove('pulse-animation');
        }, 300);
    }

    // Quick View Modal
    initQuickView() {
        document.addEventListener('click', (e) => {
            const quickViewBtn = e.target.closest('[data-quick-view]');
            if (quickViewBtn) {
                e.preventDefault();
                this.showQuickView(quickViewBtn.dataset.productId);
            }

            // Close modal
            if (e.target.closest('.close-quick-view') || e.target.closest('.quick-view-modal')) {
                this.hideQuickView();
            }
        });

        // Close with Escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') this.hideQuickView();
        });
    }

    async showQuickView(productId) {
        // Show loading modal
        const modal = this.createQuickViewModal();
        modal.innerHTML = `
            <div class="modal-content">
                <div class="loading-spinner"></div>
            </div>
        `;
        modal.classList.add('active');

        // Simulate loading product data
        setTimeout(() => {
            this.renderQuickViewContent(productId, modal);
        }, 800);
    }

    renderQuickViewContent(productId, modal) {
        // Mock product data
        const product = {
            id: productId,
            name: "Premium Product",
            price: "$99.99",
            description: "This is a premium quality product with excellent features.",
            image: "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400&h=400&fit=crop"
        };

        modal.innerHTML = `
            <div class="modal-content">
                <button class="close-quick-view btn btn-sm btn-outline-secondary position-absolute top-0 end-0 m-3">
                    <i class="bi bi-x-lg"></i>
                </button>
                <div class="row g-4">
                    <div class="col-md-6">
                        <img src="${product.image}" class="img-fluid rounded" alt="${product.name}">
                    </div>
                    <div class="col-md-6">
                        <h3 class="mb-3">${product.name}</h3>
                        <div class="d-flex align-items-center gap-3 mb-3">
                            <span class="h4 text-primary mb-0">${product.price}</span>
                            <span class="badge bg-success">In Stock</span>
                        </div>
                        <p class="text-muted mb-4">${product.description}</p>
                        <div class="d-flex gap-2">
                            <input type="number" value="1" min="1" class="form-control" style="width: 80px;">
                            <button class="btn btn-primary flex-grow-1" data-add-to-cart data-product-id="${productId}">
                                Add to Cart
                            </button>
                            <button class="btn btn-outline-primary">
                                <i class="bi bi-heart"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }

    hideQuickView() {
        const modal = document.querySelector('.quick-view-modal');
        if (modal) modal.classList.remove('active');
    }

    createQuickViewModal() {
        let modal = document.querySelector('.quick-view-modal');
        if (!modal) {
            modal = document.createElement('div');
            modal.className = 'quick-view-modal';
            document.body.appendChild(modal);
        }
        return modal;
    }

    // Notifications
    initNotifications() {
        this.notificationContainer = this.createNotificationContainer();
    }

    showNotification(message, type = 'info', duration = 3000) {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <i class="bi bi-${this.getNotificationIcon(type)} fs-4"></i>
            <div>${message}</div>
        `;

        this.notificationContainer.appendChild(notification);

        // Show with animation
        requestAnimationFrame(() => {
            notification.classList.add('show');
        });

        // Auto remove
        setTimeout(() => {
            notification.classList.remove('show');
            setTimeout(() => notification.remove(), 300);
        }, duration);
    }

    getNotificationIcon(type) {
        const icons = {
            success: 'check-circle-fill',
            error: 'exclamation-circle-fill',
            info: 'info-circle-fill',
            warning: 'exclamation-triangle-fill'
        };
        return icons[type] || 'info-circle-fill';
    }

    createNotificationContainer() {
        const container = document.createElement('div');
        container.className = 'notification-container';
        container.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 9999;
            display: flex;
            flex-direction: column;
            gap: 10px;
        `;
        document.body.appendChild(container);
        return container;
    }

    // Search with debounce
    initSearch() {
        const searchInput = document.querySelector('.search-input');
        if (!searchInput) return;

        let timeout;
        searchInput.addEventListener('input', (e) => {
            clearTimeout(timeout);
            timeout = setTimeout(() => {
                this.performSearch(e.target.value);
            }, 300);
        });
    }

    async performSearch(query) {
        if (query.length < 2) return;

        // Show loading
        this.showNotification('Searching...', 'info');

        // Simulate API call
        await new Promise(resolve => setTimeout(resolve, 500));

        // Show results (in real app, you would update the UI with results)
        this.showNotification(`Found results for "${query}"`, 'success');
    }

    // Wishlist functionality
    initWishlist() {
        document.addEventListener('click', (e) => {
            const wishlistBtn = e.target.closest('[data-wishlist]');
            if (wishlistBtn) {
                e.preventDefault();
                this.toggleWishlist(wishlistBtn);
            }
        });
    }

    toggleWishlist(button) {
        const isActive = button.classList.contains('active');

        if (isActive) {
            button.classList.remove('active');
            button.innerHTML = '<i class="bi bi-heart"></i>';
            this.showNotification('Removed from wishlist', 'info');
        } else {
            button.classList.add('active');
            button.innerHTML = '<i class="bi bi-heart-fill text-danger"></i>';
            this.showNotification('Added to wishlist', 'success');
        }
    }

    // Animations on scroll
    initAnimations() {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-in');
                }
            });
        }, {
            threshold: 0.1
        });

        // Observe elements with animation class
        document.querySelectorAll('.animate-on-scroll').forEach(el => {
            observer.observe(el);
        });
    }

    async refreshCartBadge() {
        try {
            const response = await fetch('/Cart/GetCartCount');
            if (response.ok) {
                const data = await response.json();
                const badge = document.querySelector('.cart-badge');
                if (badge) {
                    badge.textContent = data.count;
                    if (data.count > 0) {
                        badge.style.display = 'flex';
                        badge.classList.add('pulse-animation');
                        setTimeout(() => badge.classList.remove('pulse-animation'), 300);
                    } else {
                        badge.style.display = 'none';
                    }
                }
            }
        } catch (error) {
            console.error('Error refreshing cart badge:', error);
        }
    }

    updateCartBadge() {
        this.refreshCartBadge();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.ecomUI = new ECommerceUI();

    // Initialize tooltips
    const tooltips = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltips.map(tooltip => new bootstrap.Tooltip(tooltip));

    // Price range filter
    const priceRange = document.querySelector('.price-range');
    if (priceRange) {
        const priceValue = document.querySelector('.price-value');
        priceRange.addEventListener('input', (e) => {
            if (priceValue) {
                priceValue.textContent = `$${e.target.value}`;
            }
        });
    }

    // Product image zoom
    document.querySelectorAll('.product-image-zoom').forEach(img => {
        img.addEventListener('mousemove', (e) => {
            const { left, top, width, height } = img.getBoundingClientRect();
            const x = ((e.clientX - left) / width) * 100;
            const y = ((e.clientY - top) / height) * 100;
            img.style.transformOrigin = `${x}% ${y}%`;
            img.style.transform = 'scale(2)';
        });

        img.addEventListener('mouseleave', () => {
            img.style.transform = 'scale(1)';
        });
    });
});

// Utility functions
const Utils = {
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    formatPrice(price) {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(price);
    },

    truncateText(text, maxLength = 100) {
        return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
    }
};
// Add this after the Utils class in your site.js

// Product card hover effects
document.querySelectorAll('.product-card').forEach(card => {
    card.addEventListener('mouseenter', () => {
        card.classList.add('hovered');
    });

    card.addEventListener('mouseleave', () => {
        card.classList.remove('hovered');
    });
});

// Initialize all tooltips
function initTooltips() {
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'))
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl)
    })
}

// Cart quantity updates
function updateCartQuantity(input) {
    const min = parseInt(input.getAttribute('min')) || 1;
    const max = parseInt(input.getAttribute('max')) || 99;
    let value = parseInt(input.value) || min;

    if (value < min) value = min;
    if (value > max) value = max;

    input.value = value;
}

// Initialize cart quantity controls
document.addEventListener('click', function (e) {
    // Quantity minus
    if (e.target.classList.contains('quantity-minus')) {
        e.preventDefault();
        const input = e.target.nextElementSibling;
        input.value = Math.max(parseInt(input.value) - 1, parseInt(input.min) || 1);
        updateCartQuantity(input);
    }

    // Quantity plus
    if (e.target.classList.contains('quantity-plus')) {
        e.preventDefault();
        const input = e.target.previousElementSibling;
        input.value = Math.min(parseInt(input.value) + 1, parseInt(input.max) || 99);
        updateCartQuantity(input);
    }
});

// Form validation enhancements
document.querySelectorAll('form').forEach(form => {
    form.addEventListener('submit', function (e) {
        const submitBtn = this.querySelector('[type="submit"]');
        if (submitBtn && !submitBtn.disabled) {
            submitBtn.innerHTML = '<span class="loading-spinner"></span> Processing...';
            submitBtn.disabled = true;
        }
    });
});

// Initialize on page load
document.addEventListener('DOMContentLoaded', function () {
    initTooltips();

    // Add animation classes to elements that should animate on scroll
    document.querySelectorAll('.product-card, .featured-item, .category-card').forEach(el => {
        el.classList.add('animate-on-scroll');
    });
});