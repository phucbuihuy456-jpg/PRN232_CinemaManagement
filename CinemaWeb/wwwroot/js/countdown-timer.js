/**
 * countdown-timer.js - Đồng hồ đếm ngược 5 phút giữ ghế tạm (Task 20)
 * Quản lý thời gian giữ ghế, hiển thị đếm ngược mm:ss, cảnh báo hết hạn và tự động giải phóng ghế.
 */

const CountdownTimer = (function () {
    let timerInterval = null;
    let remainingSeconds = 300; // 5 phút = 300s
    let onTickCallback = null;
    let onExpiredCallback = null;

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
