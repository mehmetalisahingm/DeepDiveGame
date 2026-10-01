using DeepDive.Network;

namespace DeepDive.World
{
    // The water seam: given one diver's capsule, say which environment they are in.
    //
    // (a) A pure query. It carries no network state, owns no player mover, and moves nobody -
    //     it answers a question and returns. Movement stays where it already is.
    // (b) The answer is Mehmet's EnvironmentLocomotion. There is deliberately no separate
    //     World-side water enum to keep in sync; one type, one meaning.
    // (c) Composition does the binding. It is the only assembly that references both
    //     DeepDive.World and DeepDive.Network, so DeepDive.Network never sees IWaterField.
    // (d) Tick order is Classify -> SetEnvironmentLocomotionServer -> server movement. The
    //     classification is read before the move it governs, never after.
    // (e) Any tracker lives per diver and is reset on spawn, despawn, teleport and scene reset.
    //     It is local host state and is never carried over the network.
    // (f) NetworkPlayer's legacy SwimVolume.Contains query retires on the first
    //     SetEnvironmentLocomotionServer call (it flips externalEnvironmentBound), so the two
    //     never both drive the environment - there is no dual authority.
    public interface IWaterField
    {
        EnvironmentLocomotion Classify(in WaterProbe probe);
    }
}
