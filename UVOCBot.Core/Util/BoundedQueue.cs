﻿using System;
using System.Collections.Generic;

namespace UVOCBot.Core.Util;

/// <summary>
/// A <see cref="Queue{T}"/> derivative that will only ever grow to a specified maximum size, then start dequeuing items
/// </summary>
/// <typeparam name="T"></typeparam>
public class BoundedQueue<T> : Queue<T>
{
    private readonly int _maxSize;

    public BoundedQueue(int maxSize)
    {
        if (maxSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "Must be greater than zero");

        _maxSize = maxSize;
    }

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        if (Count == _maxSize)
            Dequeue();
    }
}
