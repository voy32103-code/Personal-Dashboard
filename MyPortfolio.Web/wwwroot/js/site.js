// File: wwwroot/js/site.js

document.addEventListener("DOMContentLoaded", function () {
    console.log("Site.js: Đã sẵn sàng!");

    // --- TÍNH NĂNG KÉO THẢ (SORTABLE) ---
    // Tìm thẻ chứa danh sách bài hát (Bạn nhớ đặt id="song-list" cho thẻ div bao quanh các bài hát ở file Index.cshtml nhé)
    var songList = document.getElementById('song-list');

    if (songList && typeof Sortable !== 'undefined') {
        Sortable.create(songList, {
            animation: 150, // Hiệu ứng mượt 150ms
            ghostClass: 'sortable-ghost', // Class CSS khi đang kéo
            onEnd: function (evt) {
                console.log("Đã kéo bài hát từ vị trí " + evt.oldIndex + " sang " + evt.newIndex);
            }
        });
        console.log("--> Đã kích hoạt tính năng Kéo Thả!");
    }
});