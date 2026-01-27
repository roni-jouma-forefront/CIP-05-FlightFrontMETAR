using System.Text.RegularExpressions;
using Flightfront.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Flightfront.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class MetarController : ControllerBase
{
    private readonly IMetarService _metarService;
    private readonly ILogger<MetarController> _logger;

    public MetarController(IMetarService metarService, ILogger<MetarController> logger)
    {
        _metarService = metarService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves METAR data for a specific airport by ICAO code.
    /// </summary>
    /// <param name="icaoCode">The 4-character ICAO airport code (e.g., KJFK, EGLL).</param>
    /// <returns>The parsed METAR data for the specified airport.</returns>
    /// <response code="200">Returns the METAR data</response>
    /// <response code="400">Invalid ICAO code format</response>
    /// <response code="404">No METAR data found for the specified airport</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{icaoCode}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMetar(string icaoCode)
    {
        if (string.IsNullOrWhiteSpace(icaoCode))
        {
            return BadRequest(
                new ErrorResponse
                {
                    Error = "ICAO code is required.",
                    ErrorCode = "VALIDATION_ERROR",
                    Details = "The ICAO code parameter cannot be empty.",
                }
            );
        }

        if (icaoCode.Length != 4)
        {
            return BadRequest(
                new ErrorResponse
                {
                    Error = "Invalid ICAO code length.",
                    ErrorCode = "VALIDATION_ERROR",
                    Details = "ICAO codes must be exactly 4 characters long.",
                }
            );
        }

        if (!Regex.IsMatch(icaoCode, "^[A-Za-z]{4}$"))
        {
            return BadRequest(
                new ErrorResponse
                {
                    Error = "Invalid ICAO code format.",
                    ErrorCode = "VALIDATION_ERROR",
                    Details = "ICAO codes must contain only letters (A-Z).",
                }
            );
        }

        try
        {
            var metar = await _metarService.GetMetarByIcaoAsync(icaoCode);

            if (metar == null)
            {
                return NotFound(
                    new ErrorResponse
                    {
                        Error = $"No METAR data found for {icaoCode.ToUpper()}.",
                        ErrorCode = "NOT_FOUND",
                        Details =
                            "The airport may not have current METAR data available, or the ICAO code may be incorrect.",
                    }
                );
            }

            return Ok(metar);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error fetching METAR for {IcaoCode}", icaoCode);
            return StatusCode(
                503,
                new ErrorResponse
                {
                    Error = "Service temporarily unavailable.",
                    ErrorCode = "NETWORK_ERROR",
                    Details =
                        "Unable to connect to the METAR data provider. Please try again later.",
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching METAR for {IcaoCode}", icaoCode);
            return StatusCode(
                500,
                new ErrorResponse
                {
                    Error = "An error occurred while fetching METAR data.",
                    ErrorCode = "INTERNAL_ERROR",
                    Details = "An unexpected error occurred. Please try again later.",
                }
            );
        }
    }

    /// <summary>
    /// Validates an ICAO airport code format.
    /// </summary>
    /// <param name="icaoCode">The ICAO code to validate.</param>
    /// <returns>Validation result indicating whether the code is valid.</returns>
    /// <response code="200">Returns validation result</response>
    [HttpGet("validate/{icaoCode}")]
    [ProducesResponseType(typeof(IcaoValidationResponse), StatusCodes.Status200OK)]
    public IActionResult ValidateIcao(string icaoCode)
    {
        var response = new IcaoValidationResponse
        {
            IcaoCode = icaoCode?.ToUpper() ?? string.Empty,
            IsValid = false,
            Errors = new List<string>(),
        };

        if (string.IsNullOrWhiteSpace(icaoCode))
        {
            response.Errors.Add("ICAO code cannot be empty.");
            return Ok(response);
        }

        if (icaoCode.Length != 4)
        {
            response.Errors.Add(
                $"ICAO code must be exactly 4 characters. Provided: {icaoCode.Length} characters."
            );
        }

        if (!Regex.IsMatch(icaoCode, "^[A-Za-z]{4}$"))
        {
            response.Errors.Add("ICAO code must contain only letters (A-Z).");
        }

        response.IsValid = response.Errors.Count == 0;

        return Ok(response);
    }

    /// <summary>
    /// Parses a raw METAR string into structured data.
    /// </summary>
    /// <param name="request">The request containing the raw METAR string.</param>
    /// <returns>The parsed METAR data.</returns>
    /// <response code="200">Returns the parsed METAR data</response>
    /// <response code="400">Invalid or empty METAR string</response>
    [HttpPost("parse")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public IActionResult ParseMetar([FromBody] MetarParseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.MetarString))
        {
            return BadRequest(
                new ErrorResponse
                {
                    Error = "METAR string cannot be empty.",
                    ErrorCode = "VALIDATION_ERROR",
                    Details = "Please provide a valid METAR string to parse.",
                }
            );
        }

        try
        {
            var metar = _metarService.ParseMetarString(request.MetarString);

            if (metar == null)
            {
                return BadRequest(
                    new ErrorResponse
                    {
                        Error = "Invalid METAR string.",
                        ErrorCode = "PARSE_ERROR",
                        Details =
                            "The provided METAR string could not be parsed. Please check the format.",
                    }
                );
            }

            return Ok(metar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing METAR string");
            return BadRequest(
                new ErrorResponse
                {
                    Error = "Failed to parse METAR string.",
                    ErrorCode = "PARSE_ERROR",
                    Details = ex.Message,
                }
            );
        }
    }
}

/// <summary>
/// Request model for parsing a raw METAR string.
/// </summary>
public class MetarParseRequest
{
    /// <summary>
    /// The raw METAR string to parse.
    /// </summary>
    public string MetarString { get; set; } = string.Empty;
}

/// <summary>
/// Error response model.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// The error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// The error code.
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Additional details about the error.
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Response model for ICAO code validation.
/// </summary>
public class IcaoValidationResponse
{
    /// <summary>
    /// The ICAO code that was validated.
    /// </summary>
    public string IcaoCode { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the ICAO code is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors, if any.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}
