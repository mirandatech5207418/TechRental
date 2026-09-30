using FluentAssertions;
using TechRental.DTOs;
using TechRental.Exceptions;
using TechRental.Models;
using TechRental.Services;

namespace TechRental.Tests;

public class EquipmentItemServiceTests
{
    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ReturnsAllItems()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        var result = await svc.GetAllAsync();

        result.Should().HaveCount(9);
    }

    [Fact]
    public async Task GetAll_FilterByAvailable_ReturnsOnlyAvailableItems()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        var result = await svc.GetAllAsync(EquipmentStatus.Available);

        result.Should().OnlyContain(x => x.Status == "Available");
    }

    [Fact]
    public async Task GetAll_FilterByUnavailable_ReturnsOnlyUnavailableItems()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        var result = await svc.GetAllAsync(EquipmentStatus.Unavailable);

        result.Should().OnlyContain(x => x.Status == "Unavailable");
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingId_ReturnsItemWithComponents()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        var result = await svc.GetByIdAsync(6); // MOUSE OPTION

        result.ItemId.Should().Be(6);
        result.ItemName.Should().Be("MOUSE OPTION");
        result.Type.Should().Be("Option");
        result.Components.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetById_NonExistingId_ThrowsNotFoundException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        await svc.Invoking(s => s.GetByIdAsync(999))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidEquipment_CreatesWithUnavailableStatus()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        var result = await svc.CreateAsync(new CreateEquipmentItemRequest("MONITOR LG", 1));

        result.ItemName.Should().Be("MONITOR LG");
        result.Type.Should().Be("Equipment");
        result.Status.Should().Be("Unavailable");
    }

    [Fact]
    public async Task Create_InvalidType_ThrowsInvalidOperationException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        await svc.Invoking(s => s.CreateAsync(new CreateEquipmentItemRequest("X", 99)))
            .Should().ThrowAsync<Exceptions.InvalidOperationException>();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_ExistingItem_UpdatesNameAndType()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        var result = await svc.UpdateAsync(1, new UpdateEquipmentItemRequest("NOTEBOOK LENOVO", 1));

        result.ItemName.Should().Be("NOTEBOOK LENOVO");
    }

    [Fact]
    public async Task Update_NonExistingItem_ThrowsNotFoundException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        await svc.Invoking(s => s.UpdateAsync(999, new UpdateEquipmentItemRequest("X", 1)))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ItemWithNoParents_DeletesSuccessfully()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // WEBCAM LOGITECH (5) is a component of ACCESSORY OPTION (7) - we must remove that first
        // Instead, let's create a standalone product
        var newItem = await svc.CreateAsync(new CreateEquipmentItemRequest("STANDALONE", 1));

        await svc.Invoking(s => s.DeleteAsync(newItem.ItemId))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Delete_ItemUsedAsComponent_ThrowsBusinessRuleException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // NOTEBOOK DELL (1) is a component of DEVELOPER KIT (8) and HOME OFFICE KIT (9)
        await svc.Invoking(s => s.DeleteAsync(1))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Delete_NonExistingItem_ThrowsNotFoundException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        await svc.Invoking(s => s.DeleteAsync(999))
            .Should().ThrowAsync<NotFoundException>();
    }

    // ── ChangeStatus: OPTION Rules ────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_Option_WithAtLeastOneAvailableComponent_Activates()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // MOUSE OPTION (6) has MOUSE MICROSOFT (3 = Available) as component
        var result = await svc.ChangeStatusAsync(6, (int)EquipmentStatus.Available);

        result.Status.Should().Be("Available");
    }

    [Fact]
    public async Task ChangeStatus_Option_WithNoAvailableComponents_ThrowsBusinessRuleException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // Deactivate MOUSE MICROSOFT (3) and MOUSE LOGITECH (2) is already Unavailable
        await svc.ChangeStatusAsync(3, (int)EquipmentStatus.Unavailable);

        // Now MOUSE OPTION (6) has no available components
        await svc.Invoking(s => s.ChangeStatusAsync(6, (int)EquipmentStatus.Available))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*OPTION*at least one available component*");
    }

    // ── ChangeStatus: KIT Rules ────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_Kit_WithAllComponentsAvailable_Activates()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // Activate MOUSE OPTION (6) first - needs MOUSE MICROSOFT (3, active) -> OK
        await svc.ChangeStatusAsync(6, (int)EquipmentStatus.Available);
        // Activate ACCESSORY OPTION (7) - needs HEADSET JBL (4) and WEBCAM LOGITECH (5), both active -> OK
        await svc.ChangeStatusAsync(7, (int)EquipmentStatus.Available);
        // Now DEVELOPER KIT (8): NOTEBOOK DELL(1)=Available, MOUSE OPTION(6)=Available, ACCESSORY OPTION(7)=Available
        var result = await svc.ChangeStatusAsync(8, (int)EquipmentStatus.Available);

        result.Status.Should().Be("Available");
    }

    [Fact]
    public async Task ChangeStatus_Kit_WithUnavailableComponent_ThrowsBusinessRuleException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // DEVELOPER KIT (8) has MOUSE OPTION (6) which is Unavailable
        await svc.Invoking(s => s.ChangeStatusAsync(8, (int)EquipmentStatus.Available))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*KIT*all components must be available*");
    }

    // ── ChangeStatus: Propagation ─────────────────────────────────────────────

    [Fact]
    public async Task ChangeStatus_DeactivatingEquipment_PropagatesAndDeactivatesParentOption()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // First activate MOUSE OPTION (6) - MOUSE MICROSOFT (3) is active, so valid
        await svc.ChangeStatusAsync(6, (int)EquipmentStatus.Available);
        // Deactivate MOUSE MICROSOFT (3), MOUSE LOGITECH (2) is already inactive → MOUSE OPTION should auto-deactivate
        await svc.ChangeStatusAsync(3, (int)EquipmentStatus.Unavailable);

        var drinkOption = await svc.GetByIdAsync(6);
        drinkOption.Status.Should().Be("Unavailable");
    }

    [Fact]
    public async Task ChangeStatus_DeactivatingComponent_PropagatesAndDeactivatesParentKit()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // Activate the full chain so DEVELOPER KIT becomes active
        await svc.ChangeStatusAsync(6, (int)EquipmentStatus.Available); // MOUSE OPTION
        await svc.ChangeStatusAsync(7, (int)EquipmentStatus.Available); // ACCESSORY OPTION
        await svc.ChangeStatusAsync(8, (int)EquipmentStatus.Available); // DEVELOPER KIT

        // Now deactivate NOTEBOOK DELL (1) → DEVELOPER KIT should auto-deactivate
        await svc.ChangeStatusAsync(1, (int)EquipmentStatus.Unavailable);

        var bigMacMeal = await svc.GetByIdAsync(8);
        bigMacMeal.Status.Should().Be("Unavailable");
    }

    // ── AddComponent / RemoveComponent ────────────────────────────────────────

    [Fact]
    public async Task AddComponent_ValidOptionEquipment_AddsSuccessfully()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // Create a new product and add it to MOUSE OPTION (6)
        var newEquipment = await svc.CreateAsync(new CreateEquipmentItemRequest("FANTA", 1));

        await svc.Invoking(s => s.AddComponentAsync(6, newEquipment.ItemId))
            .Should().NotThrowAsync();

        var detail = await svc.GetByIdAsync(6);
        detail.Components.Should().Contain(x => x.ItemName == "FANTA");
    }

    [Fact]
    public async Task AddComponent_OptionWithNonEquipment_ThrowsBusinessRuleException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // Try to add ACCESSORY OPTION (7, a Option) as component of MOUSE OPTION (6)
        await svc.Invoking(s => s.AddComponentAsync(6, 7))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*OPTION*only have EQUIPMENT_ITEM*");
    }

    [Fact]
    public async Task AddComponent_EquipmentAsParent_ThrowsBusinessRuleException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        // Equipments cannot have components
        await svc.Invoking(s => s.AddComponentAsync(1, 2))
            .Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*EQUIPMENT_ITEM cannot have components*");
    }

    [Fact]
    public async Task AddComponent_SameItem_ThrowsBusinessRuleException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        await svc.Invoking(s => s.AddComponentAsync(6, 6))
            .Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task RemoveComponent_ExistingRelationship_RemovesSuccessfully()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        await svc.Invoking(s => s.RemoveComponentAsync(6, 2))
            .Should().NotThrowAsync();

        var detail = await svc.GetByIdAsync(6);
        detail.Components.Should().NotContain(x => x.ItemId == 2);
    }

    [Fact]
    public async Task RemoveComponent_NonExistingRelationship_ThrowsNotFoundException()
    {
        using var ctx = TestDbHelper.CreateContext();
        var svc = new EquipmentItemService(ctx);

        await svc.Invoking(s => s.RemoveComponentAsync(6, 99))
            .Should().ThrowAsync<NotFoundException>();
    }
}
