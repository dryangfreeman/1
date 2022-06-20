﻿// Copyright (c) Richasy. All rights reserved.

using Bili.Models.Enums.App;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

namespace Bili.App.Controls.Player
{
    /// <summary>
    /// 媒体播放器.
    /// </summary>
    public sealed partial class BiliMediaPlayer
    {
        private const string MediaPlayerElementName = "MediaPlayerElement";
        private const string InteractionControlName = "InteractionControl";
        private const string MediaTransportControlsName = "MediaTransportControls";
        private const string TempMessageContaienrName = "TempMessageContainer";
        private const string TempMessageBlockName = "TempMessageBlock";

        private DispatcherTimer _unitTimer;

        private MediaPlayerElement _mediaPlayerElement;
        private BiliMediaTransportControls _mediaTransportControls;
        private Rectangle _interactionControl;
        private GestureRecognizer _gestureRecognizer;
        private Grid _tempMessageContainer;
        private TextBlock _tempMessageBlock;

        private double _cursorStayTime;
        private double _tempMessageStayTime;
        private double _transportStayTime;

        private double _manipulationDeltaX = 0d;
        private double _manipulationDeltaY = 0d;
        private double _manipulationProgress = 0d;
        private double _manipulationVolume = 0d;
        private double _manipulationUnitLength = 0d;
        private bool _manipulationBeforeIsPlay = false;
        private PlayerManipulationType _manipulationType = PlayerManipulationType.None;

        private bool _isTouch = false;
        private bool _isHolding = false;
        private bool _isCursorInPlayer = false;
    }
}