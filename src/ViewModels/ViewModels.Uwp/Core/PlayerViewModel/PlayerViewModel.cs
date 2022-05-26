﻿// Copyright (c) Richasy. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Bili.Locator.Uwp;
using Bili.Models.App;
using Bili.Models.App.Constants;
using Bili.Models.App.Other;
using Bili.Models.BiliBili;
using Bili.Models.Data.Live;
using Bili.Models.Data.Pgc;
using Bili.Models.Data.Video;
using Bili.Models.Enums;
using Bili.ViewModels.Uwp.Common;
using Bilibili.App.View.V1;
using FFmpegInterop;
using Splat;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace Bili.ViewModels.Uwp.Core
{
    /// <summary>
    /// 播放器视图模型.
    /// </summary>
    public partial class PlayerViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerViewModel"/> class.
        /// </summary>
        public PlayerViewModel()
        {
            RelatedVideoCollection = new ObservableCollection<VideoViewModel>();
            ViewLaterVideoCollection = new ObservableCollection<VideoViewModel>();
            VideoPartCollection = new ObservableCollection<VideoPartViewModel>();
            FormatCollection = new ObservableCollection<VideoFormatViewModel>();
            EpisodeCollection = new ObservableCollection<PgcEpisodeViewModel>();
            SeasonCollection = new ObservableCollection<PgcSeasonViewModel>();
            PgcSectionCollection = new ObservableCollection<PgcSectionViewModel>();
            LiveAppPlayLineCollection = new ObservableCollection<LiveAppPlayLineViewModel>();
            LiveAppQualityCollection = new ObservableCollection<LiveAppQualityViewModel>();
            LiveDanmakuCollection = new ObservableCollection<LiveDanmakuMessage>();
            FavoriteMetaCollection = new ObservableCollection<FavoriteMetaViewModel>();
            SubtitleIndexCollection = new ObservableCollection<SubtitleIndexItemViewModel>();
            StaffCollection = new ObservableCollection<UserViewModel>();
            ChoiceCollection = new ObservableCollection<InteractionChoice>();
            TagCollection = new ObservableCollection<VideoTag>();
            UgcSectionCollection = new ObservableCollection<Section>();
            UgcEpisodeCollection = new ObservableCollection<VideoViewModel>();
            PlaybackRateNodeCollection = new ObservableCollection<double>();
            _audioList = new List<DashItem>();
            _videoList = new List<DashItem>();
            _subtitleList = new List<SubtitleItem>();
            _lastReportProgress = TimeSpan.Zero;
            _historyVideoList = new List<string>();

            _liveFFConfig = new FFmpegInteropConfig();

            _liveFFConfig.FFmpegOptions.Add("referer", "https://live.bilibili.com/");
            _liveFFConfig.FFmpegOptions.Add("user-agent", "Mozilla/5.0 BiliDroid/1.12.0 (bbcallen@gmail.com)");
            ServiceLocator.Instance.LoadService(out _numberToolkit)
                                   .LoadService(out _resourceToolkit)
                                   .LoadService(out _settingsToolkit)
                                   .LoadService(out _fileToolkit)
                                   .LoadService(out _logger);
            _navigationViewModel = Splat.Locator.Current.GetService<NavigationViewModel>();
            _appViewModel = Splat.Locator.Current.GetService<AppViewModel>();

            PlayerDisplayMode = _settingsToolkit.ReadLocalSetting(SettingNames.DefaultPlayerDisplayMode, PlayerDisplayMode.Default);
            _appViewModel.IsShowTitleBar = PlayerDisplayMode == PlayerDisplayMode.Default;
            IsShowDanmakuBar = _settingsToolkit.ReadLocalSetting(SettingNames.IsShowDanmakuBar, false);
            CanShowSubtitle = _settingsToolkit.ReadLocalSetting(SettingNames.CanShowSubtitle, true);
            SubtitleConvertType = _settingsToolkit.ReadLocalSetting(SettingNames.SubtitleConvertType, Models.Enums.App.SubtitleConvertType.None);
            Volume = _settingsToolkit.ReadLocalSetting(SettingNames.Volume, 100d);
            PlaybackRate = _settingsToolkit.ReadLocalSetting(SettingNames.PlaybackRate, 1d);
            IsOnlyShowIndex = _settingsToolkit.ReadLocalSetting(SettingNames.IsOnlyShowIndex, false);
            IsLiveAudioOnly = _settingsToolkit.ReadLocalSetting(SettingNames.IsLiveAudioOnly, false);
            InitializeTimer();
            PropertyChanged += OnPropertyChanged;
            LiveDanmakuCollection.CollectionChanged += OnLiveDanmakuCollectionChanged;
            TagCollection.CollectionChanged += OnTagCollectionChanged;
            Controller.LiveMessageReceived += OnLiveMessageReceivedAsync;
            Controller.LoggedOut += OnUserLoggedOut;

            ApplicationView.GetForCurrentView().VisibleBoundsChanged += OnAppViewVisibleBoundsChanged;
            SubtitleConvertTypeCollection = new ObservableCollection<Models.Enums.App.SubtitleConvertType>
            {
                Models.Enums.App.SubtitleConvertType.None,
                Models.Enums.App.SubtitleConvertType.ToTraditionalChinese,
                Models.Enums.App.SubtitleConvertType.ToSimplifiedChinese,
            };
        }

        /// <summary>
        /// 媒体播放器数据已更新.
        /// </summary>
        public event EventHandler MediaPlayerUpdated;

        /// <summary>
        /// 数据请求完成时发生.
        /// </summary>
        public event EventHandler Loaded;

        /// <summary>
        /// 保存媒体控件.
        /// </summary>
        /// <param name="playerControl">标准播放器控件.</param>
        public void ApplyMediaControl(MediaPlayerElement playerControl)
        {
            if (BiliPlayer != null)
            {
                BiliPlayer.PointerEntered -= OnBiliPlayerPointerEntered;
                BiliPlayer.PointerExited -= OnBiliPlayerPointerExited;
                BiliPlayer.PointerMoved -= OnBiliPlayerPointerMoved;
            }

            BiliPlayer = playerControl;
            BiliPlayer.PointerEntered += OnBiliPlayerPointerEntered;
            BiliPlayer.PointerExited += OnBiliPlayerPointerExited;
            BiliPlayer.PointerMoved += OnBiliPlayerPointerMoved;
        }

        /// <summary>
        /// 检查是否可以返回到主页，或是返回至上一个视频.
        /// </summary>
        /// <returns>是否可以返回到主页.</returns>
        public async Task<bool> CheckBackAsync()
        {
            _progressTimer.Stop();
            DanmakuViewModel.Instance.Reset();
            if (_historyVideoList.Count > 0)
            {
                var lastVideo = _historyVideoList.Last();
                _historyVideoList.Remove(lastVideo);
                IsDetailCanLoaded = true;
                IsPlayInformationError = false;
                _appViewModel.CanShowHomeButton = _historyVideoList.Count > 0;

                await LoadVideoDetailAsync(lastVideo, true);

                _progressTimer.Start();
                InitDownload();
                Loaded?.Invoke(this, EventArgs.Empty);
                return false;
            }
            else
            {
                await ClearInitViewModelAsync();
            }

            return true;
        }

        /// <summary>
        /// 直接返回首页.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task BackToHomeAsync()
        {
            _progressTimer.Stop();
            _historyVideoList.Clear();
            DanmakuViewModel.Instance.Reset();
            IsPlayInformationError = false;
            IsShowNextVideoTip = false;
            await ClearInitViewModelAsync();
        }

        /// <summary>
        /// 视频加载.
        /// </summary>
        /// <param name="vm">视图模型.</param>
        /// <param name="isRefresh">是否刷新.</param>
        /// <param name="shouldResetMode">是否需要重置播放模式.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task LoadAsync(object vm, bool isRefresh = false, bool shouldResetMode = true)
        {
            var videoId = string.Empty;
            var seasonId = 0;

            var isReleated = false;
            var type = VideoType.Video;
            var title = string.Empty;
            var needBiliPlus = false;
            IsPv = false;

            CurrentPlayingRecord record = null;

            if (vm is VideoViewModel videoVM)
            {
                HandleVideoViewModel(videoVM);
            }
            else if (vm is VideoInformation videoInfo)
            {
                videoId = videoInfo.Identifier.Id;
                type = VideoType.Video;
                title = videoInfo.Identifier.Title;
                IsShowViewLater = false;
                ViewLaterVideoCollection.Clear();
            }
            else if (vm is LiveInformation liveInfo)
            {
                videoId = liveInfo.Identifier.Id;
                type = VideoType.Live;
                title = liveInfo.Identifier.Title;
                IsShowViewLater = false;
                ViewLaterVideoCollection.Clear();
            }
            else if (vm is EpisodeInformation episodeInfo)
            {
                videoId = episodeInfo.Identifier.Id;
                type = VideoType.Pgc;
                title = episodeInfo.Identifier.Title;
                isReleated = episodeInfo.IsPreviewVideo;
                IsShowViewLater = false;
                ViewLaterVideoCollection.Clear();
            }
            else if (vm is SeasonViewModel seasonVM)
            {
                videoId = seasonVM.EpisodeId.ToString();
                seasonId = seasonVM.SeasonId;
                title = seasonVM.Title;
                type = VideoType.Pgc;
            }
            else if (vm is PgcSeason seasonData)
            {
                videoId = "0";
                seasonId = seasonData.SeasonId;
                title = seasonData.Title;
                type = VideoType.Pgc;
            }
            else if (vm is PgcEpisodeDetail episodeData)
            {
                videoId = episodeData.Id.ToString();
                seasonId = 0;
                IsPv = episodeData.IsPV == 1;
                title = episodeData.Title;
                type = VideoType.Pgc;
            }
            else if (vm is CurrentPlayingRecord r)
            {
                record = r;
            }
            else if (vm is List<VideoViewModel> viewLaterItems)
            {
                IsShowViewLater = true;
                ViewLaterVideoCollection.Clear();
                viewLaterItems.ForEach(p => ViewLaterVideoCollection.Add(p));
                var first = viewLaterItems.First();
                HandleVideoViewModel(first);
            }

            if (record == null)
            {
                record = new CurrentPlayingRecord(videoId, seasonId, type)
                {
                    IsRelated = isReleated,
                    Title = title,
                    NeedBiliPlus = needBiliPlus,
                };
            }

            await LoadAsync(record, isRefresh, shouldResetMode);

            void HandleVideoViewModel(VideoViewModel internalVM)
            {
                videoId = internalVM.VideoId;
                type = internalVM.VideoType;
                isReleated = internalVM.IsRelated;
                title = internalVM.Title;

                // 番剧出差账户作为特例，将被归为番剧.
                if (internalVM.Publisher != null && internalVM.Publisher.IsRegionalAnimeUser())
                {
                    type = VideoType.Pgc;
                    needBiliPlus = true;
                }

                if (ViewLaterVideoCollection.Contains(internalVM))
                {
                    IsShowViewLater = true;
                    foreach (var item in ViewLaterVideoCollection)
                    {
                        item.IsSelected = item.VideoId == internalVM.VideoId;
                    }
                }
                else
                {
                    IsShowViewLater = false;
                    ViewLaterVideoCollection.Clear();
                }
            }
        }

        /// <summary>
        /// 视频加载.
        /// </summary>
        /// <param name="record">播放快照.</param>
        /// <param name="isRefresh">是否刷新.</param>
        /// <param name="shouldResetMode">是否需要重置播放模式.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task LoadAsync(CurrentPlayingRecord record, bool isRefresh = false, bool shouldResetMode = true)
        {
            _videoType = record.VideoType;
            var isReleated = record.IsRelated;
            var videoId = record.VideoId;
            var seasonId = record.SeasonId;
            IsDetailCanLoaded = true;
            IsPlayInformationError = false;
            InitializePlaybackRateProperties();
            IsInfiniteLoop = false;

            if (shouldResetMode)
            {
                var preferPlayerMode = _settingsToolkit.ReadLocalSetting(SettingNames.DefaultPlayerDisplayMode, PlayerDisplayMode.Default);
                PlayerDisplayMode = preferPlayerMode;
            }

            if (!isReleated)
            {
                _historyVideoList.Clear();
            }
            else
            {
                if (_historyVideoList.Contains(AvId))
                {
                    _historyVideoList.Remove(AvId);
                }

                _historyVideoList.Add(AvId);
            }

            _appViewModel.CanShowHomeButton = _historyVideoList.Count > 0;
            switch (_videoType)
            {
                case VideoType.Video:
                    await LoadVideoDetailAsync(videoId, isRefresh);
                    break;
                case VideoType.Pgc:
                    if (record.NeedBiliPlus)
                    {
                        // 先通过 BiliPlus 获取真实剧集信息.
                        IsDetailLoading = true;
                        var data = await Controller.GetBiliPlusBangumiAsync(record.VideoId);
                        if (data != null)
                        {
                            var epId = data.PlayUrl.Split('/').Last();
                            videoId = !string.IsNullOrEmpty(epId) && epId.Contains("ep")
                                ? epId.Replace("ep", string.Empty)
                                : string.Empty;
                            seasonId = Convert.ToInt32(data.SeasonId);
                            record.Title = data.Title;
                        }
                    }

                    await LoadPgcDetailAsync(string.IsNullOrEmpty(videoId) ? 0 : Convert.ToInt32(videoId), seasonId, isRefresh, record.Title);
                    break;
                case VideoType.Live:
                    await LoadLiveDetailAsync(Convert.ToInt32(videoId));
                    break;
                default:
                    break;
            }

            _progressTimer.Start();
            InitDownload();

            if (_videoType != VideoType.Live)
            {
                await RecordInitViewModelToLocalAsync(videoId, seasonId, _videoType, Title);
            }

            if (record.DisplayMode != PlayerDisplayMode.Default)
            {
                PlayerDisplayMode = record.DisplayMode;
            }

            Loaded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 改变合集分区.
        /// </summary>
        /// <param name="section">合集分区.</param>
        public void ChangeUgcSection(Section section)
        {
            UgcEpisodeCollection.Clear();
            CurrentUgcSection = section;
            if (section?.Episodes?.Count > 0)
            {
                section.Episodes.ToList().ForEach(p => UgcEpisodeCollection.Add(new VideoViewModel(p)));
            }

            CheckUgcEpisodeSelection();
        }

        /// <summary>
        /// 改变当前分P.
        /// </summary>
        /// <param name="partId">分P Id.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ChangeVideoPartAsync(long partId)
        {
            if (partId != 0 && VideoPartCollection.Any(p => p.Data.Page.Cid == partId))
            {
                var targetPart = VideoPartCollection.Where(p => p.Data.Page.Cid == partId).FirstOrDefault();
                CurrentVideoPart = targetPart.Data;
            }
            else if (!IsInteraction)
            {
                if (VideoPartCollection.Any())
                {
                    CurrentVideoPart = VideoPartCollection.First().Data;
                }
            }

            CheckPartSelection();

            var id = GetCurrentPartId();
            try
            {
                IsPlayInformationLoading = true;
                var play = await Controller.GetVideoPlayInformationAsync(_videoId, id);
                if (play != null)
                {
                    _playerInformation = play;
                }

                // 剔除 P2P CDN URL
                if (_settingsToolkit.ReadLocalSetting(SettingNames.DisableP2PCdn, false))
                {
                    if (_playerInformation.VideoInformation.Audio != null)
                    {
                        foreach (var item in _playerInformation.VideoInformation.Audio.Where(p => !p.BaseUrl.Contains("bilivideo.com")))
                        {
                            item.BaseUrl = item.BackupUrl.Find(url => url.Contains("bilivideo.com")) ?? item.BaseUrl;
                        }
                    }

                    if (_playerInformation.VideoInformation.Video != null)
                    {
                        foreach (var item in _playerInformation.VideoInformation.Video.Where(p => !p.BaseUrl.Contains("bilivideo.com")))
                        {
                            item.BaseUrl = item.BackupUrl.Find(url => url.Contains("bilivideo.com")) ?? item.BaseUrl;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                IsPlayInformationError = true;
                PlayInformationErrorText = _resourceToolkit.GetLocaleString(LanguageNames.RequestVideoFailed) + $"\n{ex.Message}";
            }

            IsPlayInformationLoading = false;

            if (_playerInformation != null)
            {
                ClearPlayer();

                if (_videoDetail.History != null && _videoDetail.History.Cid == partId)
                {
                    _initializeProgress = TimeSpan.FromSeconds(_videoDetail.History.Progress);
                }

                await InitializeVideoPlayInformationAsync(_playerInformation);
                await DanmakuViewModel.Instance.LoadAsync(_videoDetail.Arc.Aid, id);
                await InitializeSubtitleIndexAsync();
                ViewerCount = await Controller.GetOnlineViewerCountAsync(Convert.ToInt32(_videoDetail.Arc.Aid), Convert.ToInt32(id));
            }
        }

        /// <summary>
        /// 改变PGC当前分集.
        /// </summary>
        /// <param name="episodeId">分集Id.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ChangePgcEpisodeAsync(int episodeId)
        {
            if (episodeId != 0 && EpisodeCollection.Any(p => p.Data.Id == episodeId))
            {
                var targetPart = EpisodeCollection.Where(p => p.Data.Id == episodeId).FirstOrDefault();
                CurrentPgcEpisode = targetPart.Data;
            }
            else
            {
                if (PgcSectionCollection.Count > 0)
                {
                    foreach (var section in PgcSectionCollection)
                    {
                        var epi = section.Episodes.FirstOrDefault(p => p.Data.Id == episodeId);
                        if (epi != null)
                        {
                            CurrentPgcEpisode = epi.Data;
                        }
                    }
                }

                if (CurrentPgcEpisode == null)
                {
                    CurrentPgcEpisode = EpisodeCollection.FirstOrDefault()?.Data;
                }
            }

            if (CurrentPgcEpisode == null)
            {
                // 没有分集，弹出警告.
                if (_pgcDetail.Warning != null)
                {
                    IsPlayInformationError = true;
                    PlayInformationErrorText = _pgcDetail.Warning.Message;
                }

                return;
            }

            EpisodeId = CurrentPgcEpisode?.Id.ToString() ?? string.Empty;
            CheckEpisodeSelection();
            ReplyModuleViewModel.Instance.SetInformation(Convert.ToInt32(CurrentPgcEpisode.Aid), Models.Enums.Bili.ReplyType.Video);

            try
            {
                IsPlayInformationLoading = true;
                var proxyPack = GetProxyAndArea(_pgcDetail?.Title, true);
                var play = await Controller.GetPgcPlayInformationAsync(CurrentPgcEpisode.PartId, CurrentPgcEpisode.Id, Convert.ToInt32(CurrentPgcEpisode.Report.SeasonType), proxyPack.Item1, proxyPack.Item2);
                if (play != null && play.VideoInformation != null)
                {
                    _playerInformation = play;
                }
                else
                {
                    IsPlayInformationError = true;
                    PlayInformationErrorText = _resourceToolkit.GetLocaleString(LanguageNames.RequestPgcFailed);
                }
            }
            catch (Exception ex)
            {
                IsPlayInformationError = true;
                PlayInformationErrorText = _resourceToolkit.GetLocaleString(LanguageNames.RequestPgcFailed) + $"\n{ex.Message}";
            }

            IsPlayInformationLoading = false;

            if (_playerInformation != null)
            {
                ClearPlayer();
                await InitializeVideoPlayInformationAsync(_playerInformation);
                await DanmakuViewModel.Instance.LoadAsync(CurrentPgcEpisode.Aid, CurrentPgcEpisode.PartId);
                await InitializeSubtitleIndexAsync();
            }

            try
            {
                ViewerCount = await Controller.GetOnlineViewerCountAsync(CurrentPgcEpisode.Aid, CurrentPgcEpisode.PartId);
                var interaction = await Controller.GetPgcEpisodeInteractionAsync(CurrentPgcEpisode.Id);
                IsLikeChecked = interaction.IsLike != 0;
                IsCoinChecked = interaction.CoinNumber > 0;
                IsFavoriteChecked = interaction.IsFavorite != 0;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 修改清晰度.
        /// </summary>
        /// <param name="formatId">清晰度Id.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ChangeFormatAsync(int formatId)
        {
            var preferCodecId = GetPreferCodecId();

            if (_videoList != null && _videoList.Count > 0)
            {
                var conditionStreams = _videoList.Where(p => p.Id == formatId).ToList();
                if (conditionStreams.Count == 0)
                {
                    var maxQuality = _videoList.Max(p => p.Id);
                    _currentVideo = _videoList.Where(p => p.Id == maxQuality).FirstOrDefault();
                }
                else
                {
                    var tempVideo = conditionStreams.Where(p => p.Codecs.Contains(preferCodecId)).FirstOrDefault(p => p.Id == formatId);
                    if (tempVideo == null)
                    {
                        tempVideo = conditionStreams.First();
                    }

                    _currentVideo = tempVideo;
                }

                CurrentFormat = FormatCollection.Where(p => p.Data.Quality == _currentVideo.Id).FirstOrDefault()?.Data;
                _currentAudio = _audioList.FirstOrDefault();
            }

            CheckFormatSelection();

            if (_currentVideo != null)
            {
                await InitializeOnlineDashVideoAsync();
            }
            else
            {
                IsPlayInformationError = true;
                PlayInformationErrorText = _resourceToolkit.GetLocaleString(LanguageNames.SourceNotSupported);
            }
        }

        /// <summary>
        /// 修改直播播放行为.
        /// </summary>
        /// <param name="quality">清晰度.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ChangeLivePlayBehaviorAsync(int quality)
        {
            IsShowAudioCover = IsLiveAudioOnly;
            var playInfo = await Controller.GetAppLivePlayInformationAsync(Convert.ToInt32(RoomId), quality, IsLiveAudioOnly);

            if (playInfo != null)
            {
                await InitializeAppLivePlayInformationAsync(playInfo);
            }
            else
            {
                IsPlayInformationError = true;
                PlayInformationErrorText = _resourceToolkit.GetLocaleString(LanguageNames.RequestLivePlayInformationFailed);
                CurrentAppLiveQuality = LiveAppQualityCollection.FirstOrDefault()?.Data;
            }
        }

        /// <summary>
        /// 切换直播音频.
        /// </summary>
        /// <param name="audioOnly">是否仅播放音频.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ToggleLiveAudioAsync(bool audioOnly)
        {
            IsLiveAudioOnly = audioOnly;
            _settingsToolkit.WriteLocalSetting(SettingNames.IsLiveAudioOnly, audioOnly);
            await ChangeLivePlayBehaviorAsync(CurrentAppLiveQuality.Quality);
        }

        /// <summary>
        /// 修改直播线路.
        /// </summary>
        /// <param name="url">线路数据.</param>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ChangeAppLivePlayLineAsync(LiveAppPlayUrl url)
        {
            var playUrl = LiveAppPlayLineCollection.Where(p => p.Data.Host == url.Host).FirstOrDefault();
            if (playUrl != null)
            {
                CurrentPlayUrl = playUrl;
                await InitializeLiveDashAsync(CurrentPlayUrl.Url);
            }
        }

        /// <summary>
        /// 清理播放数据.
        /// </summary>
        public void ClearPlayer()
        {
            if (BiliPlayer != null)
            {
                BiliPlayer.SetMediaPlayer(null);
            }

            if (_currentVideoPlayer != null)
            {
                if (_currentVideoPlayer.PlaybackSession.CanPause)
                {
                    _currentVideoPlayer.Pause();
                }

                if (_currentPlaybackItem != null)
                {
                    _currentPlaybackItem.Source.Dispose();
                    _currentPlaybackItem = null;
                }

                _currentVideoPlayer.Source = null;
                _currentVideoPlayer = null;
            }

            _lastReportProgress = TimeSpan.Zero;
            _progressTimer.Stop();
            _heartBeatTimer.Stop();
            _subtitleTimer.Stop();

            if (_interopMSS != null)
            {
                _interopMSS.Dispose();
                _interopMSS = null;
            }

            PlayerStatus = PlayerStatus.NotLoad;
        }

        /// <summary>
        /// 切换播放/暂停状态.
        /// </summary>
        public void TogglePlayPause()
        {
            if (_currentVideoPlayer != null)
            {
                if (PlayerStatus == PlayerStatus.Playing)
                {
                    _currentVideoPlayer.Pause();
                }
                else
                {
                    _currentVideoPlayer.Play();
                }
            }
        }

        /// <summary>
        /// 跳转到之前的历史记录.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task JumpToHistoryAsync()
        {
            if (_videoType == VideoType.Video)
            {
                await CheckVideoHistoryAsync();
            }
            else if (_videoType == VideoType.Pgc)
            {
                await CheckPgcHistoryAsync();
            }
        }

        /// <summary>
        /// 加载收藏夹列表.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task LoadFavoritesAsync()
        {
            var accVM = AccountViewModel.Instance;
            if (accVM.Status != AccountViewModelStatus.Login || IsRequestingFavorites)
            {
                return;
            }

            try
            {
                IsRequestFavoritesError = false;
                FavoriteMetaCollection.Clear();
                IsRequestingFavorites = true;
                var favorites = await Controller.GetFavoriteListAsync(AccountViewModel.Instance.Mid.Value, Convert.ToInt32(GetAid()));
                if (favorites.Count > 0)
                {
                    favorites.ForEach(p => FavoriteMetaCollection.Add(new FavoriteMetaViewModel(p, p.FavoriteState == 1)));
                }
            }
            catch (Exception)
            {
                IsRequestFavoritesError = true;
            }

            IsRequestingFavorites = false;
        }

        /// <summary>
        /// 使用本机组件分享当前正在播放的内容.
        /// </summary>
        public void Share()
        {
            var dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += OnDataRequested;
            DataTransferManager.ShowShareUI();
        }

        /// <summary>
        /// 初始化BBDown下载命令.
        /// </summary>
        public void InitDownload()
        {
            var para = string.Empty;
            var partList = new List<int>();
            if (IsPgc)
            {
                if (CurrentPgcEpisode != null)
                {
                    para = $"ep{CurrentPgcEpisode.Id}";
                }
                else if (_pgcDetail != null)
                {
                    para = $"ss{_pgcDetail.SeasonId}";
                }

                partList = EpisodeCollection.Select((_, index) => index + 1).ToList();
            }
            else if (!IsLive)
            {
                if (!string.IsNullOrEmpty(BvId))
                {
                    para = BvId;
                }
                else if (!string.IsNullOrEmpty(AvId))
                {
                    para = $"av{AvId}";
                }

                partList = VideoPartCollection.Select((_, index) => index + 1).ToList();
            }

            DownloadViewModel.Instance.Load(para, partList);
        }

        /// <summary>
        /// 加载视频参数信息.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task InitVideoStatusAsync()
        {
            if (IsPgc || IsLive)
            {
                return;
            }

            if (_videoId > 0)
            {
                try
                {
                    // 延迟1s以等待数据同步.
                    await Task.Delay(500);
                    var info = await Controller.GetVideoStatusAsync(_videoId);
                    LikeCount = _numberToolkit.GetCountText(info.LikeCount);
                    CoinCount = _numberToolkit.GetCountText(info.CoinCount);
                    FavoriteCount = _numberToolkit.GetCountText(info.FavoriteCount);
                    DanmakuCount = _numberToolkit.GetCountText(info.DanmakuCount);
                    PlayCount = _numberToolkit.GetCountText(info.PlayCount);
                    ReplyCount = _numberToolkit.GetCountText(info.ReplyCount);
                    ShareCount = _numberToolkit.GetCountText(info.ShareCount);
                }
                catch (Exception)
                {
                }
            }
        }

        /// <summary>
        /// 获取本地的初始化视图模型.
        /// </summary>
        /// <returns>视图模型.</returns>
        public async Task<object> GetInitViewModelFromLocalAsync()
        {
            var data = await _fileToolkit.ReadLocalDataAsync<CurrentPlayingRecord>(AppConstants.LastOpenVideoFileName);
            return data;
        }

        /// <summary>
        /// 清除本地的继续播放视图模型.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ClearInitViewModelAsync()
        {
            await _fileToolkit.DeleteLocalDataAsync(AppConstants.LastOpenVideoFileName);
            _settingsToolkit.WriteLocalSetting(SettingNames.CanContinuePlay, false);
            _settingsToolkit.DeleteLocalSetting(SettingNames.ContinuePlayTitle);
        }

        /// <summary>
        /// 播放下一个视频.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task PlayNextVideoAsync()
        {
            var previousDisplayMode = PlayerDisplayMode;
            if (IsShowViewLater)
            {
                var index = ViewLaterVideoCollection.IndexOf(ViewLaterVideoCollection.FirstOrDefault(p => p.IsSelected));
                if (index != -1 && index < ViewLaterVideoCollection.Count)
                {
                    var nextVideo = ViewLaterVideoCollection[index + 1];
                    await LoadAsync(nextVideo, shouldResetMode: false);
                }
            }
            else if (RelatedVideoCollection.Count > 0)
            {
                var first = RelatedVideoCollection.First();
                await LoadAsync(first, shouldResetMode: false);
                PlayerDisplayMode = previousDisplayMode;
            }
        }

        /// <summary>
        /// 切换固定状态.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task ToggleFixStateAsync()
        {
            var item = ConvertToFixItem();
            if (IsContentFixed)
            {
                await AccountViewModel.Instance.RemoveFixedItemAsync(item.Id);
            }
            else
            {
                await AccountViewModel.Instance.AddFixedItemAsync(item);
            }

            IsContentFixed = !IsContentFixed;
        }

        /// <summary>
        /// 开始临时快进.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task<bool> StartTempQuickPlayAsync()
        {
            if (_currentVideoPlayer == null
                || _currentVideoPlayer.PlaybackSession == null
                || _currentVideoPlayer.PlaybackSession.PlaybackState != Windows.Media.Playback.MediaPlaybackState.Playing
                || PlaybackRate >= 3)
            {
                return false;
            }

            await Splat.Locator.Current.GetService<CoreDispatcher>().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _originalPlayRate = PlaybackRate;
                _originalDanmakuSpeed = DanmakuViewModel.Instance.DanmakuSpeed;
                PlaybackRate = PlaybackRate * 2 > 3 ? 3 : PlaybackRate * 2;
                DanmakuViewModel.Instance.DanmakuSpeed = _originalDanmakuSpeed * 1.5 > 2 ? 2 : _originalDanmakuSpeed * 1.5;
            });

            return true;
        }

        /// <summary>
        /// 停止临时快进.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        public async Task StopTempQuickPlayAsync()
        {
            if (_originalDanmakuSpeed <= 0 || _originalPlayRate <= 0)
            {
                return;
            }

            await Splat.Locator.Current.GetService<CoreDispatcher>().RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlaybackRate = _originalPlayRate;
                DanmakuViewModel.Instance.DanmakuSpeed = _originalDanmakuSpeed;
                _originalPlayRate = 0;
                _originalDanmakuSpeed = 0;
            });
        }

        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var request = args.Request;
            var url = string.Empty;
            switch (_videoType)
            {
                case VideoType.Video:
                    url = _videoDetail.ShortLink;
                    break;
                case VideoType.Pgc:
                    url = CurrentPgcEpisode.Link;
                    break;
                case VideoType.Live:
                    url = $"https://live.bilibili.com/{_liveDetail.RoomInformation.RoomId}";
                    break;
                default:
                    break;
            }

            request.Data.Properties.Title = Title;
            request.Data.Properties.Description = Description;
            request.Data.Properties.Thumbnail = RandomAccessStreamReference.CreateFromUri(new Uri(CoverUrl));
            request.Data.Properties.ContentSourceWebLink = new Uri(url);

            request.Data.SetText(Description);
            request.Data.SetWebLink(new Uri(url));
            request.Data.SetBitmap(RandomAccessStreamReference.CreateFromUri(new Uri(CoverUrl)));
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            switch (args.PropertyName)
            {
                case nameof(CurrentFormat):
                    if (CurrentFormat != null)
                    {
                        _settingsToolkit.WriteLocalSetting(SettingNames.DefaultVideoFormat, CurrentFormat.Quality);
                    }

                    break;
                case nameof(Volume):
                    if (Volume > 0)
                    {
                        _settingsToolkit.WriteLocalSetting(SettingNames.Volume, Volume);
                    }

                    break;
                case nameof(IsDetailError):
                    PlayerDisplayMode = PlayerDisplayMode.Default;
                    break;
                case nameof(IsLikeChecked):
                case nameof(IsCoinChecked):
                case nameof(IsFavoriteChecked):
                    IsEnableLikeHolding = !IsLikeChecked || !IsCoinChecked || !IsFavoriteChecked;
                    break;
                case nameof(IsShowDanmakuBar):
                    _settingsToolkit.WriteLocalSetting(SettingNames.IsShowDanmakuBar, IsShowDanmakuBar);
                    break;
                case nameof(CanShowSubtitle):
                    _settingsToolkit.WriteLocalSetting(SettingNames.CanShowSubtitle, CanShowSubtitle);
                    break;
                case nameof(SubtitleConvertType):
                    _settingsToolkit.WriteLocalSetting(SettingNames.SubtitleConvertType, SubtitleConvertType);
                    break;
                case nameof(PlaybackRate):
                    if (_currentVideoPlayer != null && _currentVideoPlayer.PlaybackSession != null)
                    {
                        _currentVideoPlayer.PlaybackSession.PlaybackRate = PlaybackRate;
                    }

                    _settingsToolkit.WriteLocalSetting(SettingNames.PlaybackRate, PlaybackRate);
                    break;
                case nameof(IsOnlyShowIndex):
                    _settingsToolkit.WriteLocalSetting(SettingNames.IsOnlyShowIndex, IsOnlyShowIndex);
                    break;
                case nameof(IsInfiniteLoop):
                    {
                        if (_currentVideoPlayer != null)
                        {
                            _currentVideoPlayer.IsLoopingEnabled = IsInfiniteLoop;
                        }
                    }

                    break;
                case nameof(PlayerDisplayMode):
                    _appViewModel.IsShowTitleBar = PlayerDisplayMode == PlayerDisplayMode.Default;
                    break;
                default:
                    break;
            }
        }

        private void OnLiveDanmakuCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var count = LiveDanmakuCollection.Count;
            IsShowEmptyLiveMessage = count == 0;

            if (count > 0 && IsLiveMessageAutoScroll)
            {
                RequestLiveMessageScrollToBottom?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnTagCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            => IsShowTags = TagCollection.Count > 0;

        private void OnAppViewVisibleBoundsChanged(ApplicationView sender, object args)
        {
            if (!sender.IsFullScreenMode && PlayerDisplayMode == PlayerDisplayMode.FullScreen)
            {
                PlayerDisplayMode = PlayerDisplayMode.FullWindow;
            }
        }
    }
}