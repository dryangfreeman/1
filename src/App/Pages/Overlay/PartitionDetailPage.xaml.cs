﻿// Copyright (c) Richasy. All rights reserved.

using System;
using Bili.Models.Data.Community;
using Bili.Models.Enums;
using Bili.ViewModels.Uwp.Community;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Bili.App.Pages.Overlay
{
    /// <summary>
    /// 分区详情页面.
    /// </summary>
    public sealed partial class PartitionDetailPage : PartitionDetailPageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionDetailPage"/> class.
        /// </summary>
        public PartitionDetailPage() => InitializeComponent();

        /// <inheritdoc/>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter is Partition partition)
            {
                ViewModel.SetPartition(partition);
            }
        }

        private void OnDetailNavigationViewItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            var data = args.InvokedItem as Partition;
            ContentScrollViewer.ChangeView(0, 0, 1);
            ViewModel.SelectPartitionCommand.Execute(data).Subscribe();
        }

        private void OnVideoSortComboBoxSlectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideoSortComboBox.SelectedItem is VideoSortType type
                && ViewModel.SortType != type)
            {
                ViewModel.SortType = type;
                ViewModel.ReloadCommand.Execute().Subscribe();
            }
        }
    }

    /// <summary>
    /// <see cref="PartitionDetailPage"/> 的基类.
    /// </summary>
    public class PartitionDetailPageBase : AppPage<PartitionDetailPageViewModel>
    {
    }
}
