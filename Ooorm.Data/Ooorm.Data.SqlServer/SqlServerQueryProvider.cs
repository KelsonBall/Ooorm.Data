﻿using Ooorm.Data.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Ooorm.Data.SqlServer
{
    internal class SqlServerQueryProvider : IDatabaseManagementQueryProvider
    {
        public string DatabaseSql(string name)
            => $"USE MASTER; IF db_id('{name}') is null CREATE DATABASE [{name}];";

        public string DropDatabaseSql(string name)
            => $@"USE[master];
IF db_id('{name}') is not null
BEGIN
DECLARE @kill varchar(8000) = '';
SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'
FROM sys.dm_exec_sessions
WHERE database_id = db_id('{name}')
EXEC(@kill);
EXEC('DROP DATABASE [{name}]');
END;";
    }

    internal class SqlServerQueryProvider<T, TId> : SqlServerQueryProvider, QueryProviders.IQueryProvider<T, TId> 
        where T : DbItem<T, TId> 
        where TId : struct, IEquatable<TId>
    {
        protected readonly IExtendableTypeResolver types;

        public SqlServerQueryProvider(Func<IDatabase> db) => types = new SqlServerTypeProvider(db);

        protected static readonly Column[] COLUMNS = typeof(T).GetColumns().ToArray();
        protected static readonly Column[] NON_ID_COLUMNS = typeof(T).GetColumns(exceptId: true).ToArray();
        protected static readonly Column ID_COLUMN = COLUMNS.Single(c => c.Info.HasAttribute<IdAttribute>() || c.PropertyName == nameof(DbItem<T,TId>.ID));
        protected static readonly string TABLE = typeof(T).HasAttribute<TableAttribute>() ? $"[{typeof(T).GetCustomAttribute<TableAttribute>().Value}]" : $"[{typeof(T).Name}]";
        protected static readonly string WHERE_ID = $"WHERE [{nameof(DbItem<T, TId>.ID)}] = @Id;";
        protected static readonly string DELETE_PREFIX = $"DELETE FROM {TABLE} ";
        protected static readonly string DELETE_WHERE_ID = DELETE_PREFIX + WHERE_ID;
        protected static readonly string UPDATE_PREFIX = $"UPDATE {TABLE} SET ";
        protected static readonly string COLUMNS_LIST = string.Join(", ", COLUMNS.Select(c => $"[{c.ColumnName}]"));
        protected static readonly string READ_PREFIX = $"SELECT {COLUMNS_LIST} FROM {TABLE} ";
        protected static readonly string READ_WHERE_ID = READ_PREFIX + WHERE_ID;
        protected static readonly string WRITE_SQL = $"INSERT INTO {TABLE} ({string.Join(", ", NON_ID_COLUMNS.Select(c => $"[{c.ColumnName}]"))}) OUTPUT INSERTED.* VALUES ({string.Join(", ", NON_ID_COLUMNS.Select(c => $"@{c.ColumnName}"))});";

        public string DeleteSqlById()
            => DELETE_WHERE_ID;

        public string DeleteSql(Expression<Func<T>> constructor)
            => DELETE_PREFIX.Append(constructor.ToSql()).Append(";").ToString();

        public string DeleteSql(Expression<Func<T, bool>> predicate)
            => DELETE_PREFIX.Append(WhereClause(predicate)).Append(";").ToString();

        public string DeleteSql<TParam>(Expression<Func<T, TParam, bool>> predicate, TParam param)
            => DELETE_PREFIX.Append(WhereClause(predicate, param)).Append(";").ToString();

        public string WriteSql()
            => WRITE_SQL;

        public string ReadSql()
            => READ_PREFIX.Append(";").ToString();

        public string ReadById()
            => READ_PREFIX.Append(WHERE_ID).ToString();

        public string ReadSql(Expression<Func<T>> constructor) =>
            READ_PREFIX.Append(constructor.ToSql()).Append(";").ToString();

        public string ReadSql(Expression<Func<T, bool>> predicate)
            => READ_PREFIX.Append(WhereClause(predicate)).Append(";").ToString();

        public string ReadSql<TParam>(Expression<Func<T, TParam, bool>> predicate, TParam param)
            => READ_PREFIX.Append(WhereClause(predicate, param)).Append(";").ToString();

        public string ReadSqlById()
            => READ_WHERE_ID;

        public string UpdateSql<TParam>()
        {
            var paramNames = new HashSet<string>(typeof(TParam).GetDataProperties().Select(p => p.PropertyName));
            return UPDATE_PREFIX.Append(string.Join(", ", NON_ID_COLUMNS.Where(c => paramNames.Contains(c.ColumnName)).Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName))).Append(" WHERE [ID] = @ID").ToString();
        }

        public string WhereClause<TParam>(Expression<Func<T, TParam, bool>> predicate, TParam param)
            => "WHERE ".Append(predicate.ToSql(param)).ToString();

        public string WhereClause(Expression<Func<T, bool>> predicate)
            => "WHERE ".Append(predicate.ToSql()).ToString();

        public string DropTableSql()
        {
            string name = typeof(T).Name;
            if (typeof(T).TryGetAttribute(out TableAttribute table))
                name = table.Value;
            return $"DROP TABLE [{name}];";
        }

        public string CreateTableSql()
        {
            string name = typeof(T).Name;
            if (typeof(T).TryGetAttribute(out TableAttribute table))
                name = table.Value;
            string sql =
$@"CREATE TABLE [{name}] (
    [{ID_COLUMN.ColumnName}] {types.GetDbTypeString(ID_COLUMN)} IDENTITY(1,1) PRIMARY KEY,
    {string.Join($",{Environment.NewLine}    ", NON_ID_COLUMNS.Select(c => $"[{c.ColumnName}] {types.GetDbTypeString(c)}"))}
);";
            return sql;
        }
    }
}
