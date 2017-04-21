using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using CoreServiceLibrary.Mapping;

namespace CoreServiceLibrary
{
    public interface IGenerator
    {
        //string ExecuteSql { get;  set; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mapping"></param>
        /// <param name="parameters"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        string Select<T>(IMapping mapping, Expression<Func<T, bool>> parameters = null, params ISort[] sort) where T : IMapping;

        string SelectPage<T>(IMapping mapping, int startIndex, int endIndex,
            Expression<Func<T, bool>> parameters = null, params ISort[] sort);

        string Insert<T>(IMapping mapping, out PropertyMap keyIdentity);

        string Update<T>(IMapping mapping, Expression<Func<T, bool>> parameters);

        string Delete<T>(Expression<Func<T, bool>> parameters) where T : class, IMapping, new();
    }

    /// <summary>
    /// 解析表达式树
    /// </summary>
    internal class ExpressionAnalyzer
    {
        internal ExpressionAnalyzer()
        {
            Maps = new List<PropertyMap>();
            NickName = "";
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
                return string.IsNullOrEmpty(NickName) ? map.ColumnName : $"{NickName}.{map.ColumnName}";
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
