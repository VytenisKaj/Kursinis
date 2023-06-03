using Kursinis.Enums;

namespace Kursinis.Services.AuthorizationHelper
{
    public class AuthorizationHelper : IAuthorizationHelper
    {
        public bool IsAuthenticated => throw new NotImplementedException();

        public int WorkPlaceId => throw new NotImplementedException();

        public int UserId => throw new NotImplementedException();

        public bool HasPermission(Permissions permission)
        {
            throw new NotImplementedException();
        }
    }
}
