﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Richasy.Bili.Models.App.Args;
using Richasy.Bili.Models.App.Other;
using Richasy.Bili.Models.Enums;

using static Richasy.Bili.Models.App.Constants.ControllerConstants.Search;

namespace Richasy.Bili.ViewModels.Uwp
{
    /// <summary>
    /// 搜索模块视图模型.
    /// </summary>
    /// <typeparam name="T">内部数据类型.</typeparam>
    public partial class SearchSubModuleViewModel : WebRequestViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSubModuleViewModel"/> class.
        /// </summary>
        /// <param name="type">模块类型.</param>
        public SearchSubModuleViewModel(SearchModuleType type)
            : base()
        {
            Type = type;
            switch (type)
            {
                case SearchModuleType.Video:
                    VideoCollection = new ObservableCollection<VideoViewModel>();
                    IsEnabled = true;
                    Controller.VideoSearchIteration += OnVideoSearchIteration;
                    InitializeVideoFiltersAsync();
                    break;
                case SearchModuleType.Bangumi:
                    PgcCollection = new ObservableCollection<SeasonViewModel>();
                    Controller.BangumiSearchIteration += OnPgcSearchIteration;
                    break;
                case SearchModuleType.Live:
                    break;
                case SearchModuleType.User:
                    break;
                case SearchModuleType.Movie:
                    PgcCollection = new ObservableCollection<SeasonViewModel>();
                    Controller.MovieSearchIteration += OnPgcSearchIteration;
                    break;
                case SearchModuleType.Article:
                    break;
                default:
                    break;
            }

            this.PropertyChanged += OnPropertyChangedAsync;
        }

        /// <summary>
        /// 请求数据.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task RequestDataAsync()
        {
            if (!IsRequested)
            {
                await InitializeRequestAsync();
            }
            else
            {
                await DeltaRequestAsync();
            }

            IsRequested = PageNumber != 0;
        }

        /// <summary>
        /// 初始化请求.
        /// </summary>
        /// <param name="isClearFilter">是否清除筛选条件.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task InitializeRequestAsync(bool isClearFilter = true)
        {
            if (!IsInitializeLoading && !IsDeltaLoading)
            {
                IsInitializeLoading = true;
                Reset(isClearFilter);
                try
                {
                    var queryParameters = GetQueryParameters();
                    await Controller.RequestSearchModuleDataAsync(Type, Keyword, 1, queryParameters);
                }
                catch (ServiceException ex)
                {
                    IsError = true;
                    ErrorText = $"{ResourceToolkit.GetLocaleString(LanguageNames.RequestPopularFailed)}\n{ex.Error?.Message ?? ex.Message}";
                }
                catch (InvalidOperationException invalidEx)
                {
                    IsError = true;
                    ErrorText = invalidEx.Message;
                }

                IsInitializeLoading = false;
            }
        }

        /// <summary>
        /// 增量请求.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task DeltaRequestAsync()
        {
            if (!IsInitializeLoading && !IsDeltaLoading && !IsLoadCompleted)
            {
                IsDeltaLoading = true;
                var queryParameters = GetQueryParameters();
                await Controller.RequestSearchModuleDataAsync(Type, Keyword, PageNumber, queryParameters);
                IsDeltaLoading = false;
            }
        }

        /// <summary>
        /// 清空视图模型中已缓存的数据.
        /// </summary>
        /// <param name="isClearFilter">是否清除筛选条件.</param>
        public void Reset(bool isClearFilter)
        {
            PageNumber = 0;
            IsError = false;
            ErrorText = string.Empty;
            IsLoadCompleted = false;
            IsRequested = false;
            switch (Type)
            {
                case SearchModuleType.Video:
                    if (isClearFilter)
                    {
                        CurrentOrder = VideoOrderCollection.First();
                        CurrentDuration = VideoDurationCollection.First();
                        CurrentPartitionId = VideoPartitionCollection.FirstOrDefault();
                    }

                    VideoCollection.Clear();
                    break;
                case SearchModuleType.Bangumi:
                case SearchModuleType.Movie:
                    PgcCollection.Clear();
                    break;
                case SearchModuleType.Live:
                    break;
                case SearchModuleType.User:
                    break;
                case SearchModuleType.Article:
                    break;
                default:
                    break;
            }
        }

