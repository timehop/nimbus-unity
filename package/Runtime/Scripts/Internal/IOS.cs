﻿using System;
using System.Runtime.InteropServices;
using Nimbus.Runtime.Scripts.ScriptableObjects;
using UnityEngine;

namespace Nimbus.Runtime.Scripts.Internal
{
    public class IOS : NimbusAPI
    {

        #region Declare external C interface    
        [DllImport("__Internal")]
        private static extern void _initializeSDKWithPublisher(string publisher,
            string apiKey,
            bool enableSDKInTestMode,
            int logLevel,
            string appName,
            string appDomain,
            string bundleId,
            string storeUrl,
            bool showMuteButton);

        [DllImport("__Internal")]
        private static extern void _showBannerAd(string position);

        [DllImport("__Internal")]
        private static extern void _showInterstitialAd(string position);

        [DllImport("__Internal")]
        private static extern void _showRewardedVideoAd(string position);

        [DllImport("__Internal")]
        private static extern void _setGDPRConsentString(string consent);
        #endregion

        #region Wrapped methods and properties

        private readonly IOSAdManager iOSAdManager;

        public IOS()
        {
            iOSAdManager = new IOSAdManager();
        }

        internal override void InitializeSDK(ILogger logger, NimbusSDKConfiguration configuration)
        {
            _initializeSDKWithPublisher(configuration.publisherKey,
                configuration.apiKey,
                true, // TODO: enableSDKInTestMode is enabled
                2, // TODO: logLevel is hardcoded to DEBUG
                configuration.appName,
                configuration.appDomain,
                configuration.iosBundleID,
                configuration.iosAppStoreURL,
                true); // TODO: showMuteButton is hardcoded to true
        }

        internal override NimbusAdUnit LoadAndShowAd(ILogger logger, ref NimbusAdUnit nimbusAdUnit)
        {
            iOSAdManager.SetAdUnit(nimbusAdUnit);
            //var listener = new AdManagerListener(logger, in _helper, ref nimbusAdUnit);
            //var closeButtonDelayMillis = nimbusAdUnit.CloseButtonDelayMillis;
            //string functionCall;
            switch (nimbusAdUnit.AdType)
            {
                case AdUnityType.Banner:
                    _showBannerAd(nimbusAdUnit.Position);
                    break;
                case AdUnityType.Interstitial:
                    //closeButtonDelayMillis = 5000;
                    _showInterstitialAd(nimbusAdUnit.Position);
                    break;
                case AdUnityType.Rewarded:
                    _showRewardedVideoAd(nimbusAdUnit.Position);
                    break;
                default:
                    throw new Exception("ad type not supported");
            }
            //_helper.CallStatic(functionCall, _currentActivity, nimbusAdUnit.Position,
            //	nimbusAdUnit.BidFloors.BannerFloor, nimbusAdUnit.BidFloors.VideoFloor, closeButtonDelayMillis,
            //	listener);
            return nimbusAdUnit;
        }

        internal override void SetGDPRConsentString(string consent)
        {
            _setGDPRConsentString(consent);
        }

        #endregion
    }
}