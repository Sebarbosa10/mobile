using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

// Anuncios reales con Google Mobile Ads. Antes de pedir anuncios resuelve el consentimiento (UMP),
// obligatorio en Europa y Reino Unido: Google solo muestra el formulario donde hace falta.
public sealed class AdMobAds : IAdsProvider
{
    private readonly Action changed;

    private BannerView banner;
    private InterstitialAd interstitial;

    private bool sdkRequested;
    private bool sdkReady;
    private bool bannerWanted;
    private bool loadingInterstitial;

    public AdMobAds(Action changed)
    {
        this.changed = changed;
    }

    public bool InterstitialReady
    {
        get
        {
            if (interstitial != null && interstitial.CanShowAd())
                return true;

            LoadInterstitial();
            return false;
        }
    }

    public bool PrivacyOptionsRequired =>
        ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    public void Start()
    {
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // ExecuteInUpdate necesita este ejecutor, y el SDK recién lo crea al iniciarse
        MobileAdsEventExecutor.Initialize();

        ConsentInformation.Update(new ConsentRequestParameters(), error =>
            MobileAdsEventExecutor.ExecuteInUpdate(() => HandleConsentInfo(error)));

        // Si ya hubo consentimiento en otra sesión no hace falta esperar el formulario
        if (ConsentInformation.CanRequestAds())
            InitializeSdk();
    }

    private void HandleConsentInfo(FormError error)
    {
        if (error != null)
        {
            Debug.LogWarning($"[AdMob] No se pudo actualizar el consentimiento: {error.Message}");
            HandleConsentResolved();
            return;
        }

        ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (formError != null)
                    Debug.LogWarning($"[AdMob] Formulario de consentimiento: {formError.Message}");

                HandleConsentResolved();
            }));
    }

    private void HandleConsentResolved()
    {
        changed?.Invoke();

        // Los IDs de prueba no tienen mensaje de privacidad en AdMob: Google rechaza la consulta
        // de consentimiento, pero los anuncios de test igual se pueden pedir
        if (ConsentInformation.CanRequestAds() || MonetizationConfig.UsingTestAds)
            InitializeSdk();
    }

    private void InitializeSdk()
    {
        if (sdkRequested)
            return;

        sdkRequested = true;

        MobileAds.Initialize(_ =>
        {
            sdkReady = true;
            LoadInterstitial();

            if (bannerWanted)
                ShowBanner();
        });
    }

    public void ShowBanner()
    {
        bannerWanted = true;

        if (!sdkReady)
            return;

        if (banner != null)
        {
            banner.Show();
            return;
        }

        banner = new BannerView(MonetizationConfig.AndroidBannerUnitId, AdSize.Banner, AdPosition.Bottom);
        banner.OnBannerAdLoadFailed += error => Debug.LogWarning($"[AdMob] Banner: {error.GetMessage()}");
        banner.LoadAd(new AdRequest());
    }

    public void HideBanner()
    {
        bannerWanted = false;
        banner?.Hide();
    }

    private void LoadInterstitial()
    {
        if (!sdkReady || loadingInterstitial || interstitial != null)
            return;

        loadingInterstitial = true;

        InterstitialAd.Load(MonetizationConfig.AndroidInterstitialUnitId, new AdRequest(), (ad, error) =>
        {
            loadingInterstitial = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdMob] Intersticial: {error?.GetMessage()}");
                return;
            }

            interstitial = ad;
        });
    }

    public void ShowInterstitial(Action closed)
    {
        InterstitialAd ad = interstitial;
        interstitial = null;

        bool finished = false;

        void Finish()
        {
            if (finished)
                return;

            finished = true;
            ad.Destroy();
            closed?.Invoke();
            LoadInterstitial();
        }

        ad.OnAdFullScreenContentClosed += Finish;
        ad.OnAdFullScreenContentFailed += _ => Finish();
        ad.Show();
    }

    public void ShowPrivacyOptions()
    {
        ConsentForm.ShowPrivacyOptionsForm(error =>
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (error != null)
                    Debug.LogWarning($"[AdMob] Opciones de privacidad: {error.Message}");

                HandleConsentResolved();
            }));
    }

    public void Shutdown()
    {
        bannerWanted = false;

        banner?.Destroy();
        banner = null;

        interstitial?.Destroy();
        interstitial = null;
    }
}
