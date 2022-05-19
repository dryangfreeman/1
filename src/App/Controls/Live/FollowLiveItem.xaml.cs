﻿// Copyright (c) Richasy. All rights reserved.

using System;
using Bili.ViewModels.Interfaces;
using Bili.ViewModels.Uwp;
using Splat;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Bili.App.Controls
{
    /// <summary>
    /// 关注的直播间.
    /// </summary>
    public sealed partial class FollowLiveItem : UserControl
    {
        /// <summary>
        /// <see cref="ViewModel"/>的依赖属性.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(VideoViewModel), typeof(FollowLiveItem), new PropertyMetadata(null));

        /// <summary>
        /// <see cref="Orientation"/>的依赖属性.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(FollowLiveItem), new PropertyMetadata(default, new PropertyChangedCallback(OnOrientationChanged)));

        private readonly INavigationViewModel _navigationViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="FollowLiveItem"/> class.
        /// </summary>
        public FollowLiveItem()
        {
            InitializeComponent();
            _navigationViewModel = Splat.Locator.Current.GetService<INavigationViewModel>();
            Loaded += OnLoaded;
        }

        /// <summary>
        /// 条目被点击时触发.
        /// </summary>
        public event EventHandler ItemClick;

        /// <summary>
        /// 视图模型.
        /// </summary>
        public VideoViewModel ViewModel
        {
            get { return (VideoViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        /// <summary>
        /// 布局方向.
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is Orientation)
            {
                var instance = d as FollowLiveItem;
                instance.CheckOrientation();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
            => CheckOrientation();

        private void CheckOrientation()
        {
            switch (Orientation)
            {
                case Orientation.Vertical:
                    HorizontalContainer.Visibility = Visibility.Collapsed;
                    VerticalContainer.Visibility = Visibility.Visible;
                    break;
                case Orientation.Horizontal:
                    HorizontalContainer.Visibility = Visibility.Visible;
                    VerticalContainer.Visibility = Visibility.Collapsed;
                    break;
                default:
                    break;
            }
        }

        private void OnCardClick(object sender, RoutedEventArgs e)
        {
            ItemClick?.Invoke(this, EventArgs.Empty);
            _navigationViewModel.NavigateToPlayView(ViewModel);
        }

        private void OnItemClick(object sender, VideoViewModel e)
            => ItemClick?.Invoke(this, EventArgs.Empty);
    }
}
