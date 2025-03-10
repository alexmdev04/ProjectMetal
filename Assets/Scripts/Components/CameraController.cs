using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Metal.Components {
    public class CameraController : MonoBehaviour {
        [SerializeField] private float 
            cameraDistance = 15.0f,
            cameraLerpSpeed = 5.0f;
        private EntityManager entityManager;
        private Entity playerEntity;
        private const float cameraDiagonalMultiplier = -(2.0f / 3.0f); // -0.666...

        private void Start() {
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            //Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
        }

        private void LateUpdate() {
            if (playerEntity == Entity.Null) {
                EntityQuery playerEntityQuery = entityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Domain)
                    .WithAll<Tags.Player>());//.ToEntityArray(Allocator.Temp);

                if (!playerEntityQuery.IsEmpty) {
                    NativeArray<Entity> playerEntityQueryArray = playerEntityQuery.ToEntityArray(Allocator.Temp);
                    playerEntity = playerEntityQueryArray[0];
                    playerEntityQueryArray.Dispose();
                }

                playerEntityQuery.Dispose();
                return;
            }

            Vector3 playerPosition = entityManager.GetComponentData<LocalTransform>(playerEntity).Position;
            float cameraDiagonalOffset = cameraDistance * cameraDiagonalMultiplier;
            transform.position = Vector3.Lerp(
                transform.position,
                playerPosition + new Vector3(cameraDiagonalOffset, cameraDistance, cameraDiagonalOffset),
                cameraLerpSpeed * Time.deltaTime);
        }
    }
}