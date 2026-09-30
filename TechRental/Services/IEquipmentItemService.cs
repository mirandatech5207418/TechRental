using TechRental.DTOs;
using TechRental.Models;

namespace TechRental.Services;

public interface IEquipmentItemService
{
    Task<IEnumerable<EquipmentItemDto>> GetAllAsync(EquipmentStatus? statusFilter = null);
    Task<EquipmentItemDetailDto> GetByIdAsync(int id);
    Task<EquipmentItemDto> CreateAsync(CreateEquipmentItemRequest request);
    Task<EquipmentItemDto> UpdateAsync(int id, UpdateEquipmentItemRequest request);
    Task DeleteAsync(int id);
    Task<EquipmentItemDto> ChangeStatusAsync(int id, int newStatus);
    Task AddComponentAsync(int parentId, int childId);
    Task RemoveComponentAsync(int parentId, int childId);
}
