using RestIdentity.Shared.Wrapper;
using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Models.Response;
using RestIdentity.Client.Services.Storage;
using RestIdentity.Client.Infrastructure.Facades.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RestIdentity.Client.Services
{
    internal class AuthStateProvider : AuthenticationStateProvider
    {
        private const string TokenName = "access_token";
        private const string RefreshTokenName = "refresh_token";

        public ClaimsPrincipal AuthenticationStateUser { get; private set; }

        private readonly HttpClient _httpClient;
        private readonly IAuthenticationFacade _authenticationFacade;
        private readonly IUserFacade _userFacade;
        private readonly ILocalStorage _localStorage;

        public AuthStateProvider(HttpClient httpClient, IAuthenticationFacade authenticationFacade, IUserFacade userFacade, ILocalStorage localStorage)
        {
            _httpClient = httpClient;
            _authenticationFacade = authenticationFacade;
            _userFacade = userFacade;
            _localStorage = localStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string token = GetLocalStorageAuthToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                ClaimsPrincipal anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
                AuthenticationStateUser = anonymousUser;

                return new AuthenticationState(anonymousUser);
            }
            
            MarkUserAsAuthenticated(token);

            return new AuthenticationState(AuthenticationStateUser);
        }

        public async Task<IResult> MarkUserAsLoggedOut()
        {
            IResult logoutResult = await _authenticationFacade.LogoutAsync();

            ClaimsPrincipal anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            Task<AuthenticationState> authState = Task.FromResult(new AuthenticationState(anonymousUser));

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer");

            _localStorage.RemoveItem(TokenName);
            _localStorage.RemoveItem(RefreshTokenName);

            AuthenticationStateUser = anonymousUser;
            NotifyAuthenticationStateChanged(authState);

            return logoutResult;
        }

        public async Task<IResult> LoginAsync(LoginRequest loginRequest)
        {
            IResult<TokenResponse> loginResult = await _authenticationFacade.LoginAsync(loginRequest);

            if (!loginResult.Succeeded)
                return loginResult;

            _localStorage.SetItem(TokenName, loginResult.Data.Token);
            _localStorage.SetItem(RefreshTokenName, loginResult.Data.RefreshToken.Token);

            MarkUserAsAuthenticated(loginResult.Data.Token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

            return loginResult;
        }

        public async Task<IResult> RegisterAsync(RegisterRequest registerRequest)
        {
            IResult registerResult = await _userFacade.RegisterUserAsync(registerRequest);

            return registerResult;
        }

        private string GetLocalStorageAuthToken()
        {
            return _localStorage.GetItem(TokenName);
        }

        private void MarkUserAsAuthenticated(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Console.WriteLine("wow");
            Console.WriteLine(token);
            Console.WriteLine(_httpClient.DefaultRequestHeaders.Authorization);

            AuthenticationStateUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
        }

        private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            List<Claim> claims = new List<Claim>();
            string payload = jwt.Split('.')[1];
            byte[] jsonBytes = ParseBase64WithoutPadding(payload);
            Dictionary<string, object> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            keyValuePairs.TryGetValue(ClaimTypes.Role, out object roles);

            if (roles != null)
            {
                if (roles.ToString().Trim().StartsWith("["))
                {
                    string[] parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString());

                    foreach (string parsedRole in parsedRoles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, parsedRole));
                    }
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
                }

                keyValuePairs.Remove(ClaimTypes.Role);
            }

            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString())));

            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}

