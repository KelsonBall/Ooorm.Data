﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Ooorm.Data.Core
{
    public interface ICrudRepository
    {
        Task<int> CreateTable();
        Task<int> DropTable();
        Task<IEnumerable<object>> ReadUntyped();
    }

    public interface ICrudRepository<T> : ICrudRepository where T : IDbItem
    {
        Task<int> Write(params T[] values);
        IAsyncEnumerable<T> Read();
        Task<T> Read(int id);
        IAsyncEnumerable<T> Read(Expression<Func<T, bool>> predicate);
        IAsyncEnumerable<T> Read<TParam>(Expression<Func<T, TParam, bool>> predicate, TParam param);
        Task<int> Update(params T[] values);
        Task<int> Delete(params T[] values);
        Task<int> Delete(Expression<Func<T, bool>> predicate);
        Task<int> Delete<TParam>(Expression<Func<T, TParam, bool>> predicate, TParam param);
    }
}