        private void OnVideoSearchIteration(object sender, VideoSearchEventArgs e)
        {
            if (e.Keyword == Keyword)
            {
                IsLoadCompleted = false;
                PageNumber = e.NextPageNumber;
                IsRequested = PageNumber != 0;
                foreach (var item in e.List)
                {
                    if (!VideoCollection.Any(p => p.VideoId == item.Parameter))
                    {
                        VideoCollection.Add(new VideoViewModel(item));
                    }
                }
            }
        }

        private void OnPgcSearchIteration(object sender, PgcSearchEventArgs e)
        {
            if (e.Keyword == Keyword)
            {
                IsLoadCompleted = e.NextPageNumber == -1;
                PageNumber = e.NextPageNumber;
                IsRequested = PageNumber != 0;

                foreach (var item in e.List)
                {
                    if (!PgcCollection.Any(p => p.SeasonId == item.SeasonId))
                    {
                        PgcCollection.Add(SeasonViewModel.CreateFromSearchItem(item));
                    }
                }
            }
        }

        private Dictionary<string, string> GetQueryParameters()
        {
            var result = new Dictionary<string, string>();
            switch (Type)
            {
                case SearchModuleType.Video:
                    result.Add(OrderType, CurrentOrder.Key);
                    result.Add(Duration, CurrentDuration.Key);
                    result.Add(PartitionId, CurrentPartitionId.Key);
                    break;
                case SearchModuleType.Live:
                    break;
                case SearchModuleType.User:
                    break;
                case SearchModuleType.Article:
                    break;
                default:
                    break;
            }

            return result;
        }

        private async void InitializeVideoFiltersAsync()
        {
            VideoOrderCollection = new ObservableCollection<KeyValue<string>>();
            VideoDurationCollection = new ObservableCollection<KeyValue<string>>();
            VideoPartitionCollection = new ObservableCollection<KeyValue<string>>();

            VideoOrderCollection.Add(new KeyValue<string>("default", ResourceToolkit.GetLocaleString(LanguageNames.SortByDefault)));
            VideoOrderCollection.Add(new KeyValue<string>("view", ResourceToolkit.GetLocaleString(LanguageNames.SortByPlay)));
            VideoOrderCollection.Add(new KeyValue<string>("pubdate", ResourceToolkit.GetLocaleString(LanguageNames.SortByNewest)));
            VideoOrderCollection.Add(new KeyValue<string>("danmaku", ResourceToolkit.GetLocaleString(LanguageNames.SortByDanmaku)));

            VideoDurationCollection.Add(new KeyValue<string>("0", ResourceToolkit.GetLocaleString(LanguageNames.FilterByTotalDuration)));
            VideoDurationCollection.Add(new KeyValue<string>("1", ResourceToolkit.GetLocaleString(LanguageNames.FilterByLessThan10Min)));
            VideoDurationCollection.Add(new KeyValue<string>("2", ResourceToolkit.GetLocaleString(LanguageNames.FilterByLessThan30Min)));
            VideoDurationCollection.Add(new KeyValue<string>("3", ResourceToolkit.GetLocaleString(LanguageNames.FilterByLessThan60Min)));
            VideoDurationCollection.Add(new KeyValue<string>("4", ResourceToolkit.GetLocaleString(LanguageNames.FilterByGreaterThan60Min)));

            var totalPartition = PartitionModuleViewModel.Instance.PartitionCollection;
            if (totalPartition.Count == 0)
            {
                await PartitionModuleViewModel.Instance.InitializeAllPartitionAsync();
                totalPartition = PartitionModuleViewModel.Instance.PartitionCollection;
            }

            totalPartition.ToList().ForEach(p => VideoPartitionCollection.Add(new KeyValue<string>(p.PartitionId.ToString(), p.Title)));
            VideoPartitionCollection.Insert(0, new KeyValue<string>("0", ResourceToolkit.GetLocaleString(LanguageNames.Total)));
        }

        private async void OnPropertyChangedAsync(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Contains("Current"))
            {
                if (IsRequested)
                {
                    await InitializeRequestAsync(false);
                }
            }
        }
    }
}
