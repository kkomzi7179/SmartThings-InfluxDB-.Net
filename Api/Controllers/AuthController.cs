namespace Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Api.Services;

[ApiController]
[Route("Auth")]
public class AuthController(ISmartThingsService smartThingsService, ILogger<AuthController> logger) : ControllerBase {
	[HttpGet(nameof(GetAuthorizationUrl))]
	public async Task<IActionResult> GetAuthorizationUrl() {
		var url = await smartThingsService.GetAuthorizationUrlAsync();

		return Ok(url);
	}

	[HttpGet(nameof(AuthorizationCallback))]
	public async Task<IActionResult> AuthorizationCallback([FromQuery] string code) {
		logger.LogInformation($"Authorization code: {code}");

		if(string.IsNullOrEmpty(code)) {
			return BadRequest("Authorization code is missing.");
		}

		var result = await smartThingsService.GetTokenWithCodeAsync(code);

		if(result.Item1) {
			return Content(result.Item2.ExBeautify(), "application/json");
		} else {
			return Content(result.Item2);
		}
	}

	[HttpGet(nameof(ViewToken))]
	public async Task<IActionResult> ViewToken() {
		return Ok(await smartThingsService.ViewTokenAsync());
	}
}
