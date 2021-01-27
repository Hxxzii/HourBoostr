namespace SingleBoostr.Client
{
    internal enum ErrorCodes : byte
    {
        Success = 0, // Success
        SteamworksFail = 1, // Steam client is NOT open
        ClientFail = 2, // Unknown
        PipeFail = 3, // Related to SteamworksFail, needs more testing
        UserFail = 4, // User doesn't own the app/appId
        AppsFail = 5, // Unknown
        InvalidArguments = 10, // Process called with invalid arguments - should only be returned when user tries to open background exe without the client exe
        InvalidParentProcessId = 11 // Should never return under normal circumstances 
    }
}
