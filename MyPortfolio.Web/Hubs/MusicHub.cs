using Microsoft.AspNetCore.SignalR;

namespace MyPortfolio.Web.Hubs
{
    public class MusicHub : Hub
    {
        // Gửi lệnh phát nhạc cho tất cả mọi người
        public async Task SendMusicAction(string action, string audioUrl, double currentTime)
        {
            await Clients.Others.SendAsync("ReceiveMusicAction", action, audioUrl, currentTime);
        }
    }
}