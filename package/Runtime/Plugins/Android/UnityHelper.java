package com.adsbynimbus.unity;

import static android.view.ViewGroup.LayoutParams.*;

import android.app.Activity;
import android.content.res.Configuration;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.widget.FrameLayout;

import androidx.appcompat.content.res.AppCompatResources;

import com.adsbynimbus.NimbusAdManager;
import com.adsbynimbus.NimbusError;
import com.adsbynimbus.openrtb.response.BidResponse;
import com.adsbynimbus.render.AdController;
import com.adsbynimbus.render.AdEvent;
import com.adsbynimbus.render.BlockingAdRenderer;
import com.adsbynimbus.render.CompanionAd;
import com.adsbynimbus.render.Renderer;
import com.adsbynimbus.request.NimbusRequest;
import com.adsbynimbus.request.NimbusResponse;

public final class UnityHelper {
    static final NimbusAdManager manager = new NimbusAdManager();
    
    public static void render(Object obj, String jsonResponse, boolean isBlocking, int closeButtonDelay, Object listener) {
        if (obj instanceof Activity) {
            final Activity activity = (Activity) obj;
            BlockingAdRenderer.setMuteButton(AppCompatResources.getDrawable(activity,
                    android.R.drawable.stat_notify_call_mute));
            final NimbusResponse nimbusResponse = new NimbusResponse(BidResponse.fromJson(jsonResponse));
            if (isBlocking) {
                nimbusResponse.companionAds = new CompanionAd[]{activity.getResources().getConfiguration().orientation ==
                        Configuration.ORIENTATION_LANDSCAPE ?
                        CompanionAd.end(480, 320) : CompanionAd.end(320, 480)};
                
                activity.runOnUiThread(() -> {
                    BlockingAdRenderer.setsCloseButtonDelayRender(closeButtonDelay * 1000);
                    final AdController controller = Renderer.loadBlockingAd(nimbusResponse, activity);
                    final NimbusAdManager.Listener callback = (NimbusAdManager.Listener) listener;
                    if (controller != null) {
                              callback.onAdRendered(controller);
                              controller.start();
                    } else {
                        callback.onError(new NimbusError(NimbusError.ErrorType.RENDERER_ERROR, "Error rendering blocking ad", null));
                    }
                });
            } else {
                activity.runOnUiThread(new BannerHandler(activity, null, nimbusResponse, (NimbusAdManager.Listener) listener));
            }
        }
    }
    
    public static void addListener(Object controller, Object listener) {
        if (controller instanceof AdController) {
            ((AdController) controller).listeners().add((AdController.Listener) listener);
        }
    }
    
    public static void destroyController(Object obj, Object controller) {
        if (obj instanceof Activity) {
            final Activity activity = (Activity) obj;
            if (controller instanceof AdController) {
                activity.runOnUiThread(() -> ((AdController)controller).destroy());
            }
        }
    }

    static final class BannerHandler implements Runnable, NimbusAdManager.Listener,
        AdController.Listener {

        protected final NimbusRequest request;
        protected final NimbusResponse response;
        protected Activity activity;
        protected FrameLayout adFrame;
        protected NimbusAdManager.Listener loadListener;

        public BannerHandler(Activity activity, NimbusRequest request, NimbusAdManager.Listener listener) {
            this.activity = activity;
            this.request = request;
            this.loadListener = listener;
            this.response = null;
        }

        public BannerHandler(Activity activity, NimbusRequest request, NimbusResponse response,
            NimbusAdManager.Listener listener) {
            this.activity = activity;
            this.request = request;
            this.loadListener = listener;
            this.response = response;
        }

        @Override
        public void run() {
            if (adFrame == null && activity != null) {
                adFrame = new FrameLayout(activity) {
                    @Override
                    public void onViewAdded(View child) {
                        super.onViewAdded(child);
                        ((LayoutParams) child.getLayoutParams()).gravity = Gravity.BOTTOM | Gravity.CENTER_HORIZONTAL;
                    }
                };
                activity.addContentView(adFrame, new ViewGroup.LayoutParams(MATCH_PARENT, MATCH_PARENT));
                if (response == null) manager.showAd(request, adFrame, this); else {
                    Renderer.loadAd(response, adFrame, this);
                }
                activity = null;
            }
        }

        @Override
        public void onError(NimbusError error) {
            if (loadListener != null) {
                loadListener.onError(error);
                loadListener = null;
            }
            if (adFrame != null) {
                if (adFrame.getParent() instanceof ViewGroup) {
                    ((ViewGroup) adFrame.getParent()).removeView(adFrame);
                }
                adFrame = null;
            }
        }

        @Override
        public void onAdResponse(NimbusResponse nimbusResponse) {
            if (loadListener != null) {
                loadListener.onAdResponse(nimbusResponse);
            }
        }

        @Override
        public void onAdRendered(AdController controller) {
            controller.listeners().add(this);
            if (loadListener != null) {
                loadListener.onAdRendered(controller);
                loadListener = null;
            }
        }

        @Override
        public void onAdEvent(AdEvent adEvent) {
            if ((adEvent == AdEvent.DESTROYED || adEvent == AdEvent.COMPLETED) && adFrame != null) {
                if (adFrame.getParent() instanceof ViewGroup) {
                    ((ViewGroup) adFrame.getParent()).removeView(adFrame);
                }
                adFrame = null;
            }
        }
    }
}