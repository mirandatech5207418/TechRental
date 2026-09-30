using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechRental.Data;
using TechRental.Models;

namespace TechRental.Controllers;

[ApiController]
[Route("api/rentals")]
public class RentalsController : ControllerBase
{
    private readonly MenuContext _db;
    public RentalsController(MenuContext db) => _db = db;

    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer(Customer customer)
    {
        _db.Customers.Add(customer); await _db.SaveChangesAsync();
        return Created($"api/rentals/customers/{customer.CustomerId}", customer);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRental([FromBody] CreateRentalRequest request)
    {
        var customer = await _db.Customers.FindAsync(request.CustomerId);
        if (customer is null) return NotFound("Customer not found.");
        var items = await _db.EquipmentItems.Include(x => x.Status).Where(x => request.ItemIds.Contains(x.ItemId)).ToListAsync();
        if (items.Count != request.ItemIds.Distinct().Count()) return BadRequest("One or more equipment items do not exist.");
        if (items.Any(x => x.Status?.Status != EquipmentStatus.Available)) return BadRequest("Unavailable equipment cannot be rented.");
        var rental = new Rental { CustomerId = request.CustomerId, StartDate = DateTime.UtcNow, Status = "OPEN" };
        _db.Rentals.Add(rental); await _db.SaveChangesAsync();
        foreach (var item in items) { _db.RentalItems.Add(new RentalItem { RentalId = rental.RentalId, ItemId = item.ItemId }); item.Status!.Status = EquipmentStatus.Unavailable; }
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = rental.RentalId }, new { rental.RentalId, rental.CustomerId, request.ItemIds });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var rental = await _db.Rentals.Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.EquipmentItem).FirstOrDefaultAsync(x => x.RentalId == id);
        return rental is null ? NotFound() : Ok(new { rental.RentalId, Customer = rental.Customer.Name, rental.StartDate, rental.EndDate, rental.Status, Items = rental.Items.Select(i => i.EquipmentItem.ItemName) });
    }

    [HttpGet]
    public async Task<IActionResult> GetByCustomer([FromQuery] int? customerId)
    {
        var q = _db.Rentals.Include(x => x.Customer).AsQueryable();
        if (customerId.HasValue) q = q.Where(x => x.CustomerId == customerId.Value);
        return Ok(await q.Select(x => new { x.RentalId, Customer = x.Customer.Name, x.StartDate, x.EndDate, x.Status }).ToListAsync());
    }

    [HttpPost("{id:int}/return")]
    public async Task<IActionResult> Return(int id)
    {
        var rental = await _db.Rentals.Include(x => x.Items).ThenInclude(x => x.EquipmentItem).ThenInclude(x => x.Status).FirstOrDefaultAsync(x => x.RentalId == id);
        if (rental is null) return NotFound();
        rental.Status = "RETURNED"; rental.EndDate = DateTime.UtcNow;
        foreach (var ri in rental.Items) if (ri.EquipmentItem.Status is not null) ri.EquipmentItem.Status.Status = EquipmentStatus.Available;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpGet("report.csv")]
    public async Task<IActionResult> Report()
    {
        var rows = await _db.Rentals.Include(x => x.Customer).Include(x => x.Items).ThenInclude(x => x.EquipmentItem).ToListAsync();
        var csv = "RentalId;Customer;Item;StartDate;EndDate;Status\n" + string.Join("\n", rows.SelectMany(r => r.Items.Select(i => $"{r.RentalId};{r.Customer.Name};{i.EquipmentItem.ItemName};{r.StartDate:O};{r.EndDate:O};{r.Status}")));
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "rental-report.csv");
    }
}

public record CreateRentalRequest(int CustomerId, List<int> ItemIds);
