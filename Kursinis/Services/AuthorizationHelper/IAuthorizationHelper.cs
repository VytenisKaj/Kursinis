using Kursinis.Enums;

namespace Kursinis.Services.AuthorizationHelper
{
    public interface IAuthorizationHelper
    {
        bool IsAuthenticated { get; }

        bool HasPermission(Permissions permission);

        int WorkPlaceId { get; }

        int UserId { get; }
    }
}
