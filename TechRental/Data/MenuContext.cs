using TechRental.Models;
using Microsoft.EntityFrameworkCore;

namespace TechRental.Data;

public class MenuContext : DbContext
{
    public MenuContext(DbContextOptions<MenuContext> options) : base(options) { }

    public DbSet<EquipmentItem> EquipmentItems { get; set; }
    public DbSet<EquipmentAvailability> EquipmentAvailabilities { get; set; }
    public DbSet<EquipmentComponent> EquipmentComponents { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Rental> Rentals { get; set; }
    public DbSet<RentalItem> RentalItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // EQUIPMENT_ITEM table
        modelBuilder.Entity<EquipmentItem>(e =>
        {
            e.ToTable("EQUIPMENT_ITEM");
            e.HasKey(x => x.ItemId);
            e.Property(x => x.ItemId).HasColumnName("ITEM_ID");
            e.Property(x => x.ItemName).HasColumnName("ITEM_NAME").HasMaxLength(255).IsRequired();
            e.Property(x => x.Type).HasColumnName("TYPE");
        });

        // EQUIPMENT_STATUS table
        modelBuilder.Entity<EquipmentAvailability>(e =>
        {
            e.ToTable("EQUIPMENT_STATUS");
            e.HasKey(x => x.ItemId);
            e.Property(x => x.ItemId).HasColumnName("ITEM_ID");
            e.Property(x => x.Status).HasColumnName("STATUS");

            e.HasOne(x => x.EquipmentItem)
             .WithOne(x => x.Status)
             .HasForeignKey<EquipmentAvailability>(x => x.ItemId);
        });

        // EQUIPMENT_COMPONENTS table
        modelBuilder.Entity<EquipmentComponent>(e =>
        {
            e.ToTable("EQUIPMENT_COMPONENTS");
            e.HasKey(x => new { x.ItemId, x.ChildItemId });
            e.Property(x => x.ItemId).HasColumnName("ITEM_ID");
            e.Property(x => x.ChildItemId).HasColumnName("CHILD_ITEM_ID");

            e.HasOne(x => x.Parent)
             .WithMany(x => x.ParentComponents)
             .HasForeignKey(x => x.ItemId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Child)
             .WithMany(x => x.ChildComponents)
             .HasForeignKey(x => x.ChildItemId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Customer>(e => { e.ToTable("CUSTOMER"); e.HasKey(x => x.CustomerId); e.Property(x => x.Name).HasMaxLength(150).IsRequired(); e.Property(x => x.Email).HasMaxLength(200).IsRequired(); });
        modelBuilder.Entity<Rental>(e => { e.ToTable("RENTAL"); e.HasKey(x => x.RentalId); e.HasOne(x => x.Customer).WithMany(x => x.Rentals).HasForeignKey(x => x.CustomerId); });
        modelBuilder.Entity<RentalItem>(e => { e.ToTable("RENTAL_ITEM"); e.HasKey(x => new { x.RentalId, x.ItemId }); e.HasOne(x => x.Rental).WithMany(x => x.Items).HasForeignKey(x => x.RentalId); e.HasOne(x => x.EquipmentItem).WithMany().HasForeignKey(x => x.ItemId).OnDelete(DeleteBehavior.Restrict); });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentItem>().HasData(
            new EquipmentItem { ItemId = 1, ItemName = "NOTEBOOK DELL", Type = EquipmentItemType.Equipment },
            new EquipmentItem { ItemId = 2, ItemName = "MOUSE LOGITECH", Type = EquipmentItemType.Equipment },
            new EquipmentItem { ItemId = 3, ItemName = "MOUSE MICROSOFT", Type = EquipmentItemType.Equipment },
            new EquipmentItem { ItemId = 4, ItemName = "HEADSET JBL", Type = EquipmentItemType.Equipment },
            new EquipmentItem { ItemId = 5, ItemName = "WEBCAM LOGITECH", Type = EquipmentItemType.Equipment },
            new EquipmentItem { ItemId = 6, ItemName = "MOUSE OPTION", Type = EquipmentItemType.Option },
            new EquipmentItem { ItemId = 7, ItemName = "ACCESSORY OPTION", Type = EquipmentItemType.Option },
            new EquipmentItem { ItemId = 8, ItemName = "DEVELOPER KIT", Type = EquipmentItemType.Kit },
            new EquipmentItem { ItemId = 9, ItemName = "HOME OFFICE KIT", Type = EquipmentItemType.Kit }
        );

        modelBuilder.Entity<EquipmentAvailability>().HasData(
            new EquipmentAvailability { ItemId = 1, Status = EquipmentStatus.Available },
            new EquipmentAvailability { ItemId = 2, Status = EquipmentStatus.Unavailable },
            new EquipmentAvailability { ItemId = 3, Status = EquipmentStatus.Available },
            new EquipmentAvailability { ItemId = 4, Status = EquipmentStatus.Available },
            new EquipmentAvailability { ItemId = 5, Status = EquipmentStatus.Available },
            new EquipmentAvailability { ItemId = 6, Status = EquipmentStatus.Unavailable },
            new EquipmentAvailability { ItemId = 7, Status = EquipmentStatus.Unavailable },
            new EquipmentAvailability { ItemId = 8, Status = EquipmentStatus.Unavailable },
            new EquipmentAvailability { ItemId = 9, Status = EquipmentStatus.Unavailable }
        );

        modelBuilder.Entity<EquipmentComponent>().HasData(
            new EquipmentComponent { ItemId = 6, ChildItemId = 2 },
            new EquipmentComponent { ItemId = 6, ChildItemId = 3 },
            new EquipmentComponent { ItemId = 7, ChildItemId = 4 },
            new EquipmentComponent { ItemId = 7, ChildItemId = 5 },
            new EquipmentComponent { ItemId = 8, ChildItemId = 1 },
            new EquipmentComponent { ItemId = 8, ChildItemId = 6 },
            new EquipmentComponent { ItemId = 8, ChildItemId = 7 },
            new EquipmentComponent { ItemId = 9, ChildItemId = 1 },
            new EquipmentComponent { ItemId = 9, ChildItemId = 6 },
            new EquipmentComponent { ItemId = 9, ChildItemId = 7 }
        );
    }
}
