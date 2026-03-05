using F1.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace F1.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DriversController : ControllerBase
    {
        private readonly IDriverRepository _driverRepository;

        public DriversController(IDriverRepository driverRepository)
        {
            _driverRepository = driverRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetDrivers()
        {
            var drivers = await _driverRepository.GetDriversAsync();
            if (drivers == null || drivers.Count == 0)
            {
                return NotFound();
            }
            return Ok(drivers);
        }
    }
}
