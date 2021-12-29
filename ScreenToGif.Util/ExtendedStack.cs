using System.Collections.Generic;

namespace ScreenToGif.Util;

public class ExtendedStack<T> : LinkedList<T>
{
    public T Pop()
    {
        var first = First.Value;
            
        RemoveFirst();

        return first;
    }

    public T Peek()
    {
        return First.Value;
    }

    public void Push(T obj)
    {
        AddFirst(obj);
    }


    public T PopBottom()
    {
        var last = Last.Value;

        RemoveLast();

        return last;
    }

    public T PeekBottom()
    {
        return Last.Value;
    }

    public void PushBottom(T obj)
    {
        AddLast(obj);
    }
}