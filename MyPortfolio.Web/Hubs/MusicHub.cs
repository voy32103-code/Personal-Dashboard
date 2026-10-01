using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace MyPortfolio.Web.Hubs
{
    public class PlaybackState
    {
        public string Action { get; set; } = "pause";
        public string AudioUrl { get; set; } = string.Empty;
        public string? VideoUrl { get; set; }
        public double CurrentTime { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class MusicHub : Hub
    {
        private static readonly ConcurrentDictionary<string, PlaybackState> RoomStates = new();
        private readonly ILogger<MusicHub> _logger;

        public MusicHub(ILogger<MusicHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gửi lệnh phát nhạc toàn cục (Default fallback cho client cũ)
        /// </summary>
        public async Task SendMusicAction(string action, string audioUrl, double currentTime, string? videoUrl = null)
        {
            await SendRoomMusicAction("default", action, audioUrl, currentTime, videoUrl);
        }

        /// <summary>
        /// Tham gia vào phòng nghe nhạc riêng biệt (Session-based listening party)
        /// </summary>
        public async Task JoinRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) roomName = "default";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            // Gửi trạng thái hiện tại của phòng cho người vừa tham gia
            if (RoomStates.TryGetValue(roomName, out var state) && state.Action == "play")
            {
                // Tính toán thời gian thực tế đã trôi qua kể từ lần cập nhật cuối
                var elapsed = (DateTime.UtcNow - state.LastUpdated).TotalSeconds;
                var estimatedTime = state.CurrentTime + elapsed;

                await Clients.Caller.SendAsync("ReceiveMusicAction", state.Action, state.AudioUrl, estimatedTime, state.VideoUrl);
            }

            _logger.LogInformation("Connection {ConnectionId} joined room {Room}", Context.ConnectionId, roomName);
        }

        /// <summary>
        /// Rời khỏi phòng nghe nhạc
        /// </summary>
        public async Task LeaveRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName)) roomName = "default";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
            _logger.LogInformation("Connection {ConnectionId} left room {Room}", Context.ConnectionId, roomName);
        }

        /// <summary>
        /// Phát lệnh điều khiển âm nhạc / video trong phạm vi một phòng (Room)
        /// </summary>
        public async Task SendRoomMusicAction(string roomName, string action, string audioUrl, double currentTime, string? videoUrl = null)
        {
            if (string.IsNullOrWhiteSpace(roomName)) roomName = "default";

            // Cập nhật trạng thái phòng
            RoomStates[roomName] = new PlaybackState
            {
                Action = action,
                AudioUrl = audioUrl,
                VideoUrl = videoUrl,
                CurrentTime = currentTime,
                LastUpdated = DateTime.UtcNow
            };

            // Chỉ phát sóng tới các client khác trong cùng phòng
            await Clients.OthersInGroup(roomName).SendAsync("ReceiveMusicAction", action, audioUrl, currentTime, videoUrl);
        }
    }
}