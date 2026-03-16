# Spatial Audio with Agora on Oculus
## NOTE THIS PACKAGE IS BASED ON LEGACY 3.x SDK ##

This Unity project package includes the scene file and its dependent resources for running a spatial audio test on individual user objects.  Remote users from non VR platforms can be viewed on the Capsule. And their sound will be originated from the Capsule object in the VR environment. 
![oculus scene](https://user-images.githubusercontent.com/1261195/123018737-f0db1c00-d383-11eb-90e1-e2bacf3e03d7.gif)

## Developer Environment Requirements
- Unity3d 2019 LTS or above
- Oculus Quest
- extra non VR device with camera and microphone input

## Quick Start

This section shows you how to prepare, build, and run the sample application. 

### Obtain an App ID

To build and run the sample application, get an App ID:
1. Create a developer account at [agora.io](https://dashboard.agora.io/signin/). Once you finish the signup process, you will be redirected to the Dashboard.
2. Navigate in Agora Console on the left to **Projects** > **More** > **Create** > **Create New Project**.
3. Save the **App ID** from the Dashboard for later use.

### Run the Application   

#### Build to Oculus
1. Set up Unity environment for Oculus Quest ([see offical guide](https://developer.oculus.com/documentation/unity/unity-gs-overview/))
2. Download [Agora Video SDK for Unity](https://assetstore.unity.com/packages/tools/video/agora-video-sdk-for-unity-134502)
3. Import this package
4. Fill in App ID and make sure all other fields gets filled too.  Capsule is the default prefab but you may make your own for replacement.
5. Please make sure if your AppID has token or not.  Things won't work if you don't supply a token if your AppID requires one.
![AgoraRoot](https://user-images.githubusercontent.com/1261195/123020656-7dd3a480-d387-11eb-9fee-d4308cfed33d.png)
6. Build and run
7. [optional recommendation] Use Side Quest app to run/kill/unload App


## Resources
- [Tutorial blog](https://www.agora.io/en/blog/how-to-build-a-vr-video-chat-app-with-spatial-audio-on-oculus/) describes this project
- For potential Agora SDK issues, take a look at our [FAQ](https://docs.agora.io/en/faq) first
- Dive into [Agora SDK Samples](https://github.com/AgoraIO/Agora-Unity-Quickstart) to see more API samples for Unity
- Repositories managed by developer communities can be found at [Agora Community](https://github.com/AgoraIO-Community)


## License
The MIT License (MIT).

