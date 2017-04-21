using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using CoreServiceLibrary.Mapping;
using CoreServiceLibrary.Pager;
using CoreServiceLibrary.Sql;
using Dapper;

namespace CoreServiceLibrary
{
    public static class DapperExtend
    {
        #region Get single data

        public static T Get<T>(this IDbConnection connection) where T : class, IMapping,new()
        {
            return connection.Get<T>(null,sort: null);
        }

        public static T Get<T>(this IDbConnection connection, params ISort[] sort) where T : class, IMapping,new()
        {
            return connection.Get<T>(null, sort);
        }

        public static T Get<T>(this IDbConnection connection, Expression<Func<T, bool>> filter)
            where T : class, IMapping,new()
        {
            return connection.Get(filter, null);
        }


        public static T Get<T>(this IDbConnection connection, Expression<Func<T, bool>> filter, params ISort[] sort) where T : class, IMapping,new()
        {
            return connection.Get(filter, out string _,sort);
        }


        public static T Get<T>(this IDbConnection connection, Expression<Func<T, bool>> filter, 
            out string sql, params ISort[] sort) where T : class, IMapping,new()
        {
            var generator = new SqlGenerator();
            var entity = (IMapping)Activator.CreateInstance(typeof(T));
            sql = generator.Select(entity, filter, sort);
            using (var reader = connection.ExecuteReader(sql))
            {
                return DataRead<T>(entity.Properties, reader).FirstOrDefault();
            }
            //return connection.QueryFirstOrDefault<T>(sql);
        }
        #endregion



        #region Get list data
        public static List<T> GetList<T>(this IDbConnection connection) where T : class, IMapping,new()
        {
            return connection.GetList<T>(null, sort: null);
        }

        public static List<T> GetList<T>(this IDbConnection connection, params ISort[] sort) where T : class, IMapping,new()
        {
            return connection.GetList<T>(null, sort);
        }

        public static List<T> GetList<T>(this IDbConnection connection, Expression<Func<T, bool>> filter)
            where T : class, IMapping,new()
        {
            return connection.GetList(filter, null);
        }


        public static List<T> GetList<T>(this IDbConnection connection, Expression<Func<T, bool>> filter, params ISort[] sort) where T : class, IMapping,new()
        {
            return connection.GetList(filter, out string _, sort);
        }


        public static List<T> GetList<T>(this IDbConnection connection, Expression<Func<T, bool>> filter,
            out string sql, params ISort[] sort) where T : class, IMapping,new()
        {
            var generator = new SqlGenerator();
            var entity = (IMapping)Activator.CreateInstance(typeof(T));
            sql = generator.Select(entity, filter, sort);
            using (var reader = connection.ExecuteReader(sql))
            {
                var source = DataRead<T>(entity.Properties, reader);
                return source.ToList();
            }
            //return connection.Query<T>(sql).ToList();
        }
        #endregion

