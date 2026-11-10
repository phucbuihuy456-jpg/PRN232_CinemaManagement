param(
    [string]$BaseUrl = "http://localhost:5027",
    [int]$ShowtimeId = 1,
    [int]$SeatId = 4
)

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host " TASK 33: KIEM THU XUNG DOT & CONCURRENCY DAT GHE REAL-TIME" -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Muc tieu: 2 nguoi dung chon cung ghe $SeatId tai cung 1 thoi diem." -ForegroundColor Gray
Write-Host "API URL: $BaseUrl" -ForegroundColor Gray
Write-Host ""

# 1. Kiem tra ket noi API
try {
    $null = Invoke-RestMethod -Uri "$BaseUrl/api/showtimes/$ShowtimeId/seat-map" -Method Get -TimeoutSec 5
    Write-Host "[OK] Ket noi API thanh cong!" -ForegroundColor Green
} catch {
    Write-Host "[LOI] Khong the ket noi toi $BaseUrl" -ForegroundColor Red
    exit 1
}

# 2. Lam sach du lieu cu cua ghe nay
try {
    sqlcmd -S ".\SQLEXPRESS" -U sa -P 123 -Q "USE CinemaSystemDB; DELETE FROM SeatHolds WHERE ShowtimeId = $ShowtimeId AND SeatId = $SeatId;" | Out-Null
    Write-Host "[OK] Da reset trang thai ghe $SeatId ve trong." -ForegroundColor Gray
} catch {
    Write-Host "[CANH BAO] Khong the reset DB bang sqlcmd." -ForegroundColor Yellow
}

$worker = {
    param($url, $showtime, $seat, $userId)

    $json = "{`"showtimeId`": $showtime, `"seatIds`": [$seat]}"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $res = Invoke-RestMethod -Uri "$url/api/bookings/hold-seats" -Method Post -Body $json -ContentType "application/json" -TimeoutSec 10
        $sw.Stop()
        return [PSCustomObject]@{
            User = "User $userId"
            Status = 200
            Message = $res.message
            DurationMs = $sw.ElapsedMilliseconds
        }
    } catch {
        $sw.Stop()
        $status = 500
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        }
        $msg = $_.Exception.Message
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $err = $reader.ReadToEnd() | ConvertFrom-Json
            $msg = $err.message
        } catch {}

        return [PSCustomObject]@{
            User = "User $userId"
            Status = $status
            Message = $msg
            DurationMs = $sw.ElapsedMilliseconds
        }
    }
}

Write-Host "--> Phat dong thoi 2 requests giu ghe tai cung 1 thoi diem..." -ForegroundColor Yellow

$j1 = Start-Job -ScriptBlock $worker -ArgumentList $BaseUrl, $ShowtimeId, $SeatId, 101
$j2 = Start-Job -ScriptBlock $worker -ArgumentList $BaseUrl, $ShowtimeId, $SeatId, 102

$null = Wait-Job $j1, $j2
$r1 = Receive-Job $j1
$r2 = Receive-Job $j2
Remove-Job $j1, $j2

Write-Host ""
Write-Host "----------------- KET QUA PHAN HOI DONG THOI -----------------" -ForegroundColor Cyan
Write-Host "Luot 1 ($($r1.User)):" -ForegroundColor White
Write-Host "   - Status      : $($r1.Status)" -ForegroundColor $(if ($r1.Status -eq 200) { "Green" } else { "Red" })
Write-Host "   - Thong bao   : $($r1.Message)"
Write-Host "   - Thoi gian   : $($r1.DurationMs) ms"

Write-Host "Luot 2 ($($r2.User)):" -ForegroundColor White
Write-Host "   - Status      : $($r2.Status)" -ForegroundColor $(if ($r2.Status -eq 200) { "Green" } else { "Red" })
Write-Host "   - Thong bao   : $($r2.Message)"
Write-Host "   - Thoi gian   : $($r2.DurationMs) ms"
Write-Host "-----------------------------------------------------------------" -ForegroundColor Cyan

$successCount = 0
$conflictCount = 0
if ($r1.Status -eq 200) { $successCount++ }
if ($r2.Status -eq 200) { $successCount++ }
if ($r1.Status -eq 409) { $conflictCount++ }
if ($r2.Status -eq 409) { $conflictCount++ }

# Kiem tra CSDL
$dbCount = 0
try {
    $dbOutput = sqlcmd -S ".\SQLEXPRESS" -U sa -P 123 -Q "SET NOCOUNT ON; USE CinemaSystemDB; SELECT COUNT(*) FROM SeatHolds WHERE ShowtimeId = $ShowtimeId AND SeatId = $SeatId;" -h -1
    $dbCount = [int]($dbOutput | Where-Object { $_.Trim() -match '^\d+$' } | Select-Object -First 1)
} catch {}

Write-Host ""
Write-Host "================== DANH GIA TINH TOAN VEN ==================" -ForegroundColor Cyan
if ($successCount -eq 1 -and $conflictCount -eq 1) {
    Write-Host "[DAT] Concurrency & Race condition xu ly chinh xac:" -ForegroundColor Green
    Write-Host "      - Dung 1 nguoi giu ghe thanh cong (200 OK)." -ForegroundColor Green
    Write-Host "      - Dung 1 nguoi bi tu choi do xung dot (409 Conflict)." -ForegroundColor Green
} else {
    Write-Host "[KHONG DAT] Xuat hien xung dot hoac loi: Success=$successCount, Conflict=$conflictCount" -ForegroundColor Red
}

if ($dbCount -eq 1) {
    Write-Host "[DAT] Tinh toan ven CSDL: Bang SeatHolds chi luu DUY NHAT 1 ban ghi con han." -ForegroundColor Green
} else {
    Write-Host "[CANH BAO] So ban ghi SeatHolds trong DB: $dbCount" -ForegroundColor Yellow
}
Write-Host "============================================================" -ForegroundColor Cyan
