## Dynamic Data

Dynamic data is a portable class library which brings the power of reactive (rx) to collections.  

A collection which mutates can have adds, updates and removes (plus moves and re-evaluates but more about that another time). Out of the box rx does nothing to manage any changes in a collection which is why Dynamic Data exists.  In Dynamic Data collection changes are notified via an observable change set which is the heart of the system.  An operator receives these notifications and then applies some logic and subsequently provides it's own notifications. In this way operators can be chained together to apply powerful and often very complicated operations with some very simple fluent code.

The benefit of at least 50 operators which are borne from pragmatic experience is that the management of in-memory data becomes easy and it is no exaggeration to say it can save thousands of lines of code by abstracting complicated and often repetitive operations.

### Why is the first Nuget release version 3
Even before rx existed I had implemented a similar concept using old fashioned events but the code was very ugly and my implementation full of race conditions so it never existed outside of my own private sphere. My second attempt was a similar implementation to the first but using rx when it first came out. This also failed as my understanding of rx was flawed and limited and my design forced consumers to implement interfaces.  Then finally I got my design head on and in 2011-ish I started writing what has become dynamic data. No inheritance, no interfaces, just the ability to plug in and use it as you please.  All along I meant to open source it but having so utterly failed on my first 2 attempts I decided to wait until the exact design had settled down. The wait lasted longer than I expected and end up taking over 2 years but the benefit is it has been trialled for 2 years on a very busy high volume low latency trading system which has seriously complicated data management. And what's more that system has gathered a load of attention for how slick and cool and reliable it is both from the user and IT point of view. So I present this library with the confidence of it being tried, tested, optimised and mature. I hope it can make your life easier like it has done for me.

### Version 4 is under development

[![Build status]( https://ci.appveyor.com/api/projects/status/occtlji3iwinami5/branch/develop?svg=true)](https://ci.appveyor.com/project/RolandPheasant/DynamicData/branch/develop)

The core of dynamic data is an observable cache which for most circumstances is great but sometimes there is the need simply for an observable list. Version 4 will deliver this.

The aim is the observable list will have as comprehensive a set of operators as the observable cache. It is now available from Nuget as a pre-release product.


### I've seen it before so give me some links

- [![Join the chat at https://gitter.im/RolandPheasant/DynamicData](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/RolandPheasant/DynamicData?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
- [![Downloads](https://img.shields.io/nuget/dt/DynamicData.svg)](http://www.nuget.org/packages/DynamicData/)	
- Sample wpf project https://github.com/RolandPheasant/TradingDemo
- Blog at  http://dynamic-data.org/
- You can contact me on twitter  [@RolandPheasant](https://twitter.com/RolandPheasant) or email at [roland@dynamic-data.org]

### Getting Started

As stated in the blurb, dynamic data is based on the concept of an observable change set.  The easiest way to create one is directly from an observable.
```csharp
IObservable<T> myObservable;
IObservable<IEnumerable<T>> myObservable;
//1. This option will create an observable where item's are identified using the hash code.
var mydynamicdatasource = myObservable.ToObservableChangeSet();
//2. Or specify a key like this
var mydynamicdatasource = myObservable.ToObservableChangeSet(t=> key);
```
The problem with the above is the collection will grow forever so there are overloads to specify size limitation or expiry times (not shown). 

To have much more control over the root collection then we need a local data store which has the requisite crud methods. Like the above the cache can be created with or without specifying a key
```csharp
//1. Create a cache where item's are identified using the hash code.
var mycache  = new SourceCache<TObject>();
//2. Or specify a key like this
var mycache  = new SourceCache<TObject,TKey>(t => key);
```
Another way is to directly from an observable collection, you can do this
```csharp
var myobservablecollection= new ObservableCollection<T>();
// Use the hashcode for the key
var mydynamicdatasource = myobservablecollection.ToObservableChangeSet();
// or specify a key like this
var mydynamicdatasource = myobservablecollection.ToObservableChangeSet(t => t.Key);
```
This method is only recommended for simple queries which act only on the UI thread as ```ObservableCollection``` is not thread safe.

One other point worth making here is any steam can be covered to as cache.
```csharp
var mycache = somedynamicdatasource.AsObservableCache();
```
This cache has the same connection methods as a source cache but is read only.

### Now lets the games begin

Phew, got the boring stuff out of the way with so a few quick fire examples based on the assumption that we already have an observable change set. In all of these examples the resulting sequences always exactly reflect the items is the cache i.e. adds, updates and removes are always propagated.

**Example 1:** filters a stream of live trades, creates a proxy for each trade and order the result by most recent first. As the source is modified the observable collection will automatically reflect changes.

```csharp
//Dynamic data has it's own take on an observable collection (optimised for populating f
var list = new ObservableCollectionExtended<TradeProxy>();
var myoperation = somedynamicdatasource
					.Filter(trade=>trade.Status == TradeStatus.Live) 
					.Transform(trade => new TradeProxy(trade))
					.Sort(SortExpressionComparer<TradeProxy>.Descending(t => t.Timestamp))
					.ObserveOnDispatcher()
					.Bind(list) 
					.DisposeMany()
					.Subscribe()
```
Oh and I forgot to say, ```TradeProxy``` is disposable and DisposeMany() ensures items are disposed when no longer part of the stream.

**Example 2:** produces a stream which is grouped by status. If an item's changes status it will be moved to the new group and when a group has no items the group will automatically be removed.
```csharp
var myoperation = somedynamicdatasource
            .Group(trade=>trade.Status) //This is NOT Rx's GroupBy 
			.Subscribe(changeSet=>//do something with the groups)
```
**Example 3:** Suppose I am editing some trades and I have an observable on each trades which validates but I want to know when all items are valid then this will do the job.
```csharp
IObservable<bool> allValid = somedynamicdatasource
                .TrueForAll(trade => trade.IsValidObservable, (trade, isvalid) => isvalid)
```
This operator flattens the observables and returns the combined state in one line of code. I love it.

**Example 4:**  will wire and un-wire items from the observable when they are added, updated or removed from the source.
```csharp
var myoperation = somedynamicdatasource.Connect() 
			.MergeMany(trade=> trade.ObservePropertyChanged(t=>t.Amount))
			.Subscribe(ObservableOfAmountChangedForAllItems=>//do something with IObservable<PropChangedArg>)
```
### Want to know more?
I could go on endlessly but this is not the place for full documentation.  I promise this will come but for now I suggest downloading my WPF sample app (links above)  as I intend it to be a 'living document' and I promise it will be continually maintained. 

Also if you following me on Twitter you will find out when new samples or blog posts have been updated.

Additionally if you have read up to here and not pressed star then why not? Ha. A star may make me be more responsive to any requests or queries.

### Before you sign off, tell me a little more about the changeset?

Simple, any change to a collection can be represented using a change set where the change set is a collection of changed items as follows.

```csharp
	//NB Exact implementation is  simplified 
	public interface IChangeSet<TObject,  TKey> : IEnumerable<Change<TObject, TKey>>
    {
    }

	//and the change is something like this
	public struct Change<TObject, TKey>
	{
		public ChangeReason Reason {get;}
		public TKey Key {get;}
		public TObject Current {get;}
		public Optional<TObjec>t Previous {get;}
	}
```
This structure is observed like this ```IObservable<IChangeSet<TObject,  TKey>>``` and voila, we can start building operators around this idea.
