﻿using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;

namespace Ooorm.Data.Sqlite
{

    /// <summary>
    /// Data Access Object for Sql Server connections
    /// </summary>
    internal class SqliteDao : BaseDao<Microsoft.Data.Sqlite.SqliteConnection, SqliteCommand, SqliteDataReader>
    {
        public SqliteDao(Func<IDatabase> db) : base(new SqliteDataConsumer(), new SqliteTypeProvider(db), db) { }

        public override void AddKeyValuePair(SqliteCommand command, string key, object value)
        {
            var paramValue = value;
            if (value is IdConvertable valId)
                paramValue = valId.ToId();
            else if (value is IdConvertable refId)
                paramValue = refId.ToId();
            command.Parameters.AddWithValue(key, value);
        }

        public override SqliteCommand GetCommand(string sql, Microsoft.Data.Sqlite.SqliteConnection connection) =>
            new SqliteCommand(sql, connection) { CommandType = CommandType.Text };

        public async Task<int> ExecuteAsync(Microsoft.Data.Sqlite.SqliteConnection connection, string sql, object parameter)
        {
            using var command = GetCommand(sql, connection);
            command.CommandText = sql;            
            AddParameters(command, sql, parameter);
            var result = await command.ExecuteNonQueryAsync();
            return result;
        }

        public async Task<T> ExecuteScalarAsync<T>(Microsoft.Data.Sqlite.SqliteConnection connection, string sql, T parameter)
        {
            using var command = GetCommand(sql, connection);
            command.CommandText = sql;
            AddParameters(command, sql, parameter);
            return (T)(await command.ExecuteScalarAsync());
        }

        public Task<T[]> ExecuteBatchAsync<T, TId>(Microsoft.Data.Sqlite.SqliteConnection connection, string sql, params T[] parameters) where T : DbItem<T, TId> where TId : struct, IEquatable<TId>
        {       
            CheckColumnCache<T, TId>();
            return Task.Run(() => 
            {
                using var command = GetCommand(sql, connection);

                command.CommandText = sql;
                foreach (var parameter in parameters)
                    AddParameters(command, sql, parameter);

                T[] results = new T[parameters.Length];
                int item_number = 0;
                foreach (var result in ExecuteReader<T, TId>(command))
                    results[item_number++] = result;
                return results;
            });         
        }

        public async Task<List<T>> ReadAsync<T, TId>(Microsoft.Data.Sqlite.SqliteConnection connection, string sql, object parameter) where T : DbItem<T, TId> where TId : struct, IEquatable<TId>
        {
            CheckColumnCache<T, TId>();
            using var command = GetCommand(sql, connection);
            AddParameters(command, sql, parameter);
            return await ExecuteReaderAsync<T, TId>(command);
        }

        public async Task<List<T>> ReadAsync<T, TId>(Microsoft.Data.Sqlite.SqliteConnection connection, string sql, (string name, object value) parameter) where T : DbItem<T, TId> where TId : struct, IEquatable<TId>
        {
            CheckColumnCache<T, TId>();
            using var command = GetCommand(sql, connection);
            AddParameters(command, sql, parameter);
            return await ExecuteReaderAsync<T, TId>(command);
        }

        protected Task<List<T>> ExecuteReaderAsync<T, TId>(SqliteCommand command) where T : DbItem<T, TId> where TId : struct, IEquatable<TId> =>
            Task.Run(() => ExecuteReader<T, TId>(command));

        protected override List<T> ExecuteReader<T, TId>(SqliteCommand command)
        {
            using var reader = command.ExecuteReader(CommandBehavior.SequentialAccess);
            return ParseReader<T, TId>(reader);
        }
    }
}
