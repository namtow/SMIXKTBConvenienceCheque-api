﻿using SMIXKTBConvenienceCheque.DTOs.Auth;

namespace SMIXKTBConvenienceCheque.Services.Auth
{
    public interface ILoginDetailServices
    {
        string Token { get; }

        string[] Roles { get; }

        string[] Permissions { get; }

        LoginDetailDto GetClaim();

        bool CheckPermission(string permission);

        bool CheckRole(string role);
    }
}