// ==========================================================================
// EventEase - Global Site Script
// ==========================================================================

document.addEventListener('DOMContentLoaded', function () {
    // 1. Initialize all Bootstrap Toasts with DOM cleanup on dismiss
    var toastElList = [].slice.call(document.querySelectorAll('.toast'));
    toastElList.forEach(function (toastEl) {
        var toast = new bootstrap.Toast(toastEl, { delay: 4500 });
        toastEl.addEventListener('hidden.bs.toast', function () {
            toast.dispose();
            toastEl.remove();
        });
        toast.show();
    });

    // 2. Initialize Tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.forEach(function (tooltipTriggerEl) {
        new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // 3. Countdown Timer for Public Event Page
    var countdownEl = document.getElementById('eventCountdown');
    if (countdownEl) {
        var targetDateStr = countdownEl.getAttribute('data-event-date');
        if (targetDateStr) {
            var targetDate = new Date(targetDateStr).getTime();

            if (window._eventCountdownInterval) {
                clearInterval(window._eventCountdownInterval);
            }

            window._eventCountdownInterval = setInterval(function () {
                // Check if element still exists in DOM to prevent memory leaks
                var el = document.getElementById('eventCountdown');
                if (!el) {
                    if (window._eventCountdownInterval) {
                        clearInterval(window._eventCountdownInterval);
                        window._eventCountdownInterval = null;
                    }
                    return;
                }

                var now = new Date().getTime();
                var distance = targetDate - now;

                if (distance < 0) {
                    clearInterval(window._eventCountdownInterval);
                    window._eventCountdownInterval = null;
                    var daysEl = document.getElementById('cdDays');
                    var hoursEl = document.getElementById('cdHours');
                    var minsEl = document.getElementById('cdMins');
                    var secsEl = document.getElementById('cdSecs');
                    if (daysEl) daysEl.innerText = "00";
                    if (hoursEl) hoursEl.innerText = "00";
                    if (minsEl) minsEl.innerText = "00";
                    if (secsEl) secsEl.innerText = "00";
                    return;
                }

                var days = Math.floor(distance / (1000 * 60 * 60 * 24));
                var hours = Math.floor((distance % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                var minutes = Math.floor((distance % (1000 * 60 * 60)) / (1000 * 60));
                var seconds = Math.floor((distance % (1000 * 60)) / 1000);

                var d = document.getElementById('cdDays');
                var h = document.getElementById('cdHours');
                var m = document.getElementById('cdMins');
                var s = document.getElementById('cdSecs');

                if (d) d.innerText = days < 10 ? "0" + days : days;
                if (h) h.innerText = hours < 10 ? "0" + hours : hours;
                if (m) m.innerText = minutes < 10 ? "0" + minutes : minutes;
                if (s) s.innerText = seconds < 10 ? "0" + seconds : seconds;
            }, 1000);
        }
    }
});

// Utility to copy shareable public event link to clipboard
function copyShareLink(inputOrUrlId) {
    var copyText = "";
    var el = document.getElementById(inputOrUrlId);
    if (el) {
        if (el.tagName === "INPUT") {
            copyText = el.value;
        } else {
            copyText = el.getAttribute('data-url') || el.innerText;
        }
    } else {
        copyText = inputOrUrlId;
    }

    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(copyText).then(function () {
            showToast("Success", "Public event link copied to clipboard!");
        });
    } else {
        // Fallback
        var textArea = document.createElement("textarea");
        textArea.value = copyText;
        textArea.style.position = "fixed";
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        try {
            document.execCommand('copy');
            showToast("Success", "Public event link copied to clipboard!");
        } catch (err) {
            console.error('Copy error', err);
        }
        document.body.removeChild(textArea);
    }
}

// Global dynamically generated toast alert
function showToast(title, message, isError) {
    var toastContainer = document.getElementById('globalToastContainer');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'globalToastContainer';
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }

    var toastHtml = `
        <div class="toast align-items-center ${isError ? 'text-bg-danger' : 'text-bg-dark'} border-0 shadow-lg" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body d-flex align-items-center gap-2">
                    <i class="bi ${isError ? 'bi-exclamation-triangle-fill' : 'bi-check-circle-fill'} fs-5 text-white"></i>
                    <div>
                        <strong>${title}:</strong> ${message}
                    </div>
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        </div>
    `;

    var div = document.createElement('div');
    div.innerHTML = toastHtml.trim();
    var newToast = div.firstChild;
    toastContainer.appendChild(newToast);

    var bsToast = new bootstrap.Toast(newToast, { delay: 4000 });
    newToast.addEventListener('hidden.bs.toast', function () {
        bsToast.dispose();
        newToast.remove();
    });
    bsToast.show();
}

// Clean up countdown timer on page unload or before navigation
window.addEventListener('beforeunload', function () {
    if (window._eventCountdownInterval) {
        clearInterval(window._eventCountdownInterval);
        window._eventCountdownInterval = null;
    }
});
