using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Acts like a stack but allows to remove values from anywhere
/// </summary>
public class StackPlus<T>
{
    private List<T> items = new List<T>();
    
    public void Push(T item)
    {
        items.Add(item);
    }

    public T Pop()
    {
        if(items.Count < 1)
            return default(T);

        T item = items[items.Count-1];
        items.RemoveAt(items.Count-1);

        return item;
    }

    public T Peek()
    {
        return items[items.Count-1];
    }

    public void RemoveAt(int index)
    {
        items.RemoveAt(index);
    }

    public void Remove(T item)
    {
        items.Remove(item);
    }

    public void Clear()
    {
        items.Clear();
    }
}
