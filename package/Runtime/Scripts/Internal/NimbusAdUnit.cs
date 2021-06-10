using System;
using UnityEngine;

namespace Nimbus.Runtime.Scripts.Internal {

	public delegate void DestroyAdDelegate();

	public sealed class NimbusAdUnit {
		private readonly AdEvents _adEvents;
		public readonly AdUnityType AdType;

		// Delay before close button is shown in milliseconds, set to max value to only show close button after video completion
		// where setting a value higher than the video length forces the x to show up at the end of the video
		internal readonly int CloseButtonDelayMillis;
		public readonly int InstanceID;
		public readonly string Position;
		
		internal AdError AdControllerError;
		internal AdError AdListenerError;
		internal bool AdWasRendered;
		internal BidFloors BidFloors;
		internal AdEventTypes CurrentAdState;
		internal MetaData MetaData;
		
		# region IOS specific
		internal event DestroyAdDelegate DestroyIOSAd;
		private void OnDestroyIOSAd() {
			DestroyIOSAd?.Invoke();
		}
		
		#endregion
	

		#region Android Specific

		private AndroidJavaObject _androidController;
		private AndroidJavaClass _androidHelper;

		#endregion



		public NimbusAdUnit(AdUnityType adType, string position, float bannerFloor, float videoFloor,
			in AdEvents adEvents) {
			AdType = adType;
			InstanceID = GetHashCode();
			CurrentAdState = AdEventTypes.NOT_LOADED;
			Position = position;
			_adEvents = adEvents;
			BidFloors = new BidFloors(bannerFloor, videoFloor);
			// leave this at MaxValue for now
			CloseButtonDelayMillis = int.MaxValue;
		}

		~NimbusAdUnit() {
			Destroy();
		}

		/// <summary>
		///     Destroys the ad at the mobile bridge level
		/// </summary>
		public void Destroy() {
#if UNITY_ANDROID
			if (_androidController == null || _androidHelper == null) return;
			var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
			var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
			_androidHelper.CallStatic("destroyController", currentActivity, _androidController);
			_androidController = null;
			_androidHelper = null;
# elif UNITY_IOS
			OnDestroyIOSAd();
#endif
		}

		/// <summary>
		///     Checks to see of an error was returned from either the ad listener or controller and returns true if there
		///     was an error at any step
		/// </summary>
		public bool DidHaveAnError() {
			return AdListenerError != null || AdControllerError != null;
		}

		/// <summary>
		///     Returns the combined error output from the ad listener and controller error
		/// </summary>
		public string ErrorMessage() {
			var message = "";
			if (AdListenerError != null) message = $"AdListener Error: {AdListenerError.Message} ";
			if (AdControllerError != null) message += $"AdController Error: {AdControllerError.Message}";
			return message;
		}

		/// <summary>
		///     Returns the unique auction id associated to the request to Nimbus, can be used by the Nimbus team to debug
		///     a particular auction event
		/// </summary>
		public string GetAuctionID() {
			return MetaData.AuctionID;
		}

		/// <summary>
		///     Return the Ecpm value associated to the winning ad
		/// </summary>
		public double GetBidValue() {
			return MetaData.Bid;
		}

		/// <summary>
		///     Returns the current state of the ad, this can be used instead of event listeners to execute conditional code
		/// </summary>
		public AdEventTypes GetCurrentAdState() {
			return CurrentAdState;
		}

		/// <summary>
		///     Returns the name of the demand source that won the auction
		/// </summary>
		public string GetNetwork() {
			return MetaData.Network;
		}


		/// <summary>
		///     Returns returns true of the ad was rendered even if the ad has already been destroyed
		/// </summary>
		public bool WasAdRendered() {
			return AdWasRendered;
		}

		internal void EmitOnAdRendered(NimbusAdUnit obj) {
			_adEvents.EmitOnAdRendered(obj);
		}

		internal void EmitOnAdError(NimbusAdUnit obj) {
			_adEvents.EmitOnAdError(obj);
		}

		internal void EmitOnAdEvent(AdEventTypes e) {
			switch (e) {
				case AdEventTypes.LOADED:
					_adEvents.EmitOnOnAdLoaded(this);
					break;
				case AdEventTypes.IMPRESSION:
					_adEvents.EmitOnOnAdImpression(this);
					break;
				case AdEventTypes.CLICKED:
					_adEvents.EmitOnOnAdClicked(this);
					break;
				case AdEventTypes.PAUSED:
					_adEvents.EmitOnOnVideoAdPaused(this);
					break;
				case AdEventTypes.RESUME:
					_adEvents.EmitOnOnVideoAdResume(this);
					break;
				case AdEventTypes.COMPLETED:
					_adEvents.EmitOnOnVideoAdCompleted(this);
					break;
				case AdEventTypes.DESTROYED:
					// when Interstitial ads are destroyed by the user clicking the x button
					// the ad viewing is also technically completed, adding this if check in-case the user is expecting
					// a completed event to be fired
					if (AdType == AdUnityType.Interstitial) _adEvents.EmitOnOnVideoAdCompleted(this);
					_adEvents.EmitOnOnAdDestroyed(this);
					break;
			}
		}

		internal void SetAndroidController(AndroidJavaObject controller) {
			if (_androidController != null) return;
			_androidController = controller;
		}

		internal void SetAndroidHelper(AndroidJavaClass helper) {
			if (_androidHelper != null) return;
			_androidHelper = helper;
		}

		
		
		
	}


	// ReSharper disable MemberCanBePrivate.Global
	internal class AdError {
		public readonly string Message;

		public AdError(string errMessage) {
			Message = errMessage;
		}
	}

	internal readonly struct BidFloors {
		internal readonly float BannerFloor;
		internal readonly float VideoFloor;

		public BidFloors(float bannerFloor, float videoFloor) {
			BannerFloor = bannerFloor;
			VideoFloor = videoFloor;
		}
	}

	internal class MetaData {
		public readonly string AuctionID;
		public readonly double Bid;
		public readonly string Network;

		public MetaData(in AndroidJavaObject response) {
			AuctionID = response.Get<string>("auction_id");
			Bid = response.Get<double>("bid_raw");
			Network = response.Get<string>("network");
		}
	}
}