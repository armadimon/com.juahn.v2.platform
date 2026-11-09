using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Juahn.V2.Platform
{
    /// <summary>Application-defined event names and fields. Vendor adapters translate this envelope.</summary>
    public sealed class AnalyticsEvent
    {
        public string Name { get; }
        public IReadOnlyDictionary<string, string> Fields { get; }
        public AnalyticsEvent(string name, IReadOnlyDictionary<string, string> fields = null)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("An event name is required.", nameof(name));
            Name = name;
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            if (fields != null) foreach (var pair in fields) copy.Add(pair.Key, pair.Value);
            Fields = new ReadOnlyDictionary<string, string>(copy);
        }
    }
    public interface IAnalyticsSink { void Track(AnalyticsEvent value); }
    public sealed class NoOpAnalyticsSink : IAnalyticsSink { public void Track(AnalyticsEvent value) { } }
    /// <summary>One optional vendor failure does not suppress delivery to the remaining vendors.</summary>
    public sealed class CompositeAnalyticsSink : IAnalyticsSink
    {
        private readonly IAnalyticsSink[] _sinks;
        private readonly Action<IAnalyticsSink, Exception> _onError;
        public CompositeAnalyticsSink(IEnumerable<IAnalyticsSink> sinks, Action<IAnalyticsSink, Exception> onError = null)
        {
            _sinks = new List<IAnalyticsSink>(sinks ?? throw new ArgumentNullException(nameof(sinks))).ToArray();
            if (Array.Exists(_sinks, sink => sink == null)) throw new ArgumentException("A sink is null.", nameof(sinks));
            _onError = onError;
        }
        public void Track(AnalyticsEvent value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var errors = new List<KeyValuePair<IAnalyticsSink, Exception>>();
            foreach (var sink in _sinks)
                try { sink.Track(value); } catch (Exception error) { errors.Add(new KeyValuePair<IAnalyticsSink, Exception>(sink, error)); }
            if (_onError != null) foreach (var failure in errors) _onError(failure.Key, failure.Value);
        }
    }
    public enum RewardedAdStatus { Unavailable, Completed, Canceled, Failed }
    /// <summary>Completed reports the SDK callback, not authorization to grant game rewards.</summary>
    public readonly struct RewardedAdResult
    {
        public RewardedAdStatus Status { get; }
        public string ProviderTransactionId { get; }
        public RewardedAdResult(RewardedAdStatus status, string providerTransactionId = null)
        { Status = status; ProviderTransactionId = providerTransactionId; }
    }
    public interface IRewardedAdProvider
    {
        bool IsAvailable(string placement);
        Task<RewardedAdResult> ShowAsync(string placement, CancellationToken token = default);
    }
    public sealed class UnavailableRewardedAdProvider : IRewardedAdProvider
    {
        public bool IsAvailable(string placement) => false;
        public Task<RewardedAdResult> ShowAsync(string placement, CancellationToken token = default)
        { token.ThrowIfCancellationRequested(); return Task.FromResult(new RewardedAdResult(RewardedAdStatus.Unavailable)); }
    }
    public sealed class AuthCredential
    {
        public string Provider { get; }
        public string Credential { get; }
        public AuthCredential(string provider, string credential)
        {
            if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(credential)) throw new ArgumentException("Provider and credential are required.");
            Provider = provider; Credential = credential;
        }
        public override string ToString() => Provider + " credential [redacted]";
    }
    /// <summary>Platform credential acquisition only. Account linking/session authority belongs to the backend.</summary>
    public interface IAuthProvider { Task<AuthCredential> AuthenticateAsync(CancellationToken token = default); }
    public enum PurchaseStatus { Unavailable, ReceiptReceived, Pending, Canceled, Failed }
    /// <summary>A receipt is evidence for verification, not proof that a reward may be granted.</summary>
    public sealed class PurchaseResult
    {
        public PurchaseStatus Status { get; }
        public string ProductId { get; }
        public string TransactionId { get; }
        public string Receipt { get; }
        public PurchaseResult(PurchaseStatus status, string productId, string transactionId = null, string receipt = null)
        {
            if (string.IsNullOrWhiteSpace(productId)) throw new ArgumentException("A product ID is required.", nameof(productId));
            if (status == PurchaseStatus.ReceiptReceived && (string.IsNullOrWhiteSpace(transactionId) || string.IsNullOrWhiteSpace(receipt)))
                throw new ArgumentException("ReceiptReceived requires a transaction ID and receipt.");
            Status = status; ProductId = productId; TransactionId = transactionId; Receipt = receipt;
        }
    }
    public interface IPurchaseProvider
    {
        Task<PurchaseResult> PurchaseAsync(string productId, CancellationToken token = default);
        /// <summary>Called by application policy after verification and durable transaction recording.</summary>
        Task ConfirmAsync(string transactionId, CancellationToken token = default);
    }
}
