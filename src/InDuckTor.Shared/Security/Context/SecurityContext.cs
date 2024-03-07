namespace InDuckTor.Shared.Security.Context;

public class SecurityContext : ISecurityContext
{
    private readonly Stack<UserContext> _userContextScopes = new(1);

    public UserContext Currant => _userContextScopes.Peek();

    public bool IsImpersonated => _userContextScopes.Count > 0;

    public IDisposable Impersonate(UserContext userContext)
    {
        _userContextScopes.Push(userContext);
        return new UserContextScope(userContext, _userContextScopes);
    }

    private class UserContextScope(UserContext associatedUserContext, Stack<UserContext> scopesStack) : IDisposable
    {
        public void Dispose()
        {
            if (object.ReferenceEquals(associatedUserContext, scopesStack.Peek()))
                throw new InvalidOperationException($"Область видимости для {associatedUserContext} закрыта в неправильном порядке");

            scopesStack.Pop();
        }
    }
}

