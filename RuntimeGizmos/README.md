An alternative for Unity's Gizmos that uses UnityEngine's [GL library](https://docs.unity3d.com/ScriptReference/GL.html) to work in builds. Direct calls must take place inside of OnRenderObject or an analogous Unity Message that is called ***after the camera is rendered***. Alternatively, attach the gizmo buffer MonoBehaviour to any object to allow for draw calls to the buffer to be stored and rendered at the appropriate time.

Requires my Utility package to work (uses MyMath for circle generation).