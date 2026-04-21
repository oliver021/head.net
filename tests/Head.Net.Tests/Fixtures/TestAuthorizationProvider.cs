using Head.Net.Abstractions;

namespace Head.Net.Tests.Fixtures;

public sealed class TestAuthorizationProvider
{
    private int _currentUserId;
    private string _currentUserRole = "user";

    public int CurrentUserId
    {
        get => _currentUserId;
        set => _currentUserId = value;
    }

    public string CurrentUserRole
    {
        get => _currentUserRole;
        set => _currentUserRole = value;
    }

    public HeadAuthorizationContext GetContext()
    {
        return new HeadAuthorizationContext(
            userId: _currentUserId,
            role: _currentUserRole
        );
    }

    public void SetUser(int userId, string role = "user")
    {
        _currentUserId = userId;
        _currentUserRole = role;
    }

    public void ClearUser()
    {
        _currentUserId = 0;
        _currentUserRole = "user";
    }
}
