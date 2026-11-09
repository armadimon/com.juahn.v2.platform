using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Juahn.V2.Platform;
using NUnit.Framework;

public class PlatformTests
{
    private sealed class Sink : IAnalyticsSink
    {
        public int Calls;public bool Throw;
        public void Track(AnalyticsEvent e){Calls++;if(Throw)throw new Exception("vendor failed");}
    }
    [Test] public void EventsCopyApplicationFields()
    {var fields=new Dictionary<string,string>{{"level","1"}};var e=new AnalyticsEvent("enter",fields);fields["level"]="2";Assert.That(e.Fields["level"],Is.EqualTo("1"));}
    [Test] public void OneVendorFailureDoesNotBlockAnotherVendor()
    {
        var a=new Sink{Throw=true};var b=new Sink();var errors=new List<Exception>();
        new CompositeAnalyticsSink(new[]{a,b},(_,e)=>errors.Add(e)).Track(new AnalyticsEvent("enter"));
        Assert.That(b.Calls,Is.EqualTo(1));Assert.That(errors.Count,Is.EqualTo(1));
    }
    [Test] public void ErrorReporterRunsAfterAllVendors()
    {
        var a=new Sink{Throw=true};var b=new Sink();
        Assert.Throws<Exception>(()=>new CompositeAnalyticsSink(new[]{a,b},(_,e)=>throw e).Track(new AnalyticsEvent("enter")));
        Assert.That(b.Calls,Is.EqualTo(1));
    }
    [Test] public async Task MissingAdProviderNeverReportsRewardCompletion()
    {var ads=new UnavailableRewardedAdProvider();Assert.That(ads.IsAvailable("test"),Is.False);Assert.That((await ads.ShowAsync("test")).Status,Is.EqualTo(RewardedAdStatus.Unavailable));}
    [Test] public void AdCancellationIsObserved()
    {Assert.Catch<OperationCanceledException>(()=>new UnavailableRewardedAdProvider().ShowAsync("test",new CancellationToken(true)));}
    [Test] public void CredentialsAreNotExposedByDefaultLogging()
    {Assert.That(new AuthCredential("guest","secret").ToString(),Does.Not.Contain("secret"));}
    [Test] public void ReceiptReceivedRequiresActualReceiptAndTransaction()
    {Assert.Throws<ArgumentException>(()=>new PurchaseResult(PurchaseStatus.ReceiptReceived,"item"));}
    [Test] public void PendingPurchaseRemainsPending()
    {Assert.That(new PurchaseResult(PurchaseStatus.Pending,"item").Status,Is.EqualTo(PurchaseStatus.Pending));}
}
