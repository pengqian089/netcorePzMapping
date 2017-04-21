using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CoreServiceLibrary.Sql;

namespace CoreServiceLibrary.Mapping
{
    public interface IMapping
    {
        string TableName { get; }
        IList<PropertyMap> Properties { get; }
        Type EntityType { get; }
    }

    public interface IMapping<T> : IMapping where T : IMapping
    {
    }

    /// <summary>
    /// 设置映射
    /// </summary>
    public class MappingModel<T>:IMapping<T> where T : IMapping
    {
        public MappingModel()
        {
            var type = GetType();
            TableName = type.Name;
            var modelProperties = type.GetProperties().Where(x => !typeof(MappingModel<>).GetProperties().Select(y => y.Name).Contains(x.Name)).Select(x => x).ToList();
            Properties = new List<PropertyMap>();
            modelProperties.ForEach(x =>
            {
                if (Properties.Any(y => y.Name == x.Name))
                    return;
                Map(x);
            });

        }

        /// <summary>
        /// 映射到数据库表中列的集合
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public IList<PropertyMap> Properties { get; private set; }

        /// <summary>
        /// 映射表的名称
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public string TableName { get; private set; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public Type EntityType => typeof(T);

        /// <summary>
        /// 设置映射表的名称 （默认为model名称）
        /// </summary>
        /// <param name="tableName"></param>
        protected virtual void Table(string tableName)
        {
            TableName = tableName;
        }


        /// <summary>
        /// 属性映射
        /// </summary>
        protected PropertyMap Map(Expression<Func<T, object>> expression)
        {
            Func<Expression, MemberInfo> func = x =>
            {
                var expr = x;
                for (;;)
                {
                    switch (expr.NodeType)
                    {
                        case ExpressionType.Lambda:
                            expr = ((LambdaExpression)expr).Body;
                            break;
                        case ExpressionType.Convert:
                            expr = ((UnaryExpression)expr).Operand;
                            break;
                        case ExpressionType.MemberAccess:
                            var memberExpression = (MemberExpression)expr;
                            return memberExpression.Member;
                        default:
                            return null;
                    }
                }
            };
            var propertyInfo = func(expression) as PropertyInfo;
            if (Properties.Any(x => propertyInfo != null && x.Name == propertyInfo.Name))
            {
                var currentProperty = Properties.FirstOrDefault(x => x.Name == propertyInfo.Name);
                if (currentProperty != null)
                {
                    //var index = Properties.IndexOf(currentProperty);
                    //Properties.RemoveAt(index);
                    //var map = new PropertyMap(propertyInfo);
                    //Properties.Insert(index, map);
                    return currentProperty;
                }
            }
            return Map(propertyInfo);
        }

        /// <summary>
        /// 添加映射
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        protected PropertyMap Map(PropertyInfo propertyInfo)
        {
            var result = new PropertyMap(propertyInfo);
            GuardForDuplicatePropertyMap(result);
            Properties.Add(result);
            return result;
        }

        /// <summary>
        /// 检测重复映射
        /// </summary>
        /// <param name="result"></param>
        /// <exception cref="ArgumentException"></exception>
        private void GuardForDuplicatePropertyMap(PropertyMap result)
        {
            if (Properties.Any(x => x.Name == result.Name))
            {
                throw new ArgumentException(string.Format("检测到‘{0}’属性有重复映射。", result.Name));
            }
        }
    }
}
