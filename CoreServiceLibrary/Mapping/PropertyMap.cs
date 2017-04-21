using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CoreServiceLibrary.Mapping
{
    public class PropertyMap
    {
        public PropertyMap(PropertyInfo property)
        {
            PropertyInfo = property;
            ColumnName = PropertyInfo.Name;
        }

        /// <summary>
        /// 获取当前列名称（默认为属性名称）
        /// </summary>
        public string ColumnName { get; private set; }

        /// <summary>
        /// 获取当前属性名称
        /// </summary>
        public string Name => PropertyInfo.Name;

        /// <summary>
        /// 获取当前属性是否忽略
        /// </summary>
        public bool Ignored { get; private set; }

        /// <summary>
        /// 获取当前属性是否为主键
        /// </summary>
        public bool IsPrimaryKey { get; private set; }

        /// <summary>
        /// 获取当前属性的信息
        /// </summary>
        public PropertyInfo PropertyInfo { get; }

        /// <summary>
        /// 是否为Identity列
        /// </summary>
        public bool Identity { get; private set; }

        /// <summary>
        /// 是否为只读列
        /// </summary>
        public bool IsReadOnly { get; private set; }

        /// <summary>
        /// 设置当前属性映射的字段名称
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public PropertyMap Column(string columnName)
        {
            ColumnName = columnName;
            return this;
        }

        /// <summary>
        /// 将当前属性忽略映射
        /// </summary>
        /// <returns></returns>
        public PropertyMap Ignore()
        {
            Ignored = true;
            return this;
        }

        /// <summary>
        /// 将当前属性设置为主键
        /// </summary>
        /// <returns></returns>
        public PropertyMap Key()
        {
            IsPrimaryKey = true;
            return this;
        }

        /// <summary>
        /// 将当前属性设置为Identity列
        /// </summary>
        /// <returns></returns>
        public PropertyMap SetIdentity()
        {
            Identity = true;
            return this;
        }

        /// <summary>
        /// 将当前属性设置为只读列
        /// </summary>
        /// <returns></returns>
        public PropertyMap ReadOnly()
        {
            IsReadOnly = true;
            return this;
        }
    }
}
