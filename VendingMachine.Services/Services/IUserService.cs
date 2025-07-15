using VendingMachine.Common.Enums;

namespace VendingMachine.Services.Services;

public interface IUserService
{
    public UserRole RequestUserRole();
}
