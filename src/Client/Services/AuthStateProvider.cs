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
            string token = "";
            if (string.IsNullOrWhiteSpace(token))
            {
                ClaimsPrincipal anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
                AuthenticationStateUser = anonymousUser;

                return new AuthenticationState(anonymousUser);
            }
            
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
            IResult loginResult = await _authenticationFacade.LoginAsync(loginRequest);

            if (!loginResult.Succeeded)
                return loginResult;

            var userClaims = (await _userFacade.GetMeAsync()).Data.Claims.Select(x => new Claim(x.Key, x.Value));
            AuthenticationStateUser = new ClaimsPrincipal(new ClaimsIdentity(userClaims));

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

            return loginResult;
        }

        public async Task<IResult> RegisterAsync(RegisterRequest registerRequest)
        {
            IResult registerResult = await _userFacade.RegisterUserAsync(registerRequest);

            return registerResult;
        }
    }
}

