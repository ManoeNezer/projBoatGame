using BoatGame.Player;
using UnityEngine;

namespace BoatGame.Interaction
{
    public interface IPlayerStation
    {
        Transform BodyAnchor { get; }
        Transform CameraAnchor { get; }
        Rigidbody PlatformBody { get; }
        void ExitStation(FpsPlayerController player);
    }
}
