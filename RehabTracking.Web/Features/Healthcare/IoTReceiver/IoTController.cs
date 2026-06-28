using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace RehabTracking.Web.Features.Healthcare.IoTReceiver;

[ApiController]
[Route("api/[controller]")]
public class IoTController : ControllerBase
{
    private readonly IMediator _mediator;

    public IoTController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("submit-emg")]
    public async Task<IActionResult> SubmitEmgData([FromBody] SubmitEMGDataCommand command)
    {
        try
        {
            // ESP32 sends JSON to this endpoint
            var result = await _mediator.Send(command);
            
            if (result)
            {
                return Ok(new { Message = RehabTracking.Web.Resources.Messages.DataReceivedSuccess });
            }
            
            return BadRequest(new { Message = RehabTracking.Web.Resources.ErrorMessages.DataProcessingFailed });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = RehabTracking.Web.Resources.ErrorMessages.InternalServerError, Error = ex.Message });
        }
    }
}
