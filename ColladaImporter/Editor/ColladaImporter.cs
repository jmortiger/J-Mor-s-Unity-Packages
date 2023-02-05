using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using UnityEditor;
using UnityEngine;
using FB = JMor.AssetPostprocessors.EditorScripts.FallbackSchemas.Fallback;

namespace JMor.AssetPostprocessors.EditorScripts
{
	public class ColladaPostprocessor : AssetPostprocessor
	{
		void OnPostprocessModel(GameObject g)
		{
			if (!assetPath.EndsWith(".dae", true, System.Globalization.CultureInfo.CurrentCulture))
				return;
			var doc = GetXmlDocumentValidated().DocumentElement;
			var library_camerasNode = FindElementRecursively(n => n.LocalName == "library_cameras" || n.Name == "library_cameras", doc);
			Dictionary<string, ValueTuple<float, float>> camsToConvertInfo = new();
			// var camsToConvert = FindElementsRecursively(n =>
			// {
			// 	if (n.LocalName != "camera" && n.Name != "camera")
			// 		return false;
			// 	var perspectiveNode = FindElementRecursively(MatchName("perspective"), n);
			// 	if (perspectiveNode == null) return false;
			// 	var yFovNode = FindElementRecursively(MatchName("yfov"), perspectiveNode);
			// 	var xFovNode = FindElementRecursively(MatchName("xfov"), perspectiveNode);
			// 	return xFovNode != null && yFovNode != null;
			// }, library_camerasNode);
			foreach (XmlElement n in library_camerasNode.GetElementsByTagName("camera"))
			{
				var perspectiveNode = FindElementRecursively(MatchName("perspective"), n);
				if (perspectiveNode != null)
				{
					var yFovNode = FindElementRecursively(MatchName("yfov"), perspectiveNode);
					var xFovNode = FindElementRecursively(MatchName("xfov"), perspectiveNode);
					if (xFovNode != null && yFovNode != null)
					{
						var cameraName = (n as XmlElement).GetAttribute("id");
						// Not sure if this is how Unity's importer works if there's no id field, but makes sense.
						if (string.IsNullOrWhiteSpace(cameraName))
							cameraName = (n as XmlElement).GetAttribute("name");
						if (string.IsNullOrWhiteSpace(cameraName))
							cameraName = (n as XmlElement).GetAttribute("sid");
						camsToConvertInfo.Add(cameraName, (xfov: float.Parse(xFovNode.InnerText), yfov: float.Parse(yFovNode.InnerText)));
					}
				}
			}
			var scenesRoot = FindElementRecursively(MatchName("library_visual_scenes"), doc);
			foreach (XmlNode visualScene in scenesRoot.ChildNodes)
			{
				if (visualScene.NodeType == XmlNodeType.Element)
					foreach (XmlNode nodeNode in visualScene.ChildNodes)
					{
						if (nodeNode.NodeType == XmlNodeType.Element)
						{
							var referencedCamera = FindElementRecursively(MatchName("instance_camera"), nodeNode as XmlElement)?.GetAttribute("url");
							if (referencedCamera != null)
							{
								if (referencedCamera[0] == '#')
									referencedCamera = referencedCamera.Substring(1);
								if (camsToConvertInfo.ContainsKey(referencedCamera))
								{
									var cameraName = (nodeNode as XmlElement).GetAttribute("name");
									// Not sure if this is how Unity's importer works if there's no name field, but makes sense.
									if (string.IsNullOrWhiteSpace(cameraName))
										cameraName = (nodeNode as XmlElement).GetAttribute("id");
									if (string.IsNullOrWhiteSpace(cameraName))
										cameraName = (nodeNode as XmlElement).GetAttribute("sid");
									var c = g.transform.Find(cameraName)?.GetComponent<Camera>();
									if (c != null)
									{
										var (x, y) = camsToConvertInfo[referencedCamera];
										var aspect = GetAspectRatioFromHorizontalAndVerticalFov(x, y, true);
										Debug.Assert(c.fieldOfView == y, $"Unity's importer doesn't correctly grab yFov?\n\tAssigned: {c.fieldOfView}\n\tFrom File:{y}");
										Debug.Log($"xFov = {x}");
										var old = (fieldOfView: c.fieldOfView, aspect: c.aspect);
										c.fieldOfView = y;
										c.aspect = aspect;
										Debug.Log($"Changed {c.name} fov/aspect ratio.\n\tFrom node: {nodeNode.OuterXml}\n\tFrom Camera:{{Name:{c.name},aspect:{old.aspect},fieldOfView:{old.fieldOfView}}}\n\tUpdated Camera:{{Name:{c.name},aspect:{c.aspect},fieldOfView:{c.fieldOfView}}}");
										var icd = c.gameObject.AddComponent<ImportedCameraData>();
										icd.Initialize(
											xfov: x,
											yfov: y,
											aspect_ratio: aspect
										);
									}
								}
							}
						}
					}
			}
		}

		// public static float GetAspectRatioFromHorizontalAndVerticalFov(
		// 	float hFov,
		// 	float vFov,
		// 	bool isInputDegrees = true) => isInputDegrees ? 
		// 		Mathf.Tan(hFov * Mathf.Deg2Rad / 2f) / Mathf.Tan(vFov * Mathf.Deg2Rad / 2f) : 
		// 		Mathf.Tan(hFov / 2f) / Mathf.Tan(vFov / 2f);
		
		public static float GetAspectRatioFromHorizontalAndVerticalFov(
			float hFov,
			float vFov,
			bool isInputDegrees = true)
		{
			if (isInputDegrees)
			{
				hFov *= Mathf.Deg2Rad;
				vFov *= Mathf.Deg2Rad;
			}
			return Mathf.Tan(hFov / 2f) / Mathf.Tan(vFov / 2f);
		}

