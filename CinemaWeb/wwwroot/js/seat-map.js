document.addEventListener('DOMContentLoaded', function () {
    const seatButtons = document.querySelectorAll('.seat-btn');
    const selectedSeatsContainer = document.getElementById('selectedSeatsList');
    const selectedCountEl = document.getElementById('selectedCount');
    const totalPriceEl = document.getElementById('totalPrice');
    const btnContinue = document.getElementById('btnContinue');

    const MAX_SEATS = 8;
    const selectedSeats = new Map(); // seatId -> { name, price, type }

    seatButtons.forEach(btn => {
        btn.addEventListener('click', function () {
            const status = this.getAttribute('data-status');
            if (status !== 'Available') {
                return; // Không cho phép click nếu đã bán, đang giữ hoặc bảo trì
            }

            const seatId = this.getAttribute('data-seat-id');
            const seatName = this.getAttribute('data-seat-name');
            const seatPrice = parseFloat(this.getAttribute('data-price') || '0');
            const seatType = this.getAttribute('data-type');

            if (selectedSeats.has(seatId)) {
                // Hủy chọn
                selectedSeats.delete(seatId);
                this.classList.remove('seat-selected');
            } else {
                // Kiểm tra giới hạn số ghế tối đa
                if (selectedSeats.size >= MAX_SEATS) {
                    alert(`Bạn chỉ có thể chọn tối đa ${MAX_SEATS} ghế trong một đơn đặt.`);
                    return;
                }
                // Chọn ghế
                selectedSeats.set(seatId, {
                    name: seatName,
                    price: seatPrice,
                    type: seatType
                });
                this.classList.add('seat-selected');
            }

            updateSummary();
        });
    });

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

        selectedSeats.forEach((item, id) => {
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
});
