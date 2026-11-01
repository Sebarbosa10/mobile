// Datos de las cuentas de anuncios y tienda. Para publicar hay que reemplazar los IDs de prueba por
// los de AdMob (y el App ID en Assets > Google Mobile Ads > Settings). Pasos en PUBLICAR.md.
public static class MonetizationConfig
{
    // IDs públicos de prueba de Google: muestran anuncios de test y no generan ingresos
    public const string TestBannerUnitId = "ca-app-pub-3940256099942544/6300978111";
    public const string TestInterstitialUnitId = "ca-app-pub-3940256099942544/1033173712";

    public const string AndroidBannerUnitId = TestBannerUnitId;
    public const string AndroidInterstitialUnitId = TestInterstitialUnitId;

    // Tiene que coincidir con el producto "Sin anuncios" creado en Google Play Console
    public const string RemoveAdsProductId = "remove_ads";

    public const string FallbackPrice = "US$1";

    // Evita mostrar un anuncio en cada derrota seguida. 0 = en todas.
    public const float MinSecondsBetweenInterstitials = 45f;

    public static bool UsingTestAds =>
        AndroidBannerUnitId == TestBannerUnitId || AndroidInterstitialUnitId == TestInterstitialUnitId;
}
