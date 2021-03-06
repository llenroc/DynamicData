using System;
using System.Reactive.Linq;
using DynamicData.Annotations;

namespace DynamicData.Internal
{
	internal class MergeMany<T, TDestination>
	{
		private readonly IObservable<IChangeSet<T>> _source;
		private readonly Func<T, IObservable<TDestination>> _observableSelector;

		public MergeMany([NotNull] IObservable<IChangeSet<T>> source,
			[NotNull] Func<T, IObservable<TDestination>> observableSelector)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (observableSelector == null) throw new ArgumentNullException("observableSelector");

			_source = source;
			_observableSelector = observableSelector;
		}
		
		public IObservable<TDestination> Run()
		{
			return Observable.Create<TDestination>
				(
					observer =>
					{
						var locker = new object();
						return _source.SubscribeMany(t => _observableSelector(t).Synchronize(locker).SubscribeSafe(observer))
						.Subscribe(t => { }, observer.OnError);

					});
		}
	}
}