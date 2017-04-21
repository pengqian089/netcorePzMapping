using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using CoreServiceLibrary.Mapping;

namespace CoreServiceLibrary.Sql
{
    [DebuggerDisplay("Sql = {" + nameof(ExecuteSql) + "}")]
    public class SqlGenerator: IGenerator
    {
        /// <summary>
        /// 生成的SQL语句
        /// </summary>
        public virtual string ExecuteSql { get; private set; }


        /// <summary>
        /// 生成查询语句
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mapping"></param>
        /// <param name="parameters">根据谓词筛选</param>
        /// <param name="sort">排序</param>
        /// <returns></returns>
        public virtual string Select<T>(IMapping mapping, Expression<Func<T, bool>> parameters = null, params ISort[] sort) where T : IMapping
        {
            //表别称
            const string nickName = "T";
            var sql = new StringBuilder($"SELECT {string.Join(",", mapping.Properties.Where(x => !x.Ignored).Select(x => $"{nickName}.[{x.ColumnName}]"))} FROM {mapping.TableName} AS {nickName}");

            if (parameters != null)
            {
                var analyzer = new ExpressionAnalyzer(mapping.Properties, nickName);
                var predicate = analyzer.DealExpress(parameters);
                if (!string.IsNullOrEmpty(predicate))
                {
                    sql.Append($" WHERE {predicate}");
                }
            }
            if (sort != null && sort.Any())
            {
                sql.Append($" ORDER BY {string.Join(",", sort.Select(x => $"{nickName}.{x.PropertyName} {(x.Ascending ? "ASC" : "DESC")}"))}");
            }
            ExecuteSql = sql.ToString();
            return ExecuteSql;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mapping"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="parameters"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        public virtual string SelectPage<T>(IMapping mapping, int startIndex, int endIndex, Expression<Func<T, bool>> parameters = null, params ISort[] sort)
        {
            if (sort == null || !sort.Any())
            {
                throw new ArgumentNullException(nameof(sort), "分页必须传入排序条件！");
            }

            const string nickName = "T";
            //var countSql = $"SELECT COUNT(*) FROM {mapping.TableName} AS {nickName}";
            var innerSql = new StringBuilder($"SELECT ROW_NUMBER() OVER(ORDER BY {string.Join(",", sort.Select(x => $"{nickName}.{x.PropertyName} {(x.Ascending ? "ASC" : "DESC")}"))}) AS RWID,{string.Join(",", mapping.Properties.Where(x => !x.Ignored).Select(x => $"{nickName}.[{x.ColumnName}]"))} FROM {mapping.TableName} AS {nickName}");
            if (parameters != null)
            {
                var analyzer = new ExpressionAnalyzer(mapping.Properties, nickName);
                var predicate = analyzer.DealExpress(parameters);
                if (!string.IsNullOrEmpty(predicate))
                {
                    var resultPredicate = $" WHERE {predicate}";
                    innerSql.Append(resultPredicate);
                    //countSql += resultPredicate;
                }

            }
            var sql = $"SELECT * FROM ({innerSql}) T2 WHERE T2.RWID BETWEEN {startIndex} and {endIndex}";
            ExecuteSql = sql;
            return ExecuteSql;
        }


        public virtual string Insert<T>(IMapping mapping,out PropertyMap keyIdentity)
        {
            var columns = mapping.Properties.Where(x => !x.Ignored || x.IsReadOnly).Where(x => !x.Identity && !x.IsPrimaryKey).ToList();
            if (!columns.Any())
                throw new ArgumentException("没有映射的列");
            keyIdentity = mapping.Properties.FirstOrDefault(x => x.Identity && x.IsPrimaryKey);

            if (keyIdentity == null)
            {
                ExecuteSql = $"INSERT INTO {mapping.TableName} ({string.Join(",", columns.Select(x => "["+ x.ColumnName + "]"))}) VALUES({string.Join(",", columns.Select(x => "@" + x.Name))}) "; ;
            }
            ExecuteSql = $"INSERT INTO {mapping.TableName} ({string.Join(",", columns.Select(x => "[" + x.ColumnName + "]"))}) OUTPUT INSERTED.{keyIdentity.ColumnName} VALUES({string.Join(",",columns.Select(x => "@" + x.Name))}) ";
            return ExecuteSql;
        }

        public virtual string Update<T>(IMapping mapping, Expression<Func<T, bool>> parameters)
        {
            var columns = mapping.Properties.Where(x => !x.Ignored || x.IsReadOnly).Where(x => !x.Identity && !x.IsPrimaryKey).ToList();
            if (!columns.Any())
                throw new ArgumentException("没有映射的列");
            var sql = "UPDATE {0} SET {1}";
            sql = string.Format(sql, mapping.TableName, string.Join(",", columns.Select(x => $"[{x.ColumnName}]=@{x.Name}")));
            if (parameters != null)
            {
                var analyzer = new ExpressionAnalyzer(mapping.Properties,"");
                var predicate = analyzer.DealExpress(parameters);
                if (!string.IsNullOrEmpty(predicate))
                {
                    sql += " WHERE " + predicate;
                }
            }
            ExecuteSql = sql;
            return ExecuteSql;
        }

        public virtual string Delete<T>(Expression<Func<T, bool>> parameters) where T:class ,IMapping,new()
        {
            var mapping = Activator.CreateInstance<T>();
            var sql = "DELETE FROM {0}";
            sql = string.Format(sql, mapping.TableName);
            if (parameters != null)
            {
                var analyzer = new ExpressionAnalyzer(mapping.Properties, "");
                var predicate = analyzer.DealExpress(parameters);
                if (!string.IsNullOrEmpty(predicate))
                {
                    sql += " WHERE " + predicate;
                }
            }
            ExecuteSql = sql;
            return ExecuteSql;
        }
    }
   
}
