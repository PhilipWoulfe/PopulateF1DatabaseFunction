using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using F1.Services;

namespace F1.Api.Controllers
{
    [ApiController]
    [Route("admin/mock-date")]
    [Authorize(Roles = "Admin")]
    public class MockDateController : ControllerBase
    {
        private readonly IGlobalMockDateService _globalMockDateService;

        public MockDateController(IGlobalMockDateService globalMockDateService)
        {
            _globalMockDateService = globalMockDateService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var mockDate = _globalMockDateService.GetMockDateUtc();
            return Ok(new { mockDate });
        }

        [HttpPost]
        public IActionResult Set([FromBody] SetMockDateRequest request)
        {
            if (request.MockDateUtc.HasValue)
            {
                _globalMockDateService.SetMockDateUtc(request.MockDateUtc.Value);
            }
            else
            {
                _globalMockDateService.SetMockDateUtc(null);
            }
            return NoContent();
        }

        public class SetMockDateRequest
        {
            public DateTime? MockDateUtc { get; set; }
        }
    }
}
