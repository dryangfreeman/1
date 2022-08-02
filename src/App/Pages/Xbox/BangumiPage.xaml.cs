﻿// Copyright (c) Richasy. All rights reserved.

using Bili.App.Pages.Base;

namespace Bili.App.Pages.Xbox
{
    /// <summary>
    /// 番剧页面.
    /// </summary>
    public sealed partial class BangumiPage : BangumiPageBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BangumiPage"/> class.
        /// </summary>
        public BangumiPage() => InitializeComponent();

        /// <inheritdoc/>
        protected override void OnPageLoaded()
            => Bindings.Update();

        /// <inheritdoc/>
        protected override void OnPageUnloaded()
            => Bindings.StopTracking();
    }
}
