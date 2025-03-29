using Unity.Entities;

namespace Metal.Components {
    public struct SharedBlobAssets : IComponentData {
        public BlobAssetReference<FloatArrayBlob> vehicleHiluxTurningCurve;
    }
}