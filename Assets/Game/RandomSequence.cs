using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class RandomSequence
{
    private readonly List<int> list;
    public RandomSequence(int num)
    {
        list = Enumerable.Range(0, num).ToList();
    }
    public int Get()
    {
        int i = Random.Range(0, list.Count), j = list[i];
        list.RemoveAt(i);
        return j;
    }
}
