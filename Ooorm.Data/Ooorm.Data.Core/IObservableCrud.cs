﻿using System;

namespace Ooorm.Data.Core
{
    public interface IObservableCrud<T> : ICrudRepository<T> where T : IDbItem
    {
        event Action<T> OnCreated;
        event Action<T,T> OnUpdated;
        event Action<int> OnDeleted;
    }
}
