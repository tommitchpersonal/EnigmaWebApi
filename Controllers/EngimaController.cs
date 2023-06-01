using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;

namespace EnigmaWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EnigmaController : ControllerBase
{
    private ILogger<EnigmaController> _logger;
    private IEnigmaMachine _enigmaMachine;
    private DefaultSettings _settings;
    private IRandomSettingsGenerator _settingsGenerator;
    private ICredentialRepository _credentialRepository;
    public EnigmaController(IEnigmaMachine enigmaMachine, ILogger<EnigmaController> logger, IOptions<DefaultSettings> options, IRandomSettingsGenerator settingsGenerator, ICredentialRepository credRepository)
    {
        _enigmaMachine = enigmaMachine;
        _logger = logger;
        _settings = options.Value;
        _settingsGenerator = settingsGenerator;
        _credentialRepository = credRepository;
    
    }

    [HttpPost]
    [Route("encrypt")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EncryptionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Encrypt(EncryptionRequest request)
    {
        try
        {
            if (request.PlainText == null)
            {
                // Log Error
                return Problem("Request string was null or empty", statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (!request.IsValid())
            {
                // Log Error
                return Problem("Request string contained invalid characters", statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (_settings.UseRandomWheelSettings && _enigmaMachine.GetSettings() == null)
            {
                var settings = _settingsGenerator.GenerateRandomSettings();
                _enigmaMachine.UpdateSettings(settings);
            }
            

            var CipherText = _enigmaMachine.Encrypt(request.PlainText);

            var responseContent = new EncryptionResponse()
            {
                PlainText = request.PlainText,
                CipherText = CipherText
            };

            return Ok(responseContent);
        }
        catch (EncryptionException)
        {
            return Problem("Enigma machine is not configured. Call update endpoint to configure", statusCode: (int)HttpStatusCode.Conflict);         
        }
        catch (Exception ex)
        {
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);
        }

    }

    [HttpPut]
    [Route("update")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]

    public IActionResult UpdateSettings(UpdateSettingsRequest request)
    {
        try
        {
            if (request.NewSettings == null)
            {
                return Problem("New settings were null", statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (!request.IsValid())
            {
                return Problem("New settings were invalid", statusCode: (int)HttpStatusCode.BadRequest);
            }

            _enigmaMachine.UpdateSettings(request.NewSettings);

            var responseContent = new UpdateSettingsResponse()
            {
                NewSettings = request.NewSettings
            };

            return Ok(responseContent);
        }
        catch (Exception ex)
        {
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpGet]
    [Route("settings")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult GetSettings()
    {
        var settings = _enigmaMachine.GetSettings();

        if (settings == null)
        {
            return Problem("Cannot return settings as they have not been set", statusCode: (int)HttpStatusCode.Conflict);
        }

        var response = new UpdateSettingsResponse()
        {
            NewSettings = settings
        };

        return Ok(response);
    }

    [HttpPost]
    [Route("reset")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult ResetEnigmaMachine()
    {
        var settings = _enigmaMachine.GetSettings();

        if (settings == null)
        {
            return Problem("Settings must be set before they can be reset", statusCode: (int)HttpStatusCode.Conflict);
        }

        _enigmaMachine.Reset();

        return Accepted();
    }
}


