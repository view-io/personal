namespace View.Personal.Helpers
{
    using Avalonia.Controls;
    using Avalonia.Threading;
    using Classes;
    using LiteGraph;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides helper methods for managing file pagination in the application.
    /// </summary>
    public static class FilePaginationHelper
    {
        private static readonly Dictionary<Guid, PaginationInfo> _paginationState = new();

        /// <summary>
        /// Gets or creates pagination info for a specific graph.
        /// </summary>
        /// <param name="graphGuid">The graph GUID</param>
        /// <param name="pageSize">The page size to use if creating new pagination</param>
        /// <returns>The pagination info for the graph</returns>
        public static PaginationInfo GetPaginationInfo(Guid graphGuid, int pageSize = 10)
        {
            if (!_paginationState.ContainsKey(graphGuid))
            {
                _paginationState[graphGuid] = new PaginationInfo { PageSize = pageSize };
            }
            return _paginationState[graphGuid];
        }

        /// <summary>
        /// Clears pagination state for a specific graph.
        /// </summary>
        /// <param name="graphGuid">The graph GUID</param>
        public static void ClearPaginationState(Guid graphGuid)
        {
            _paginationState.Remove(graphGuid);
        }

        /// <summary>
        /// Loads a specific page of files for the given graph.
        /// </summary>
        /// <param name="liteGraph">The LiteGraph client</param>
        /// <param name="tenantGuid">The tenant GUID</param>
        /// <param name="graphGuid">The graph GUID</param>
        /// <param name="window">The main window</param>
        /// <param name="pageNumber">The page number to load (1-based)</param>
        /// <param name="pageSize">The page size</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task LoadPageAsync(
          LiteGraphClient liteGraph,
          Guid tenantGuid,
          Guid graphGuid,
          Window window,
          int pageNumber = 1,
          int pageSize = 10)
        {
            var pagination = GetPaginationInfo(graphGuid, pageSize);
            pagination.CurrentPage = pageNumber;

            // Calculate skip value for the requested page
            var skip = (pageNumber - 1) * pageSize;

            var result = await Task.Run(() =>
                MainWindowHelpers.GetDocumentNodes(liteGraph, tenantGuid, graphGuid, pageSize, skip));

            var files = result.Files;
            var newPagination = result.Pagination;

            // Update pagination state
            pagination.PageSize = pageSize;
            pagination.TotalItems = newPagination.TotalItems;
            pagination.RecordsRemaining = newPagination.RecordsRemaining;
            pagination.ItemsOnCurrentPage = files.Count;
            pagination.FirstItemIndex = ((pagination.CurrentPage - 1) * pagination.PageSize) + 1;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var filesDataGrid = window.FindControl<DataGrid>("FilesDataGrid");
                var fileOperationsPanel = window.FindControl<Grid>("FileOperationsPanel");
                var uploadFilesPanel = window.FindControl<Border>("UploadFilesPanel");
                var filePaginationControls = window.FindControl<Border>("FilePaginationControls");

                if (filesDataGrid != null && fileOperationsPanel != null && uploadFilesPanel != null && filePaginationControls !=null)
                {
                    if (filesDataGrid.ItemsSource is not ObservableCollection<FileViewModel> ingestedFiles)
                    {
                        ingestedFiles = new ObservableCollection<FileViewModel>();
                        filesDataGrid.ItemsSource = ingestedFiles;
                    }

                    // Clear existing items and add new ones
                    ingestedFiles.Clear();
                    foreach (var file in files)
                    {
                        ingestedFiles.Add(file);
                    }

                    filesDataGrid.IsVisible = files.Count > 0;
                    fileOperationsPanel.IsVisible = files.Count > 0;
                    uploadFilesPanel.IsVisible = files.Count == 0;
                    filePaginationControls.IsVisible = files.Count > 0;

                    // Update pagination controls
                    UpdatePaginationControls(window, pagination);
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// Loads the next page of files.
        /// </summary>
        /// <param name="liteGraph">The LiteGraph client</param>
        /// <param name="tenantGuid">The tenant GUID</param>
        /// <param name="graphGuid">The graph GUID</param>
        /// <param name="window">The main window</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task LoadNextPageAsync(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            var pagination = GetPaginationInfo(graphGuid);
            await LoadPageAsync(liteGraph, tenantGuid, graphGuid, window, pagination.CurrentPage + 1, pagination.PageSize);
        }

        /// <summary>
        /// Loads the previous page of files.
        /// </summary>
        /// <param name="liteGraph">The LiteGraph client</param>
        /// <param name="tenantGuid">The tenant GUID</param>
        /// <param name="graphGuid">The graph GUID</param>
        /// <param name="window">The main window</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task LoadPreviousPageAsync(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            var pagination = GetPaginationInfo(graphGuid);
            if (pagination.HasPreviousPage)
            {
                await LoadPageAsync(liteGraph, tenantGuid, graphGuid, window, pagination.CurrentPage - 1, pagination.PageSize);
            }
        }

        /// <summary>
        /// Loads the first page of files.
        /// </summary>
        /// <param name="liteGraph">The LiteGraph client</param>
        /// <param name="tenantGuid">The tenant GUID</param>
        /// <param name="graphGuid">The graph GUID</param>
        /// <param name="window">The main window</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task LoadFirstPageAsync(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            await LoadPageAsync(liteGraph, tenantGuid, graphGuid, window, 1, GetPaginationInfo(graphGuid).PageSize);
        }

        /// <summary>
        /// Loads the last page of files.
        /// </summary>
        /// <param name="liteGraph">The LiteGraph client</param>
        /// <param name="tenantGuid">The tenant GUID</param>
        /// <param name="graphGuid">The graph GUID</param>
        /// <param name="window">The main window</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task LoadLastPageAsync(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            var pagination = GetPaginationInfo(graphGuid);
            await LoadPageAsync(liteGraph, tenantGuid, graphGuid, window, pagination.TotalPages, pagination.PageSize);
        }

        /// <summary>
        /// Refreshes Grid.
        /// </summary>
        /// <param name="liteGraph">The LiteGraph client</param>
        /// <param name="tenantGuid">The tenant GUID</param>
        /// <param name="graphGuid">The graph GUID</param>
        /// <param name="window">The main window</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task RefreshGridAsync(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window)
        {
            var pagination = GetPaginationInfo(graphGuid);
            await LoadPageAsync(liteGraph, tenantGuid, graphGuid, window, 1, pagination.PageSize);
        }

        /// <summary>
        /// Updates the pagination controls in the UI.
        /// </summary>
        /// <param name="window">The main window</param>
        /// <param name="pagination">The pagination info</param>
        private static void UpdatePaginationControls(Window window, PaginationInfo pagination)
        {
            var pageInfoText = window.FindControl<TextBlock>("PageInfoText");
            var pageRangeText = window.FindControl<TextBlock>("PageRangeText");
            var previousButton = window.FindControl<Button>("PreviousPageButton");
            var nextButton = window.FindControl<Button>("NextPageButton");
            var firstButton = window.FindControl<Button>("FirstPageButton");
            var lastButton = window.FindControl<Button>("LastPageButton");
            var pageSizeComboBox = window.FindControl<ComboBox>("PageSizeComboBox");

            if (pageInfoText != null)
                pageInfoText.Text = pagination.PageInfoText;

            if (pageRangeText != null)
                pageRangeText.Text = pagination.PageRangeText;

            if (previousButton != null)
                previousButton.IsEnabled = pagination.HasPreviousPage;

            if (nextButton != null)
                nextButton.IsEnabled = pagination.HasNextPage;

            if (firstButton != null)
                firstButton.IsEnabled = pagination.HasPreviousPage;

            if (lastButton != null)
                lastButton.IsEnabled = pagination.HasNextPage;

            if (pageSizeComboBox != null && pageSizeComboBox.Items != null)
            {
                var pageSize = pagination.PageSize.ToString();
                if (pageSizeComboBox.Items.Contains(pageSize))
                {
                    pageSizeComboBox.SelectedItem = pageSize;
                }
            }
        }

        /// <summary>
        /// Changes the page size and reloads the first page.
        /// </summary>
        /// <param name="liteGraph">The LiteGraph client</param>
        /// <param name="tenantGuid">The tenant GUID</param>
        /// <param name="graphGuid">The graph GUID</param>
        /// <param name="window">The main window</param>
        /// <param name="newPageSize">The new page size</param>
        /// <returns>A task representing the asynchronous operation</returns>
        public static async Task ChangePageSizeAsync(LiteGraphClient liteGraph, Guid tenantGuid, Guid graphGuid, Window window, int newPageSize)
        {
            var pagination = GetPaginationInfo(graphGuid);
            pagination.PageSize = newPageSize;
            pagination.Reset();
            await LoadPageAsync(liteGraph, tenantGuid, graphGuid, window, 1, newPageSize);
        }
    }
}