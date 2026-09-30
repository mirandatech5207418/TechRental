using TechRental.Data;
using TechRental.DTOs;
using TechRental.Exceptions;
using TechRental.Models;
using Microsoft.EntityFrameworkCore;

namespace TechRental.Services;

public class EquipmentItemService : IEquipmentItemService
{
    private readonly MenuContext _context;

    public EquipmentItemService(MenuContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EquipmentItemDto>> GetAllAsync(EquipmentStatus? statusFilter = null)
    {
        var query = _context.EquipmentItems
            .Include(x => x.Status)
            .AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(x => x.Status != null && x.Status.Status == statusFilter.Value);

        var items = await query.ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<EquipmentItemDetailDto> GetByIdAsync(int id)
    {
        var item = await _context.EquipmentItems
            .Include(x => x.Status)
            .Include(x => x.ParentComponents)
                .ThenInclude(c => c.Child)
                    .ThenInclude(c => c.Status)
            .FirstOrDefaultAsync(x => x.ItemId == id)
            ?? throw new NotFoundException($"Equipment item with id {id} not found.");

        var components = item.ParentComponents
            .Select(c => MapToDto(c.Child));

        return new EquipmentItemDetailDto(
            item.ItemId,
            item.ItemName,
            item.Type.ToString(),
            item.Status?.Status.ToString() ?? "Unknown",
            components
        );
    }

    public async Task<EquipmentItemDto> CreateAsync(CreateEquipmentItemRequest request)
    {
        if (!Enum.IsDefined(typeof(EquipmentItemType), request.Type))
            throw new Exceptions.InvalidOperationException($"Invalid type: {request.Type}. Valid values: 1=Equipment, 2=Option, 3=Kit");

        var item = new EquipmentItem
        {
            ItemName = request.ItemName,
            Type = (EquipmentItemType)request.Type
        };

        _context.EquipmentItems.Add(item);
        await _context.SaveChangesAsync();

        var status = new EquipmentAvailability
        {
            ItemId = item.ItemId,
            Status = EquipmentStatus.Unavailable
        };
        _context.EquipmentAvailabilities.Add(status);
        await _context.SaveChangesAsync();

        item.Status = status;
        return MapToDto(item);
    }

    public async Task<EquipmentItemDto> UpdateAsync(int id, UpdateEquipmentItemRequest request)
    {
        var item = await _context.EquipmentItems
            .Include(x => x.Status)
            .FirstOrDefaultAsync(x => x.ItemId == id)
            ?? throw new NotFoundException($"Equipment item with id {id} not found.");

        if (!Enum.IsDefined(typeof(EquipmentItemType), request.Type))
            throw new Exceptions.InvalidOperationException($"Invalid type: {request.Type}");

        item.ItemName = request.ItemName;
        item.Type = (EquipmentItemType)request.Type;
        await _context.SaveChangesAsync();

        return MapToDto(item);
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _context.EquipmentItems
            .Include(x => x.Status)
            .Include(x => x.ParentComponents)
            .Include(x => x.ChildComponents)
            .FirstOrDefaultAsync(x => x.ItemId == id)
            ?? throw new NotFoundException($"Equipment item with id {id} not found.");

        if (item.ChildComponents.Any())
            throw new BusinessRuleException($"Cannot delete '{item.ItemName}' because it is a component of other equipment items.");

        if (item.Status != null)
            _context.EquipmentAvailabilities.Remove(item.Status);

        _context.EquipmentComponents.RemoveRange(item.ParentComponents);
        _context.EquipmentItems.Remove(item);

        await _context.SaveChangesAsync();
    }

    public async Task<EquipmentItemDto> ChangeStatusAsync(int id, int newStatus)
    {
        if (!Enum.IsDefined(typeof(EquipmentStatus), newStatus))
            throw new Exceptions.InvalidOperationException($"Invalid status: {newStatus}. Valid values: 0=Unavailable, 1=Available");

        var item = await _context.EquipmentItems
            .Include(x => x.Status)
            .Include(x => x.ParentComponents)
                .ThenInclude(c => c.Child)
                    .ThenInclude(c => c.Status)
            .FirstOrDefaultAsync(x => x.ItemId == id)
            ?? throw new NotFoundException($"Equipment item with id {id} not found.");

        var targetStatus = (EquipmentStatus)newStatus;

        if (targetStatus == EquipmentStatus.Available)
            ValidateActivation(item);

        item.Status!.Status = targetStatus;
        await _context.SaveChangesAsync();

        await PropagateStatusChangeAsync(item, targetStatus);

        return MapToDto(item);
    }

    public async Task AddComponentAsync(int parentId, int childId)
    {
        var parent = await _context.EquipmentItems
            .Include(x => x.ParentComponents)
            .FirstOrDefaultAsync(x => x.ItemId == parentId)
            ?? throw new NotFoundException($"Parent equipment item with id {parentId} not found.");

        var child = await _context.EquipmentItems.FindAsync(childId)
            ?? throw new NotFoundException($"Child equipment item with id {childId} not found.");

        ValidateComponentAssignment(parent, child);

        if (parent.ParentComponents.Any(c => c.ChildItemId == childId))
            throw new BusinessRuleException($"'{child.ItemName}' is already a component of '{parent.ItemName}'.");

        _context.EquipmentComponents.Add(new EquipmentComponent
        {
            ItemId = parentId,
            ChildItemId = childId
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveComponentAsync(int parentId, int childId)
    {
        var component = await _context.EquipmentComponents
            .FirstOrDefaultAsync(x => x.ItemId == parentId && x.ChildItemId == childId)
            ?? throw new NotFoundException($"Component relationship between {parentId} and {childId} not found.");

        _context.EquipmentComponents.Remove(component);
        await _context.SaveChangesAsync();
    }

    // ── Business Rules ──────────────────────────────────────────────────────────

    /// <summary>
    /// Rule 1: A OPTION must have at least one ACTIVE component to be activated.
    /// Rule 2: A KIT must have ALL components ACTIVE to be activated.
    /// </summary>
    private static void ValidateActivation(EquipmentItem item)
    {
        switch (item.Type)
        {
            case EquipmentItemType.Option:
                var hasAvailableComponent = item.ParentComponents
                    .Any(c => c.Child.Status?.Status == EquipmentStatus.Available);

                if (!hasAvailableComponent)
                    throw new BusinessRuleException(
                        $"Cannot activate OPTION '{item.ItemName}': it must have at least one available component.");
                break;

            case EquipmentItemType.Kit:
                var allComponentsAvailable = item.ParentComponents.Any()
                    && item.ParentComponents.All(c => c.Child.Status?.Status == EquipmentStatus.Available);

                if (!allComponentsAvailable)
                    throw new BusinessRuleException(
                        $"Cannot activate KIT '{item.ItemName}': all components must be available.");
                break;
        }
    }

    /// <summary>
    /// When a product is deactivated, propagate: re-evaluate parent OPTION and KIT items.
    /// </summary>
    private async Task PropagateStatusChangeAsync(EquipmentItem item, EquipmentStatus newStatus)
    {
        if (newStatus == EquipmentStatus.Available)
            return; // Activation propagation not needed

        // Find all parents of this item
        var parentIds = await _context.EquipmentComponents
            .Where(c => c.ChildItemId == item.ItemId)
            .Select(c => c.ItemId)
            .ToListAsync();

        foreach (var parentId in parentIds)
        {
            var parent = await _context.EquipmentItems
                .Include(x => x.Status)
                .Include(x => x.ParentComponents)
                    .ThenInclude(c => c.Child)
                        .ThenInclude(c => c.Status)
                .FirstOrDefaultAsync(x => x.ItemId == parentId);

            if (parent?.Status == null || parent.Status.Status == EquipmentStatus.Unavailable)
                continue;

            bool shouldDeactivate = parent.Type switch
            {
                EquipmentItemType.Option =>
                    !parent.ParentComponents.Any(c => c.Child.Status?.Status == EquipmentStatus.Available),
                EquipmentItemType.Kit =>
                    parent.ParentComponents.Any(c => c.Child.Status?.Status == EquipmentStatus.Unavailable),
                _ => false
            };

            if (shouldDeactivate)
            {
                parent.Status.Status = EquipmentStatus.Unavailable;
                await _context.SaveChangesAsync();
                await PropagateStatusChangeAsync(parent, EquipmentStatus.Unavailable); // Recursive
            }
        }
    }

    private static void ValidateComponentAssignment(EquipmentItem parent, EquipmentItem child)
    {
        if (parent.ItemId == child.ItemId)
            throw new BusinessRuleException("A equipment item cannot be a component of itself.");

        switch (parent.Type)
        {
            case EquipmentItemType.Equipment:
                throw new BusinessRuleException("A EQUIPMENT_ITEM cannot have components.");

            case EquipmentItemType.Option:
                if (child.Type != EquipmentItemType.Equipment)
                    throw new BusinessRuleException(
                        "A OPTION can only have EQUIPMENT_ITEM items as components.");
                break;

            case EquipmentItemType.Kit:
                if (child.Type == EquipmentItemType.Kit)
                    throw new BusinessRuleException(
                        "A KIT cannot have another KIT as a component.");
                break;
        }
    }

    private static EquipmentItemDto MapToDto(EquipmentItem item) => new(
        item.ItemId,
        item.ItemName,
        item.Type.ToString(),
        item.Status?.Status.ToString() ?? "Unknown"
    );
}
