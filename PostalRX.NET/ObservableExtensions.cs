﻿using Postal.NET;
using System;
using System.Collections.Generic;

namespace PostalRX.NET
{
    public static class ObservableExtensions
    {
        class DisposableObserver : IDisposable, IObserver<Envelope>
        {
            private readonly IObserver<Envelope> observer;
            private readonly ICollection<IObserver<Envelope>> subscribers;

            public DisposableObserver(IObserver<Envelope> observer, ICollection<IObserver<Envelope>> subscribers)
            {
                this.observer = observer;
                this.subscribers = subscribers;
            }

            public void Dispose()
            {
                this.OnCompleted();
                this.subscribers.Remove(this.observer);
            }

            public void OnNext(Envelope value)
            {
                this.observer.OnNext(value);
            }

            public void OnError(Exception error)
            {
                this.observer.OnError(error);
            }

            public void OnCompleted()
            {
                this.observer.OnCompleted();
            }
        }

        class PostalObservable : IObservable<Envelope>, IDisposable
        {
            private readonly LinkedList<IObserver<Envelope>> subscribers = new LinkedList<IObserver<Envelope>>();

            private readonly IBox box;
            private readonly string channel;
            private readonly string topic;
            private IDisposable subscription;

            public PostalObservable(IBox box, string channel, string topic)
            {
                this.box = box;
                this.channel = channel;
                this.topic = topic;
                this.subscription = this.box.Subscribe(this.channel, this.topic, this.Notification);
            }

            private void Notification(Envelope env)
            {
                foreach (var observer in this.subscribers)
                {
                    observer.OnNext(env);
                }
            }

            public IDisposable Subscribe(IObserver<Envelope> observer)
            {
                subscribers.AddLast(observer);

                return new DisposableObserver(observer, this.subscribers);
            }

            public void Dispose()
            {
                foreach (var observer in this.subscribers)
                {
                    observer.OnCompleted();
                }

                this.subscribers.Clear();
                this.subscription.Dispose();
            }
        }

        public static IObservable<Envelope> Observe(this IBox box, string channel, string topic)
        {
            return new PostalObservable(box, channel, topic);
        }

        public static IDisposable Observe(this IBox box, string channel, string topic, IObserver<Envelope> observer)
        {
            return new PostalObservable(box, channel, topic).Subscribe(observer);
        }
    }
}