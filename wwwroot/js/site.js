// Form validation
(function () {
    'use strict'

    // Fetch all forms that need validation
    var forms = document.querySelectorAll('.needs-validation')

    // Loop over them and prevent submission
    Array.prototype.slice.call(forms)
        .forEach(function (form) {
            form.addEventListener('submit', function (event) {
                if (!form.checkValidity()) {
                    event.preventDefault()
                    event.stopPropagation()
                }

                form.classList.add('was-validated')
            }, false)
        })
})()

// Password visibility toggle
function togglePasswordVisibility() {
    const passwordInput = document.querySelector('input[type="password"]');
    const toggleButton = document.querySelector('.toggle-password');
    
    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleButton.textContent = 'Hide';
    } else {
        passwordInput.type = 'password';
        toggleButton.textContent = 'Show';
    }
}

// Session timeout handling
let sessionTimeout;
const warningTime = 5 * 60 * 1000; // 5 minutes
const logoutTime = 30 * 60 * 1000; // 30 minutes

function resetSessionTimeout() {
    clearTimeout(sessionTimeout);
    sessionTimeout = setTimeout(showTimeoutWarning, warningTime);
}

function showTimeoutWarning() {
    // You can implement a modal or notification here
    console.log('Session about to expire');
    setTimeout(logout, logoutTime - warningTime);
}

function logout() {
    window.location.href = '/Account/Logout';
}

// Event listeners for session timeout
document.addEventListener('mousemove', resetSessionTimeout);
document.addEventListener('keypress', resetSessionTimeout);
document.addEventListener('click', resetSessionTimeout);
document.addEventListener('scroll', resetSessionTimeout);

// Initialize session timeout
resetSessionTimeout();
