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
            return AuthState;
        }

        if (AuthState == AuthState.Authenticating)
        {
            await AuthenticatingAsync();
            return AuthState;
        }

        Debug.Log("AuthenticationHandler: Starting anonymous authentication...");
        await SignInAnonymouslyAsync(maxRetries);

        if (AuthState == AuthState.Authenticated)
        {
            Debug.Log("AuthenticationHandler: Authentication succeeded.");
        }
        else if (AuthState == AuthState.TimeOut)
        {
            Debug.LogError("AuthenticationHandler: Authentication timed out after multiple attempts.");
        }
        else if (AuthState == AuthState.Failed)
        {
            Debug.LogError("AuthenticationHandler: Authentication failed.");
        }

        return AuthState;
    }

    private static async Task SignInAnonymouslyAsync(int maxRetries = 5)
    {
        AuthState = AuthState.Authenticating;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = AuthState.Authenticated;
                    break;
                }
            }
            catch (AuthenticationException authException)
            {
                Debug.LogWarning($"AuthenticationHandler: AuthenticationException on attempt {i + 1}: {authException.Message}");
                AuthState = AuthState.Failed;
            }
            catch (RequestFailedException requestException)
            {
                Debug.LogWarning($"AuthenticationHandler: RequestFailedException on attempt {i + 1}: {requestException.Message}");
                AuthState = AuthState.Failed;
            }

            await Task.Delay(1000);
        }

        if (AuthState != AuthState.Authenticated)
        {
            AuthState = AuthState.TimeOut;
        }
    }

    private static async Task<AuthState> AuthenticatingAsync()
    {
        while (AuthState == AuthState.Authenticating || AuthState == AuthState.NotAuthenticated)
        {
            await Task.Delay(200);
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