using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Abstractions;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class LlmProxyController : ControllerBase
{
    private readonly IDownstreamApi _downstreamApi;
    public LlmProxyController(IDownstreamApi downstreamApi) => _downstreamApi = downstreamApi;


}
