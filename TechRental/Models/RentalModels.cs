namespace TechRental.Models;

public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
}

public class Rental
{
    public int RentalId { get; set; }
    public int CustomerId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "OPEN";
    public Customer Customer { get; set; } = null!;
    public ICollection<RentalItem> Items { get; set; } = new List<RentalItem>();
}

public class RentalItem
{
    public int RentalId { get; set; }
    public int ItemId { get; set; }
    public Rental Rental { get; set; } = null!;
    public EquipmentItem EquipmentItem { get; set; } = null!;
}
