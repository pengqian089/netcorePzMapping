using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using CoreServiceLibrary.Mapping;

namespace CoreServiceLibrary.Sql
{
    [DebuggerDisplay("Sql = {" + nameof(ExecuteSql) + "}")]
    public class SqlGenerator
    {
        /// <summary>
        /// 生成的SQL语句
        /// </summary>
        public virtual string  ExecuteSql { get; private set; }


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
            return sql.ToString();
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
        /// <returns><see cref="Tuple{T1}"/>Item1:执行sql，Item2:当前执行sql的总记录数</returns>
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
            return sql;
        }


        public virtual string Insert<T>(IMapping mapping,out PropertyMap keyIdentity)
        {
            var columns = mapping.Properties.Where(x => !x.Ignored || x.IsReadOnly).Where(x => !x.Identity && !x.IsPrimaryKey).ToList();
            if (!columns.Any())
                throw new ArgumentException("没有映射的列");
            keyIdentity = mapping.Properties.FirstOrDefault(x => x.Identity && x.IsPrimaryKey);
            if (keyIdentity == null)
            {
                return $"INSERT INTO {mapping.TableName} ({string.Join(",", columns.Select(x => "["+ x.ColumnName + "]"))}) VALUES({string.Join(",", columns.Select(x => "@" + x.Name))}) "; ;
            }
            return $"INSERT INTO {mapping.TableName} ({string.Join(",", columns.Select(x => "[" + x.ColumnName + "]"))}) OUTPUT INSERTED.{keyIdentity.ColumnName} VALUES({string.Join(",",columns.Select(x => "@" + x.Name))}) ";
            
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
            return sql;
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
            return sql;
        }
    }

    /// <summary>
    /// 解析表达式树
    /// </summary>
    internal class ExpressionAnalyzer
    {
        internal ExpressionAnalyzer()
        {
            Maps = new List<PropertyMap>();
            NickName = "T";
        }

        internal ExpressionAnalyzer(IList<PropertyMap> maps, string nickName)
        {
            Maps = maps;
            NickName = nickName;
        }

        /// <summary>
        /// 映射列表
        /// </summary>
        public IList<PropertyMap> Maps { get; set; }

        /// <summary>
        /// 别称
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// 解析表达式树，生成条件
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public string DealExpress(Expression exp)
        {
            if (exp is LambdaExpression)
            {
                var lExp = exp as LambdaExpression;
                return DealExpress(lExp.Body);
            }
            if (exp is BinaryExpression)
            {
                return DealBinaryExpression(exp as BinaryExpression);
            }
            if (exp is MemberExpression)
            {
                return DealMemberExpression(exp as MemberExpression);
            }
            if (exp is ConstantExpression)
            {
                return DealConstantExpression(exp as ConstantExpression);
            }
            if (exp is UnaryExpression)
            {
                return DealUnaryExpression(exp as UnaryExpression);
            }
            return "";
        }

        /// <summary>
        /// 一元运算符解析
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public string DealUnaryExpression(UnaryExpression exp)
        {
            return DealExpress(exp.Operand);
        }

        /// <summary>
        /// 表达式值解析
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public string DealConstantExpression(ConstantExpression exp)
        {
            var vaule = exp.Value;
            string vStr;
            if (vaule == null)
            {
                return "NULL";
            }
            if (vaule is string)
            {
                vStr = string.Format("'{0}'", vaule.ToString());
            }
            else if (vaule is DateTime)
            {
                var time = (DateTime)vaule;
                vStr = string.Format("'{0}'", time.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                vStr = vaule.ToString();
            }
            return vStr;
        }

        /// <summary>
        /// 操作解析 等于、大于、小于、不等于 等
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public string DealBinaryExpression(BinaryExpression exp)
        {

            var left = DealExpress(exp.Left);
            var oper = GetOperStr(exp.NodeType);
            var right = DealExpress(exp.Right);
            if (right == "NULL")
            {
                oper = oper == "=" ? " is " : " is not ";
            }
            return left + oper + right;
        }

        /// <summary>
        /// 属性解析
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public string DealMemberExpression(MemberExpression exp)
        {
            var map = Maps.FirstOrDefault(x => x.Name == exp.Member.Name);
            if (map != null)
            {
                return  string.IsNullOrEmpty(NickName) ? map.ColumnName : $"{NickName}.{map.ColumnName}";
            }
            return string.IsNullOrEmpty(NickName) ? exp.Member.Name : $"{NickName}.{exp.Member.Name}";
        }

        /// <summary>
        /// 获取操作符
        /// </summary>
        /// <param name="eType"></param>
        /// <returns></returns>
        public static string GetOperStr(ExpressionType eType)
        {
            switch (eType)
            {
                case ExpressionType.OrElse: return " OR ";
                case ExpressionType.Or: return "|";
                case ExpressionType.AndAlso: return " AND ";
                case ExpressionType.And: return "&";
                case ExpressionType.GreaterThan: return ">";
                case ExpressionType.GreaterThanOrEqual: return ">=";
                case ExpressionType.LessThan: return "<";
                case ExpressionType.LessThanOrEqual: return "<=";
                case ExpressionType.NotEqual: return "<>";
                case ExpressionType.Add: return "+";
                case ExpressionType.Subtract: return "-";
                case ExpressionType.Multiply: return "*";
                case ExpressionType.Divide: return "/";
                case ExpressionType.Modulo: return "%";
                case ExpressionType.Equal: return "=";
            }
            return "";
        }
    }

    public interface ISort
    {
        /// <summary>
        /// 属性名称
        /// </summary>
        string PropertyName { get; set; }

        /// <summary>
        /// 是否正序排列
        /// </summary>
        bool Ascending { get; set; }
    }

    public class Sort : ISort
    {

        public string PropertyName { get; set; }


        public bool Ascending { get; set; }
    }
}
