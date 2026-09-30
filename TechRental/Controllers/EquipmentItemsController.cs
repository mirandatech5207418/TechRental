using TechRental.DTOs;
using TechRental.Exceptions;
using TechRental.Models;
using TechRental.Services;
using Microsoft.AspNetCore.Mvc;

namespace TechRental.Controllers;

[ApiController]
[Route("api/equipment-items")]
public class EquipmentItemsController : ControllerBase
{
    private readonly IEquipmentItemService _service;

    public EquipmentItemsController(IEquipmentItemService service)
    {
        _service = service;
    }

    /// <summary>Gets all equipment items, optionally filtered by status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EquipmentItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? status)
    {
        EquipmentStatus? statusFilter = status.HasValue && Enum.IsDefined(typeof(EquipmentStatus), status.Value)
            ? (EquipmentStatus)status.Value
            : null;

        var items = await _service.GetAllAsync(statusFilter);
        return Ok(items);
    }

    /// <summary>Gets a equipment item by ID including its components.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EquipmentItemDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return Ok(item);
    }

    /// <summary>Creates a new equipment item.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EquipmentItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateEquipmentItemRequest request)
    {
        var item = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = item.ItemId }, item);
    }

    /// <summary>Updates a equipment item's name and type.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EquipmentItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEquipmentItemRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        return Ok(item);
    }

    /// <summary>Deletes a equipment item.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Changes the active/inactive status of a equipment item, applying business rules.</summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(typeof(EquipmentItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeStatusRequest request)
    {
        var item = await _service.ChangeStatusAsync(id, request.Status);
        return Ok(item);
    }

    /// <summary>Adds a component to a OPTION or KIT equipment item.</summary>
    [HttpPost("{parentId:int}/components")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddComponent(int parentId, [FromBody] AddComponentRequest request)
    {
        await _service.AddComponentAsync(parentId, request.ChildItemId);
        return NoContent();
    }

    /// <summary>Removes a component from a equipment item.</summary>
    [HttpDelete("{parentId:int}/components/{childId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveComponent(int parentId, int childId)
    {
        await _service.RemoveComponentAsync(parentId, childId);
        return NoContent();
    }
}
