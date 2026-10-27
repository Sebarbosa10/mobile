using UnityEngine;

// Qué sonido corresponde a cada collider contra el que pega la pelota. Usa los nombres que arma
// el CourtBuilder (RimColliders, Backboard, Pole); un AudioSurface en el collider o un padre
// tiene prioridad sobre los nombres.
public static class BallSurfaces
{
    public static BallSurface Classify(Collider collider, string groundTag)
    {
        if (collider.GetComponentInParent<AudioSurface>() is { } explicitSurface)
            return explicitSurface.Surface;

        if (collider.CompareTag(groundTag))
            return BallSurface.Floor;

        for (Transform t = collider.transform; t != null; t = t.parent)
        {
            string name = t.name;

            if (name == "RimColliders" || name == "Rim")
                return BallSurface.Rim;

            if (name.StartsWith("Backboard"))
                return BallSurface.Backboard;

            if (name.StartsWith("Pole"))
                return BallSurface.Pole;
        }

        return BallSurface.Soft;
    }
}
