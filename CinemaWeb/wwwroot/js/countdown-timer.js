/**
 * countdown-timer.js - Đồng hồ đếm ngược 5 phút giữ ghế tạm (Task 20 & Task 34)
 * Quản lý thời gian giữ ghế, hiển thị đếm ngược mm:ss, cảnh báo sắp hết giờ (<60s) và tự động giải phóng ghế.
 */

const CountdownTimer = (function () {
    let timerInterval = null;
    let remainingSeconds = 300; // 5 phút = 300s
    let onTickCallback = null;
    let onExpiredCallback = null;
    let hasShownWarning60s = false;

    function formatTime(seconds) {
        if (seconds < 0) seconds = 0;
        const mins = Math.floor(seconds / 60);
        const secs = seconds % 60;
        return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
    }

    function start(seconds, onTick, onExpired) {
        stop();

        remainingSeconds = seconds && seconds > 0 ? seconds : 300;
        onTickCallback = onTick;
        onExpiredCallback = onExpired;
        hasShownWarning60s = remainingSeconds <= 60;

        // Lưu thời điểm hết hạn vào sessionStorage để duy trì khi chuyển trang
        const expiresAtTimestamp = Date.now() + (remainingSeconds * 1000);
        sessionStorage.setItem('cinema_hold_expires_at', expiresAtTimestamp.toString());

        tick();
        timerInterval = setInterval(tick, 1000);
    }

    function initFromSession(onTick, onExpired) {
        const storedExpiresAt = sessionStorage.getItem('cinema_hold_expires_at');
        if (!storedExpiresAt) return false;

        const expiresAt = parseInt(storedExpiresAt, 10);
        const diffSeconds = Math.floor((expiresAt - Date.now()) / 1000);

        if (diffSeconds <= 0) {
            clearSession();
            if (typeof onExpired === 'function') onExpired();
            return false;
        }

        start(diffSeconds, onTick, onExpired);
        return true;
    }

    function tick() {
        if (typeof onTickCallback === 'function') {
            onTickCallback(remainingSeconds, formatTime(remainingSeconds));
        }

        // Task 34: Cảnh báo khi thời gian còn dưới 60 giây
        if (remainingSeconds <= 60 && !hasShownWarning60s) {
            hasShownWarning60s = true;
            showExpiringSoonToast();
        }

        // Cập nhật class cảnh báo nhấp nháy trên các phần tử hiển thị đồng hồ
        const timerBadges = document.querySelectorAll('#checkoutTimer, #countdownBadge');
        timerBadges.forEach(el => {
            if (remainingSeconds <= 60) {
                el.classList.add('timer-warning-pulse');
            } else {
                el.classList.remove('timer-warning-pulse');
            }
        });

        if (remainingSeconds <= 0) {
            stop();
            clearSession();
            if (typeof onExpiredCallback === 'function') {
                onExpiredCallback();
            } else {
                handleDefaultExpiration();
            }
            return;
        }

        remainingSeconds--;
    }

    function showExpiringSoonToast() {
        let toastContainer = document.getElementById('cinemaToastContainer');
        if (!toastContainer) {
            toastContainer = document.createElement('div');
            toastContainer.id = 'cinemaToastContainer';
            toastContainer.style.position = 'fixed';
            toastContainer.style.top = '20px';
            toastContainer.style.right = '20px';
            toastContainer.style.zIndex = '9999';
            document.body.appendChild(toastContainer);
        }

        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-danger alert-dismissible fade show shadow-lg border-2 border-danger';
        alertDiv.style.maxWidth = '360px';
        alertDiv.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="bi bi-exclamation-octagon-fill fs-3 text-warning me-2"></i>
                <div>
                    <strong class="d-block">Sắp hết thời gian giữ ghế!</strong>
                    <span class="small">Bạn chỉ còn dưới 1 phút để hoàn tất đơn đặt vé.</span>
                </div>
            </div>
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        toastContainer.appendChild(alertDiv);

        setTimeout(() => {
            if (alertDiv && alertDiv.parentNode) {
                alertDiv.classList.remove('show');
                setTimeout(() => alertDiv.remove(), 300);
            }
        }, 8000);
    }

    function stop() {
        if (timerInterval) {
            clearInterval(timerInterval);
            timerInterval = null;
        }
    }

    function clearSession() {
        sessionStorage.removeItem('cinema_hold_expires_at');
        sessionStorage.removeItem('cinema_hold_token');
        sessionStorage.removeItem('cinema_hold_showtime_id');
        sessionStorage.removeItem('cinema_hold_seats');
    }

    function handleDefaultExpiration() {
        alert('Đã hết thời gian giữ ghế 5 phút! Ghế của bạn đã được giải phóng tự động. Vui lòng chọn lại ghế.');
        window.location.reload();
    }

    return {
        start: start,
        stop: stop,
        initFromSession: initFromSession,
        formatTime: formatTime,
        clearSession: clearSession,
        getRemainingSeconds: function () { return remainingSeconds; }
    };
})();
