using Microsoft.EntityFrameworkCore;
using NexWearAPI.Data;
using NexWearAPI.DTOs;
using NexWearAPI.Models;

namespace NexWearAPI.Services
{
    public interface IAddressService
    {
        Task<IEnumerable<AddressResponseDto>> GetAllAsync(Guid userId);
        Task<AddressResponseDto?> GetByIdAsync(Guid userId, Guid addressId);
        Task<AddressResponseDto> CreateAsync(Guid userId, CreateAddressDto dto);
        Task<AddressResponseDto?> UpdateAsync(Guid userId, Guid addressId, UpdateAddressDto dto);
        Task<bool> DeleteAsync(Guid userId, Guid addressId);
        Task<AddressResponseDto?> SetDefaultAsync(Guid userId, Guid addressId);
    }

    public class AddressService : IAddressService
    {
        private readonly AppDbContext _context;

        public AddressService(AppDbContext context)
        {
            _context = context;
        }

        // ── Listar direcciones ────────────────────────────────────
        public async Task<IEnumerable<AddressResponseDto>> GetAllAsync(Guid userId)
        {
            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return addresses.Select(MapToDto);
        }

        // ── Obtener una dirección ─────────────────────────────────
        public async Task<AddressResponseDto?> GetByIdAsync(Guid userId, Guid addressId)
        {
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            return address is null ? null : MapToDto(address);
        }

        // ── Crear dirección ───────────────────────────────────────
        public async Task<AddressResponseDto> CreateAsync(Guid userId, CreateAddressDto dto)
        {
            // Si es predeterminada, quitar la anterior
            if (dto.IsDefault)
                await ClearDefaultAsync(userId);

            // Si es la primera dirección, hacerla predeterminada automáticamente
            var hasAddresses = await _context.Addresses.AnyAsync(a => a.UserId == userId);

            var address = new Address
            {
                UserId = userId,
                Alias = dto.Alias.Trim(),
                Street = dto.Street.Trim(),
                Interior = dto.Interior?.Trim(),
                City = dto.City.Trim(),
                State = dto.State.Trim(),
                ZipCode = dto.ZipCode.Trim(),
                Country = dto.Country.Trim(),
                Phone = dto.Phone?.Trim(),
                IsDefault = dto.IsDefault || !hasAddresses,
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return MapToDto(address);
        }

        // ── Actualizar dirección ──────────────────────────────────
        public async Task<AddressResponseDto?> UpdateAsync(Guid userId, Guid addressId, UpdateAddressDto dto)
        {
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address is null) return null;

            if (dto.Alias is not null) address.Alias = dto.Alias.Trim();
            if (dto.Street is not null) address.Street = dto.Street.Trim();
            if (dto.Interior is not null) address.Interior = dto.Interior.Trim();
            if (dto.City is not null) address.City = dto.City.Trim();
            if (dto.State is not null) address.State = dto.State.Trim();
            if (dto.ZipCode is not null) address.ZipCode = dto.ZipCode.Trim();
            if (dto.Country is not null) address.Country = dto.Country.Trim();
            if (dto.Phone is not null) address.Phone = dto.Phone.Trim();

            if (dto.IsDefault == true)
            {
                await ClearDefaultAsync(userId);
                address.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            return MapToDto(address);
        }

        // ── Eliminar dirección ────────────────────────────────────
        public async Task<bool> DeleteAsync(Guid userId, Guid addressId)
        {
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address is null) return false;

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            // Si era la predeterminada, hacer predeterminada la más reciente
            if (address.IsDefault)
            {
                var next = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                if (next is not null)
                {
                    next.IsDefault = true;
                    await _context.SaveChangesAsync();
                }
            }

            return true;
        }

        // ── Marcar como predeterminada ────────────────────────────
        public async Task<AddressResponseDto?> SetDefaultAsync(Guid userId, Guid addressId)
        {
            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address is null) return null;

            await ClearDefaultAsync(userId);
            address.IsDefault = true;
            await _context.SaveChangesAsync();

            return MapToDto(address);
        }

        // ── Helper: quitar predeterminada anterior ────────────────
        private async Task ClearDefaultAsync(Guid userId)
        {
            var current = await _context.Addresses
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync();

            current.ForEach(a => a.IsDefault = false);
            await _context.SaveChangesAsync();
        }

        // ── Mapper ────────────────────────────────────────────────
        private static AddressResponseDto MapToDto(Address a) => new()
        {
            Id = a.Id,
            Alias = a.Alias,
            Street = a.Street,
            Interior = a.Interior,
            City = a.City,
            State = a.State,
            ZipCode = a.ZipCode,
            Country = a.Country,
            Phone = a.Phone,
            IsDefault = a.IsDefault,
        };
    }
}
