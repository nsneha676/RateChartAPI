using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

[Route("api/[controller]")]
[ApiController]
public class RateChartController : ControllerBase
{
    private readonly IRateChartService _rateChartService;
    private readonly RateChartDbContext _context;
    private readonly ILogger<RateChartController> _logger;

    public RateChartController(IRateChartService rateChartService, RateChartDbContext context, ILogger<RateChartController> logger)
    {
        _rateChartService = rateChartService ?? throw new ArgumentNullException(nameof(rateChartService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadRateChart(IFormFile file, int clientId)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("No file uploaded.");
            return BadRequest("No file uploaded.");
        }

        // Validate file extension
        var fileExtension = Path.GetExtension(file.FileName).ToLower();
        if (fileExtension != ".csv")
        {
            _logger.LogWarning("Invalid file format.");
            return BadRequest("Invalid file format. Please upload a CSV file.");
        }

        try
        {
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                stream.Position = 0; // Rewind the stream to the beginning
                _logger.LogInformation("File upload successful, calling ImportRateChart.");
                _rateChartService.ImportRateChart(stream, clientId);
            }

            _logger.LogInformation("Rate chart imported successfully.");
            return Ok("Rate chart imported successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error uploading rate chart: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error: " + ex.Message);
        }
    }

    [HttpGet("testdb")]
    public IActionResult TestDatabaseConnection()
    {
        try
        {
            var canConnect = _context.Database.CanConnect();
            return Ok(canConnect ? "Database connection successful" : "Database connection failed");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: {ex.Message}");
            return StatusCode(StatusCodes.Status500InternalServerError, "Internal server error: " + ex.Message);
        }
    }

    [HttpGet("getrate")]

    public IActionResult GetRate([FromQuery] int clientId, [FromQuery] decimal fat, [FromQuery] decimal snf)
    {
        _logger.LogInformation($"GetRate called with clientId: {clientId}, fat: {fat}, snf: {snf}");
        var rate = _rateChartService.GetRate(clientId, fat, snf);
        if (rate == null)
        {
            _logger.LogWarning("Rate not found.");
            return NotFound("Rate not found.");
        }

        return Ok(rate);
    }

    
    
}
