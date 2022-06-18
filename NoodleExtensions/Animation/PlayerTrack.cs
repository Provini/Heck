﻿using Heck;
using Heck.Animation;
using IPA.Utilities;
using JetBrains.Annotations;
using NoodleExtensions.HarmonyPatches.SmallFixes;
using UnityEngine;
using Zenject;
using static Heck.HeckController;
using static NoodleExtensions.NoodleController;

namespace NoodleExtensions.Animation
{
    internal class PlayerTrack : MonoBehaviour
    {
        private static readonly FieldAccessor<PauseController, bool>.Accessor _pausedAccessor = FieldAccessor<PauseController, bool>.GetAccessor("_paused");

        // because camera2 is cringe
        // stop using reflection you jerk
        [UsedImplicitly]
        private static PlayerTrack? _instance;

        [UsedImplicitly]
        private Transform _transform = null!;

        private bool _leftHanded;

        private Vector3 _startPos = Vector3.zero;
        private Quaternion _startLocalRot = Quaternion.identity;

        private Track _track = null!;
        private PauseController _pauseController = null!;
        private BeatmapObjectSpawnMovementData _movementData = null!;

        internal void AssignTrack(Track track)
        {
            _track = track;
        }

        [UsedImplicitly]
        [Inject]
        private void Construct(
            PauseController pauseController,
            [Inject(Id = LEFT_HANDED_ID)] bool leftHanded,
            InitializedSpawnMovementData movementData)
        {
            _pauseController = pauseController;

            pauseController.didPauseEvent += OnDidPauseEvent;
            Transform origin = transform;
            _startLocalRot = origin.localRotation;
            _startPos = origin.localPosition;
            _leftHanded = leftHanded;
            _movementData = movementData.MovementData;

            // cam2 is cringe cam2 is cringe cam2 is cringe
            _instance = this;
            _transform = origin;
        }

        private void OnDidPauseEvent()
        {
            Transform transform1 = transform;
            transform1.localRotation = _startLocalRot;
            transform1.localPosition = _startPos;
        }

        private void OnDestroy()
        {
            if (_pauseController != null)
            {
                _pauseController.didPauseEvent -= OnDidPauseEvent;
            }
        }

        private void Update()
        {
            if (_pausedAccessor(ref _pauseController))
            {
                return;
            }

            Quaternion? rotation = _track.GetProperty<Quaternion?>(OFFSET_ROTATION)?.Mirror(_leftHanded);
            Vector3? position = _track.GetProperty<Vector3?>(OFFSET_POSITION)?.Mirror(_leftHanded);

            Quaternion worldRotationQuatnerion = Quaternion.identity;
            Vector3 positionVector = _startPos;
            if (rotation.HasValue || position.HasValue)
            {
                Quaternion finalRot = rotation ?? Quaternion.identity;
                worldRotationQuatnerion *= finalRot;
                Vector3 finalPos = position ?? Vector3.zero;
                positionVector = worldRotationQuatnerion * ((finalPos * _movementData.noteLinesDistance) + _startPos);
            }

            worldRotationQuatnerion *= _startLocalRot;
            Quaternion? localRotation = _track.GetProperty<Quaternion?>(LOCAL_ROTATION)?.Mirror(_leftHanded);
            if (localRotation.HasValue)
            {
                worldRotationQuatnerion *= localRotation.Value;
            }

            Transform transform1 = transform;
            transform1.localRotation = worldRotationQuatnerion;
            transform1.localPosition = positionVector;
        }

        [UsedImplicitly]
        internal class PlayerTrackFactory : IFactory<PlayerTrack>
        {
            private readonly DiContainer _container;

            private PlayerTrackFactory(DiContainer container)
            {
                _container = container;
            }

            public PlayerTrack Create()
            {
                Transform player = GameObject.Find("LocalPlayerGameCore").transform;
                GameObject noodleObject = new("NoodlePlayerTrack");
                Transform origin = noodleObject.transform;
                origin.SetParent(player.parent, true);
                player.SetParent(origin, true);
                return _container.InstantiateComponent<PlayerTrack>(noodleObject);
            }
        }
    }
}
