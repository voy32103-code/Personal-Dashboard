namespace MyPortfolio.Core.Entities
{
    public class PortfolioItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;       // Tên dự án
        public string Description { get; set; } = string.Empty; // Mô tả ngắn
        public string ImageUrl { get; set; } = string.Empty;    // Link ảnh demo
        public string ProjectUrl { get; set; } = string.Empty;  // Link Github/Web
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}