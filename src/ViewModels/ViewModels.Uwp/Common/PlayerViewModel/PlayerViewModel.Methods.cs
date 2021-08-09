﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Linq;
using System.Threading.Tasks;
using Richasy.Bili.Models.App.Constants;
using Richasy.Bili.Models.BiliBili;
using Richasy.Bili.Models.Enums;

namespace Richasy.Bili.ViewModels.Uwp
{
    /// <summary>
    /// 播放器视图模型.
    /// </summary>
    public partial class PlayerViewModel
    {
        private void Reset()
        {
            _videoDetail = null;
            _pgcDetail = null;
            IsDetailError = false;
            _dashInformation = null;
            CurrentFormat = null;
            CurrentPgcEpisode = null;
            CurrentVideoPart = null;
            Publisher = null;
            _initializeProgress = TimeSpan.Zero;
            _lastReportProgress = TimeSpan.Zero;
            IsShowEpisode = false;
            IsShowParts = false;
            IsShowPgcActivityTab = false;
            IsShowSeason = false;
            IsShowRelatedVideos = false;
            VideoPartCollection.Clear();
            RelatedVideoCollection.Clear();
            FormatCollection.Clear();
            PgcSectionCollection.Clear();
            EpisodeCollection.Clear();
            SeasonCollection.Clear();
            _audioList.Clear();
            _videoList.Clear();
            ClearPlayer();
            IsPgc = false;
        }

        private async Task LoadVideoDetailAsync(string videoId)
        {
            if (_videoDetail == null || videoId != AvId)
            {
                Reset();
                IsDetailLoading = true;
                _videoId = Convert.ToInt64(videoId);
                try
                {
                    var detail = await Controller.GetVideoDetailAsync(_videoId);
                    _videoDetail = detail;
                }
                catch (Exception ex)
                {
                    IsDetailError = true;
                    DetailErrorText = _resourceToolkit.GetLocaleString(LanguageNames.RequestVideoFailed) + $"\n{ex.Message}";
                    IsDetailLoading = false;
                    return;
                }

                InitializeVideoDetail();
                IsDetailLoading = false;
            }

            var partId = CurrentVideoPart == null ? 0 : CurrentVideoPart.Page.Cid;
            await ChangeVideoPartAsync(partId);
        }

        private async Task LoadPgcDetailAsync(int episodeId, int seasonId = 0)
        {
            if (_pgcDetail == null ||
                episodeId.ToString() != EpisodeId ||
                seasonId.ToString() != SeasonId)
            {
                Reset();
                IsPgc = true;
                IsDetailLoading = true;
                EpisodeId = episodeId.ToString();
                SeasonId = seasonId.ToString();

                try
                {
                    var detail = await Controller.GetPgcDisplayInformationAsync(episodeId, seasonId);
                    _pgcDetail = detail;
                }
                catch (Exception ex)
                {
                    IsDetailError = true;
                    DetailErrorText = _resourceToolkit.GetLocaleString(LanguageNames.RequestPgcFailed) + $"\n{ex.Message}";
                    IsDetailLoading = false;
                    return;
                }

                InitializePgcDetail();
                IsDetailLoading = false;
            }

            var id = 0;
            if (CurrentPgcEpisode != null)
            {
                id = CurrentPgcEpisode.Id;
            }
            else if (episodeId > 0)
            {
                id = episodeId;
            }
            else if (_pgcDetail.UserStatus?.Progress != null)
            {
                id = _pgcDetail.UserStatus.Progress.LastEpisodeId;
                _initializeProgress = TimeSpan.FromSeconds(_pgcDetail.UserStatus.Progress.LastTime);
            }

            await ChangePgcEpisodeAsync(id);
        }

        private void InitializeVideoDetail()
        {
            if (_videoDetail == null)
            {
                return;
            }

            Title = _videoDetail.Arc.Title;
            Subtitle = DateTimeOffset.FromUnixTimeSeconds(_videoDetail.Arc.Pubdate).ToString("yy/MM/dd HH:mm");
            Description = _videoDetail.Arc.Desc;
            Publisher = new PublisherViewModel(_videoDetail.Arc.Author);
            AvId = _videoDetail.Arc.Aid.ToString();
            BvId = _videoDetail.Bvid;
            SeasonId = string.Empty;
            EpisodeId = string.Empty;
            PlayCount = _numberToolkit.GetCountText(_videoDetail.Arc.Stat.View);
            DanmakuCount = _numberToolkit.GetCountText(_videoDetail.Arc.Stat.Danmaku);
            LikeCount = _numberToolkit.GetCountText(_videoDetail.Arc.Stat.Like);
            CoinCount = _numberToolkit.GetCountText(_videoDetail.Arc.Stat.Coin);
            FavoriteCount = _numberToolkit.GetCountText(_videoDetail.Arc.Stat.Fav);
            ShareCount = _numberToolkit.GetCountText(_videoDetail.Arc.Stat.Share);
            ReplyCount = _numberToolkit.GetCountText(_videoDetail.Arc.Stat.Reply);
            CoverUrl = _videoDetail.Arc.Pic;

            IsLikeChecked = _videoDetail.ReqUser.Like == 1;
            IsCoinChecked = _videoDetail.ReqUser.Coin == 1;
            IsFavoriteChecked = _videoDetail.ReqUser.Favorite == 1;

            foreach (var page in _videoDetail.Pages)
            {
                VideoPartCollection.Add(new VideoPartViewModel(page));
            }

            IsShowParts = VideoPartCollection.Count > 1;

            var relates = _videoDetail.Relates.Where(p => p.Goto.Equals(ServiceConstants.Pgc, StringComparison.OrdinalIgnoreCase) || p.Goto.Equals(ServiceConstants.Av, StringComparison.OrdinalIgnoreCase));
            IsShowRelatedVideos = relates.Count() > 0;
            foreach (var video in relates)
            {
                RelatedVideoCollection.Add(new VideoViewModel(video));
            }
        }

