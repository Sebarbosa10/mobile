    using UnityEngine;

    public enum HoopPositionMode
    {
        Fixed,
        Depth,
        Horizontal,
        Diagonal,
        Circular,
        Vertical
    }

    public enum HoopMovementMode
    {
        Static,
        Moving
    }

    [CreateAssetMenu(
        fileName = "HoopDifficultyProfile",
        menuName = "Basketball/Hoop Difficulty Profile")]
    public sealed class HoopDifficultyProfile : ScriptableObject
    {
        [SerializeField]
        private HoopStage[] stages;

        [Header("Size")]
        [Tooltip("Tamaño del aro al empezar la partida.")]
        [SerializeField, Min(0.01f)]
        private float startScale = 1.15f;

        [Tooltip("Tamaño mínimo que puede tocar después de cada encestada.")]
        [SerializeField, Min(0.01f)]
        private float minScale = 0.95f;

        [Tooltip("Tamaño máximo que puede tocar después de cada encestada.")]
        [SerializeField, Min(0.01f)]
        private float maxScale = 1.15f;

        [Tooltip("Diferencia mínima con el tamaño anterior, para que el cambio se note.")]
        [SerializeField, Min(0f)]
        private float minScaleChange = 0.05f;

        // Sin puntos usa el tamaño inicial; después, uno al azar que no quede pegado al anterior
        public float PickScale(int score, float currentScale)
        {
            if (score <= 0)
                return startScale;

            float blockedLow = Mathf.Clamp(currentScale - minScaleChange, minScale, maxScale);
            float blockedHigh = Mathf.Clamp(currentScale + minScaleChange, minScale, maxScale);

            float roomBelow = blockedLow - minScale;
            float roomAbove = maxScale - blockedHigh;

            float pick = Random.Range(0f, roomBelow + roomAbove);

            return pick < roomBelow
                ? minScale + pick
                : blockedHigh + (pick - roomBelow);
        }

        public HoopStage GetStageForScore(int score)
        {
            if (stages == null || stages.Length == 0)
                return default;

            HoopStage selectedStage = stages[0];

            for (int i = 0; i < stages.Length; i++)
            {
                if (score < stages[i].requiredScore)
                    break;

                selectedStage = stages[i];
            }

            return selectedStage;
        }
    }

    [System.Serializable]
    public struct HoopStage
    {
        [Header("Progression")]
        [Min(0)]
        public int requiredScore;

        [Header("Position")]
        public HoopPositionMode positionMode;

        [Tooltip("Posición fija respecto a la posición inicial del aro.")]
        public Vector3 positionOffset;

        [Header("Movement")]
        public HoopMovementMode movementMode;

        [Min(0f)]
        public float movementAmplitude;

        [Min(0f)]
        public float movementSpeed;
    }
