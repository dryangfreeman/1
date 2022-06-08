﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Bili.Toolkit.Interfaces;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;

namespace Bili.ViewModels.Uwp.Toolbox
{
    /// <summary>
    /// 封面下载器视图模型.
    /// </summary>
    public sealed class CoverDownloaderViewModel : ViewModelBase
    {
        private readonly IResourceToolkit _resourceToolkit;
        private readonly IVideoToolkit _videoToolkit;
        private readonly CoreDispatcher _dispatcher;
        private readonly ObservableAsPropertyHelper<bool> _isDownloading;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoverDownloaderViewModel"/> class.
        /// </summary>
        public CoverDownloaderViewModel(
             IResourceToolkit resourceToolkit,
             IVideoToolkit videoToolkit,
             CoreDispatcher dispatcher)
        {
            _resourceToolkit = resourceToolkit;
            _videoToolkit = videoToolkit;
            _dispatcher = dispatcher;

            DownloadCommand = ReactiveCommand.CreateFromTask(DownloadCoverAsync, outputScheduler: RxApp.MainThreadScheduler);
            LoadPreviewCommand = ReactiveCommand.CreateFromTask(LoadPreviewAsync, outputScheduler: RxApp.MainThreadScheduler);

            _isDownloading = DownloadCommand.IsExecuting.ToProperty(this, x => x.IsDownloading, scheduler: RxApp.MainThreadScheduler);

            this.WhenAnyValue(x => x.ErrorMessage)
                .Subscribe(x => IsShowError = !string.IsNullOrEmpty(x));

            DownloadCommand.ThrownExceptions
                .Merge(LoadPreviewCommand.ThrownExceptions)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(DisplayExAsync);
        }

        /// <summary>
        /// 预览图片.
        /// </summary>
        [Reactive]
        public string CoverUrl { get; set; }

        /// <summary>
        /// 输入Id.
        /// </summary>
        [Reactive]
        public string InputId { get; set; }

        /// <summary>
        /// 是否显示错误信息.
        /// </summary>
        [Reactive]
        public bool IsShowError { get; set; }

        /// <summary>
        /// 错误信息.
        /// </summary>
        [Reactive]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 加载预览命令.
        /// </summary>
        public ReactiveCommand<Unit, Unit> LoadPreviewCommand { get; }

        /// <summary>
        /// 下载命令.
        /// </summary>
        public ReactiveCommand<Unit, Unit> DownloadCommand { get; }

        /// <summary>
        /// 是否正在下载.
        /// </summary>
        public bool IsDownloading => _isDownloading.Value;

        private async Task LoadPreviewAsync()
        {
            if (!string.IsNullOrEmpty(InputId))
            {
                IsShowError = false;
                var type = _videoToolkit.GetVideoIdType(InputId, out var avid);
                if (type == Models.Enums.VideoIdType.Av)
                {
                    // reply = await BiliController.Instance.GetVideoDetailAsync(avid);
                    await Task.CompletedTask;
                }
                else if (type == Models.Enums.VideoIdType.Bv)
                {
                    // reply = await BiliController.Instance.GetVideoDetailAsync(InputId);
                }
                else
                {
                    throw new ArgumentException(_resourceToolkit.GetLocaleString(Models.Enums.LanguageNames.InvalidVideoId));
                }

                CoverUrl = string.Empty;
            }
        }

        private async Task DownloadCoverAsync()
        {
            if (string.IsNullOrEmpty(CoverUrl))
            {
                return;
            }

            IsShowError = false;
            var folder = KnownFolders.PicturesLibrary;

            using (var httpClient = new HttpClient())
            {
                var bytes = await httpClient.GetByteArrayAsync(new Uri(CoverUrl));
                var imgFile = await folder.CreateFileAsync($"{DateTime.Now:yyyy-MM-dd}-{Guid.NewGuid():N}.png");
                await FileIO.WriteBytesAsync(imgFile, bytes);

                await Launcher.LaunchFileAsync(imgFile);
            }
        }

        private async void DisplayExAsync(Exception ex)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ErrorMessage = ex.Message;
            });
        }
    }
}
