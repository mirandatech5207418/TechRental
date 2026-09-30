namespace TechRental.DTOs;

public record EquipmentItemDto(
    int ItemId,
    string ItemName,
    string Type,
    string Status
);

public record CreateEquipmentItemRequest(
    string ItemName,
    int Type
);

public record UpdateEquipmentItemRequest(
    string ItemName,
    int Type
);

public record ChangeStatusRequest(
    int Status
);

public record AddComponentRequest(
    int ChildItemId
);

public record EquipmentItemDetailDto(
    int ItemId,
    string ItemName,
    string Type,
    string Status,
    IEnumerable<EquipmentItemDto> Components
);
