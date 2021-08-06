namespace RestIdentity.Client
{
    internal static class PageUrls
    {
        public const string Home = "/";

        private const string Auth = "/auth";
        public const string SignIn = Auth + "/signin";
        public const string TwoFactorSignIn = Auth + "/2fa";
        public const string SignOut = Auth + "/signout";
        public const string Register = Auth + "/register";
        public const string Profile = Auth + "/profile";
        
        public const string ForgotPassword = "/forgotpassword";

        public const string Terms = "/terms";
        public const string Conditions = "/conditions";
    }
}
