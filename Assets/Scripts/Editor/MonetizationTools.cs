using UnityEditor;
using UnityEngine;

public static class MonetizationTools
{
    [MenuItem("Tools/Monetización/Restablecer compra de quitar anuncios")]
    private static void ResetPurchase()
    {
        PlayerPrefs.DeleteKey(Monetization.AdsRemovedKey);
        PlayerPrefs.Save();
        Debug.Log("[Monetization] Compra restablecida: los anuncios vuelven a aparecer.");
    }
}
