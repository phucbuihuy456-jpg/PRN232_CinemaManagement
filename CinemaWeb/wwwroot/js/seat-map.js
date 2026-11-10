/**
 * seat-map.js - Sơ đồ ghế tương tác hoàn chỉnh (Task 20)
 * Chọn/bỏ chọn ghế, tính tiền tạm tính, tự động refresh trạng thái mỗi 10s, gọi API hold-seats và chuyển sang checkout.
 */

document.addEventListener('DOMContentLoaded', function () {
    const seatMapContainer = document.querySelector('.seat-map-wrapper');
    if (!seatMapContainer) return;

    const showtimeId = parseInt(seatMapContainer.getAttribute('data-showtime-id') || '1', 10);
    const seatButtons = document.querySelectorAll('.seat-btn');
    const selectedSeatsContainer = document.getElementById('selectedSeatsList');
    const selectedCountEl = document.getElementById('selectedCount');
    const totalPriceEl = document.getElementById('totalPrice');
    const btnContinue = document.getElementById('btnContinue');
    const countdownBadge = document.getElementById('countdownBadge');
    const countdownTimeEl = document.getElementById('countdownTime');
    const availableCountEl = document.getElementById('availableSeatsCount');

    const MAX_SEATS = 8;
    const selectedSeats = new Map(); // seatId -> { name, price, type, row, number }

    // 1. Lắng nghe sự kiện click chọn / bỏ chọn ghế
    seatButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const status = this.getAttribute('data-status');
            if (status !== 'Available') {
                return; // Không cho phép chọn ghế Đã bán, Đang giữ, hoặc Bảo trì
            }

            const seatId = parseInt(this.getAttribute('data-seat-id'), 10);
            const seatName = this.getAttribute('data-seat-name');
            const seatPrice = parseFloat(this.getAttribute('data-price') || '0');
            const seatType = this.getAttribute('data-type');

            if (selectedSeats.has(seatId)) {
                // Bỏ chọn ghế
                selectedSeats.delete(seatId);
                this.classList.remove('seat-selected');
            } else {
                // Kiểm tra giới hạn số ghế tối đa
                if (selectedSeats.size >= MAX_SEATS) {
                    alert(`Bạn chỉ có thể chọn tối đa ${MAX_SEATS} ghế trong một đơn đặt vé.`);
                    return;
                }
                // Thêm ghế vào danh sách chọn
                selectedSeats.set(seatId, {
                    id: seatId,
                    name: seatName,
                    price: seatPrice,
                    type: seatType
                });
                this.classList.add('seat-selected');
            }

            updateSummary();
        });
    });

    // 2. Cập nhật thanh tóm tắt ghế đã chọn & tổng tiền tạm tính
    function updateSummary() {
        if (!selectedSeatsContainer || !totalPriceEl || !selectedCountEl) return;

        selectedSeatsContainer.innerHTML = '';
        let total = 0;

        if (selectedSeats.size === 0) {
            selectedSeatsContainer.innerHTML = '<span class="text-secondary small fst-italic">Chưa có ghế nào được chọn</span>';
            selectedCountEl.textContent = '0';
            totalPriceEl.textContent = '0 đ';
            if (btnContinue) btnContinue.disabled = true;
            return;
        }

        selectedSeats.forEach((item) => {
            total += item.price;
            const badge = document.createElement('span');
            badge.className = 'selected-seat-badge';
            badge.innerHTML = `<i class="bi bi-check2"></i> ${item.name} (${item.type})`;
            selectedSeatsContainer.appendChild(badge);
        });

        selectedCountEl.textContent = selectedSeats.size.toString();
        totalPriceEl.textContent = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(total);

        if (btnContinue) btnContinue.disabled = false;
    }

    // 3. Xử lý khi nhấn nút "Tiếp tục" -> Gọi API Hold Seats (Task 19)
    if (btnContinue) {
        btnContinue.addEventListener('click', async function () {
            if (selectedSeats.size === 0) {
                alert('Vui lòng chọn ít nhất 1 ghế để tiếp tục.');
                return;
            }

            const seatIds = Array.from(selectedSeats.keys());
            const originalBtnHtml = btnContinue.innerHTML;

            btnContinue.disabled = true;
            btnContinue.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span> Đang giữ ghế...';

            try {
                // Lấy holdToken cũ nếu có
                const existingHoldToken = sessionStorage.getItem('cinema_hold_token') || null;

                const response = await fetch('/Booking/HoldSeats', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        showtimeId: showtimeId,
                        seatIds: seatIds,
                        holdToken: existingHoldToken
                    })
                });

                const data = await response.json();

                if (response.ok) {
                    // Giữ ghế thành công -> Lưu session và chuyển hướng sang trang Combo & Checkout (Task 21)
                    sessionStorage.setItem('cinema_hold_token', data.holdToken);
                    sessionStorage.setItem('cinema_hold_showtime_id', showtimeId.toString());
                    sessionStorage.setItem('cinema_hold_seats', JSON.stringify(seatIds));

                    // Khởi động đồng hồ đếm ngược 5 phút
                    if (typeof CountdownTimer !== 'undefined') {
                        CountdownTimer.start(data.remainingSeconds || 300);
                    }

                    // Chuyển hướng sang trang Chọn Combo & Checkout
                    window.location.href = `/Booking/Checkout?showtimeId=${showtimeId}&holdToken=${data.holdToken}&seatIds=${seatIds.join(',')}`;
                } else {
                    // Xung đột ghế (409 Conflict) hoặc lỗi khác
                    alert(data.message || 'Không thể giữ ghế. Ghế có thể đã được người khác chọn. Vui lòng thử lại!');
                    btnContinue.disabled = false;
                    btnContinue.innerHTML = originalBtnHtml;
                    // Tự động làm mới sơ đồ ghế để hiển thị trạng thái mới nhất
                    refreshSeatMap();
                }
            } catch (err) {
                console.error('Lỗi khi gọi API giữ ghế:', err);
                alert('Đã xảy ra lỗi kết nối khi giữ ghế. Vui lòng kiểm tra lại mạng hoặc máy chủ API.');
                btnContinue.disabled = false;
                btnContinue.innerHTML = originalBtnHtml;
            }
        });
    }

    // 4. Tự động refresh trạng thái ghế mỗi 10 giây (Task 20)
    let refreshInterval = setInterval(refreshSeatMap, 10000);

    async function refreshSeatMap() {
        try {
            const res = await fetch(`/Booking/GetSeatMapStatus/${showtimeId}`);
            if (!res.ok) return;

            const seatMapData = await res.json();
            if (!seatMapData || !seatMapData.seats) return;

            // Cập nhật số ghế trống còn lại
            if (availableCountEl && typeof seatMapData.availableCount === 'number') {
                availableCountEl.textContent = seatMapData.availableCount;
            }

            // Duyệt từng ghế và cập nhật trạng thái trên giao diện
            seatMapData.seats.forEach(seat => {
                const btn = document.querySelector(`.seat-btn[data-seat-id="${seat.seatId}"]`);
                if (!btn) return;

                const currentStatus = btn.getAttribute('data-status');
                const newStatus = seat.status;

                // Nếu người dùng đang chọn ghế này, giữ nguyên class selected
                const isSelectedByMe = selectedSeats.has(seat.seatId);

                // Nếu ghế này vừa bị người khác mua hoặc giữ mà mình đang chọn -> Cảnh báo
                if (isSelectedByMe && (newStatus === 'Sold' || newStatus === 'Held')) {
                    const myHoldToken = sessionStorage.getItem('cinema_hold_token');
                    // Chỉ loại bỏ nếu không phải do chính token này giữ
                    if (newStatus === 'Sold') {
                        selectedSeats.delete(seat.seatId);
                        btn.classList.remove('seat-selected');
                        alert(`Ghế ${seat.seatName} vừa được người khác thanh toán thành công. Ghế đã được tự động bỏ chọn.`);
                        updateSummary();
                    }
                }

                if (!isSelectedByMe) {
                    btn.setAttribute('data-status', newStatus);
                    btn.classList.remove('seat-sold', 'seat-held', 'seat-disabled');

                    if (newStatus === 'Sold') {
                        btn.classList.add('seat-sold');
                        btn.disabled = true;
                        btn.innerHTML = '<i class="bi bi-x-lg"></i>';
                        btn.title = `${seat.seatName} - Đã bán`;
                    } else if (newStatus === 'Held') {
                        btn.classList.add('seat-held');
                        btn.disabled = true;
                        btn.innerHTML = `<span>${seat.seatNumber}</span>`;
                        btn.title = `${seat.seatName} - Đang giữ chỗ`;
                    } else if (newStatus === 'Disabled') {
                        btn.classList.add('seat-disabled');
                        btn.disabled = true;
                        btn.innerHTML = '<i class="bi bi-slash-circle"></i>';
                        btn.title = `${seat.seatName} - Bảo trì`;
                    } else if (newStatus === 'Available') {
                        btn.disabled = false;
                        if (seat.seatTypeName === 'Couple') {
                            btn.innerHTML = `<span><i class="bi bi-heart-fill me-1 small"></i>${seat.seatName}</span>`;
                        } else {
                            btn.innerHTML = `<span>${seat.seatNumber}</span>`;
                        }
                        btn.title = `${seat.seatName} - ${seat.seatTypeName} - ${seat.finalPrice} đ (Available)`;
                    }
                }
            });
        } catch (e) {
            console.warn('Lỗi khi auto-refresh sơ đồ ghế:', e);
        }
    }

    // Dọn dẹp interval khi người dùng rời trang
    window.addEventListener('beforeunload', function () {
        if (refreshInterval) clearInterval(refreshInterval);
    });
});
