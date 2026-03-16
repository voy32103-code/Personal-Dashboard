using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MyPortfolio.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SystemMetricsController : ControllerBase
{
    // API endpoint mẫu: /api/systemmetrics
    [HttpGet]
    public IActionResult GetSystemInfo()
    {
        var process = Process.GetCurrentProcess();
        
        return Ok(new
        {
            Status = "Healthy",
            Uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime(),
            MemoryUsageMB = process.WorkingSet64 / (1024 * 1024),
            ThreadCount = process.Threads.Count,
            Timestamp = DateTime.UtcNow
        });
    }

    // API endpoint mẫu test Fluent Validation: /api/systemmetrics/contact
    [HttpPost("contact")]
    public IActionResult SubmitContact([FromBody] DTOs.ContactFormDto contactForm)
    {
        // Nhờ FluentValidation, Model State sẽ tự động được kiểm tra trước khi vào action
        // Nếu input không hợp lệ, ASP.NET Core sẽ tự động trả về 400 Bad Request kèm theo error detail (FluentValidation.AspNetCore).
        
        return Ok(new
        {
            Message = "Nhận thông tin thành công!",
            Data = contactForm
        });
    }
}
