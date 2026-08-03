using AquaBlend.Data;
using AquaBlend.DTOs.WaterSources;
using AquaBlend.Entities;
using Microsoft.EntityFrameworkCore;

namespace AquaBlend.Services;

public class WaterSourceService
{
    private readonly AquaBlendDbContext _context;

    public WaterSourceService(AquaBlendDbContext context)
    {
        _context = context;
    }

    public async Task<List<WaterSourceResponseDto>> GetAllAsync()
    {
        return await _context.WaterSources
            .AsNoTracking()
            .Select(waterSource => new WaterSourceResponseDto
            {
                Id = waterSource.Id,
                Name = waterSource.Name,
                Type = waterSource.Type,
                CreatedAt = waterSource.CreatedAt,
                UpdatedAt = waterSource.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<WaterSourceResponseDto?> GetByIdAsync(int id)
    {
        var waterSource = await _context.WaterSources
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        return waterSource is null ? null : ToResponseDto(waterSource);
    }

    public async Task<WaterSourceResponseDto> CreateAsync(CreateWaterSourceDto dto)
    {
        var waterSource = new WaterSource
        {
            Name = dto.Name.Trim(),
            Type = dto.Type.Trim()
        };

        _context.WaterSources.Add(waterSource);
        await _context.SaveChangesAsync();

        return ToResponseDto(waterSource);
    }

    public async Task<bool> UpdateAsync(int id, UpdateWaterSourceDto dto)
    {
        var waterSource = await _context.WaterSources.FindAsync(id);
        if (waterSource is null) return false;

        waterSource.Name = dto.Name.Trim();
        waterSource.Type = dto.Type.Trim();
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var waterSource = await _context.WaterSources.FindAsync(id);
        if (waterSource is null) return false;

        _context.WaterSources.Remove(waterSource);
        await _context.SaveChangesAsync();
        return true;
    }

    private static WaterSourceResponseDto ToResponseDto(WaterSource waterSource) => new()
    {
        Id = waterSource.Id,
        Name = waterSource.Name,
        Type = waterSource.Type,
        CreatedAt = waterSource.CreatedAt,
        UpdatedAt = waterSource.UpdatedAt
    };
}
