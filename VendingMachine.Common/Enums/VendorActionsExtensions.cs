namespace VendingMachine.Common.Enums;

public static class VendorActionsExtensions
{
    public static string StringifyAction(this VendorActions action) => 
        action.ToString().Replace('_', ' ');
}
