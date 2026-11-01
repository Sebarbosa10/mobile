using System;
using UnityEngine;

public interface IAdsProvider
{
    bool InterstitialReady { get; }

    bool PrivacyOptionsRequired { get; }

    void ShowInterstitial(Action closed);

    void ShowBanner();

    void HideBanner();

    void ShowPrivacyOptions();

    // El jugador compró "sin anuncios": liberar todo lo cargado
    void Shutdown();
}

public interface IStoreProvider
{
    string RemoveAdsPrice { get; }

    void BuyRemoveAds(Action<bool> finished);
}

// Punto único de entrada a los anuncios y a la compra de "quitar anuncios".
// En Android usa AdMob y Google Play Billing; en el editor, una simulación.
public static class Monetization
{
    public const string AdsRemovedKey = "Monetization.AdsRemoved";
    public const float BannerHeight = 150f;

    private static IAdsProvider ads;
    private static IStoreProvider store;
    private static float lastInterstitialTime = float.NegativeInfinity;

    public static event Action Changed;

    public static bool AdsRemoved =>
        PlayerPrefs.GetInt(AdsRemovedKey, 0) == 1;

    public static bool InterstitialShowing { get; private set; }

    public static string RemoveAdsPrice
    {
        get
        {
            EnsureProviders();
            return store.RemoveAdsPrice;
        }
    }

    public static bool PrivacyOptionsRequired
    {
        get
        {
            EnsureProviders();
            return ads.PrivacyOptionsRequired;
        }
    }

    // Si hay anuncio para mostrar: sin compra, respetando la pausa entre anuncios y ya cargado
    public static bool InterstitialDue
    {
        get
        {
            EnsureProviders();

            return !AdsRemoved && ads.InterstitialReady &&
                   Time.realtimeSinceStartup - lastInterstitialTime >= MonetizationConfig.MinSecondsBetweenInterstitials;
        }
    }

    // Conecta la tienda al abrir el juego: recupera la compra tras reinstalar y prepara los anuncios
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureProviders()
    {
        if (ads != null)
            return;

        if (Application.platform == RuntimePlatform.Android)
        {
            AdMobAds adMob = new(NotifyChanged);
            ads = adMob;
            store = new UnityIapStore(GrantRemoveAds, NotifyChanged);

            if (!AdsRemoved)
                adMob.Start();
        }
        else
        {
            SimulatedMonetization simulated = SimulatedMonetization.Create();
            ads = simulated;
            store = simulated;
        }
    }

    public static void ShowBanner()
    {
        EnsureProviders();

        if (!AdsRemoved)
            ads.ShowBanner();
    }

    public static void HideBanner()
    {
        ads?.HideBanner();
    }

    // Anuncio de pantalla completa al perder. Si no corresponde mostrar uno, sigue directo.
    public static void ShowInterstitial(Action closed)
    {
        if (!InterstitialDue)
        {
            closed?.Invoke();
            return;
        }

        InterstitialShowing = true;
        lastInterstitialTime = Time.realtimeSinceStartup;
        AudioManager.SetPlayState(PlayState.Paused);

        ads.ShowInterstitial(() =>
        {
            InterstitialShowing = false;
            AudioManager.SetPlayState(PlayState.Results);
            closed?.Invoke();
        });
    }

    public static void BuyRemoveAds()
    {
        EnsureProviders();

        store.BuyRemoveAds(purchased =>
        {
            if (purchased)
                AudioManager.PlayUI(UISound.Unlock);
        });
    }

    public static void ShowPrivacyOptions()
    {
        EnsureProviders();
        ads.ShowPrivacyOptions();
    }

    // La llama la tienda al confirmar una compra, nueva o recuperada
    private static void GrantRemoveAds()
    {
        if (AdsRemoved)
            return;

        PlayerPrefs.SetInt(AdsRemovedKey, 1);
        PlayerPrefs.Save();

        ads?.Shutdown();
        NotifyChanged();
    }

    public static void GrantSimulatedPurchase() =>
        GrantRemoveAds();

    private static void NotifyChanged() =>
        Changed?.Invoke();
}
