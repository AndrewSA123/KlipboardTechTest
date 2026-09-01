using FaultTriage.Api.Models;
using FaultTriage.Core;
using FaultTriage.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FaultTriage.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FaultsController : ControllerBase
{
    private readonly IFaultAnalyser _faultAnalyser;
    private readonly ILogger<FaultsController> _logger;

    public FaultsController(IFaultAnalyser faultAnalyser, ILogger<FaultsController> logger)
    {
        _faultAnalyser = faultAnalyser;
        _logger = logger;
    }

    [HttpPost("analyze")]
    [ProducesResponseType(typeof(FaultAnalysis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<FaultAnalysis>> Analyze(
        [FromBody] FaultAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return BadRequest("Fault description cannot be empty.");
        }

        try
        {
            var analysis = await _faultAnalyser.AnalyzeAsync(request.Description, cancellationToken);
            return Ok(analysis);
        }
        catch (FaultAnalyserException ex)
        {
            _logger.LogError(ex, "Fault analysis failed for description: {Description}", request.Description);
            return StatusCode(StatusCodes.Status502BadGateway, "Unable to analyze fault description at this time. Please try again.");
        }
    }
}