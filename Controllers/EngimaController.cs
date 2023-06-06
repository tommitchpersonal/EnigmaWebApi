using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;

namespace EnigmaWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EnigmaController : ControllerBase
{
    private ILogger<EnigmaController> _logger;
    private IEnigmaWrapperFactory _enigmaWrapperFactory;
    private DefaultSettings _settings;
    private IRandomSettingsGenerator _settingsGenerator;
    private ICredentialRepository _credentialRepository;
    private IWebSocketService _webSocketService;
    private static Dictionary<string, IEnigmaWrapper> _enigmaWrapperDictionary = new();
    public EnigmaController(IEnigmaWrapperFactory enigmaWrapperFactory, ILogger<EnigmaController> logger, IOptions<DefaultSettings> options, IRandomSettingsGenerator settingsGenerator, ICredentialRepository credRepository, IWebSocketService webSocketService)
    {
        _enigmaWrapperFactory = enigmaWrapperFactory;
        _logger = logger;
        _settings = options.Value;
        _settingsGenerator = settingsGenerator;
        _credentialRepository = credRepository;
        _webSocketService = webSocketService;
    }

    [HttpPost]
    [Route("add")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AddEnigmaResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult AddEnigmaMachine(AddEnigmaMachineRequest request)
    {
        try
        {
            var headers = HttpContext.Request.Headers;

            var loggedInUser = User.Identities?.FirstOrDefault()?.Claims?.FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(loggedInUser))
            {
                return Problem("You do not have access to this resource", statusCode: (int)HttpStatusCode.Forbidden);
            }

            if (request == null || !request.IsValid())
            {
                return Problem("Bad Request", statusCode: (int)HttpStatusCode.BadRequest);
            }

            var guid = Guid.NewGuid().ToString();

            IEnigmaWrapper enigmaWrapper;
            EnigmaSettings settings;

            if (request.UseRandomWheels)
            {
                settings = _settingsGenerator.GenerateRandomSettings(request.NumberOfWheels);
                enigmaWrapper = _enigmaWrapperFactory.CreateEnigmaWrapper(loggedInUser, settings);
            }
            else
            {
                settings = request.EnigmaMachineSettings!;
                enigmaWrapper = _enigmaWrapperFactory.CreateEnigmaWrapper(loggedInUser, settings);
            }

            _enigmaWrapperDictionary.Add(guid, enigmaWrapper);

            var response = new AddEnigmaResponse(guid, settings);

            return Ok(response);
        }
        catch(Exception ex)
        {
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);

        }
    }

    [HttpPost]
    [Route("encrypt/{id?}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(EncryptionResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult Encrypt(string id, EncryptionRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.PlainText))
            {
                // Log Error
                return Problem("Request string was null or empty", statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (!request.IsValid())
            {
                // Log Error
                return Problem("Request string contained invalid characters", statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
            {
                return Problem("Enigma machine ID not found", statusCode: (int)HttpStatusCode.NotFound);
            }

            if (!UserHasAccessToResource(enigmaWrapper))
            {
                return Problem("You do not have access to this resource", statusCode: (int)HttpStatusCode.Forbidden);
            }

            var CipherText = enigmaWrapper.EnigmaMachine.Encrypt(request.PlainText);

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
    [Route("update/{id?}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult UpdateSettings(string id, UpdateSettingsRequest request)
    {
        try
        {
            if (request == null || !request.IsValid())
            {
                return Problem("Bad Request", statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
            {
                return Problem("Enigma machine ID not found", statusCode: (int)HttpStatusCode.NotFound);
            }

            if (!UserHasAccessToResource(enigmaWrapper))
            {
                return Problem("You do not have access to this resource", statusCode: (int)HttpStatusCode.Forbidden);
            }

            var newSettings = request.UseRandomWheels ? _settingsGenerator.GenerateRandomSettings(request.NumberOfWheels) : request.EnigmaMachineSettings;

            if (newSettings == null)
            {
                return Problem("Bad Request", statusCode: (int)HttpStatusCode.BadRequest);
            }

            enigmaWrapper.EnigmaMachine.UpdateSettings(newSettings);

            var responseContent = new UpdateSettingsResponse()
            {
                NewSettings = newSettings
            };

            return Ok(responseContent);
        }
        catch (Exception ex)
        {
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpGet]
    [Route("settings/{id?}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetSettings(string id)
    {
        if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
        {
            return Problem("Enigma machine ID not found", statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!UserHasAccessToResource(enigmaWrapper))
        {
            return Problem("You do not have access to this resource", statusCode: (int)HttpStatusCode.Forbidden);
        }

        var settings = enigmaWrapper.EnigmaMachine.GetSettings();

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
    [Route("reset/{id?}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult ResetEnigmaMachine(string id)
    {
        if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
        {
            return Problem("Enigma machine ID not found", statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!UserHasAccessToResource(enigmaWrapper))
        {
            return Problem("You do not have access to this resource", statusCode: (int)HttpStatusCode.Forbidden);
        }

        enigmaWrapper.EnigmaMachine.Reset();
        return NoContent();
    }

    [HttpDelete]
    [Route("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteEnigmaMachine(string id)
    {
        if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
        {
            return Problem("Enigma machine ID not found", statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!UserHasAccessToResource(enigmaWrapper))
        {
            return Problem("You do not have access to this resource", statusCode: (int)HttpStatusCode.Forbidden);
        }

        _enigmaWrapperDictionary.Remove(id);
        return NoContent();
    }

    [HttpGet]
    [Authorize]
    [Route("/WebSocketEncryption/{id?}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> WebsocketEncryption(string id)
    {
        if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
        {
            return Problem("Enigma machine ID not found", statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!UserHasAccessToResource(enigmaWrapper))
        {
            return Problem("You do not have access to this resource", statusCode: (int)HttpStatusCode.Forbidden);
        }

        try
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _webSocketService.RunWebSocketAsync(ws, enigmaWrapper.EnigmaMachine);
                enigmaWrapper.EnigmaMachine.Reset();
                return NoContent();
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
                return Problem("Bad Request", statusCode: (int)HttpStatusCode.BadRequest);
            }

        }
        catch (Exception ex)
        {
            HttpContext.Response.StatusCode = 500;
            enigmaWrapper.EnigmaMachine.Reset();
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);

        }
    }

    private bool UserHasAccessToResource(IEnigmaWrapper resource)
    {
        return  User.Identities?.FirstOrDefault()?.Claims?.FirstOrDefault()?.Value == resource.Owner;
    }
}


