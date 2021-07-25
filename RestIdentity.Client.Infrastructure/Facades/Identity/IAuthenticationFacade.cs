﻿using RestIdentity.Shared.Models.Requests;
using RestIdentity.Shared.Wrapper;
using System.Threading.Tasks;

namespace RestIdentity.Client.Infrastructure.Managers.Identity
{
    public interface IAuthenticationFacade
    {
        Task<IResult> LoginAsync(LoginRequest loginRequest);

        Task<IResult> LoginWith2faAsync(LoginWith2faRequest loginWith2faRequest);

        Task<IResult> LoginWithRecoveryCodeAsync(LoginWithRecoveryCodeRequest loginWithRecoveryCodeRequest);

        Task<IResult> LogoutAsync();
    }
}
