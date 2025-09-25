using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Interfaces
{
    public interface ISaveProvider
    {
        IPromise Save(string data);
        IPromise<string> Load();
    }
}