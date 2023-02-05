using UnityEngine;

namespace JMor.AssetPostprocessors
{
	[RequireComponent(typeof(Camera))]
    public class ImportedCameraData : MonoBehaviour
    {
		private Camera camera;
		public bool logDesynced = true;
		public float xfov = float.NaN;
		public float yfov = float.NaN;
		public float aspect_ratio = float.NaN;
		public float znear = float.NaN;
		public float zfar = float.NaN;
		public bool force_xfovToRespectImportedValues = true;
		public bool force_yfovToRespectImportedValues = true;
		public bool force_aspect_ratioToRespectImportedValues = true;
		public bool force_znearToRespectImportedValues = true;
		public bool force_zfarToRespectImportedValues = true;

		public void Initialize(
			float xfov = float.NaN,
			float yfov = float.NaN,
			float aspect_ratio = float.NaN,
			float znear = float.NaN,
			float zfar = float.NaN)
		{
			this.xfov = xfov;
			this.yfov = yfov;
			this.aspect_ratio = aspect_ratio;
			this.znear = znear;
			this.zfar = zfar;
		}

        void OnValidate()
		{
			if (camera == null)
				camera = GetComponent<Camera>();
			
			if (float.IsFinite(xfov) && Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect) != xfov)
			{
				if (force_xfovToRespectImportedValues)
					camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(xfov, aspect_ratio);
				else if (logDesynced)
					Debug.LogWarning($"Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect)({Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect)}) != xfov ({xfov})");
			}
			if (float.IsFinite(yfov) && camera.fieldOfView != yfov)
			{
				if (force_yfovToRespectImportedValues)
					camera.fieldOfView = yfov;
				else if (logDesynced)
					Debug.LogWarning($"camera.fieldOfView({camera.fieldOfView}) != yfov ({yfov})");
			}
			if (float.IsFinite(aspect_ratio) && camera.aspect != aspect_ratio)
			{
				if (force_aspect_ratioToRespectImportedValues)
					camera.aspect = aspect_ratio;
				else if (logDesynced)
					Debug.LogWarning($"camera.aspect({camera.aspect}) != aspect_ratio ({aspect_ratio})");
			}
			if (float.IsFinite(znear) && camera.nearClipPlane != znear)
			{
				if (force_znearToRespectImportedValues)
					camera.nearClipPlane = znear;
				else if (logDesynced)
					Debug.LogWarning($"camera.nearClipPlane({camera.nearClipPlane}) != znear ({znear})");
			}
			if (float.IsFinite(zfar) && camera.farClipPlane != zfar)
			{
				if (force_zfarToRespectImportedValues)
					camera.farClipPlane = zfar;
				else if (logDesynced)
					Debug.LogWarning($"camera.farClipPlane({camera.farClipPlane}) != zfar ({zfar})");
			}
		}
		
		[ContextMenu("CheckCameraValues")]
		public void CheckCameraValues()
		{
			Debug.Log($"\tCamera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect): {Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect)}");
			Debug.Log($"\tcamera.fieldOfView: {camera.fieldOfView}");
			Debug.Log($"\tcamera.aspect: {camera.aspect}");
			Debug.Log($"\tcamera.nearClipPlane: {camera.nearClipPlane}");
			Debug.Log($"\tcamera.farClipPlane: {camera.farClipPlane}");
		}
		
		[ContextMenu("ReapplyCameraValues")]
		public void ReapplyCameraValues()
		{
			Debug.Log($"STORED");
			Debug.Log($"\txfov: {xfov}");
			Debug.Log($"\tyfov: {yfov}");
			Debug.Log($"\taspect_ratio: {aspect_ratio}");
			Debug.Log($"\tznear: {znear}");
			Debug.Log($"\tzfar: {zfar}");
			Debug.Log($"CURRENT CAMERA EQUIVALENTS");
			Debug.Log($"\tCamera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect): {Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect)}");
			Debug.Log($"\tcamera.fieldOfView: {camera.fieldOfView}");
			Debug.Log($"\tcamera.aspect: {camera.aspect}");
			Debug.Log($"\tcamera.nearClipPlane: {camera.nearClipPlane}");
			Debug.Log($"\tcamera.farClipPlane: {camera.farClipPlane}");
			if (float.IsFinite(yfov)) camera.fieldOfView = yfov;
			if (float.IsFinite(aspect_ratio)) camera.aspect = aspect_ratio;
			if (float.IsFinite(znear)) camera.nearClipPlane = znear;
			if (float.IsFinite(zfar)) camera.farClipPlane = zfar;
			Debug.Log($"NEW CAMERA EQUIVALENTS");
			Debug.Log($"\tCamera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect): {Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect)}");
			Debug.Log($"\tcamera.fieldOfView: {camera.fieldOfView}");
			Debug.Log($"\tcamera.aspect: {camera.aspect}");
			Debug.Log($"\tcamera.nearClipPlane: {camera.nearClipPlane}");
			Debug.Log($"\tcamera.farClipPlane: {camera.farClipPlane}");
		}
    }
}
