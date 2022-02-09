using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper class for generating incrementing indexes
/// </summary>
public class IndexGenerator
{
    int index = -1;

    public int GetNewIndex()
    {
        index++;
        return index;
    }

    public void SetIndex(int index)
    {
        this.index = index;
    }
}
