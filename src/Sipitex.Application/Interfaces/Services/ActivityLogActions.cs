namespace Sipitex.Application.Interfaces.Services;

// Nombres estables de Action / Entity en ActivityLog (no traducir: se filtran por igualdad).
public static class ActivityLogActions
{
    public const string CreateUser = "CreateUser";
    public const string UpdateUser = "UpdateUser";
    public const string ToggleUserStatus = "ToggleUserStatus";
    public const string DeleteUser = "DeleteUser";

    public const string CreateBodega = "CreateBodega";
    public const string UpdateBodega = "UpdateBodega";
    public const string DeleteBodega = "DeleteBodega";

    public const string CreateOrder = "CreateOrder";
    public const string UpdateOrder = "UpdateOrder";
    public const string ApproveOrder = "ApproveOrder";
    public const string CancelOrder = "CancelOrder";

    public const string CreateBom = "CreateBom";
    public const string UpdateBom = "UpdateBom";
    public const string DeleteBom = "DeleteBom";
    public const string AssignBomInstructor = "AssignBomInstructor";
    public const string RemoveBomInstructor = "RemoveBomInstructor";
}

public static class ActivityLogEntities
{
    public const string User = "User";
    public const string Bodega = "Bodega";
    public const string ProductionOrder = "ProductionOrder";
    public const string BomProduct = "BomProduct";
}
