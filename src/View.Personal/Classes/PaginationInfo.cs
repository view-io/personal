namespace View.Personal.Classes
{
    using System;
    using View.Personal.Services;

    /// <summary>
    /// Represents pagination information for data grids.
    /// </summary>
    public class PaginationInfo
    {
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalItems = 0;

        /// <summary>
        /// Gets or sets the current page number (1-based).
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set => _currentPage = Math.Max(1, value);
        }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Max(1, value);
        }

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        public int TotalItems
        {
            get => _totalItems;
            set => _totalItems = Math.Max(0, value);
        }

        /// <summary>
        /// Gets or sets the number of records remaining after the current page.
        /// </summary>
        public int RecordsRemaining { get; set; }

        /// <summary>
        /// Gets or sets the index (1-based) of the first item on the current page.
        /// </summary>
        public int FirstItemIndex { get; set; } = 1;

        /// <summary>
        /// Gets or sets the number of items on the current page.
        /// </summary>
        public int ItemsOnCurrentPage { get; set; }

        /// <summary>
        /// Gets the index (1-based) of the last item on the current page.
        /// </summary>
        public int LastItemIndex => Math.Min(FirstItemIndex + ItemsOnCurrentPage - 1, TotalItems);

        /// <summary>
        /// Gets the total number of pages based on the total item count and page size.
        /// </summary>
        public int TotalPages => TotalItems > 0
            ? (int)Math.Ceiling((double)TotalItems / PageSize)
            : 0;

        /// <summary>
        /// Gets the 0-based index of the first item on the current page (for offset-based paging).
        /// </summary>
        public int StartIndex => (CurrentPage - 1) * PageSize;

        /// <summary>
        /// Gets the 0-based exclusive end index for the current page (for offset-based paging).
        /// </summary>
        public int EndIndex => Math.Min(StartIndex + PageSize, TotalItems);

        /// <summary>
        /// Gets a value indicating whether a previous page exists.
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Gets a value indicating whether a next page exists.
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;

        /// <summary>
        /// Gets a formatted string showing the range of items currently displayed.
        /// </summary>
        public string PageRangeText => TotalItems > 0
            ? string.Format(ResourceManagerService.GetString("PaginationShowingItems"), FirstItemIndex, LastItemIndex, TotalItems)
            : ResourceManagerService.GetString("PaginationNoItems");

        /// <summary>
        /// Gets a formatted string showing the current page number and total pages.
        /// </summary>
        public string PageInfoText => TotalPages > 0
            ? string.Format(ResourceManagerService.GetString("PaginationPageInfo"), CurrentPage, TotalPages)
            : ResourceManagerService.GetString("PaginationNoPages");

        /// <summary>
        /// Advances to the next page, if available.
        /// </summary>
        /// <returns><c>true</c> if the page changed; otherwise, <c>false</c>.</returns>
        public bool NextPage()
        {
            if (HasNextPage)
            {
                CurrentPage++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Moves to the previous page, if available.
        /// </summary>
        /// <returns><c>true</c> if the page changed; otherwise, <c>false</c>.</returns>
        public bool PreviousPage()
        {
            if (HasPreviousPage)
            {
                CurrentPage--;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the current page to the first page.
        /// </summary>
        public void FirstPage()
        {
            CurrentPage = 1;
        }

        /// <summary>
        /// Sets the current page to the last page based on <see cref="TotalPages"/>.
        /// </summary>
        public void LastPage()
        {
            CurrentPage = TotalPages;
        }

        /// <summary>
        /// Resets the pagination state to the first page with no items.
        /// </summary>
        public void Reset()
        {
            CurrentPage = 1;
            TotalItems = 0;
            FirstItemIndex = 1;
            ItemsOnCurrentPage = 0;
        }
    }
}
