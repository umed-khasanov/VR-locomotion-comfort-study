# Week 6 – Locomotion Comfort Study (Unity XR)

VR locomotion practical for Meta Quest 2: comparing **teleportation** and **smooth locomotion** for comfort.

## What's inside
- `Assets/Week6/Week5_Locomotion.unity` – test scene: 40x40 floor with Teleportation Area, obstacles, a ~34 m route with 5 markers (90° and 180° turns), 2 Teleportation Anchors, XR Origin (Starter Assets), Tunneling Vignette.
- `Assets/Week6/Scripts/ComfortSettings.cs` – switches Scheme (Teleport / Smooth), turning type and vignette.
- `Assets/Week6/Editor/LocomotionSceneBuilder.cs` – menu **Week6 > Build Locomotion Scene** rebuilds the scene.

## Requirements
Unity 6000.3.22f1, XR Interaction Toolkit 3.3.2 (import the Starter Assets sample), OpenXR, Meta Quest 2 (or Meta XR Simulator).

## How to run
1. Open the folder in Unity Hub.
2. Open the scene, select the `ComfortSettings` object and choose a Scheme.
3. Press Play with the simulator, or Build And Run to Quest.

## Study
Compare both schemes on the same route and rate discomfort (FMS 0–20) after each run.

## Author
Umedchon Khasanov – TUS Midwest, BSc Creative Digital Computing
