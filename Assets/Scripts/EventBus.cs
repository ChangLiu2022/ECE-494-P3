using System;
using System.Collections.Generic;


// usage:
//   EventBus.Subscribe<AlertEvent>(OnAlert);
//   EventBus.Publish(new AlertEvent());
//   EventBus.Unsubscribe<AlertEvent>(OnAlert);
public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _events = new();


    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_events.TryGetValue(type, out var existing))
            _events[type] = Delegate.Combine(existing, handler);
        else
            _events[type] = handler;
    }


    public static void Unsubscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (_events.TryGetValue(type, out var existing))
        {
            var result = Delegate.Remove(existing, handler);
            if (result == null)
                _events.Remove(type);
            else
                _events[type] = result;
        }
    }


    public static void Publish<T>(T evt)
    {
        if (_events.TryGetValue(typeof(T), out var d))
            ((Action<T>)d)?.Invoke(evt);
    }


    // call on scene load / restart to avoid stale references
    public static void Clear() => _events.Clear();
}