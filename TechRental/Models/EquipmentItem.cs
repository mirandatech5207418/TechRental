namespace TechRental.Models;

public enum EquipmentItemType
{
    Equipment = 1,
    Option = 2,
    Kit = 3
}

public enum EquipmentStatus
{
    Unavailable = 0,
    Available = 1
}

public class EquipmentItem
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public EquipmentItemType Type { get; set; }

    // Navigation
    public EquipmentAvailability? Status { get; set; }
    public ICollection<EquipmentComponent> ParentComponents { get; set; } = new List<EquipmentComponent>();
    public ICollection<EquipmentComponent> ChildComponents { get; set; } = new List<EquipmentComponent>();
}

public class EquipmentAvailability
{
    public int ItemId { get; set; }
    public EquipmentStatus Status { get; set; }

    // Navigation
    public EquipmentItem EquipmentItem { get; set; } = null!;
}

public class EquipmentComponent
{
    public int ItemId { get; set; }
    public int ChildItemId { get; set; }

    // Navigation
    public EquipmentItem Parent { get; set; } = null!;
    public EquipmentItem Child { get; set; } = null!;
}
