$(document).ready(function(){
    // Check if the 'cookieConsent' cookie is set
    if(!getCookie('cookieConsent')) {
        $('#cookieConsentBanner').fadeIn();
    }

    $('#cookieConsentAccept').click(function(){
        setCookie('cookieConsent', 'accepted', 365); // Set the cookie for 1 year
        $('#cookieConsentBanner').fadeOut();
    });
});

// Function to get a cookie value
function getCookie(name) {
    var value = "; " + document.cookie;
    var parts = value.split("; " + name + "=");
    if (parts.length == 2) return parts.pop().split(";").shift();
}

// Function to set a cookie
function setCookie(name, value, days) {
    var expires = "";
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days*24*60*60*1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
}
