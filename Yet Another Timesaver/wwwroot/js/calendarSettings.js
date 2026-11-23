window.calendarSettings = {
    load: function() {
        return localStorage.getItem('yats.weekViewSettings');
    },
    save: function(json) {
        localStorage.setItem('yats.weekViewSettings', json);
    }
};
