window.themeInterop = {
    getTheme: function() {
        return localStorage.getItem('pokepedia-theme') || 'dark';
    },
    setTheme: function(theme) {
        localStorage.setItem('pokepedia-theme', theme);
    }
};

// Initialize theme on page load if needed
document.addEventListener('DOMContentLoaded', function() {
    const savedTheme = localStorage.getItem('pokepedia-theme') || 'dark';
    const page = document.querySelector('.page');
    if (page) {
        page.className = 'page theme-' + savedTheme;
    }
});