		public static Predicate<XmlNode> MatchName(string nameToMatch) => subnode => subnode.LocalName == nameToMatch || subnode.Name == nameToMatch;

		#region FindNodes
		public static List<XmlNode> FindNodesRecursively(Predicate<XmlNode> predicate, XmlNode currentNode, List<XmlNode> nodes = null)
		{
			nodes ??= new List<XmlNode>();
			if (predicate(currentNode))
				nodes.Add(currentNode);
			for (int i = 0; currentNode.HasChildNodes && i < currentNode.ChildNodes.Count; i++)
				nodes = FindNodesRecursively(predicate, currentNode.ChildNodes[i], nodes);
			return nodes;
		}

		public static XmlNode FindNodeRecursively(Predicate<XmlNode> predicate, XmlNode currentNode)
		{
			if (predicate(currentNode))
				return currentNode;
			XmlNode output = null;
			for (int i = 0; output == null && currentNode.HasChildNodes && i < currentNode.ChildNodes.Count; i++)
				output = FindNodeRecursively(predicate, currentNode.ChildNodes[i]);
			return output;
		}
		#endregion

		#region FindElements
		public static List<XmlElement> FindElementsRecursively(Predicate<XmlElement> predicate, XmlElement currentElement, List<XmlElement> elements = null)
		{
			elements ??= new List<XmlElement>();
			if (predicate(currentElement))
				elements.Add(currentElement);
			for (int i = 0; currentElement.HasChildNodes && i < currentElement.ChildNodes.Count; i++)
				if (currentElement.ChildNodes[i].NodeType == XmlNodeType.Element)
					elements = FindElementsRecursively(predicate, currentElement.ChildNodes[i] as XmlElement, elements);
			return elements;
		}

		public static XmlElement FindElementRecursively(Predicate<XmlElement> predicate, XmlElement currentElement)
		{
			if (predicate(currentElement))
				return currentElement;
			XmlElement output = null;
			for (int i = 0; output == null && currentElement.HasChildNodes && i < currentElement.ChildNodes.Count; i++)
				if (currentElement.ChildNodes[i].NodeType == XmlNodeType.Element)
					output = FindElementRecursively(predicate, currentElement.ChildNodes[i] as XmlElement);
			return output;
		}
		#endregion

		// public static TextAsset schema;
		private XmlDocument GetXmlDocumentValidated()
		{
			XmlReaderSettings s = new();
			s.Schemas.Add(GetRemoteSchema());
			s.ValidationEventHandler += (object sender, ValidationEventArgs e) => 
			{
				if (e.Severity == XmlSeverityType.Warning)
					Debug.LogWarning("The following validation warning occurred: " + e.Message);
				else if (e.Severity == XmlSeverityType.Error)
					Debug.LogError("The following critical validation errors occurred: " + e.Message);
			};
			s.ValidationFlags = s.ValidationFlags | XmlSchemaValidationFlags.ReportValidationWarnings;
			s.ValidationType = ValidationType.Schema;
			var reader = XmlReader.Create(assetPath, s);
			XmlDocument d = new XmlDocument();
			d.PreserveWhitespace = false;
			d.Load(reader);
			return d;
		}

		#region GetSchema
		
		private XmlSchema GetRemoteSchema(string targetUri = defaultSchemaUri, string targetNamespace = defaultNamespace)
		{
			XmlSchema schema = null;
			XmlSchemaSet xs = new XmlSchemaSet();
			int loopSafety = 3;
			do
			{
				--loopSafety;
				try
				{
					schema = xs.Add(targetNamespace, targetUri);
					return schema;
				}
				catch (FileNotFoundException ex)
				{
					if (targetNamespace == defaultNamespace && targetUri == defaultSchemaUri)
					{
						Debug.LogWarning($"Failed so generating {defaultNamespace}{defaultSchemaUri} from local string representation: {ex.Message}");
						return GetLocalSchema(FallbackSchemas.GetFallback(defaultFallback), defaultNamespace);
					}
					else
					{
						Debug.LogWarning($"XSD file not found so trying fallback of {defaultNamespace}{defaultSchemaUri}: {ex.Message}");
						targetNamespace = defaultNamespace;
						targetUri = defaultSchemaUri;
					}
				}
			} while (loopSafety > 0);
			throw new System.Exception("IDK");
		}

		private XmlSchema GetLocalSchema(string schemaAsText = null, string targetNamespace = defaultNamespace)
		{
			XmlSchemaSet xs = new XmlSchemaSet();
			XmlSchema schema = null;

			if (schemaAsText != null)
				schemaAsText = FallbackSchemas.GetFallback(defaultFallback);

			StringReader stringReader = new StringReader(schemaAsText);
			XmlReader reader;
			try
			{
				reader = XmlReader.Create(stringReader);
			}
			catch (System.Exception ex)
			{
				if (schemaAsText == FallbackSchemas.GetFallback(FB.collada_schema_1_4_1_ms))
					throw;
				else
				{
					Debug.LogWarning($"Custom Generation failed so generating from stored Collada 1.4 schema: {ex.Message}");
					targetNamespace = defaultNamespace;
					reader = XmlReader.Create(FallbackSchemas.GetFallback(FB.collada_schema_1_4_1_ms));
				}
			}

			schema = xs.Add(targetNamespace, reader);

			return schema;
		}
		#endregion

		public FB defaultFallback = FB.collada_schema_1_4_1_ms;
		private const string defaultNamespace = @"http://www.collada.org/2005/11/COLLADASchema";//@"https://www.khronos.org/files/";
		private const string defaultSchemaUri = @"collada_schema_1_4_1_ms.xsd";
	}
}
