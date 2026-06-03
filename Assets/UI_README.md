Simple UI Screen (runtime)

Summary
- A small runtime UI builder for Unity that creates a clean welcome screen with a title, subtitle, and two buttons.

Files
- Assets/Scripts/UI/SimpleUIScreen.cs

Usage
1. In your scene, create an empty GameObject (GameObject -> Create Empty).
2. Add the `SimpleUIScreen` component to that GameObject.
3. Optionally customize the `title`, `subtitle`, colors, and button text in the inspector.
4. Optionally assign a `heroSprite` to show a real character image in the center of the portrait screen.
5. Hook UnityEvents `onPrimaryClicked` and `onSecondaryClicked` in the inspector to call your methods.
6. Press Play — the UI will be created automatically at runtime.

Mobile / Simulator tips
- Set the project orientation to portrait: `Edit -> Project Settings -> Player -> Resolution and Presentation -> Default Orientation -> Portrait`.
- The UI now applies a Safe Area and auto-sizes text for mobile. If the UI still looks thin:
    - Ensure the Canvas Scaler on the generated canvas uses `Scale With Screen Size` and a portrait reference resolution (1080x1920). The script does this automatically but you can edit the canvas prefab if you made one.
    - Increase `title` and `subtitle` font sizes in the inspector or rely on Auto Size (enabled by default).
    - Use `File -> Build Settings` and build to device (or use Unity Remote) rather than only using the editor Game view for accurate DPI and safe area.
    - If testing in the Editor Game view, set the Game view resolution to a mobile portrait resolution (e.g., 1080x1920) from the Game view dropdown.
    - If you want the screen to feel more like a real app, keep the Game view in a tall phone aspect ratio and avoid wide desktop resolutions.

Notes
- This script uses TextMeshPro for text. Make sure TextMeshPro is present in the project (your project already contains TextMesh Pro files).
- The script will create a Canvas if one is not present. If you already have a Canvas, the UI will be parented under the first Canvas found.
- Styling is minimal and programmatic; replace the panel Image sprite with a sliced rounded sprite in the Editor for rounded corners.

Quick test code
You can attach a simple test script to log button presses:

```csharp
using UnityEngine;

public class UITestHooks : MonoBehaviour
{
    public void OnStartPressed() => Debug.Log("Primary pressed");
    public void OnSettingsPressed() => Debug.Log("Secondary pressed");
}
```

Then assign these methods to the `SimpleUIScreen` events in the inspector.
