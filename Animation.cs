using LethalEmotesAPI.ImportV2;
using LethalEmotesAPI.Utils;
using UnityEngine;
using static FlickStick.Plugin;

internal class Animation
{
    private static HumanBodyBones[] ignoredRootBones = new HumanBodyBones[2]
    {
        HumanBodyBones.LeftUpperLeg,
        HumanBodyBones.RightUpperLeg
    };

    private static HumanBodyBones[] ignoredSoloBones = new HumanBodyBones[4]
    {
        HumanBodyBones.Head,
        HumanBodyBones.Neck,
        HumanBodyBones.Spine,
        HumanBodyBones.Hips
    };

    public static void instantiateAnimations()
    {
        CustomEmoteParams customEmoteParams = new CustomEmoteParams();
        customEmoteParams.primaryAnimationClips = new AnimationClip[1] { PlayerGrabAnimation };
        //customEmoteParams.secondaryAnimationClips = new AnimationClip[1] { PlayerGrabAnimation };
        customEmoteParams.visible = true; // TODO: Change to false later
        customEmoteParams.audioLevel = 0f;
        customEmoteParams.audioLoops = false;
        customEmoteParams.allowJoining = false;
        customEmoteParams.thirdPerson = false;
        customEmoteParams.forceCameraMode = true;
        customEmoteParams.allowThirdPerson = false;
        customEmoteParams.displayName = "Grab";
        customEmoteParams.internalName = "flickstickgrab";
        customEmoteParams.rootBonesToIgnore = ignoredRootBones;
        customEmoteParams.soloBonesToIgnore = ignoredSoloBones;
        customEmoteParams.useLocalTransforms = true;
        customEmoteParams.animateHealthbar = false;
        CustomEmoteParams animationClipParams = customEmoteParams;
        EmoteImporter.ImportEmote(animationClipParams);
        LoggerInstance.LogDebug("Animation instantiated");
    }
}
