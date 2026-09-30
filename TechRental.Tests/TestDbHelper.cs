using TechRental.Data;
using TechRental.Models;
using Microsoft.EntityFrameworkCore;

namespace TechRental.Tests;

public static class TestDbHelper
{
    public static MenuContext CreateContext(string dbName = "")
    {
        if (string.IsNullOrEmpty(dbName))
            dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<MenuContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var context = new MenuContext(options);
        Seed(context);
        return context;
    }

    private static void Seed(MenuContext context)
    {
        var items = new List<EquipmentItem>
        {
            new() { ItemId = 1, ItemName = "NOTEBOOK DELL",    Type = EquipmentItemType.Equipment   },
            new() { ItemId = 2, ItemName = "MOUSE LOGITECH",         Type = EquipmentItemType.Equipment   },
            new() { ItemId = 3, ItemName = "MOUSE MICROSOFT",       Type = EquipmentItemType.Equipment   },
            new() { ItemId = 4, ItemName = "HEADSET JBL",        Type = EquipmentItemType.Equipment   },
            new() { ItemId = 5, ItemName = "WEBCAM LOGITECH",      Type = EquipmentItemType.Equipment   },
            new() { ItemId = 6, ItemName = "MOUSE OPTION", Type = EquipmentItemType.Option    },
            new() { ItemId = 7, ItemName = "ACCESSORY OPTION",  Type = EquipmentItemType.Option    },
            new() { ItemId = 8, ItemName = "DEVELOPER KIT", Type = EquipmentItemType.Kit },
            new() { ItemId = 9, ItemName = "HOME OFFICE KIT",   Type = EquipmentItemType.Kit },
        };

        var statuses = new List<EquipmentAvailability>
        {
            new() { ItemId = 1, Status = EquipmentStatus.Available   },
            new() { ItemId = 2, Status = EquipmentStatus.Unavailable },
            new() { ItemId = 3, Status = EquipmentStatus.Available   },
            new() { ItemId = 4, Status = EquipmentStatus.Available   },
            new() { ItemId = 5, Status = EquipmentStatus.Available   },
            new() { ItemId = 6, Status = EquipmentStatus.Unavailable },
            new() { ItemId = 7, Status = EquipmentStatus.Unavailable },
            new() { ItemId = 8, Status = EquipmentStatus.Unavailable },
            new() { ItemId = 9, Status = EquipmentStatus.Unavailable },
        };

        var components = new List<EquipmentComponent>
        {
            new() { ItemId = 6, ChildItemId = 2 }, // MOUSE OPTION -> MOUSE LOGITECH
            new() { ItemId = 6, ChildItemId = 3 }, // MOUSE OPTION -> MOUSE MICROSOFT
            new() { ItemId = 7, ChildItemId = 4 }, // ACCESSORY OPTION  -> HEADSET JBL
            new() { ItemId = 7, ChildItemId = 5 }, // ACCESSORY OPTION  -> WEBCAM LOGITECH
            new() { ItemId = 8, ChildItemId = 1 }, // DEVELOPER KIT -> NOTEBOOK DELL
            new() { ItemId = 8, ChildItemId = 6 }, // DEVELOPER KIT -> MOUSE OPTION
            new() { ItemId = 8, ChildItemId = 7 }, // DEVELOPER KIT -> ACCESSORY OPTION
            new() { ItemId = 9, ChildItemId = 1 }, // HOME OFFICE KIT   -> NOTEBOOK DELL
            new() { ItemId = 9, ChildItemId = 6 }, // HOME OFFICE KIT   -> MOUSE OPTION
            new() { ItemId = 9, ChildItemId = 7 }, // HOME OFFICE KIT   -> ACCESSORY OPTION
        };

        context.EquipmentItems.AddRange(items);
        context.EquipmentAvailabilities.AddRange(statuses);
        context.EquipmentComponents.AddRange(components);
        context.SaveChanges();
    }
}
