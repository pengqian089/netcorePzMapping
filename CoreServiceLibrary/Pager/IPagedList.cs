using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CoreServiceLibrary.Pager
{
    public interface IPagedList:IEnumerable
    {
        /// <summary>
        /// 当前页
        /// </summary>
        int CurrentPage { get; set; }

        /// <summary>
        /// 页大小
        /// </summary>
        int PageSize { get; set; }

        /// <summary>
        /// 总记录数
        /// </summary>
        int TotalItemCount { get; set; }
    }

    public interface IPagedList<T> : IEnumerable<T>, IPagedList { }
}
