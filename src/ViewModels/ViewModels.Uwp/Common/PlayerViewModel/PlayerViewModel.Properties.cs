﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Bilibili.App.View.V1;
using ReactiveUI.Fody.Helpers;
using Richasy.Bili.Controller.Uwp;
using Richasy.Bili.Lib.Interfaces;
using Richasy.Bili.Models.BiliBili;
using Richasy.Bili.Models.Enums;
using Richasy.Bili.Toolkit.Interfaces;
using Windows.Media;
using Windows.Media.Playback;
using Windows.UI.Xaml.Controls;

namespace Richasy.Bili.ViewModels.Uwp
{
    /// <summary>
    /// 播放器视图模型.
    /// </summary>
    public partial class PlayerViewModel
    {
        private readonly INumberToolkit _numberToolkit;
        private readonly IResourceToolkit _resourceToolkit;
        private readonly ISettingsToolkit _settingsToolkit;
        private readonly IFileToolkit _fileToolkit;
        private readonly IHttpProvider _httpProvider;
        private ViewReply _detail;
        private PlayerDashInformation _dashInformation;

        private DashItem _currentAudio;
        private DashItem _currentVideo;

        private List<DashItem> _audioList;
        private List<DashItem> _streamList;

        private MediaPlayer _currentVideoPlayer;
        private MediaPlayer _currentAudioPlayer;

        private MediaTimelineController _timelineController;

        /// <summary>
        /// 单例.
        /// </summary>
        public static PlayerViewModel Instance { get; } = new Lazy<PlayerViewModel>(() => new PlayerViewModel()).Value;

        /// <summary>
        /// 媒体播放控件.
        /// </summary>
        public MediaPlayerElement MediaPlayerElement { get; private set; }

        /// <summary>
        /// 应用视频播放器.
        /// </summary>
        public Control BiliPlayer { get; private set; }

        /// <summary>
        /// 偏好的解码模式.
        /// </summary>
        public PreferCodec PreferCodec => SettingViewModel.Instance.PreferCodec;

        /// <summary>
        /// 标题.
        /// </summary>
        [Reactive]
        public string Title { get; set; }

        /// <summary>
        /// 副标题，发布时间.
        /// </summary>
        [Reactive]
        public string Subtitle { get; set; }

        /// <summary>
        /// 说明.
        /// </summary>
        [Reactive]
        public string Description { get; set; }

        /// <summary>
        /// AV Id.
        /// </summary>
        [Reactive]
        public string AvId { get; set; }

        /// <summary>
        /// BV Id.
        /// </summary>
        [Reactive]
        public string BvId { get; set; }

        /// <summary>
        /// 播放数.
        /// </summary>
        [Reactive]
        public string PlayCount { get; set; }

        /// <summary>
        /// 弹幕数.
        /// </summary>
        [Reactive]
        public string DanmakuCount { get; set; }

        /// <summary>
        /// 点赞数.
        /// </summary>
        [Reactive]
        public string LikeCount { get; set; }

        /// <summary>
        /// 硬币数.
        /// </summary>
        [Reactive]
        public string CoinCount { get; set; }

        /// <summary>
        /// 收藏数.
        /// </summary>
        [Reactive]
        public string FavoriteCount { get; set; }

        /// <summary>
        /// 转发数.
        /// </summary>
        [Reactive]
        public string ShareCount { get; set; }

        /// <summary>
        /// 评论数.
        /// </summary>
        [Reactive]
        public string ReplyCount { get; set; }

        /// <summary>
        /// 播放器地址.
        /// </summary>
        [Reactive]
        public string CoverUrl { get; set; }

        /// <summary>
        /// 发布者.
        /// </summary>
        [Reactive]
        public PublisherViewModel Publisher { get; set; }

        /// <summary>
        /// 是否正在缓冲.
        /// </summary>
        [Reactive]
        public bool IsBuffering { get; set; }

        /// <summary>
        /// 是否显示封面.
        /// </summary>
        [Reactive]
        public bool IsShowCover { get; set; }

        /// <summary>
        /// 关联视频集合.
        /// </summary>
        [Reactive]
        public ObservableCollection<VideoViewModel> RelatedVideoCollection { get; set; }

        /// <summary>
        /// 分集视频集合.
        /// </summary>
        [Reactive]
        public ObservableCollection<ViewPage> PartCollection { get; set; }

        /// <summary>
        /// 当前分P.
        /// </summary>
        [Reactive]
        public ViewPage CurrentPart { get; set; }

        /// <summary>
        /// 当前清晰度.
        /// </summary>
        [Reactive]
        public uint CurrentQuality { get; set; }

        /// <summary>
        /// 是否正在加载.
        /// </summary>
        [Reactive]
        public bool IsDetailLoading { get; set; }

        /// <summary>
        /// 是否出错.
        /// </summary>
        [Reactive]
        public bool IsDetailError { get; set; }

        /// <summary>
        /// 错误文本.
        /// </summary>
        [Reactive]
        public string DetailErrorText { get; set; }

        /// <summary>
        /// 播放信息是否正在加载中.
        /// </summary>
        [Reactive]
        public bool IsPlayInformationLoading { get; set; }

        /// <summary>
        /// 是否在请求播放信息的过程中出错.
        /// </summary>
        [Reactive]
        public bool IsPlayInformationError { get; set; }

        /// <summary>
        /// 请求播放信息出错的错误文本.
        /// </summary>
        [Reactive]
        public string PlayInformationErrorText { get; set; }

        private BiliController Controller { get; } = BiliController.Instance;
    }
}