// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/6.0.1/signalr.js"></script>
// 1. KẾT NỐI SERVER
var connection = new signalR.HubConnectionBuilder().withUrl("/musicHub").build();

connection.start().catch(err => console.error(err.toString()));

// 2. NHẬN LỆNH TỪ SERVER
connection.on("ReceiveMusicAction", function (action, url, time) {
    if (action === "play") {
        if (audio.src !== url) audio.src = url;
        audio.currentTime = time;
        audio.play();
        updatePlayIcon(true);
    } else if (action === "pause") {
        audio.pause();
        updatePlayIcon(false);
    }
});

// 3. GỬI LỆNH ĐI (Sửa lại hàm togglePlay cũ)
function togglePlay() {
    if (!audio.src) return;

    // Gửi tín hiệu lên server trước
    if (audio.paused) {
        audio.play();
        updatePlayIcon(true);
        connection.invoke("SendMusicAction", "play", audio.src, audio.currentTime);
    } else {
        audio.pause();
        updatePlayIcon(false);
        connection.invoke("SendMusicAction", "pause", audio.src, audio.currentTime);
    }
}
// Trong hàm loadSong, sau khi audio.play()
fetch(`/?handler=CountPlay&id=${song.id || currentIndex + 1}`, {
    method: 'POST',
    headers: { 'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value }
});