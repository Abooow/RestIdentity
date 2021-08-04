window.getCookie = function (name) {
    name = name + "=";

    var decodedCookie = decodeURIComponent(document.cookie);
    var ca = decodedCookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') {
            c = c.substring(1);
        }
        if (c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }
    return "";
}

window.setSessionCookie = function (name, value) {
    document.cookie = `${name}=${value}; expires=0; path=/`;
}

window.setExpirebleCookie = function (name, value, date) {
    document.cookie = `${name}=${value}; expires=${date.toUTCString()}; path=/`;
}

window.removeCookie = function (name) {
    document.cookie = encodeURIComponent(name) + "=; expires=Thu, 01 Jan 1970 00:00:00 GMT";
}

// Replaced with Session Cookies instead.
//window.getSessionItem = function (key) {
//    return window.sessionStorage.getItem(key);
//}

//window.setSessionItem = function (key, value) {
//    window.sessionStorage.setItem(key, value);
//}

//window.removeSessionItem = function (key) {
//    window.sessionStorage.removeItem(key);
//}