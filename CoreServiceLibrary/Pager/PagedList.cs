using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace CoreServiceLibrary.Pager
{
    public class PagedList<T>:List<T>,IPagedList<T>
    {

        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItemCount { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPageCount => (int)Math.Ceiling(TotalItemCount / (double)PageSize);

        /// <summary>
        /// 开始索引
        /// </summary>
        public int StartItemIndex => (CurrentPage - 1) * PageSize + 1;

        /// <summary>
        /// 结束索引
        /// </summary>
        public int EndItemIndex => TotalItemCount > CurrentPage * PageSize ? CurrentPage * PageSize : TotalItemCount;

        public PagedList(IEnumerable<T> allItems, int pageIndex, int pageSize)
        {
            PageSize = pageSize;
            var items = allItems as IList<T> ?? allItems.ToList();
            TotalItemCount = items.Count();
            CurrentPage = pageIndex;
            AddRange(items.Skip(StartItemIndex - 1).Take(pageSize));
        }

        public PagedList(IEnumerable<T> currentPageItems, int pageIndex, int pageSize, int totalItemCount)
        {
            AddRange(currentPageItems);
            TotalItemCount = totalItemCount;
            CurrentPage = pageIndex;
            PageSize = pageSize;
        }

        public PagedList(IQueryable<T> allItems, int pageIndex, int pageSize)
        {
            var startIndex = (pageIndex - 1) * pageSize;
            AddRange(allItems.Skip(startIndex).Take(pageSize));
            TotalItemCount = allItems.Count();
            CurrentPage = pageIndex;
            PageSize = pageSize;
        }

        public PagedList(IQueryable<T> currentPageItems, int pageIndex, int pageSize, int totalItemCount)
        {
            AddRange(currentPageItems);
            TotalItemCount = totalItemCount;
            CurrentPage = pageIndex;
            PageSize = pageSize;
        }
    }
}
