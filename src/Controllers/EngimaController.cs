using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using log4net;

namespace EnigmaWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EnigmaController : ControllerBase
{
    private ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    private IEnigmaWrapperFactory _enigmaWrapperFactory;
    private IRandomSettingsGenerator _settingsGenerator;
    private ICredentialRepository _credentialRepository;
    private IWebSocketService _webSocketService;
    private static Dictionary<string, IEnigmaWrapper> _enigmaWrapperDictionary = new();
    public EnigmaController(IEnigmaWrapperFactory enigmaWrapperFactory, IRandomSettingsGenerator settingsGenerator, ICredentialRepository credRepository, IWebSocketService webSocketService)
    {
        _enigmaWrapperFactory = enigmaWrapperFactory;
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
            _log.Info("Received add enigma machine request");

            var headers = HttpContext.Request.Headers;

            var loggedInUser = User.Identities?.FirstOrDefault()?.Claims?.FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(loggedInUser))
            {
                _log.Error($"User {loggedInUser} is forbidden from creating this resource.");
                return Problem("You do not have access to this resource", statusCode: (int)HttpStatusCode.Forbidden);
            }

            if (request == null || !request.IsValid())
            {
                _log.Error("Received malformed request");
                return Problem("Bad Request", statusCode: (int)HttpStatusCode.BadRequest);
            }

            var guid = Guid.NewGuid().ToString();

            IEnigmaWrapper enigmaWrapper;
            EnigmaSettings settings;

            if (request.UseRandomWheels)
            {
                _log.Info($"Creating new enigma machine {guid} with random wheel settings");
                settings = _settingsGenerator.GenerateRandomSettings(request.NumberOfWheels);
                enigmaWrapper = _enigmaWrapperFactory.CreateEnigmaWrapper(loggedInUser, settings);
            }
            else
            {
                _log.Info($"Creating new enigma machine {guid} with user defined wheel settings");
                settings = request.EnigmaMachineSettings!;
                enigmaWrapper = _enigmaWrapperFactory.CreateEnigmaWrapper(loggedInUser, settings);
            }

            _enigmaWrapperDictionary.Add(guid, enigmaWrapper);

            var response = new AddEnigmaResponse(guid, settings);

            _log.Info($"Machine {guid} successfully created");
            return Ok(response);
        }
        catch(Exception ex)
        {
            _log.Error("Unrecognised error when creating new enigma machine", ex);
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpPost]
    [Route("{id?}/encrypt")]
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
                const string err = "Request string was null or empty";
                _log.Error(err);
                return Problem(err, statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (!request.IsValid())
            {
                const string  err = "Request string contained invalid characters";
                _log.Error(err);
                return Problem(err, statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
            {
                var err = $"Enigma machine {id} not found";
                _log.Error(err);
                return Problem(err, statusCode: (int)HttpStatusCode.NotFound);
            }

            if (!UserHasAccessToResource(enigmaWrapper))
            {
                const string err = "User does not have access to this resource";
                _log.Error(err);
                return Problem(err, statusCode: (int)HttpStatusCode.Forbidden);
            }

            var CipherText = enigmaWrapper.EnigmaMachine.Encrypt(request.PlainText);

            var responseContent = new EncryptionResponse()
            {
                PlainText = request.PlainText,
                CipherText = CipherText
            };

            _log.Info($"Enigma machine {id} has successfully encrypted the plain text");

            return Ok(responseContent);
        }
        catch (EncryptionException)
        {
            var err = $"Enigma machine {id} is not configured.";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.Conflict);         
        }
        catch (Exception ex)
        {
            _log.Error($"Unrecognised error", ex);
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);
        }

    }

    [HttpPut]
    [Route("{id?}/update")]
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
                const string err = "Bad Request. Request is null or invalid";
                _log.Error(err);
                return Problem(err, statusCode: (int)HttpStatusCode.BadRequest);
            }

            if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
            {
                var err = $"Enigma machine {id} not found";
                _log.Error(err);
                return Problem(err, statusCode: (int)HttpStatusCode.NotFound);
            }

            if (!UserHasAccessToResource(enigmaWrapper))
            {
                const string err = "User does not have access to this resource";
                _log.Error(err);
                return Problem(err, statusCode: (int)HttpStatusCode.Forbidden);
            }

            var newSettings = request.UseRandomWheels ? _settingsGenerator.GenerateRandomSettings(request.NumberOfWheels) : request.EnigmaMachineSettings;

            if (newSettings == null)
            {
                const string err = "Bad Request. New settings are null or invalid";
                _log.Error(err);
                return Problem(err, statusCode: (int)HttpStatusCode.BadRequest);
            }

            enigmaWrapper.EnigmaMachine.UpdateSettings(newSettings);

            var responseContent = new UpdateSettingsResponse()
            {
                NewSettings = newSettings
            };

            _log.Info("Settings have been successfully updated.");

            return Ok(responseContent);
        }
        catch (Exception ex)
        {
            _log.Error($"Unrecognised error", ex);
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpGet]
    [Route("{id?}/settings")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateSettingsResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetSettings(string id)
    {
        if (!_enigmaWrapperDictionary.TryGetValue(id, out var enigmaWrapper))
        {
            var err = $"Enigma machine {id} not found";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!UserHasAccessToResource(enigmaWrapper))
        {
            const string err = "User does not have access to this resource";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.Forbidden);
        }

        var settings = enigmaWrapper.EnigmaMachine.Settings;

        if (settings == null)
        {
            var err = $"Cannot return settings for {id} as they have not been set";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.Conflict);
        }

        var response = new UpdateSettingsResponse()
        {
            NewSettings = settings
        };

        _log.Info($"Successfully obtained settings for {id}");
        return Ok(response);
    }

    [HttpPost]
    [Route("{id?}/reset")]
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
            var err = $"Enigma machine {id} not found";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!UserHasAccessToResource(enigmaWrapper))
        {
            const string err = "User does not have access to this resource";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.Forbidden);
        }

        enigmaWrapper.EnigmaMachine.Reset();

        _log.Info($"Successfully reset machine: {id}");
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
            var err = $"Enigma machine {id} not found";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!UserHasAccessToResource(enigmaWrapper))
        {
            const string err = "User does not have access to this resource";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.Forbidden);
        }

        _enigmaWrapperDictionary.Remove(id);

        _log.Info($"Removed machine: {id}");
        return NoContent();
    }

    [HttpGet]
    [Authorize]
    [Route("{id?}/WebsocketEncryption")]
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
            var err = $"Enigma machine {id} not found";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.NotFound);
        }

        if (!UserHasAccessToResource(enigmaWrapper))
        {
            const string err = "User does not have access to this resource";
            _log.Error(err);
            return Problem(err, statusCode: (int)HttpStatusCode.Forbidden);
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
                _log.Error("WebSocket endpoint received non-websocket request");
                return Problem("Bad Request", statusCode: (int)HttpStatusCode.BadRequest);
            }

        }
        catch (Exception ex)
        {
            HttpContext.Response.StatusCode = 500;
            enigmaWrapper.EnigmaMachine.Reset();
            _log.Error("Unrecognised error", ex);
            return Problem($"Unrecognised error: {ex}", statusCode: (int)HttpStatusCode.InternalServerError);

        }
    }

    private bool UserHasAccessToResource(IEnigmaWrapper resource)
    {
        return  User.Identities?.FirstOrDefault()?.Claims?.FirstOrDefault()?.Value == resource.Owner;
    }
}