        private void InitializePgcDetail()
        {
            if (_pgcDetail == null)
            {
                return;
            }

            Title = _pgcDetail.Title;
            Subtitle = _pgcDetail.OriginName ?? _pgcDetail.DynamicSubtitle ?? _pgcDetail.BadgeText ?? Subtitle;
            Description = $"{_pgcDetail.TypeDescription}\n" +
                $"{_pgcDetail.PublishInformation.DisplayReleaseDate}\n" +
                $"{_pgcDetail.PublishInformation.DisplayProgress}";
            AvId = string.Empty;
            BvId = string.Empty;
            SeasonId = _pgcDetail.SeasonId.ToString();
            PlayCount = _numberToolkit.GetCountText(_pgcDetail.InformationStat.PlayCount);
            DanmakuCount = _numberToolkit.GetCountText(_pgcDetail.InformationStat.DanmakuCount);
            LikeCount = _numberToolkit.GetCountText(_pgcDetail.InformationStat.LikeCount);
            CoinCount = _numberToolkit.GetCountText(_pgcDetail.InformationStat.CoinCount);
            FavoriteCount = _numberToolkit.GetCountText(_pgcDetail.InformationStat.FavoriteCount);
            ShareCount = _numberToolkit.GetCountText(_pgcDetail.InformationStat.ShareCount);
            ReplyCount = _numberToolkit.GetCountText(_pgcDetail.InformationStat.ReplyCount);
            CoverUrl = _pgcDetail.Cover;

            IsShowPgcActivityTab = _pgcDetail.ActivityTab != null;
            if (IsShowPgcActivityTab)
            {
                PgcActivityTab = _pgcDetail.ActivityTab.DisplayName;
            }

            if (_pgcDetail.UserStatus != null)
            {
                IsFollow = _pgcDetail.UserStatus.IsFollow == 1;
            }

            if (_pgcDetail.Modules != null && _pgcDetail.Modules.Count > 0)
            {
                var seasonModule = _pgcDetail.Modules.Where(p => p.Style == ServiceConstants.Season).FirstOrDefault();
                IsShowSeason = seasonModule != null && seasonModule.Data.Seasons.Count > 1;
                if (IsShowSeason)
                {
                    foreach (var item in seasonModule.Data.Seasons)
                    {
                        SeasonCollection.Add(new PgcSeasonViewModel(item, item.SeasonId.ToString() == SeasonId));
                    }
                }

                var episodeModule = _pgcDetail.Modules.Where(p => p.Style == ServiceConstants.Positive).FirstOrDefault();
                IsShowEpisode = episodeModule != null && episodeModule.Data.Episodes.Count > 1;
                if (IsShowEpisode)
                {
                    foreach (var item in episodeModule.Data.Episodes)
                    {
                        EpisodeCollection.Add(new PgcEpisodeViewModel(item, false));
                    }
                }

                var partModuleList = _pgcDetail.Modules.Where(p => p.Style == ServiceConstants.Section).ToList();
                IsShowSection = partModuleList.Count > 0;
            }
        }

        private async Task InitializeVideoPlayInformationAsync(PlayerDashInformation videoPlayView)
        {
            _audioList = videoPlayView.VideoInformation.Audio.ToList();
            _videoList = videoPlayView.VideoInformation.Video.ToList();

            _currentAudio = null;
            _currentVideo = null;

            FormatCollection.Clear();
            videoPlayView.SupportFormats.ForEach(p => FormatCollection.Add(new VideoFormatViewModel(p, false)));

            var formatId = CurrentFormat == null ?
                _settingsToolkit.ReadLocalSetting(Models.Enums.SettingNames.DefaultVideoFormat, 64) :
                CurrentFormat.Quality;

            await ChangeFormatAsync(formatId);
        }

        private void InitializeTimer()
        {
            if (_progressTimer == null)
            {
                _progressTimer = new Windows.UI.Xaml.DispatcherTimer();
                _progressTimer.Interval = TimeSpan.FromSeconds(5);
                _progressTimer.Tick += OnProgressTimerTickAsync;
            }
        }

        private int GetPreferCodecId()
        {
            var id = 7;
            switch (PreferCodec)
            {
                case Models.Enums.PreferCodec.H265:
                    id = 12;
                    break;
                default:
                    break;
            }

            return id;
        }

        private void CheckPartSelection()
        {
            foreach (var item in VideoPartCollection)
            {
                item.IsSelected = item.Data.Equals(CurrentVideoPart);
            }
        }

        private void CheckEpisodeSelection()
        {
            foreach (var item in EpisodeCollection)
            {
                item.IsSelected = item.Data.Equals(CurrentPgcEpisode);
            }
        }

        private void CheckFormatSelection()
        {
            foreach (var item in FormatCollection)
            {
                item.IsSelected = item.Data.Equals(CurrentFormat);
            }
        }

        private async void OnProgressTimerTickAsync(object sender, object e)
        {
            if (_videoDetail == null || CurrentVideoPart == null)
            {
                return;
            }

            if (_currentVideoPlayer == null || _currentVideoPlayer.PlaybackSession == null)
            {
                return;
            }

            var progress = _currentVideoPlayer.PlaybackSession.Position;
            if (progress != _lastReportProgress)
            {
                await Controller.ReportHistoryAsync(_videoId, CurrentVideoPart.Page.Cid, _currentVideoPlayer.PlaybackSession.Position);
                _lastReportProgress = progress;
            }
        }
    }
}
