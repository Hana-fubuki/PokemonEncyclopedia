window.themeInterop = {
    getTheme: function() {
        return localStorage.getItem('pokepedia-theme') || 'dark';
    },
    setTheme: function(theme) {
        localStorage.setItem('pokepedia-theme', theme);
    }
};
