using Spectre.Console;
using VendingMachine.Common.Enums;

namespace VendingMachine.Services.Services;

public class UserService(
    IAnsiConsole ansiConsole)
    : IUserService
{
    public UserRole RequestUserRole()
    {
        var selecteRole = ansiConsole.Prompt(
            new SelectionPrompt<UserRole>()
                .Title("Are you a customer or a vendor:")
                .AddChoices(Enum.GetValues<UserRole>()));

        return selecteRole;
    }
}
