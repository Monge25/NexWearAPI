using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexWearAPI.DTOs;
using NexWearAPI.Models;
using NexWearAPI.Services;
using System.Security.Claims;

namespace NexWearAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AddressesController : ControllerBase
    {
        private readonly IAddressService _addressService;

        public AddressesController(IAddressService addressService)
        {
            _addressService = addressService;
        }

        private Guid GetUserId() =>
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")!);

        /// <summary>Listar mis direcciones</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var addresses = await _addressService.GetAllAsync(GetUserId());
            return Ok(addresses);
        }

        /// <summary>Obtener una dirección por ID</summary>
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var address = await _addressService.GetByIdAsync(GetUserId(), id);
            if (address is null) return NotFound(new { message = "Dirección no encontrada." });
            return Ok(address);
        }

        /// <summary>Agregar nueva dirección</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAddressDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var address = await _addressService.CreateAsync(GetUserId(), dto);
            return CreatedAtAction(nameof(GetById), new { id = address.Id }, address);
        }

        /// <summary>Editar dirección</summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAddressDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var address = await _addressService.UpdateAsync(GetUserId(), id, dto);
            if (address is null) return NotFound(new { message = "Dirección no encontrada." });
            return Ok(address);
        }

        /// <summary>Eliminar dirección</summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _addressService.DeleteAsync(GetUserId(), id);
            if (!result) return NotFound(new { message = "Dirección no encontrada." });
            return NoContent();
        }

        /// <summary>Marcar como predeterminada</summary>
        [HttpPut("{id:guid}/default")]
        public async Task<IActionResult> SetDefault(Guid id)
        {
            var address = await _addressService.SetDefaultAsync(GetUserId(), id);
            if (address is null) return NotFound(new { message = "Dirección no encontrada." });
            return Ok(address);
        }
    }
}
