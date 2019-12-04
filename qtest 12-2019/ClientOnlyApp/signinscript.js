
var username = null;
function signinpage_signin() {
    var v = document.getElementById("username_box").value.toLowerCase();
    var regex = /^[0-9a-zA-Z]+$/;
    if (v.length < 3 || !v.match(regex)) {
        document.getElementById("signin_page_warning").innerText = "Please enter a username that is at least 3 letters long and uses only letters or numbers.";
        return;
    }
    username = v;
    document.cookie = "username=" + v + "; expires=Fri, 31 Dec 9999 23:59:59 GMT";
    document.getElementById("home_screen_page").style = "";
    document.body.removeChild(document.getElementById("sign_in_page"));
}

function getCookie(name) {
    var value = "; " + document.cookie;
    var parts = value.split("; " + name + "=");
    if (parts.length == 2) return parts.pop().split(";").shift();
}

//Search for cookie
/*if (getCookie("username") != undefined && getCookie("username").length >= 3) {
    username = getCookie("username");
    document.getElementById("home_screen_page").style = "";
    document.body.removeChild(document.getElementById("sign_in_page"));
}
*/