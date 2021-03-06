﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using sdORM.Common;
using sdORM.Common.SqlSpecifics;
using sdORM.Extensions;
using sdORM.Mapping;

namespace sdORM.Session
{
    public abstract class DataBaseSession : DataBaseSessionBase, IDataBaseSession
    {
        protected DataBaseSession(DbConnection connection, EntityMappingProvider entityMappingProvider)
            : base(connection, entityMappingProvider)
        {

        }

        public virtual void Connect()
        {
            this._connection.Open();
        }

        public virtual IList<T> Query<T>() where T : new()
        {
            return this.Query<T>(new ParameterizedSql
            {
                Sql = this.GetSelectStatementForMapping(this.EntityMappingProvider.GetMapping<T>()).ToString(),
                Parameters = new List<SqlParameter>()
            });
        }

        public virtual IList<T> Query<T>(Expression<Func<T, bool>> predicate) where T : new()
        {
            Guard.NotNull(predicate, nameof(predicate));

            return this.Query<T>(this.GetSqlForPredicate(predicate, this.EntityMappingProvider.GetMapping<T>()));
        }

        public virtual IList<T> Query<T>(ParameterizedSql parameterizedSql) where T : new()
        {
            Guard.NotNull(parameterizedSql, nameof(parameterizedSql));

            var mapping = this.EntityMappingProvider.GetMapping<T>();

            using (var command = this.GenerateIDBCommand(parameterizedSql))
            using (var reader = command.ExecuteReader())
            {
                if (reader.HasRows == false)
                    return default(IList<T>);

                var results = new List<T>();

                while (reader.Read())
                {
                    results.Add(reader.LoadWithEntityMapping(mapping));
                }

                return results;
            }
        }

        public virtual T GetByID<T>(object id) where T : new()
        {
            Guard.NotNull(id, nameof(id));

            var mapping = this.EntityMappingProvider.GetMapping<T>();
            var sql = this.GetSqlForGetById(id, mapping);

            using (var command = this.GenerateIDBCommand(sql))
            using (var reader = command.ExecuteReader())
            {
                if (reader.HasRows == false)
                    return default(T);

                reader.Read();

                return reader.LoadWithEntityMapping(mapping);
            }
        }

        public virtual T SaveOrUpdate<T>(T entity)
        {
            Guard.NotNull(entity, nameof(entity));

            var mapping = this.EntityMappingProvider.GetMapping<T>();

            return mapping.IsPrimaryKeyDefaultValue(entity)
                ? this.Save(entity)
                : this.Update(entity);
        }

        public virtual T Save<T>(T entity)
        {
            var mapping = this.EntityMappingProvider.GetMapping<T>();
            var sql = this.GetSqlForSave(entity, mapping);

            using (var command = this.GenerateIDBCommand(sql))
            {
                command.ExecuteNonQuery();

                this.SetIdAfterSave(entity, command, mapping);

                return entity;
            }
        }

        public virtual T Update<T>(T entity)
        {
            var mapping = this.EntityMappingProvider.GetMapping<T>();
            var sql = this.GetSqlForUpdate(entity, mapping);

            using (var command = this.GenerateIDBCommand(sql))
            {
                command.ExecuteNonQuery();
                return entity;
            }
        }

        public virtual TableMetaData GetTableMetaData(string tableName)
        {
            // I'm not sure if returning null if it doesnt exist is really what we want to do here.
            // Throwing an exception might be the better option but simply returning null is consitent with how the database does it. Not sure...
            using (var cmd = this.GenerateIDBCommand(this.GetSqlForCheckIfTableExtists(tableName)))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.HasRows == false)
                    return null;
            }

            using (var cmd = this.GenerateIDBCommand(this.GetSqlForTableMetaData(tableName)))
            using (var reader = cmd.ExecuteReader())
            {
                var table = new TableMetaData
                {
                    Columns = new List<ColumnMetaData>()
                };

                while (reader.Read())
                {
                    table.Columns.Add(reader.LoadColumnMetaData());
                }

                return table;
            }
        }
    }
}