using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Den.Tools;
using Den.Tools.Splines;
using Den.Tools.Matrices;
using Den.Tools.GUI;
using MapMagic.Core;
using MapMagic.Products;

namespace MapMagic.Nodes.SplinesGenerators
{
	[System.Serializable]
	[GeneratorMenu (
		menu="Spline/Standard",  
		name ="Interlink", 
		iconName="GeneratorIcons/Constant",
		colorType = typeof(SplineSys), 
		disengageable = true, 
		helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Interlink200 : Generator, IInlet<TransitionsList>, IOutlet<SplineSys>
	{
		[Val("Input", "Inlet")]		public readonly Inlet<TransitionsList> input = new Inlet<TransitionsList>();

		[Val("Iterations")]		public int iterations = 8;
		[Val("Max Links")]		public int maxLinks = 4;
		[Val("Within Tile")]	public bool withinTile = true;

		public enum Clamp { Off, Full, Active }
		[Val("Clamp")]	public Clamp clamp;


		public IEnumerable<IInlet<object>> Inlets () { yield return input; }

		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList objs = data.products.ReadInlet(this);
			if (objs == null || !enabled) return; 

			SplineSys spline = new SplineSys();

			//creating hash set
			PosTab posTab;
			if (withinTile)
				posTab = new PosTab((Vector3)data.area.active.worldPos, (Vector3)data.area.active.worldSize, 16);
			else
			{
				Vector3 min = objs.Min();  min -= Vector3.one;
				Vector3 max = objs.Max();  max += Vector3.one;
				posTab = new PosTab(min, max-min, 16);
			}
			posTab.Add(objs);

			if (stop != null && stop.stop) return;
			GabrielGraph(posTab, spline, maxLinks:maxLinks, triesPerObj:iterations);

			if (stop != null && stop.stop) return;
			if (clamp == Clamp.Full) spline.Clamp((Vector3)data.area.full.worldPos, (Vector3)data.area.full.worldSize);
			if (clamp == Clamp.Active) spline.Clamp((Vector3)data.area.active.worldPos, (Vector3)data.area.active.worldSize);

			if (stop != null && stop.stop) return;
			data.products[this] = spline;
		}


		private struct LinkIds { public int id1; public int id2; public LinkIds(int i1, int i2) {id1=i1; id2=i2;} }


		public static void GabrielGraph ( PosTab objs, SplineSys splineSys, int maxLinks=4, int triesPerObj=8 )
		{
			triesPerObj = Mathf.Min(objs.totalCount-1, triesPerObj);

			Dictionary<int,int[]> closestMap = new Dictionary<int, int[]>();

			//bool CheckIfWithin (Transition trs) 
			//{
			//	return  trs.pos.x >= worldPos.x  &&  trs.pos.x <= worldPos.x+worldSize.x  &&
			//			trs.pos.z >= worldPos.z  &&  trs.pos.z <= worldPos.x+worldSize.z;
			//}

			//filling closest map
			foreach (Transition trs in objs.All())
			{
				int[] closestIds = new int[triesPerObj];
				closestMap.Add(trs.id, closestIds);

				float minDist = 0.001f;
				for (int i=0; i<triesPerObj; i++)
				{
					Transition closest = objs.Closest(trs.pos.x, trs.pos.z, minDist); //, filterFn:CheckIfWithin);

					float curDistSq = (trs.pos.x-closest.pos.x)*(trs.pos.x-closest.pos.x) + (trs.pos.z-closest.pos.z)*(trs.pos.z-closest.pos.z);
					minDist = Mathf.Sqrt(curDistSq)  + 0.001f;

					closestIds[i] = closest.id;
				}

				//maybe could speed up by creating a list of points nearby and then sorting theese points by distance
			}

			//connecting
			HashSet<LinkIds> connections = new HashSet<LinkIds>();
			Dictionary<int,int> idToLinksCount = new Dictionary<int,int>();
			foreach (var kvp in closestMap)
			{
				int id1 = kvp.Key;
				int[] closestIds1 = kvp.Value;

				for (int num1=0; num1<closestIds1.Length; num1++)
				{
					int id2 = closestIds1[num1];
					if (id2 == 0) continue; // no more objects left during closest map

					int[] closestIds2 = closestMap[id2];
					int num2 = closestIds2.Find(id1);

					//if id1 not contains in closestIds2
					if (num2 < 0) continue; 

					//if this link was not created earlier
					if (connections.Contains( new LinkIds(id1, id2) ) || 
						connections.Contains( new LinkIds(id2, id1) ) )
							continue;

					//if there is no common ids before num1 and num2
					bool hasCommonNodeCloser = false; //SNIPPET: the ideal case of using GOTO
					for (int i=0; i<num1; i++)
					{
						for (int j=0; j<num2; j++)
							if (closestIds1[i] == closestIds2[j]) { hasCommonNodeCloser = true; break; }
						if (hasCommonNodeCloser) break;
					}
					if (hasCommonNodeCloser) continue;

					//if the maximum number of connections reached
					idToLinksCount.TryGetValue(id1, out int linksCount1);
					idToLinksCount.TryGetValue(id2, out int linksCount2);
					if (linksCount1 >= maxLinks || linksCount2 >= maxLinks)
						continue;

					connections.Add( new LinkIds(id1, id2) );
					idToLinksCount.ForceAdd(id1, linksCount1 + 1);
					idToLinksCount.ForceAdd(id2, linksCount2 + 1);
				}
			}

			//converting connection links to positions
			Dictionary<int,Vector3> idToPos = new Dictionary<int, Vector3>();
			foreach (Transition trs in objs.All())
				idToPos.Add(trs.id, trs.pos);

			foreach (LinkIds ids in connections)
			{
				Vector3 pos1 = idToPos[ids.id1];
				Vector3 pos2 = idToPos[ids.id2];

				Line line = new Line(pos1, pos2);
				line.SetAllTangentTypes(Node.TangentType.linear);
				splineSys.AddLine(line);
			}

		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Pathfinding", iconName=null, disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Pathfinding200 : Generator, IMultiInlet, IOutlet<SplineSys>
	{
		[Val("Draft", "Inlet")]		public readonly Inlet<SplineSys> draftIn = new Inlet<SplineSys>();
		[Val("Height", "Inlet")]	public readonly Inlet<MatrixWorld> heightIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets () { yield return draftIn; yield return heightIn; }

		[Val("Resolution")]			public int resolution = 32;
		[Val("Distance Factor")]		public float distanceFactor = 1f;
		[Val("Elevation Factor")]		public float elevationFactor = 1f;
		[Val("Straighten Factor")]		public float straightenFactor = 1f;
		[Val("Max Elevation")]		public float maxElevation = 0.1f;
		[Val("Max Iterations")]		public int maxIterations = 1000000;


		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys src = data.products.ReadInlet(draftIn);
			MatrixWorld heights = data.products.ReadInlet(heightIn);
			if (src == null) return; 
			if (heights == null) { data.products[this]=src; return; }

			if (stop!=null && stop.stop) return;
			MatrixWorld downsampledHeights = new MatrixWorld(new CoordRect(0,0,resolution,resolution), heights.worldPos, heights.worldSize);
			MatrixOps.Resize(heights, downsampledHeights);
			
			if (stop!=null && stop.stop) return;
			SplineSys clamped = new SplineSys(src); 
			clamped.Clamp(heights.worldPos, heights.worldSize);
			SplineSys dst = FindPaths(clamped, downsampledHeights);

			dst.Update();
			if (stop!=null && stop.stop) return;
			data.products[this] = dst;
		}


		public SplineSys FindPaths (SplineSys src, MatrixWorld downsampledHeights)
		{
			List<Line> dstLines = new List<Line>();

			Matrix weights = new Matrix(downsampledHeights.rect);
			Matrix2D<Coord> dirs = new Matrix2D<Coord>(downsampledHeights.rect);

			FixedListPathfinding pathfind = new FixedListPathfinding() {
				distanceFactor = distanceFactor,
				elevationFactor = elevationFactor,
				straightenFactor = straightenFactor,
				maxElevation = maxElevation };

			for (int l=0; l<src.lines.Length; l++)
			{
				Line line = src.lines[l];
				List<Vector3> newPath = null;

				for (int s=0; s<line.segments.Length; s++)
				{
					Vector3 fromWorld = line.segments[s].start.pos;
					Vector3 toWorld = line.segments[s].end.pos;

					//checking if this line lays within heights matrix, and clamping all of the line segments out of matrix
					Rect worldRect2D = new Rect(downsampledHeights.worldPos.x, downsampledHeights.worldPos.z, downsampledHeights.worldSize.x, downsampledHeights.worldSize.z);

					Vector2 fromWorld2D = fromWorld.V2(); 
					Vector2 toWorld2D = toWorld.V2();

					if (!worldRect2D.IntersectsLine(fromWorld2D, toWorld2D)) continue;
					worldRect2D.ClampLine(ref fromWorld2D, ref toWorld2D);

					fromWorld = fromWorld2D.V3(); 
					toWorld = toWorld2D.V3();

					//world to coordinates
					Coord fromCoord = downsampledHeights.WorldToPixel(fromWorld.x, fromWorld.z);
					Coord toCoord = downsampledHeights.WorldToPixel(toWorld.x, toWorld.z);

					fromCoord.ClampByRect(downsampledHeights.rect);
					toCoord.ClampByRect(downsampledHeights.rect);

					//pathfinding
					Coord[] pathCoord = pathfind.FindPathDijkstra(fromCoord, toCoord, downsampledHeights, weights, dirs); //Pathfinding.FindPathDijkstraList(fromCoord, toCoord, downsampledHeights, weights, dirs);
					if (pathCoord == null) break;

					//coords to world
					Vector3[] pathWorld = new Vector3[pathCoord.Length];
					for (int i=0; i<pathCoord.Length; i++)
						pathWorld[i] = downsampledHeights.PixelToWorld(pathCoord[i].x, pathCoord[i].z);

					//slightly moving all nodes to make start and end match the src nodes
					/*Vector3 startDelta = fromWorld - pathWorld[0];
					Vector3 endDelta = toWorld - pathWorld[pathWorld.Length-1];
					for (int i=0; i<pathCoord.Length; i++)
					{
						float percent = 1f * i / (pathWorld.Length-1);
						pathWorld[i] += startDelta*(1-percent) + endDelta*percent;
					}*/

					//flooring nodes
					//DebugGizmos.Clear("Path");
					pathWorld[0].y = downsampledHeights.GetWorldInterpolatedValue(pathWorld[0].x, pathWorld[1].z) * downsampledHeights.worldSize.y;
					for (int i=0; i<pathWorld.Length-1; i++)
					{
						pathWorld[i+1].y = downsampledHeights.GetWorldInterpolatedValue(pathWorld[i+1].x, pathWorld[i+1].z) * downsampledHeights.worldSize.y;
						//DebugGizmos.AddLine("Path", pathWorld[i], pathWorld[i+1]);
					}

					if (newPath == null) newPath = new List<Vector3>();
					newPath.AddRange(pathWorld);
				}

				if (newPath != null)
				{
					Line newLine = new Line();
					newLine.SetNodes(newPath.ToArray());

					dstLines.Add(newLine);
				}
			}
			
			SplineSys dst = new SplineSys();
			dst.lines = dstLines.ToArray();

			return dst;
		}
	}



	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Stroke", iconName="GeneratorIcons/Constant", disengageable = true, 
		colorType = typeof(SplineSys),
		helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Stroke200 : Generator, IInlet<SplineSys>, IOutlet<MatrixWorld>
	{
		[Val("Width")]	  public float width = 10;
		[Val("Hardness")] public float hardness = 0.0f;

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys splineSys = data.products.ReadInlet(this);
			if (splineSys == null || !enabled) return; 

			//stroking
			if (stop!=null && stop.stop) return;
			MatrixWorld strokeMatrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			SplineMatrixOps.Stroke(splineSys, strokeMatrix, white:true, antialiased:true);

			//spreading
			if (stop!=null && stop.stop) return;
			MatrixWorld spreadMatrix = Spread(strokeMatrix, width);

			//hardness
			if (hardness > 0.0001f)
			{
				float h = 1f/(1f-hardness);
				if (h > 9999) h=9999; //infinity if hardness is 1

				spreadMatrix.Multiply(h);
				spreadMatrix.Clamp01();
			}

			if (stop!=null && stop.stop) return;
			data.products[this] = spreadMatrix;
		}



		public static MatrixWorld Spread (MatrixWorld matrix, float range)
		{
			MatrixWorld spreadMatrix;
			float pixelRange = range / matrix.PixelSize.x;

			if (pixelRange < 1) //if less than a pixel making line less noticable
			{
				spreadMatrix = matrix;
				spreadMatrix.Multiply(pixelRange);
			}

			else //spreading the usual way
			{
				spreadMatrix = new MatrixWorld(matrix);
				MatrixOps.Spread(matrix, spreadMatrix, subtract:1f/pixelRange);
				
			}

			return spreadMatrix;
		}


		public static void SpreadOrMultiply (MatrixWorld src, MatrixWorld dst, float range)
		{
			float pixelRange = range / src.PixelSize.x;

			if (pixelRange < 1) //if less than a pixel making line less noticable
				dst.Multiply(pixelRange);
			else
				MatrixOps.Spread(src, dst, subtract:1f/pixelRange);
		}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Spline/Standard", 
		name ="Align", 
		iconName="GeneratorIcons/Constant",
		colorType = typeof(SplineSys), 
		disengageable = true, 
		helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Align200 : Generator, IMultiInlet, IOutlet<MatrixWorld>
	{
		[Val("Spline", "Inlet")]	public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();
		[Val("Height", "Inlet")]	public readonly Inlet<MatrixWorld> heightIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets () { yield return splineIn; yield return heightIn; }

		[Val("Range")] public float range = 30;
		[Val("Flat")] public float flat = 0.25f;
		[Val("Detail")] public float detail = 0f;


		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys splineSys = data.products.ReadInlet(splineIn);
			MatrixWorld heightMatrix = data.products.ReadInlet(heightIn);
			if (splineSys == null) return;
			if (!enabled || heightMatrix == null) { data.products[this]=splineSys; return; }

			if (stop!=null && stop.stop) return;
			MatrixWorld splineMatrix = Stamp(heightMatrix, splineSys, stop);

			if (stop!=null && stop.stop) return;
			data.products[this] = splineMatrix;
		}


		private MatrixWorld Stamp (MatrixWorld srcHeights, SplineSys splineSys, StopToken stop)
		{
			//contours matrix
			if (stop!=null && stop.stop) return null;
			MatrixWorld lineContours = new MatrixWorld(srcHeights.rect, srcHeights.worldPos, srcHeights.worldSize);
			SplineMatrixOps.Stroke(splineSys, lineContours, white:true, antialiased:true);


			//line heights matrix
			if (stop!=null && stop.stop) return null;
			MatrixWorld lineHeightsSrc = new MatrixWorld(srcHeights.rect, srcHeights.worldPos, srcHeights.worldSize);
			SplineMatrixOps.Stroke(splineSys, lineHeightsSrc, padOnePixel:true);
			MatrixWorld lineHeights = new MatrixWorld(lineHeightsSrc); //TODO: use same src/dst matrix in padding
			MatrixOps.PaddingMipped(lineHeightsSrc, lineContours, lineHeights);


			//distances matrix
			if (stop!=null && stop.stop) return null;
			MatrixWorld lineDistances = new MatrixWorld(lineContours);

			float pixelRange = range / srcHeights.PixelSize.x;

			if (pixelRange < 1) //if less than a pixel making line less noticable
				lineDistances.Multiply(pixelRange);
			else
				MatrixOps.Spread(lineContours, lineDistances, subtract:1f/pixelRange);
			

			//saving detail matrix if detail is used (and then operating on lower-detail)
			if (stop!=null && stop.stop) return null;
			MatrixWorld detailMatrix = null;
			if (detail > 0.00001f)
			{
				int downsample = (int)(detail+1);
				float blur = (detail+1) - downsample;

				MatrixWorld originalHeights = srcHeights;
				srcHeights = new MatrixWorld(srcHeights); //further operating on blurred matrix
				MatrixOps.DownsampleBlur(srcHeights, downsample, blur);

				detailMatrix = new MatrixWorld(srcHeights); //taking blurred matrix
				detailMatrix.InvSubtract(originalHeights); //and subtracting src (non-blurred) from it
			}


			//blending line heights with terrain heights
			if (stop!=null && stop.stop) return null;
			for (int i=0; i<srcHeights.arr.Length; i++)  //TODO: replace with matrix mix
			{
				float dist = lineDistances.arr[i];
				if (dist == 0) { lineHeights.arr[i] = srcHeights.arr[i]; continue; }
				if (1-dist < flat) continue;

				float percent = dist / (1-flat);
				percent = 3*percent*percent - 2*percent*percent*percent;

				lineHeights.arr[i] = lineHeights.arr[i]*percent + srcHeights.arr[i]*(1-percent);
			}

			//applying detail
			if (detailMatrix != null)
				lineHeights.Add(detailMatrix);
			
			return lineHeights;
		}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Spline/Standard", 
		name ="Stamp", 
		iconName="GeneratorIcons/Constant", 
		colorType = typeof(SplineSys), 
		disengageable = true, 
		helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Stamp200 : Generator, IMultiInlet, IOutlet<MatrixWorld>
	{
		[Val("Spline", "Inlet")]	public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();
		[Val("Height", "Inlet")]	public readonly Inlet<MatrixWorld> heightIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets () { yield return splineIn; yield return heightIn; }

		public enum Algorithm { Flatten, Detail, Both };
		public Algorithm algorithm;
		public float flatRange = 2;
		public float blendRange = 16;
		public float detailRange = 32;
		public int detail = 1;


		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys splineSys = data.products.ReadInlet(splineIn);
			MatrixWorld heightMatrix = data.products.ReadInlet(heightIn);
			if (splineSys == null ||  heightMatrix == null) return;
			if (!enabled) { data.products[this]=heightMatrix; return; }

			if (stop!=null && stop.stop) return;
			MatrixWorld splineMatrix = Stamp(heightMatrix, splineSys, stop);

			if (stop!=null && stop.stop) return;
			data.products[this] = splineMatrix;
		}


		private MatrixWorld Stamp (MatrixWorld srcHeights, SplineSys splineSys, StopToken stop)
		{
			MatrixWorld dstHeights = new MatrixWorld(srcHeights);

			//transforming ranges to relative (0-1)
			float maxRange = Mathf.Max(detailRange, blendRange);
			float relDetailRange = 1 - detailRange/maxRange;
			float relBlendRange = 1 - blendRange/maxRange;
			float relFlatRange = 1 - flatRange/maxRange;

			//contours matrix
			if (stop!=null && stop.stop) return null;
			MatrixWorld lineContours = new MatrixWorld(srcHeights.rect, srcHeights.worldPos, srcHeights.worldSize);
			SplineMatrixOps.Stroke(splineSys, lineContours, white:true, antialiased:true);


			//line heights matrix
			if (stop!=null && stop.stop) return null;
			MatrixWorld lineHeightsSrc = new MatrixWorld(srcHeights.rect, srcHeights.worldPos, srcHeights.worldSize);
			SplineMatrixOps.Stroke(splineSys, lineHeightsSrc, padOnePixel:true);
			MatrixWorld lineHeights = new MatrixWorld(lineHeightsSrc); //TODO: use same src/dst matrix in padding
			MatrixOps.PaddingMipped(lineHeightsSrc, lineContours, lineHeights);


			//distances matrix
			if (stop!=null && stop.stop) return null;
			MatrixWorld lineDistances = new MatrixWorld(lineContours);

			float pixelRange = maxRange / srcHeights.PixelSize.x;

			if (pixelRange < 1) //if less than a pixel making line less noticeable
				lineDistances.Multiply(pixelRange);
			else
				MatrixOps.Spread(lineContours, lineDistances, subtract:1f/pixelRange);
			

			//applying detail
			if (stop!=null && stop.stop) return null;
			if (algorithm == Algorithm.Detail || algorithm == Algorithm.Both)
			{
				int downsample = (int)(detail+1);
				float blur = (detail+1) - downsample;
				MatrixOps.DownsampleBlur(dstHeights, detail, 1);
				//MatrixOps.GaussianBlur(dstHeights, detail);

				MatrixWorld detailMatrix = new MatrixWorld(dstHeights);  //taking blurred matrix
				detailMatrix.InvSubtract(srcHeights);	//and subtracting non-blurred from it

				dstHeights.Mix(lineHeights, lineDistances, 0, relDetailRange, maskInvert:false, fallof:false, opacity:1);  //stamping on blurred

				dstHeights.Add(detailMatrix); //and returning details back
			}


			//applying fallof
			if (stop!=null && stop.stop) return null;
			if (algorithm == Algorithm.Flatten || algorithm == Algorithm.Both)
			{
				dstHeights.Mix(lineHeights, lineDistances, relBlendRange, relFlatRange, maskInvert:false, fallof:false, opacity:1);
			}

			return dstHeights;
		}
	}



	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Optimize", iconName=null, disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Optimize200 : Generator, IInlet<SplineSys>, IOutlet<SplineSys>
	{
		[Val("Split")] public int split = 3;
		[Val("Deviation")] public float deviation = 1;

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys src = data.products.ReadInlet(this);
			if (src == null) return;
			if (!enabled) { data.products[this]=src; return; }
			
			if (stop!=null && stop.stop) return;
			SplineSys dst = new SplineSys(src);
			dst.Subdivide(split);
			dst.UpdateTangents();
			dst.Optimize(deviation);
			dst.Update();
			
			if (stop!=null && stop.stop) return;
			data.products[this] = dst;
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Relax", iconName=null, disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Relax200 : Generator, IInlet<SplineSys>, IOutlet<SplineSys>
	{
		[Val("Spline", "Inlet")]	public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();

		[Val("Blur")] public float blur = 1;
		[Val("Iterations")] public int iterations = 3;

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys src = data.products.ReadInlet(this);
			if (src == null) return;
			if (!enabled) { data.products[this]=src; return; }
			
			if (stop!=null && stop.stop) return;
			SplineSys dst = new SplineSys(src);
			dst.Relax(blur, iterations);
			dst.Update();
			
			if (stop!=null && stop.stop) return;
			data.products[this] = dst;
		}
	}


	/*[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Merge", iconName=null, disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Merge200 : Generator, IInlet<SplineSys>, IOutlet<SplineSys>
	{
		[Val("Spline", "Inlet")]	public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();

		[Val("Threshold")] public float threshold = 1;

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys src = data.products.ReadInlet(this);
			if (src == null) return;
			if (!enabled) { data.products[this]=src; return; }
			
			if (stop!=null && stop.stop) return;
			SplineSys dst = new SplineSys(src);
			dst.WeldCloseLines(threshold);
			dst.Update();
			
			if (stop!=null && stop.stop) return;
			data.products[this] = dst;
		}
	}*/


	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Weld Close", iconName=null, disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class WeldClose200 : Generator, IInlet<SplineSys>, IOutlet<SplineSys>
	{
		[Val("Spline", "Inlet")]	public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();

		[Val("Threshold")] public float threshold = 25;

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys src = data.products.ReadInlet(this);
			if (src == null) return;
			if (!enabled) { data.products[this]=src; return; }
			
			if (stop!=null && stop.stop) return;
			SplineSys dst = new SplineSys(src);
			dst.WeldCloseLines(threshold);
			dst.Update();
			
			if (stop!=null && stop.stop) return;
			data.products[this] = dst;
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Floor", iconName=null, disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Floor200 : Generator, IMultiInlet, IOutlet<SplineSys>
	{
		[Val("Spline", "Inlet")] public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();
		[Val("Spline", "Height")] public readonly Inlet<MatrixWorld> heightIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets () { yield return splineIn; yield return heightIn; }

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys src = data.products.ReadInlet(splineIn);
			MatrixWorld heights = data.products.ReadInlet(heightIn);
			if (src == null) return;
			if (!enabled || heights==null) { data.products[this]=src; return; }
			
			if (stop!=null && stop.stop) return;
			SplineSys dst = new SplineSys(src);

			FloorSplines(dst, heights);
			dst.Update();
			
			if (stop!=null && stop.stop) return;
			data.products[this] = dst;
		}

		public static void FloorSplines (SplineSys dst, MatrixWorld heights)
		{
			foreach (Line line in dst.lines)
			{
				for (int s=0; s<line.segments.Length; s++)
					FloorPoint(ref line.segments[s].start.pos, heights);
				
				FloorPoint(ref line.segments[line.segments.Length-1].end.pos, heights);
			}
		}

		public static void FloorPoint (ref Vector3 pos, MatrixWorld heights)
		{
			if (pos.x <= heights.worldPos.x  ||  pos.x >= heights.worldPos.x +heights.worldSize.x ||
				pos.z <= heights.worldPos.z  ||  pos.z >= heights.worldPos.z +heights.worldSize.z)
					return;

			float h = heights.GetWorldInterpolatedValue(pos.x, pos.z);
			pos.y = h * heights.worldSize.y;
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Avoid", iconName=null, disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Avoid200 : Generator, IMultiInlet, IOutlet<SplineSys>
	{
		[Val("Spline", "Inlet")] public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();
		[Val("Spline", "Positions")] public readonly Inlet<TransitionsList> objsIn = new Inlet<TransitionsList>();
		public IEnumerable<IInlet<object>> Inlets () { yield return splineIn; yield return objsIn; }

		[Val("Distance")] public float distance = 10;
		[Val("Size Factor")] public int sizeFactor = 0;
		[Val("Iterations")] public int iterations = 10;

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys src = data.products.ReadInlet(splineIn);
			TransitionsList objs = data.products.ReadInlet(objsIn);
			if (src == null) return;
			if (!enabled || objs==null) { data.products[this]=src; return; }
			
			if (stop!=null && stop.stop) return;
			SplineSys dst = new SplineSys(src);

			Vector3[] points = new Vector3[objs.count];
			float[] ranges = new float[objs.count];

			for (int o=0; o<objs.count; o++)
			{
				points[o] = objs.arr[o].pos;
				ranges[o] = distance * (1-sizeFactor + objs.arr[o].scale.x*sizeFactor);
			}

			dst.SplitNearPoints(points, ranges, true, startEndProximityFactor:1f, maxIterations:iterations);
			dst.PushStartEnd(points, ranges, true, 0.5f);
			dst.PushStartEnd(points, ranges, true, 1.01f);

			dst.Update();
			
			if (stop!=null && stop.stop) return;
			data.products[this] = dst;
		}
	}

	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Push", iconName=null, disengageable = true, 
		colorType = typeof(SplineSys),
		helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Push200 : Generator, IMultiInlet, IOutlet<TransitionsList>
	{
		[Val("Spline", "Objects")] public readonly Inlet<TransitionsList> objsIn = new Inlet<TransitionsList>();
		[Val("Spline", "Spline")] public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();
		public IEnumerable<IInlet<object>> Inlets () { yield return objsIn; yield return splineIn; }

		[Val("Distance")] public float distance = 10;
		[Val("Size Factor")] public int sizeFactor = 0;
		[Val("Iterations")] public int iterations = 10;

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys spline = data.products.ReadInlet(splineIn);
			TransitionsList objs = data.products.ReadInlet(objsIn);
			if (objs == null) return;
			if (!enabled || spline==null) { data.products[this]=objs; return; }
			
			if (stop!=null && stop.stop) return;

			Vector3[] points = new Vector3[objs.count];
			float[] ranges = new float[objs.count];

			for (int o=0; o<objs.count; o++)
			{
				points[o] = objs.arr[o].pos;
				ranges[o] = distance * (1-sizeFactor + objs.arr[o].scale.x*sizeFactor);
			}

			for (int i=0; i<iterations; i++)
			{
				float distFactor = Mathf.Sqrt((i+1f)/iterations);
				spline.PushPoints(points, ranges, horizontalOnly:true, distFactor:distFactor);
			}

			TransitionsList dst = new TransitionsList(objs);
			for (int o=0; o<objs.count; o++)
				dst.arr[o].pos = points[o];
			
			if (stop!=null && stop.stop) return;
			data.products[this] = dst;
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Isoline", iconName=null, disengageable = true, 
		colorType = typeof(SplineSys),
		helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Isoline200 : Generator, IInlet<MatrixWorld>, IOutlet<SplineSys>
	{
		//[Val("Distance")] public float distance = 10;
		//[Val("Size Factor")] public int sizeFactor = 0;
		//[Val("Iterations")] public int iterations = 10;

		public override void Generate (TileData data, StopToken stop)
		{
			MatrixWorld matrix = data.products.ReadInlet(this);
			if (matrix == null || !enabled) return; 

			if (stop!=null && stop.stop) return;
			data.products[this] = matrix;
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Spline/Standard", name ="Silhouette", iconName=null, disengageable = true, 
		colorType = typeof(SplineSys),
		helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class Silhouette200 : Generator, IInlet<SplineSys>, IOutlet<MatrixWorld>
	{
		[Val("Width")] public float width = 10;
		//[Val("Size Factor")] public int sizeFactor = 0;
		//[Val("Iterations")] public int iterations = 10;

		public override void Generate (TileData data, StopToken stop)
		{
			SplineSys splineSys = data.products.ReadInlet(this);
			if (splineSys == null || !enabled) return; 

			//stroke and spread
			if (stop!=null && stop.stop) return;
			MatrixWorld strokeMatrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			SplineMatrixOps.Stroke (splineSys, strokeMatrix, white:true, intensity:0.5f, antialiased:true);
			MatrixWorld spreadMatrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			MatrixOps.Spread(strokeMatrix, spreadMatrix, subtract: spreadMatrix.PixelSize.x/width / 2);

			//silhouette
			if (stop!=null && stop.stop) return;
			MatrixWorld silhouetteMatrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			SplineMatrixOps.Stroke (splineSys, silhouetteMatrix, white:true, intensity:0.5f, antialiased:false, padOnePixel:false);
			SplineMatrixOps.Silhouette(splineSys, silhouetteMatrix, silhouetteMatrix);  

			//combining
			if (stop!=null && stop.stop) return;
			SplineMatrixOps.CombineSilhouetteSpread(silhouetteMatrix, spreadMatrix, silhouetteMatrix);

			if (stop!=null && stop.stop) return;
			data.products[this] = silhouetteMatrix;
		}

		[Obsolete] public void Generate_NoAA (TileData data, StopToken stop)
		{
			SplineSys splineSys = data.products.ReadInlet(this);
			if (splineSys == null || !enabled) return; 

			//stroke
			if (stop!=null && stop.stop) return;
			MatrixWorld strokeMatrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);
			SplineMatrixOps.Stroke (splineSys, strokeMatrix, white:true, intensity:0.5f, antialiased:false, padOnePixel:false);

			//spreading
			if (stop!=null && stop.stop) return;
			MatrixWorld spreadMatrix = new MatrixWorld(strokeMatrix);
			MatrixOps.Spread(strokeMatrix, spreadMatrix, subtract: spreadMatrix.PixelSize.x/width / 2);

			//silhouette
			if (stop!=null && stop.stop) return;
			MatrixWorld silhouetteMatrix = strokeMatrix; //just renaming it
			SplineMatrixOps.Silhouette(splineSys, silhouetteMatrix, silhouetteMatrix);

			//combining
			if (stop!=null && stop.stop) return;
			SplineMatrixOps.CombineSilhouetteSpread(silhouetteMatrix, spreadMatrix, silhouetteMatrix);

			if (stop!=null && stop.stop) return;
			data.products[this] = silhouetteMatrix;
		}
	}


	/*[System.Serializable]
	[GeneratorMenu (menu="Spline", name ="Embankment", icon="GeneratorIcons/Constant", disengageable = true, helpLink ="https://gitlab.com/denispahunov/mapmagic/wikis/map_generators/constant")]
	public class EmbankmentGenerator : StandardGenerator, IOutlet<SplineSys>
	{
		[Val("Spline", "Inlet")]		public readonly Inlet<SplineSys> splineIn = new Inlet<SplineSys>();
		[Val("Height", "Inlet")]		public readonly Inlet<MatrixWorld> heightIn = new Inlet<MatrixWorld>();

		[Val("Iterations")]		public int iterations = 8;
		[Val("Max Links")]		public int maxLinks = 4;

		public override IEnumerable<Inlet> Inlets () { yield return input; }

		public override void GenerateProduct (TileData data, StopToken stop)
		{
			PosTab objs = data.products[input);
			if (objs == null) return; 

			data.products[this] = spline);
		}
	}*/
}
