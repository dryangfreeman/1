﻿// Copyright (c) Richasy. All rights reserved.

namespace Richasy.Bili.App.Controls
{
    /// <summary>
    /// 播放数据面板.
    /// </summary>
    public sealed partial class PlayerDashboard : PlayerComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerDashboard"/> class.
        /// </summary>
        public PlayerDashboard()
        {
            this.InitializeComponent();
        }

        private async void OnLikeButtonHoldingCompletedAsync(object sender, System.EventArgs e)
        {
            await ViewModel.TripleAsync();
            CoinButton.ShowBubbles();
            FavoriteButton.ShowBubbles();
        }

        private void OnShareButtonClick(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
        }
    }
}
