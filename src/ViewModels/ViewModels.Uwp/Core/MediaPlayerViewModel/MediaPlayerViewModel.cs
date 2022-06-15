﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Bili.Lib.Interfaces;
using Bili.Models.Data.Live;
using Bili.Models.Data.Pgc;
using Bili.Models.Data.Player;
using Bili.Models.Data.Video;
using Bili.Models.Enums;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces;
using Bili.ViewModels.Uwp.Account;
using FFmpegInterop;
using ReactiveUI;
using Windows.UI.Core;

namespace Bili.ViewModels.Uwp.Core
{
    /// <summary>
    /// 媒体播放器视图模型.
    /// </summary>
    public sealed partial class MediaPlayerViewModel : ViewModelBase, IReloadViewModel, IErrorViewModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaPlayerViewModel"/> class.
        /// </summary>
        public MediaPlayerViewModel(
            IPlayerProvider playerProvider,
            ILiveProvider liveProvider,
            IResourceToolkit resourceToolkit,
            IFileToolkit fileToolkit,
            ISettingsToolkit settingsToolkit,
            AccountViewModel accountViewModel,
            CoreDispatcher dispatcher)
        {
            _playerProvider = playerProvider;
            _liveProvider = liveProvider;
            _resourceToolkit = resourceToolkit;
            _fileToolkit = fileToolkit;
            _settingsToolkit = settingsToolkit;
            _accountViewModel = accountViewModel;
            _dispatcher = dispatcher;

            _liveConfig = new FFmpegInteropConfig();
            _liveConfig.FFmpegOptions.Add("referer", "https://live.bilibili.com/");
            _liveConfig.FFmpegOptions.Add("user-agent", "Mozilla/5.0 BiliDroid/1.12.0 (bbcallen@gmail.com)");

            Volume = _settingsToolkit.ReadLocalSetting(SettingNames.Volume, 100d);
            PlaybackRate = _settingsToolkit.ReadLocalSetting(SettingNames.PlaybackRate, 1d);

            Formats = new ObservableCollection<FormatInformation>();
            ReloadCommand = ReactiveCommand.CreateFromTask(LoadAsync, outputScheduler: RxApp.MainThreadScheduler);
            ChangePlayerStatusCommand = ReactiveCommand.CreateFromTask<PlayerStatus>(ChangePlayerStatusAsync, outputScheduler: RxApp.MainThreadScheduler);
            ChangePartCommand = ReactiveCommand.CreateFromTask<VideoIdentifier>(ChangePartAsync, outputScheduler: RxApp.MainThreadScheduler);
            ResetProgressHistoryCommand = ReactiveCommand.Create(ResetProgressHistory, outputScheduler: RxApp.MainThreadScheduler);
            ClearCommand = ReactiveCommand.Create(Reset, outputScheduler: RxApp.MainThreadScheduler);
            ChangeLiveAudioOnlyCommand = ReactiveCommand.CreateFromTask<bool>(ChangeLiveAudioOnlyAsync, outputScheduler: RxApp.MainThreadScheduler);
            ChangeFormatCommand = ReactiveCommand.CreateFromTask<FormatInformation>(ChangeFormatAsync, outputScheduler: RxApp.MainThreadScheduler);

            _isReloading = ReloadCommand.IsExecuting.ToProperty(this, x => x.IsReloading, scheduler: RxApp.MainThreadScheduler);

            ReloadCommand.ThrownExceptions.Subscribe(DisplayException);
        }

        /// <summary>
        /// 设置视频播放数据.
        /// </summary>
        /// <param name="data">视频视图数据.</param>
        public void SetVideoData(VideoPlayerView data)
        {
            _viewData = data;
            _videoType = VideoType.Video;
            ReloadCommand.Execute().Subscribe();
        }

        /// <summary>
        /// 设置 PGC 播放数据.
        /// </summary>
        /// <param name="view">PGC 内容视图.</param>
        /// <param name="episode">单集信息.</param>
        public void SetPgcData(PgcPlayerView view, EpisodeInformation episode)
        {
            _viewData = view;
            _currentEpisode = episode;
            _videoType = VideoType.Pgc;
            ReloadCommand.Execute().Subscribe();
        }

        /// <summary>
        /// 设置直播播放数据.
        /// </summary>
        /// <param name="data">直播视图数据.</param>
        public void SetLiveData(LivePlayerView data)
        {
            _viewData = data;
            _videoType = VideoType.Live;
            ReloadCommand.Execute().Subscribe();
        }

        /// <inheritdoc/>
        public void DisplayException(Exception exception)
        {
            IsError = true;
            var msg = GetErrorMessage(exception);
            ErrorText = $"{_resourceToolkit.GetLocaleString(LanguageNames.RequestVideoFailed)}\n{msg}";
            LogException(exception);
        }

        private void Reset()
        {
            ResetPlayer();
            ResetMediaData();
            ResetVideoData();
            ResetLiveData();
        }

        private async Task LoadAsync()
        {
            Reset();
            if (_videoType == VideoType.Video)
            {
                await LoadVideoAsync();
            }
            else if (_videoType == VideoType.Pgc)
            {
                await LoadEpisodeAsync();
            }
            else if (_videoType == VideoType.Live)
            {
                await LoadLiveAsync();
            }
        }

        private async Task ChangePartAsync(VideoIdentifier part)
        {
            if (_videoType == VideoType.Video)
            {
                await ChangeVideoPartAsync(part);
            }
            else if (_videoType == VideoType.Pgc)
            {
                await ChangeEpisodeAsync(part);
            }
        }

        private async Task ChangeFormatAsync(FormatInformation information)
        {
            if (_videoType == VideoType.Video
                || _videoType == VideoType.Pgc)
            {
                await SelectVideoFormatAsync(information);
            }
            else if (_videoType == VideoType.Live)
            {
                await SelectLiveFormatAsync(information);
            }
        }

        private async Task ChangePlayerStatusAsync(PlayerStatus status)
        {
            Status = status;
            if (Status == PlayerStatus.Playing)
            {
                if (_videoType == VideoType.Video)
                {
                    CheckVideoHistory();
                }
                else if (_videoType == VideoType.Pgc)
                {
                    CheckEpisodeHistory();
                }
            }

            await Task.CompletedTask;
        }

        private void ResetProgressHistory()
        {
            _initializeProgress = TimeSpan.Zero;
            if (_videoType == VideoType.Video && _viewData is VideoPlayerView videoView)
            {
                videoView.Progress = null;
            }
            else if (_videoType == VideoType.Pgc && _viewData is PgcPlayerView pgcView)
            {
                pgcView.Progress = null;
            }
        }
    }
}