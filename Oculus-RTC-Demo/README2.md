# Spatial RTC Audio on Oculus
This is a Unity demo implementation for integrating Agora RTC SDK with Oculus spatial audio in a VR environment. It demonstrates real-time spatialized audio rendering for remote users in a multi-user VR session, where each remote participant's audio is played back through a dedicated 3D AudioSource positioned in the scene, creating immersive spatial audio effects.

## Sample Scene

![OculusRTCDemo](https://github.com/user-attachments/assets/89da2251-de70-4b5f-a628-55a3cc606d31)


## Developer Environment Prerequisites
- Unity 6 LTS
- Oculus Quest2 or above
- Agora Developer Account

## Quick Start

This section shows you how to prepare, build, and run the sample application.

### Obtain an App ID

To build and run the sample application, get an App ID:

1. Create a developer account at [agora.io](https://dashboard.agora.io/signin/). Once you finish the signup process, you will be redirected to the Dashboard.

2. Navigate in Agora Console on the left to **Projects** > **More** > **Create** > **Create New Project**.

3. Save the **App ID** from the Dashboard for later use.

  

### Run the Application
#### Set up Unity environment for Oculus Quest
1. Clone this repo and open the project from this folder
2. Set up Unity environment for Oculus Quest ([see offical guide](https://developer.oculus.com/documentation/unity/unity-gs-overview/))
3. Download the latest [Agora Video SDK for Unity](https://docs.agora.io/en/sdks?platform=unity)
4. Include [Meta XR Core SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169)
5. Include [Meta XR Audio SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-audio-sdk-264557)  
Configure Project Audio Settings

#### Spatial Audio Configurations
Unity Editor Menu: Edit → Project Settings → Audio <br>
Set these exactly:<br>
Spatializer Plugin → Meta XR Audio
Ambisonics Decoder Plugin → Meta XR Audio

#### Build to Oculus
6. Open the SpatialAudioDemo.unity scene
7. Fill in App ID and Channel Name.  ![OculusRTCDemo-AppID](https://user-images.githubusercontent.com/1261195/234465370-7a09702f-7429-43d7-8fea-6feb4b573149.jpg)

8. Make sure if your AppID has token or not.  Things won't work if you don't supply a token if your AppID requires one.  We recommend use an AppID for testing first before applying token logic.

9. Use [the Web Demo](https://webdemo.agora.io/basicVideoCall/index.html) as a second user to test the RTC call.  Or use Media Player demo from Agora Video SDK.  Configure the Media Player to use the same App ID and Channel Name.  The example that we used in the Recording was the iOS version.
  
## Key Programming Notes

### OculusSpatialRTCAudio Architecture Overview

**High-level Flow:**
```
Remote User Audio (Agora RTC) 
    ↓ (playbackAudioFrameBeforeMixing Observer)
Raw PCM Frames (48kHz/mono float16)
    ↓ (UserAudioFrame2SourceSample → Dispatch to Handler)
RingBuffer (10s capacity)
    ↓ (Unity AudioClip streaming callback: OnAudioRead)
AudioSource on 3D Prefab Position
    ↓ (Meta XR Audio SDK Spatializer)
Immersive Spatial Audio Rendering (Oculus Quest)
```

**Key Components:**

1. **UserAudioFrame2SourceSample.cs** (Main RTC Manager):
   - Initializes `IRtcEngine` with `CHANNEL_PROFILE_LIVE_BROADCASTING`, `AUDIO_SCENARIO_GAME_STREAMING`.
   - Enables `playbackAudioFrameBeforeMixing` observer.
   - Suppresses mixed playback: `AdjustPlaybackSignalVolume(0)`.
   - On user join: Spawns `Capsule.prefab` with `VideoSurface` (video) + `UserAudioFrameHandler` (audio).
   - Dispatches raw `AudioFrame` to per-user handlers.

2. **UserAudioFrameHandler.cs** (Per-User Audio Renderer):
   - RingBuffer for float audio data (PCM16 → float16 conversion).
   - Dynamic `AudioClip.Create` (samples/channel from frame, streaming=true).
   - `OnAudioRead` pulls from RingBuffer for zero-copy playback.
   - Attached to 3D prefab positioned in scene (e.g., `SpawnPosition[]`).

3. **Spatialization Setup** (Project Settings → Audio):
   ```
   Spatializer Plugin → Meta XR Audio
   Ambisonics Decoder Plugin → Meta XR Audio
   ```
   Oculus Avatar positions + Meta XR Audio SDK enable HRTF/spatial reverb.

**Demo Usage:**
- Open `SpatialAudioDemo.unity`.
- Set App ID/Channel/Token.
- Build to Quest 2+.
- Join same channel from Web/MediaPlayer demo → Hear spatialized remote audio.

**Dependencies:**
- .NET 4.6+ / Standard 2.0 (BlockingCollection).
- Meta XR Core/Audio SDKs (Asset Store).
- Agora RTC SDK (Plugins).

**Notes:**
- Audio params dynamic from frame but defaults 48kHz/1ch/320 samples.
- Handles multiple users; scales with `userCount`.
- Mute support via `MuteAllRemoteAudio`.

This decouples RTC raw frames from Unity mixer, enabling precise 3D spatial positioning for VR RTC.

## License

The MIT License (MIT).



