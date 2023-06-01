using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RedisTestAPI.Data;
using RedisTestAPI.Models;
using RedisTestAPI.Servies;

namespace RedisTestAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DriversController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly AppDbContext _appDbContext;

        public DriversController(ILogger<DriversController> logger, ICacheService cacheService, AppDbContext appDbContext)
        {
            _cacheService = cacheService;
            _appDbContext = appDbContext;
        }

        [HttpGet("drivers")]
        public async Task<IActionResult> Get()
        {
            var cacheData = _cacheService.GetData<IEnumerable<Driver>>("drivers");

            if (cacheData != null && cacheData.Count() > 0)
                return Ok(cacheData);

            cacheData = await _appDbContext.Drivers.ToListAsync();

            var expirationTime = DateTimeOffset.Now.AddMinutes(5);
            _cacheService.SetData<IEnumerable<Driver>>("drivers", cacheData, expirationTime);

            return Ok(cacheData);
        }

        [HttpPost("AddDriver")]
        public async Task<IActionResult> Post(Driver value)
        {
            var addedObj = await _appDbContext.Drivers.AddAsync(value);

            var expirationTime = DateTimeOffset.Now.AddMinutes(5);
            _cacheService.SetData<Driver>($"driver{value.Id}", value, expirationTime);

            await _appDbContext.SaveChangesAsync();

            return Ok(addedObj.Entity);
        }

        [HttpDelete("DeleteDriver")]
        public async Task<IActionResult> Delete(int id)
        {
            var exist = await _appDbContext.Drivers.FirstOrDefaultAsync(x => x.Id == id);
            
            if(exist != null)
            {
                _appDbContext.Remove(exist);
                _cacheService.RemoveData($"driver{id}");
                await _appDbContext.SaveChangesAsync();

                return NoContent();
            }

            return NotFound();
        }
    }
}