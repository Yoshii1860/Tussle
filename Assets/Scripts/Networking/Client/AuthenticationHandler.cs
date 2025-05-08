using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public static class AuthenticationHandler
{
    public static AuthState AuthState { get; private set; } = AuthState.NotAuthenticated;

    public static async Task<AuthState> AuthenticateAsync(int maxRetries = 5)
    {
        if (AuthState == AuthState.Authenticated)
        {
            Debug.Log("AuthenticationHandler: Already authenticated.");
            return AuthState;
        }

        if(AuthState == AuthState.Authenticating)
        {
            Debug.LogWarning("AuthenticationHandler: Already authenticating. Waiting for completion...");
            await AuthenticatingAsync();
            return AuthState;
        }

        await SignInAnonymouslyAsync(maxRetries);

        
        return AuthState;
    }

    private static async Task SignInAnonymouslyAsync(int maxRetries = 5)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try 
            {
                // Simulate authentication process
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = AuthState.Authenticated;
                    Debug.Log("AuthenticationHandler: Authentication successful.");
                    break;
                }
            }
            catch(AuthenticationException authException)
            {
                Debug.LogError(authException);
                AuthState = AuthState.Failed;
            }
            catch(RequestFailedException requestException)
            {
                Debug.LogError(requestException);
                AuthState = AuthState.Failed;
            }

            await Task.Delay(1000); // Wait for 1 second before retrying
        }

        if (AuthState != AuthState.Authenticated)
        {
            Debug.LogError("AuthenticationHandler: Authentication failed after multiple attempts.");
            AuthState = AuthState.TimeOut;
        }
        else
        {
            Debug.Log("AuthenticationHandler: Authentication completed successfully.");
        }
    }

    private static async Task<AuthState> AuthenticatingAsync()
    {
        while(AuthState == AuthState.Authenticating || AuthState == AuthState.NotAuthenticated)
        {
            Debug.Log("AuthenticationHandler: Authenticating...");
            await Task.Delay(200); // Wait for 1 second before checking again
        }

        return AuthState;
    }
}

public enum AuthState
{
    NotAuthenticated,
    Authenticating,
    Authenticated,
    Failed,
    TimeOut
}
