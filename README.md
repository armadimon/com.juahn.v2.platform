# Juahn V2 Platform Contracts

Pure C# / .NET Standard 2.1 / Unity 6000.0+ / MIT. No SDK is installed or initialized by this package.

Application code depends on `IAnalyticsSink`, `IRewardedAdProvider`, `IAuthProvider` and `IPurchaseProvider`. Consumer-specific Firebase/MAX/Apple/Google adapters implement these contracts in separate assemblies and remain outside this package. Product IDs, placements, event names, game rewards, account linkage and backend verification remain owned by the game.

`AnalyticsEvent` copies its string fields. `CompositeAnalyticsSink` delivers to every configured vendor before reporting failures; one vendor cannot suppress delivery to others. `NoOpAnalyticsSink` is available for optional analytics. `UnavailableRewardedAdProvider` reports unavailable, never successful reward completion.

`RewardedAdStatus.Completed` only describes the platform callback. `PurchaseStatus.ReceiptReceived` requires transaction evidence but is not game reward authorization. Persist and verify transactions under application policy before calling ConfirmAsync. There is no fake successful auth or purchase fallback. `AuthCredential.ToString()` omits the credential.

Implement SDK startup/shutdown as optional or required `IAsyncModule` application adapters. Tests use local providers without external accounts or service calls.

Run `dotnet test Tests~/dotnet/Tests.csproj` and `dotnet build Tools~/compile-check/Runtime.csproj`.