        #region 分页
        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="currentPage">当前页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="filter">筛选条件</param>
        /// <param name="sql"></param>
        /// <param name="sort">排序</param>
        /// <returns></returns>
        public static PagedList<T> GetListPage<T>(this IDbConnection connection, int currentPage, int pageSize, Expression<Func<T, bool>> filter,
            out string sql, params ISort[] sort) where T : class, IMapping,new()
        {
            var count = connection.Count(filter);
            var startIndex = (currentPage - 1) * pageSize + 1;
            var endIndex = count > currentPage * pageSize ? currentPage * pageSize : count;
            var generator = new SqlGenerator();
            var entity = (IMapping)Activator.CreateInstance(typeof(T));
            sql = generator.SelectPage(entity, startIndex, endIndex, filter, sort);
            using (var reader = connection.ExecuteReader(sql))
            {
                var source = DataRead<T>(entity.Properties, reader).ToList();
                var pager = new PagedList<T>(source, currentPage, pageSize, count);
                return pager;
            } 
            
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="currentPage">当前页码</param>
        /// <param name="pageSize">页大小</param>
        /// <param name="filter">筛选条件</param>
        /// <param name="sort">排序</param>
        /// <returns></returns>
        public static PagedList<T> GetListPage<T>(this IDbConnection connection, int currentPage, int pageSize,
            Expression<Func<T, bool>> filter,
            params ISort[] sort) where T : class, IMapping,new()
        {
            return connection.GetListPage(currentPage, pageSize, filter, out string _,sort);
        }

        #endregion

        public static int Count<T>(this IDbConnection connection, Expression<Func<T, bool>> filter) where T : class, IMapping
        {
            const string nickName = "T";
            var entity = (IMapping)Activator.CreateInstance(typeof(T));
            var analyzer = new ExpressionAnalyzer(entity.Properties, nickName);
            var predicate = analyzer.DealExpress(filter);
            var sql = $"SELECT COUNT(*) FROM {entity.TableName} AS {nickName}";
            sql += string.IsNullOrEmpty(predicate) ? "" : $" WHERE {predicate}";
            return connection.QueryFirst<int>(sql);
        }

        /// <summary>
        /// 新增数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="entity"></param>
        public static void Insert<T>(this IDbConnection connection, T entity) where T : class, IMapping,new()
        {
            var generator = new SqlGenerator();
            var sql = generator.Insert<T>(entity, out PropertyMap keyIdentity);
            var p = new DynamicParameters();
            entity.Properties.Where(x => !x.Ignored || x.Identity || x.IsReadOnly)
                .Where(x => !x.Identity && !x.IsPrimaryKey).ToList().ForEach(x => p.Add("@" + x.Name,x.PropertyInfo.GetValue(entity)));
            if (keyIdentity != null)
            {
                var key = connection.ExecuteScalar(sql,p);
                keyIdentity.PropertyInfo.SetValue(entity, key);
            }
            connection.Execute(sql,p);
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="entity"></param>
        /// <param name="filter">筛选条件</param>
        /// <returns>受影响行数</returns>
        public static int Update<T>(this IDbConnection connection, T entity, Expression<Func<T, bool>> filter) where T : class, IMapping, new()
        {
            var generator = new SqlGenerator();
            var sql = generator.Update(entity, filter);
            var p = new DynamicParameters();
            entity.Properties.Where(x => !x.Ignored || x.Identity || x.IsReadOnly)
                .Where(x => !x.Identity && !x.IsPrimaryKey).ToList().ForEach(x => p.Add("@" + x.Name, x.PropertyInfo.GetValue(entity)));
            return connection.Execute(sql, p);
        }


        /// <summary>
        /// 更新数据 (实体必须指定主键<see cref="PropertyMap.IsPrimaryKey"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="entity"></param>
        /// <returns>受影响行数</returns>
        public static int Update<T>(this IDbConnection connection, T entity) where T : class, IMapping, new()
        {
            var map = entity.Properties.FirstOrDefault(x => x.IsPrimaryKey);
            if (map != null)
            {
                var exp = Expression.Parameter(entity.EntityType, "x");
                var body = Expression.Equal(Expression.Property(exp, map.Name),
                    Expression.Constant(map.PropertyInfo.GetValue(entity)));
                var lambda = Expression.Lambda<Func<T, bool>>(body, exp);
                return connection.Update(entity, lambda);
            }
            return 0;
        }

        /// <summary>
        /// 删除数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="filter">筛选条件</param>
        /// <returns>受影响行数</returns>
        public static int Delete<T>(this IDbConnection connection, Expression<Func<T, bool>> filter) where T : class, IMapping, new()
        {
            var generator = new SqlGenerator();
            var sql = generator.Delete(filter);
            return connection.Execute(sql);
        }

        public static int Delete<T>(this IDbConnection connection,IMapping entity) where T : class, IMapping, new()
        {
            var map = entity.Properties.FirstOrDefault(x => x.IsPrimaryKey);
            if (map != null)
            {
                var exp = Expression.Parameter(entity.EntityType, "x");
                var body = Expression.Equal(Expression.Property(exp, map.Name),
                    Expression.Constant(map.PropertyInfo.GetValue(entity)));
                var lambda = Expression.Lambda<Func<T, bool>>(body, exp);
                return connection.Delete(lambda);
            }
            return 0;
        }

        private static IEnumerable<T> DataRead<T>(IList<PropertyMap> maps,IDataReader reader) where T : class, IMapping,new ()
        {
            while (reader.Read())
            {
                var examples = Activator.CreateInstance<T>();
                foreach (var item in maps.Where(x => !x.Ignored))
                {
                    item.PropertyInfo.SetValue(examples,reader[item.ColumnName]);
                }
                yield return examples;
            }
        }
    }
}
