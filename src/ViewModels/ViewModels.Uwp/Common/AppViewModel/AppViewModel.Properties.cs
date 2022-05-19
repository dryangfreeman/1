﻿// Copyright (c) Richasy. All rights reserved.

using System;
using Bili.Controller.Uwp;
using Bili.Models.App.Args;
using Bili.Toolkit.Interfaces;
using Bili.ViewModels.Interfaces;
using ReactiveUI.Fody.Helpers;
using Windows.UI.Xaml;

namespace Bili.ViewModels.Uwp
{
    /// <summary>
    /// <see cref="AppViewModel"/>的属性集.
    /// </summary>
    public partial class AppViewModel
    {
        private readonly IResourceToolkit _resourceToolkit;
        private readonly ISettingsToolkit _settingToolkit;
        private readonly INavigationViewModel _navigationViewModel;
        private readonly BiliController _controller;

        private bool? _isWide;

        /// <summary>
        /// 请求显示提醒.
        /// </summary>
        public event EventHandler<AppTipNotificationEventArgs> RequestShowTip;

        /// <summary>
        /// 请求显示升级提示.
        /// </summary>
        public event EventHandler<UpdateEventArgs> RequestShowUpdateDialog;

        /// <summary>
        /// 请求进行之前的播放.
        /// </summary>
        public event EventHandler RequestContinuePlay;

        /// <summary>
        /// 请求显示图片列表.
        /// </summary>
        public event EventHandler<ShowImageEventArgs> RequestShowImages;

        /// <summary>
        /// <see cref="AppViewModel"/>的单例.
        /// </summary>
        public static AppViewModel Instance { get; } = new Lazy<AppViewModel>(() => new AppViewModel()).Value;

        /// <summary>
        /// 导航面板是否已展开.
        /// </summary>
        [Reactive]
        public bool IsNavigatePaneOpen { get; set; }

        /// <summary>
        /// 是否可以显示返回首页按钮.
        /// </summary>
        [Reactive]
        public bool CanShowHomeButton { get; set; }

        /// <summary>
        /// 页面标题文本.
        /// </summary>
        [Reactive]
        public string HeaderText { get; set; }

        /// <summary>
        /// 主题.
        /// </summary>
        [Reactive]
        public ElementTheme Theme { get; set; }

        /// <summary>
        /// 是否在Xbox上运行.
        /// </summary>
        [Reactive]
        public bool IsXbox { get; set; }

        /// <summary>
        /// 页面左侧或上部的边距.
        /// </summary>
        [Reactive]
        public Thickness PageLeftPadding { get; set; }

        /// <summary>
        /// 页面右侧或下部的边距.
        /// </summary>
        [Reactive]
        public Thickness PageRightPadding { get; set; }

        /// <summary>
        /// 是否可以显示后退按钮.
        /// </summary>
        [Reactive]
        public bool CanShowBackButton { get; set; }
    }
}
