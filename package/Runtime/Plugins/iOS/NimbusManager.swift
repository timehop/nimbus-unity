//
//  NimbusManager.swift
//
//  Created by Bruno Bruggemann on 5/7/21.
//  Copyright © 2021 Timehop. All rights reserved.
//

import Foundation
import NimbusRenderStaticKit
import NimbusRenderVideoKit
import NimbusKit

@objc public class NimbusManager: NSObject {
    
    private static var managerDictionary: [Int: NimbusManager] = [:]
    
    private let adUnitInstanceId: Int
    
    private var nimbusAdManager: NimbusAdManager?
    private var adController: AdController?
    
    private var adView: AdView?
    
    // MARK: - Class Functions
    
    @objc public class func initializeNimbusSDK(publisher: String,
                                                apiKey: String,
                                                enableSDKInTestMode: Bool,
                                                enableUnityLogs: Bool) {
        Nimbus.shared.initialize(publisher: publisher, apiKey: apiKey)
        
        Nimbus.shared.logLevel = enableUnityLogs ? .debug : .off
        Nimbus.shared.testMode = enableSDKInTestMode
        
        Nimbus.shared.renderers = [
            .forAuctionType(.static): NimbusStaticAdRenderer(),
            .forAuctionType(.video): NimbusVideoAdRenderer()
        ]
    }
    
    @objc public class func nimbusManager(forAdUnityInstanceId adUnityInstanceId: Int) -> NimbusManager {
        guard let manager = managerDictionary[adUnityInstanceId] else {
            let manager = NimbusManager(adUnitInstanceId: adUnityInstanceId)
            managerDictionary[adUnityInstanceId] = manager
            return manager
        }
        return manager
    }
    
    @objc public class func setGDPRConsentString(consent: String) {
        var user = NimbusRequestManager.user ?? NimbusUser()
        user.configureGdprConsent(didConsent: true, consentString: consent)
        NimbusRequestManager.user = user
    }
    
    // MARK: - Private Functions
    
    private init(adUnitInstanceId: Int) {
        self.adUnitInstanceId = adUnitInstanceId
    }
    
    private func unityViewController() -> UIViewController? {
        return UIApplication.shared.windows.first { $0.isKeyWindow }?.rootViewController
    }
    
    // MARK: - Public Functions
    
    @objc public func showBannerAd(position: String, bannerFloor: Float) {
        guard let viewController = unityViewController() else { return }
        
        let adFormat = NimbusAdFormat.banner320x50
        let adPosition = NimbusPosition.footer
        
        let request = NimbusRequest.forBannerAd(position: position,
                                                format: adFormat,
                                                adPosition: adPosition)
        request.impressions[0].bidFloor = bannerFloor
        
        let view = AdView(bannerFormat: adFormat)
        self.adView = view
        
        view.attachToView(parentView: viewController.view, position: adPosition)
        
        nimbusAdManager = NimbusAdManager()
        nimbusAdManager?.delegate = self
        nimbusAdManager?.showAd(request: request,
                                container: view,
                                adPresentingViewController: viewController)
    }
    
    @objc public func showInterstitialAd(position: String, bannerFloor: Float, videoFloor: Float, closeButtonDelay: Double) {
        guard let viewController = unityViewController() else { return }
        
        let request = NimbusRequest.forInterstitialAd(position: position)
        request.impressions[0].banner?.bidFloor = bannerFloor
        request.impressions[0].video?.bidFloor = videoFloor
        
        
        (Nimbus.shared.renderers[.forAuctionType(.video)] as? NimbusVideoAdRenderer)?.showMuteButton = false // false by default
        
        nimbusAdManager = NimbusAdManager()
        nimbusAdManager?.delegate = self
        nimbusAdManager?.showRewardedAd(request: request,
                                        closeButtonDelay: closeButtonDelay,
                                        adPresentingViewController: viewController)
    }
    
    @objc public func showRewardedVideoAd(position: String, videoFloor: Float, closeButtonDelay: Double) {
        guard let viewController = unityViewController() else { return }
        
        let request = NimbusRequest.forVideoAd(position: position)
        request.impressions[0].video?.bidFloor = videoFloor
        
        (Nimbus.shared.renderers[.forAuctionType(.video)] as? NimbusVideoAdRenderer)?.showMuteButton = false // false by default
        
        nimbusAdManager = NimbusAdManager()
        nimbusAdManager?.delegate = self
        nimbusAdManager?.showRewardedAd(request: request,
                                        closeButtonDelay: closeButtonDelay,
                                        adPresentingViewController: viewController)
    }
    
    @objc public func destroyExistingAd() {
        adController?.destroy()
        adView?.removeFromSuperview()
        adView = nil
    }
    
}

// MARK: - NimbusAdManagerDelegate implementation

extension NimbusManager: NimbusAdManagerDelegate {
    
    public func didCompleteNimbusRequest(request: NimbusRequest, ad: NimbusAd) {
        let params: [String: Any] = [
            "adUnitInstanceID": adUnitInstanceId,
            "auctionId": ad.auctionId,
            "bidRaw": ad.bidRaw,
            "bidInCents": ad.bidInCents,
            "network": ad.network,
            "placementId": ad.placementId ?? ""
        ]
        
        UnityBinding.sendMessage(methodName: "OnAdResponse", params: params)
    }
    
    public func didFailNimbusRequest(request: NimbusRequest, error: NimbusError) {
        let params: [String: Any] = [
            "adUnitInstanceID": adUnitInstanceId,
            "errorMessage": error.localizedDescription
        ]
        
        UnityBinding.sendMessage(methodName: "OnError", params: params)
    }
    
    public func didRenderAd(request: NimbusRequest, ad: NimbusAd, controller: AdController) {
        self.adController = controller
        self.adController?.delegate = self
        
        let params: [String: Any] = [
            "adUnitInstanceID": adUnitInstanceId
        ]
        
        UnityBinding.sendMessage(methodName: "OnAdResponse", params: params)
    }
    
}

// MARK: - AdControllerDelegate implementation

extension NimbusManager: AdControllerDelegate {
    
    public func didReceiveNimbusEvent(controller: AdController, event: NimbusEvent) {
        var method = "OnAdEvent", eventName = ""
        switch event {
        case .loaded, .loadedCompanionAd(width: _, height: _), .firstQuartile, .midpoint, .thirdQuartile:
            return // Unity doesn't handle these events
        case .impression:
            eventName = "IMPRESSION"
        case .clicked:
            eventName = "CLICKED"
        case .paused:
            eventName = "PAUSED"
        case .resumed:
            eventName = "RESUME"
        case .completed:
            eventName = "COMPLETED"
        case .destroyed:
            eventName = "DESTROYED"
        @unknown default:
            print("Ad Event not sent: \(event)")
            return
        }

        let params: [String: Any] = [
            "adUnitInstanceID": adUnitInstanceId,
            "eventName": eventName
        ]
        UnityBinding.sendMessage(methodName: method, params: params)
    }
    
    /// Received an error for the ad
    public func didReceiveNimbusError(controller: AdController, error: NimbusError) {
        let params: [String: Any] = [
            "adUnitInstanceID": adUnitInstanceId,
            "errorMessage": error.localizedDescription
        ]
        
        UnityBinding.sendMessage(methodName: "OnError", params: params)
        destroyExistingAd()
    }
}
