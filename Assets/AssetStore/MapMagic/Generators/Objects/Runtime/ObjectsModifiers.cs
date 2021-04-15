using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;  
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Products;

namespace MapMagic.Nodes.ObjectsGenerators
{
	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Adjust", iconName="GeneratorIcons/Adjust", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Adjust")]
	public class Adjust200 : Generator, IMultiInlet, IOutlet<TransitionsList>
	{
		[Val("Input", "Inlet")]		public readonly Inlet<TransitionsList> input = new Inlet<TransitionsList>();
		[Val("Intensity", "Inlet")]	public readonly Inlet<MatrixWorld> intensityIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets () { yield return input; yield return intensityIn; }

		public bool useRandom = false;
		public int seed = 12345;
		public float sizeFactor = 0;

		public enum Relativeness { absolute, relative };
		public Relativeness relativeness = Relativeness.relative;

		//public enum Randomness { equally, range };
		//public Randomness randomness = Randomness.equally;

		public Vector2 height = Vector2.zero;
		public Vector2 rotation = Vector2.zero;
		public Vector2 scale = Vector2.one;


		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList src = data.products.ReadInlet(input);
			if (src == null) return; 
			if (!enabled) { data.products[this]=src; return; }

			TransitionsList dst = new TransitionsList(src);

			MatrixWorld intensityMatrix = data.products.ReadInlet(intensityIn);
			Noise rnd = useRandom ? new Noise(data.random, seed) : null;

			for (int t=0; t<dst.count; t++)
			{
				if (stop!=null && stop.stop) return;
				Adjust(ref dst.arr[t], intensityMatrix, rnd);
			}

			data.products[this] = dst;
		}


		public void Adjust (ref Transition trn, MatrixWorld intensityMatrix, Noise rnd)
		{
			//generating random vals
			float heightRnd, rotRnd, scaleRnd;
			if (rnd != null)
			{
				heightRnd = rnd.Random(trn.hash, 0);
				heightRnd = height.x + heightRnd*(height.y-height.x);

				rotRnd = rnd.Random(trn.hash, 1);
				rotRnd = rotation.x + rotRnd*(rotation.y-rotation.x);

				scaleRnd = rnd.Random(trn.hash, 2);
				scaleRnd = scale.x + scaleRnd*(scale.y-scale.x);
			}
			else
			{
				heightRnd = height.x;
				rotRnd = rotation.x;
				scaleRnd = scale.x;
			}

			//calculating intensity
			float intensity = 1;
			if (intensityMatrix != null) 
			{
				if (!intensityMatrix.ContainsWorldValue(trn.pos.x, trn.pos.z)) intensity = 0;
				else intensity = intensityMatrix.GetWorldValue(trn.pos.x, trn.pos.z);
			}


			if (relativeness == Relativeness.relative)
			{
				//scale is not affected by sizeFactor
				trn.scale *= scaleRnd * intensity;

				//everything else does
				intensity = intensity*(1-sizeFactor) + intensity*trn.scale.x*sizeFactor;
				trn.pos.y += heightRnd * intensity; // / height; //not multiplying in output
				trn.Yaw = trn.Yaw  +  rotRnd * intensity;
				//cell.poses[p].rotation = Quaternion.Euler(
				//	0,
				//	cell.poses[p].rotation.eulerAngles.y + rnd.CoordinateRandom(cell.poses[p].id+1, rotation) * intensity,
				//	0);
			}
			else 
			{
				trn.scale = new Vector3(1,1,1) * scaleRnd * intensity;
					
				intensity = intensity*(1-sizeFactor) + intensity*trn.scale.x*sizeFactor;
				trn.pos.y = heightRnd * intensity; // / height;
				trn.Yaw = rotRnd * intensity;
				//cell.poses[p].rotation = Quaternion.Euler(
				//	0,
				//	rnd.CoordinateRandom(cell.poses[p].id+1, rotation) * intensity,
				//	0);
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Mask", iconName="GeneratorIcons/ObjectsMask", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Clean_Up")]
	public class Mask200 : Generator, IMultiInlet, IOutlet<TransitionsList>
	{
		[Val("Input", "Inlet")]	public readonly Inlet<TransitionsList> srcIn = new Inlet<TransitionsList>();
		[Val("Mask", "Inlet")]	public readonly Inlet<MatrixWorld> maskIn = new Inlet<MatrixWorld>(); 
		public IEnumerable<IInlet<object>> Inlets () { yield return srcIn; yield return maskIn; }

		[Val("Seed")]	public int seed = 12345;
		[Val("Invert")]	public bool invert = false;


		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList src = data.products.ReadInlet(srcIn);
			if (src == null) return; 
			if (!enabled) { data.products[this]=src; return; }

			MatrixWorld mask = data.products.ReadInlet(maskIn);
			if (mask == null) { data.products[this]=src; return; }

			TransitionsList dst = new TransitionsList();
			Noise random = new Noise(data.random, seed);

			Mask(src, dst, mask, random, invert, stop);

			data.products[this] = dst;
		}


		public static void Mask (TransitionsList src, TransitionsList dst, MatrixWorld mask, Noise random, bool invert, StopToken stop=null)
		{
			for (int t=0; t<src.count; t++)
			{
				if (stop!=null && stop.stop) return;

				Vector3 pos = src.arr[t].pos;

				if (pos.x <= mask.worldPos.x  ||   pos.x >= mask.worldPos.x+mask.worldSize.x  ||
					pos.z <= mask.worldPos.z  ||   pos.z >= mask.worldPos.z+mask.worldSize.z)
						continue; //do remove out of range objects?

				float val = mask.GetWorldValue(pos.x, pos.z);
				float rnd = random.Random(src.arr[t].hash);

				if (val<rnd && invert) dst.Add(src.arr[t]);
				if (val>=rnd && !invert) dst.Add(src.arr[t]);
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Floor", iconName=null, disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Clean_Up")]
	public class Floor200 : Generator, IMultiInlet, IOutlet<TransitionsList>
	{
		[Val("Input", "Inlet")]		public readonly Inlet<TransitionsList> srcIn = new Inlet<TransitionsList>();
		[Val("Height", "Inlet")]	public readonly Inlet<MatrixWorld> heightIn = new Inlet<MatrixWorld>();

		public enum Relativity{ absolute, relative };
		[Val("Relativity")] public Relativity relativity = Relativity.absolute;

		public IEnumerable<IInlet<object>> Inlets () { yield return srcIn; yield return heightIn; }


		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList src = data.products.ReadInlet(srcIn);
			if (src == null) return; 
			if (!enabled) { data.products[this]=src; return; }

			MatrixWorld heights = data.products.ReadInlet(heightIn);
			if (heights == null) { data.products[this] = src; return; }

			TransitionsList dst = new TransitionsList(src);
			for (int t=0; t<dst.count; t++)
			{
				if (stop!=null && stop.stop) return;
				Floor(ref dst.arr[t], heights);
			}

			data.products[this] = dst;
		}


		public void Floor (ref Transition trn, MatrixWorld heights)
		{
			if (trn.pos.x <= heights.worldPos.x  ||  trn.pos.x >= heights.worldPos.x +heights.worldSize.x ||
				trn.pos.z <= heights.worldPos.z  ||  trn.pos.z >= heights.worldPos.z +heights.worldSize.z)
					return;

			float terrainHeight = heights.GetWorldInterpolatedValue(trn.pos.x, trn.pos.z);
			if (terrainHeight > 1) terrainHeight = 1;
			terrainHeight *= heights.worldSize.y;

			if (relativity == Relativity.relative)
				trn.pos.y += terrainHeight;
			else
				trn.pos.y = terrainHeight;
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Split", iconName="GeneratorIcons/Split", colorType = typeof(TransitionsList), disengageable = true, 
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Split")]
	public class Split200 : Generator, IInlet<TransitionsList>, ILayered<Split200.SplitLayer>
	{
		public class SplitLayer : IOutlet<TransitionsList>
		{
			public string name = "Object Layer";

			public bool heightConditionActive = false;
			public Vector2 heightCondition = new Vector2(0,1);

			public bool rotationConditionActive = false;
			public Vector2 rotationCondition = new Vector2(0,360);

			public bool scaleConditionActive = false;
			public Vector2 scaleCondition = new Vector2(0,100);

			public float chance = 1;

			public Generator Gen { get; private set; }
			public void SetGen (Generator gen) => Gen=gen;
		}

		public SplitLayer[] layers = new SplitLayer[0];
		public SplitLayer[] Layers => layers; 
		public void SetLayers(object[] ls) => layers = Array.ConvertAll(ls, i=>(SplitLayer)i);
		public bool LayersInversed => true;
		public int guiExpanded = -1;

		public enum MatchType { layered, random };
		[Val("Match")]	public MatchType matchType = MatchType.random;
		[Val("Seed")]	public int seed = 12345;

		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			TransitionsList src = data.products.ReadInlet(this);
			if (src == null) return; 
			if (!enabled) return;

			Noise random = new Noise(data.random, seed);
			bool[] match = new bool[layers.Length];
			
			//creating dst
			TransitionsList[] dst = new TransitionsList[layers.Length];
			for (int i=0; i<dst.Length; i++)
				dst[i] = new TransitionsList();

			//splitting
			for (int t=0; t<src.count; t++)
			{
				if (stop!=null && stop.stop) return;

				int layerNum = PickObjLayer(src.arr[t], random, match);
				if (layerNum >= 0)
					dst[layerNum].Add(src.arr[t]);
			}
			
			//setting results
			for (int i=0; i<dst.Length; i++)
			{
				if (stop!=null && stop.stop) return;
				data.products[layers[i]] = dst[i];
			}
		}

		private int PickObjLayer (Transition trs, Noise random, bool[] match=null)
		{
			if (match==null) match = new bool[layers.Length];

			//finding suitable objects (and sum of chances btw. And last object for non-random)
			int matchesNum = 0; //how many layers have a suitable obj
			float chanceSum = 0;
			int lastLayerNum = 0;

			for (int i=0; i<layers.Length; i++)
			{
				SplitLayer layer = (SplitLayer)layers[i];
				float yaw = (trs.Yaw+360) % 360;

				bool heightMatch = !layer.heightConditionActive || (trs.pos.y >= layer.heightCondition.x && trs.pos.y <= layer.heightCondition.y);
				bool rotationMatch = !layer.rotationConditionActive || (yaw >= layer.rotationCondition.x && yaw <= layer.rotationCondition.y);
				bool scaleMatch = !layer.scaleConditionActive || (trs.scale.x >= layer.scaleCondition.x && trs.scale.x <= layer.scaleCondition.y);

				if (heightMatch && rotationMatch && scaleMatch)
				{
					match[i] = true;

					matchesNum ++;
					chanceSum += layer.chance;
					lastLayerNum = i;
				}
				else match[i] = false;
			}

			//if no matches detected - continue withous assigning obj
			if (matchesNum == 0) return -1;

			//if one match - assigning last obj
			else if (matchesNum == 1 || matchType == MatchType.layered) return lastLayerNum;

			//selecting layer at random
			else if (matchesNum > 1 && matchType == MatchType.random)
			{
				float randomVal = random.Random(trs.hash);
				randomVal *= chanceSum;
				chanceSum = 0;

				for (int i=0; i<layers.Length; i++)
				{
					if (!match[i]) continue;
						
					SplitLayer layer = (SplitLayer)layers[i];
					if (randomVal > chanceSum  &&  randomVal < chanceSum + layer.chance) return i;
					chanceSum += layer.chance;
				}
			}

			return -1;
		}
	}


	//TODO: unify with CleanUp
/*	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Subtract", icon="GeneratorIcons/Subtract", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Subtract")]
	public class SubtractGenerator200 : StandardGenerator, IOutlet<TransitionsList>
	{
		[Val("Minuend (Changed)", "Inlet")]		public readonly Inlet<TransitionsList> minuendIn = new Inlet<TransitionsList>();
		[Val("Subtrahend", "Inlet")]	public readonly Inlet<TransitionsList> subtrahendIn = new Inlet<TransitionsList>();

		[Val("Distance")]		public float distance = 1;
		[Val("Size Factor")]	public float sizeFactor = 0;


		public override IEnumerable<Inlet> Inlets () { yield return minuendIn; yield return subtrahendIn; }


		public override void GenerateSelf (TileData data, StopToken stop)
		{
			TransitionsList minuend = data.products[minuendIn);
			TransitionsList subtrahend = data.products[subtrahendIn);
			if (minuend == null) return;
			if (!enabled || subtrahend == null) { data.products[this] = ,minuend); return; }

			TransitionsList dst = new TransitionsList(minuend);

			RemoveObjsInRange(subtrahend, minuend, dst);

			data.products[this] = dst);
		}


		public void RemoveObjsInRange (TransitionsList subtrahend, TransitionsList minuend, TransitionsList result)
		{
			PosTab posTab = new PosTab(

			//TODO: transforming distance to map-space like in rarefy
			for (int c=0; c<subtrahend.cells.arr.Length; c++)
			{
				PosTab.Cell cell = subtrahend.cells.arr[c];
				if (cell.poses == null) continue;
				for (int p=cell.poses.Length-1; p>=0; p--)
				{
					Transition obj = cell.poses[p];
					float range = distance*(1-sizeFactor) + distance*cell.poses[p].scale.x*sizeFactor;
					result.RemoveObjsInRange(obj.pos.x, obj.pos.z, range);
				}
			}
			result.Flush();
		}
	}*/


	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Rarefy", iconName="GeneratorIcons/Rarefy", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Rerefy")]
	public class Rarefy200 : Generator, IInlet<TransitionsList>, IMultiInlet, IOutlet<TransitionsList>
	{
		[Serializable]
		public struct Layer 
		{
			public Inlet<TransitionsList> inlet;
			public float distance;
			public float sizeFactor;

			public Layer (bool tmp)
			{
				inlet = new Inlet<TransitionsList>();
				distance = 1;
				sizeFactor = 0;
			}
		}

		public Layer[] layers = new Layer[0];
		public IEnumerable<IInlet<object>> Inlets () 
		{ 
			for (int i=0; i<layers.Length; i++)
				yield return layers[i].inlet;
		}

		public bool self = true;
		public float distance = 1;
		public float sizeFactor = 0;


		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList src = data.products.ReadInlet(this);
			if (src == null) return; 
			if (!enabled) { data.products[this]=src; return; }

			PosTab srcTab = new PosTab((Vector3)data.area.full.worldPos, (Vector3)data.area.full.worldSize, 16);
			srcTab.Add(src);

			(PosTab posTab, float distance, float sizeFactor)[] subtrahends = new (PosTab,float,float)[layers.Length];
			for (int i=0; i<subtrahends.Length; i++)
			{
				TransitionsList trns = data.products.ReadInlet(layers[i].inlet);
				if (trns == null) continue;

				subtrahends[i].posTab = new PosTab((Vector3)data.area.full.worldPos, (Vector3)data.area.full.worldSize, 16);
				subtrahends[i].posTab.Add(trns);

				subtrahends[i].distance = layers[i].distance;
				subtrahends[i].sizeFactor = layers[i].sizeFactor;
			}
			
			if (stop!=null && stop.stop) return;
			PosTab dst = Rarefied(srcTab, subtrahends, stop);
			data.products[this] = dst.ToTransitionsList();
		}


		private PosTab Rarefied (PosTab src, (PosTab posTab, float distance, float sizeFactor)[] subtrahends, StopToken stop=null)
		{
			PosTab dst = new PosTab(src.pos, src.size, src.resolution);

			foreach (Transition obj in src.All())
			{
				if (stop!=null && stop.stop) return dst;

				float thisRange =  distance*(1-sizeFactor) + distance*obj.scale.x*sizeFactor; //thisDistance*(1 - (obj.scale.x-1)*thisSizeFactor);

				if (self && dst.IsAnyObjInRange(obj.pos.x, obj.pos.z, thisRange+thisRange)) continue;

				bool remove = false;
				for (int s=0; s<subtrahends.Length; s++)
					if (subtrahends[s].posTab!=null && subtrahends[s].posTab.IsAnyObjInRange(obj.pos.x, obj.pos.z, thisRange+subtrahends[s].distance))
						{ remove = true; break; }
				if (remove) continue;

				dst.Add(obj);
			}
			dst.Flush();

			return dst;
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Combine", iconName="GeneratorIcons/Combine", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Combine")]
	public class Combine200 : Generator, IMultiInlet, IOutlet<TransitionsList>
	{
		public Inlet<TransitionsList>[] inlets = new Inlet<TransitionsList>[2] { new Inlet<TransitionsList>(), new Inlet<TransitionsList>() };

		public IEnumerable<IInlet<object>> Inlets () 
			{ for (int i=0; i<inlets.Length; i++) yield return inlets[i]; }

		public override void Generate (TileData data, StopToken stop)
		{	
			if (!enabled) return;

			TransitionsList dst = new TransitionsList();
			bool defined = false;

			for (int i=0; i<inlets.Length; i++)
			{
				TransitionsList src = data.products.ReadInlet(inlets[i]);
				if (src != null)
				{
					dst.Add(src);
					defined = true;
				}
			}

			if (!defined) data.products[this] = null;
			else data.products[this] = dst;
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Spread", iconName="GeneratorIcons/Spread", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Propagate")]
	public class Spread200 : Generator, IInlet<TransitionsList>, IOutlet<TransitionsList>
	{
		public bool retainOriginals = true;
		public int seed = 12345;
		public UnityEngine.Vector2 growth = new UnityEngine.Vector2(2,3);
		public UnityEngine.Vector2 distance = new UnityEngine.Vector2(1,2);
		public float sizeFactor = 0;


		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList src = data.products.ReadInlet(this);
			if (src == null) return; 
			if (!enabled) { data.products[this]=src; return; }

			TransitionsList dst = new TransitionsList(); //capacity src.count
			Noise random = new Noise(data.random, seed);

			for (int t=0; t<src.count; t++)
				Spread(src.arr[t], dst, random);

			if (retainOriginals) dst.Add(src);

			data.products[this] = dst;
		}


		private void Spread (Transition trn, TransitionsList spreadedList, Noise random)
		{
			//calculating number of propagate objects
			float rnd = random.Random(trn.hash); 
			float num = growth.x + rnd*(growth.y-growth.x);
			num = num*(1-sizeFactor) + num*trn.scale.x*sizeFactor;
			num = Mathf.Round(num);

			//creating objs
			for (int n=0; n<num; n++)
			{
				float angRnd = random.Random(trn.hash, n*2);
				float distRnd = rnd = random.Random(trn.hash, n*2+1);
				float angle = angRnd * Mathf.PI*2; //in radians
				UnityEngine.Vector2 direction = new UnityEngine.Vector2(Mathf.Sin(angle), Mathf.Cos(angle) );
				float dist = distance.x + distRnd*(distance.y-distance.x);
				dist = dist*(1-sizeFactor) + dist*trn.scale.x*sizeFactor;

				float posX = trn.pos.x + direction.x*dist;
				//if (posX <= dst.rect.offset.x+1.01f) posX = dst.rect.offset.x+1.01f;
				//if (posX >= dst.rect.offset.x+dst.rect.size.x-1.01f) posX = dst.rect.offset.x+dst.rect.size.x-1.01f;

				float posZ = trn.pos.z + direction.y*dist;
				//if (posZ <= dst.rect.offset.z+1.01f) posZ = dst.rect.offset.z+1.01f;
				//if (posZ >= dst.rect.offset.z+dst.rect.size.z-1.01f) posZ = dst.rect.offset.z+dst.rect.size.z-1.01f;

				Transition newPos = new Transition() {
					pos = new Vector3(posX, trn.pos.y, posZ),
					rotation = trn.rotation,
					scale = trn.scale,
					hash = (trn.hash<<1) + n};
				spreadedList.Add(newPos); //with auto id
			}
		}
	}

	



	//TODO: could be unified with stamp
	/*[System.Serializable]
	[GeneratorMenu (menu="Objects", name ="Blob", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Blob")]
	public class BlobGenerator : Generator, IOutlet<MatrixWorld>
	{
		[Val(name="Objects", priority=2)]	public readonly Inlet<PosTab> objectsIn = new Inlet<PosTab>();
		[Val(name="Canvas", priority=2)]	public readonly Inlet<MatrixWorld> canvasIn = new Inlet<MatrixWorld>();  //TODO: canvas could be applied with blend
		[Val(name="Mask", priority=2)]		public readonly Inlet<MatrixWorld> maskIn = new Inlet<MatrixWorld>();

		[Val(name="Intensity")]		public float intensity = 1f;
		[Val(name="Radius")]		public float radius = 10;
		[Val(name="Size Factor")]	public float sizeFactor = 0;
		[Val(name="Fallof")]		public AnimationCurve fallof = new AnimationCurve( new Keyframe[] { new Keyframe(0,0,1,1), new Keyframe(1,1,1,1) } );
		[Val(name="Noise Amount")]	public float noiseAmount = 0.1f;
		[Val(name="Noise Size")]	public float noiseSize = 100;
		[Val(name="Safe Borders")]	public int safeBorders = 0;

		public override void Generate (TileData data, StopToken stop)
		{
			//getting inputs
			PosTab objects = results.GetProduct<PosTab>(objectsIn);
			MatrixWorld src = results.GetProduct<MatrixWorld>(canvasIn);
			
			if (objects==null) { results.SetProduct(this, null); return; }  //should set anything to mark as generated

			//preparing output
			MatrixWorld dst; 
			if (src != null) dst = (MatrixWorld)src.Clone(); 
			else dst = new MatrixWorld(area.full.resolution, area.full.position, area.full.size);

			foreach (Transition obj in objects.AllObjs())
				DrawBlob(dst, obj.pos.x, obj.pos.z, intensity, radius, fallof, noiseAmount, noiseSize);

			MatrixWorld mask = results.GetProduct<MatrixWorld>(maskIn);
			if (mask != null) MatrixWorld.Mask(src, dst, mask);
			if (safeBorders != 0) MatrixWorld.SafeBorders(src, dst, safeBorders);

			//setting output
			if (stop!=null && stop(0)) return;
			results.SetProduct(this, dst);
		}

		public override void Clear (TileData data, StopToken stop) 
		{
			data.products.Remove(this);
			data.ready.CheckRemove(this);
		}

		public override bool IsReady (TileData data, StopToken stop)
		{
			return data.products.Exists(this);
		}

		public static void DrawBlob (MatrixWorld canvas, float posX, float posZ, float val, float radius, AnimationCurve fallof, float noiseAmount=0, float noiseSize=20)
		{
			Coord mapCoord = new Coord(canvas.WorldToMap(posX), canvas.WorldToMap(posZ));
			int mapRadius = canvas.WorldToMap(radius);
			CoordRect blobRect = new CoordRect(mapCoord-mapRadius, new Coord(mapRadius*2 + 1, mapRadius*2 + 1));

			Curve curve = new Curve(fallof);
			InstanceRandom noise = new InstanceRandom(noiseSize, 512, 12345, 123); //TODO: use normal noise instead

			CoordRect intersection = CoordRect.Intersected(canvas.rect, blobRect);
			Coord center = blobRect.Center;
			Coord min = intersection.Min; Coord max = intersection.Max; 
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				//float dist = Coord.Distance(center, new Coord(x,z));
				float distX = canvas.MapToWorld(x) - posX;
				float distZ = canvas.MapToWorld(z) - posZ; 
				float dist = Mathf.Sqrt(distX*distX + distZ*distZ);
					
				float percent = curve.Evaluate(1f - dist/radius);
				float result = percent;

				if (noiseAmount > 0.001f)
				{
					float maxNoise = percent; if (percent > 0.5f) maxNoise = 1-percent;
					result += (noise.Fractal(x,z)*2 - 1) * maxNoise * noiseAmount;
				}

				//canvas[x,z] = Mathf.Max(result*val, canvas[x,z]);
				canvas[x,z] = val*result + canvas[x,z]*(1-result);
			}
		}
	}*/


	[System.Serializable]
	[GeneratorMenu (
		menu="Objects/Modifiers", 
		name ="Flatten", 
		iconName="GeneratorIcons/Flatten", 
		disengageable = true, 
		colorType = typeof(TransitionsList),
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Flatten")]
	public class Flatten200 : Generator, IMultiInlet, IOutlet<MatrixWorld>
	{
		[Val("Positions", "Inlet")]	public readonly Inlet<TransitionsList> positionsIn = new Inlet<TransitionsList>();
		[Val("Heights", "Inlet")]	public readonly Inlet<MatrixWorld> heightsIn = new Inlet<MatrixWorld>();

		[Val("Radius")]			public float radius = 1;
		[Val("Hardness")]		public float hardness = 0.5f;
		[Val("Size Factor")]	public float sizeFactor = 0;

		public bool noiseFallof = false;
		public float noiseAmount = 1f;
		public float noiseSize = 10;


		public IEnumerable<IInlet<object>> Inlets () { yield return positionsIn; yield return heightsIn; }

		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList positions = data.products.ReadInlet(positionsIn);
			MatrixWorld heights = data.products.ReadInlet(heightsIn);
			if (heights == null) return;
			if (positions == null) { data.products[this]=heights; return; }
			if (!enabled) return;
			
			heights = new MatrixWorld(heights);

			Noise random = null;
			if (noiseFallof) random = new Noise(data.random, 12345);

			for (int t=0; t<positions.count; t++)
			{
				if (stop!=null && stop.stop) return;
				Transition obj = positions.arr[t];

				if (obj.pos.x < heights.worldPos.x || obj.pos.x > heights.worldPos.x+heights.worldSize.x ||
					obj.pos.z < heights.worldPos.z || obj.pos.z > heights.worldPos.z+heights.worldSize.z)
						continue;

				StampLevel(heights, 
					level: heights.GetWorldInterpolatedValue(obj.pos.x, obj.pos.z),
					center: (Vector2D)obj.pos,
					radius: radius*(1-sizeFactor) + radius*obj.scale.y*sizeFactor,
					hardness: hardness,
					noise:random, noiseAmount:noiseAmount, noiseSize:noiseSize);
			}

			data.products[this] = heights;
		}


		public static void StampLevel (MatrixWorld matrix, float level, Vector2D center, float radius, float hardness, 
			Noise noise=null, float noiseAmount=0, float noiseSize=20)
		{
			Vector2D mapCenter = (Vector2D)matrix.WorldToPixelInterpolated(center.x, center.z);
			float mapRadius = matrix.WorldDistToPixelInterpolated(radius);
			CoordRect stampRect = new CoordRect(mapCenter, mapRadius);

			CoordRect intersection = CoordRect.Intersected(matrix.rect, stampRect);
			Coord min = intersection.Min; Coord max = intersection.Max; 

			Coord coord = new Coord(); //temporary coord to call GetFallof

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				coord.x = x;
				coord.z = z;

				float falloff = coord.GetInterpolatedFalloff(mapCenter, mapRadius, hardness, smooth:2);
				if (falloff < 0.00001f) continue;

				if (noise != null)
				{
					float maxNoise = falloff; if (falloff > 0.5f) maxNoise = 1-falloff;
					falloff += (noise.Fractal(x,z,noiseSize)*2 - 1) * maxNoise * noiseAmount;
				}

				int pos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;  //coord.GetPos
				matrix.arr[pos] = level*falloff + matrix.arr[pos]*(1-falloff);
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Objects/Modifiers", 
		name ="Stroke", 
		iconName="GeneratorIcons/Flatten", 
		disengageable = true, 
		colorType = typeof(TransitionsList),
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Stroke")]
	public class Stroke200 : Generator, IInlet<TransitionsList>, IOutlet<MatrixWorld>
	{
		[Val("Radius")]			public float radius = 1;
		[Val("Hardness")]		public float hardness = 0.5f;
		[Val("Size Factor")]	public float sizeFactor = 0;

		public bool noiseFallof = false;
		public float noiseAmount = 10f;
		public float noiseSize = 5f;


		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList positions = data.products.ReadInlet(this);
			if (positions == null  ||  !enabled) return; 

			MatrixWorld matrix = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);

			Noise random = null;
			if (noiseFallof) random = new Noise(data.random, 12345);

			for (int t=0; t<positions.count; t++)
			{
				if (stop!=null && stop.stop) return;
				Transition obj = positions.arr[t];

				StampMax(matrix, 
					center: (Vector2D)obj.pos,
					radius: radius*(1-sizeFactor) + radius*obj.scale.y*sizeFactor,
					hardness: hardness,
					noise:random, noiseAmount:noiseAmount, noiseSize:noiseSize);
			}

			data.products[this] = matrix;
		}


		private static void StampMax (MatrixWorld matrix, Vector2D center, float radius, float hardness, 
			Noise noise=null, float noiseAmount=0, float noiseSize=20)
			/// Nearly copy of StampLevel with the other apply algorithm
		{
			Vector2D mapCenter = (Vector2D)matrix.WorldToPixelInterpolated(center.x, center.z);
			float mapRadius = matrix.WorldDistToPixelInterpolated(radius);
			CoordRect stampRect = new CoordRect(mapCenter, mapRadius);

			CoordRect intersection = CoordRect.Intersected(matrix.rect, stampRect);
			Coord min = intersection.Min; Coord max = intersection.Max; 

			Coord coord = new Coord(); //temporary coord to call GetFallof

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				coord.x = x;
				coord.z = z;

				float falloff = coord.GetInterpolatedFalloff(mapCenter, mapRadius, hardness, smooth:1);
				if (falloff < 0.00001f) continue;

				if (noise != null)
				{
					float maxNoise = falloff; if (falloff > 0.5f) maxNoise = 1-falloff;
					falloff += (noise.Fractal(x,z,noiseSize)*2 - 1) * maxNoise * noiseAmount;
				}

				int pos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;  //coord.GetPos
				if (falloff>matrix.arr[pos]) matrix.arr[pos] = falloff;
			}
		}
	}


	[System.Serializable]
	[GeneratorMenu (
		menu="Objects/Modifiers", 
		name ="Stamp", 
		iconName="GeneratorIcons/Stamp", 
		disengageable = true, 
		colorType = typeof(TransitionsList),
		helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Stamp")]
	public class Stamp200 : Generator, IMultiInlet, IOutlet<MatrixWorld>
	{
		[Val("Positions", "Inlet")]		public readonly Inlet<TransitionsList> positionsIn = new Inlet<TransitionsList>();
		[Val("Stamp", "Inlet")]			public readonly Inlet<MatrixWorld> stampIn = new Inlet<MatrixWorld>();
		
		[Val("Size", "Custom")]	public float size = 1;
		[Val("Intensity", "Custom")]	public float intensity = 1;
		[Val("Hardness", "UseFallof")]	public float hardness = 0.8f;
		[Val("Size Factor", "Custom")]	public float sizeFactor = 1;
		[Val("Intensity Factor", "Custom")]	public float intensityFactor = 1;

		public enum BlendType { Max, Add }
		public BlendType blendType = BlendType.Max;

		[Val("Use Rotation", "Custom", isLeft = true)]	public bool useRotation = true;
		[Val("Use Falloff", "Custom", isLeft = true)]	public bool useFalloff = false;


		public IEnumerable<IInlet<object>> Inlets () { yield return positionsIn; yield return stampIn; }

		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList positions = data.products.ReadInlet(positionsIn);
			MatrixWorld stamp = data.products.ReadInlet(stampIn);
			if (positions == null || stamp == null) return; 
			if (!enabled) return;
			
			MatrixWorld dst = new MatrixWorld(data.area.full.rect, data.area.full.worldPos, data.area.full.worldSize, data.globals.height);

			for (int t=0; t<positions.count; t++)
			{
				if (stop!=null && stop.stop) return;

				float radius = size/2;
				float currRadius = radius*(1-sizeFactor) + radius*positions.arr[t].scale.y*sizeFactor;
				if (useRotation) currRadius *= 1.414213562373095f; //it will be then reduced in stamp

				StampMatrix(dst, stamp, 
					stampRect: data.area.active.rect, 
					center:positions.arr[t].pos, 
					rotation: positions.arr[t].rotation,
					radius: currRadius,
					hardness: hardness,
					intensity: intensity*(1-intensityFactor) + intensity*positions.arr[t].scale.y*intensityFactor,
					blendAdditive: blendType==BlendType.Add,
					useRotation:useRotation, useFalloff:useFalloff);
			}

			data.products[this] = dst;
		}


		private static void StampMatrix (MatrixWorld matrix, MatrixWorld stamp, CoordRect stampRect,
			Vector3 center, Quaternion rotation, float radius, float hardness, float intensity, bool blendAdditive,
			bool useRotation, bool useFalloff)
		{
			Vector2D mapCenter = (Vector2D)matrix.WorldToPixelInterpolated(center.x, center.z);
			float mapRadius = matrix.WorldDistToPixelInterpolated(radius);
			CoordRect strokeRect = new CoordRect(mapCenter, mapRadius);

			CoordRect intersection = CoordRect.Intersected(matrix.rect, strokeRect);
			Coord min = intersection.Min; Coord max = intersection.Max; 

			Coord coord = new Coord(); //temporary coord to call GetFallof

			Vector2D rotDirection = new Vector2D(0,1);
			if (useRotation)
				rotDirection = (Vector2D)(rotation * new Vector3(1,0,0));

			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				//stamp relative coordinates
				float stampPercentX = 1f * (x - strokeRect.offset.x) / (strokeRect.size.x-1); //(size-1) 
				float stampPercentZ = 1f * (z - strokeRect.offset.z) / (strokeRect.size.z-1);
				
				if (stampPercentX > 0.999f) stampPercentX = 0.999f; //to avoid reading last pixel which could be Simple Form's new tile
				if (stampPercentZ > 0.999f) stampPercentZ = 0.999f;

				//falloff
				float falloff = 1;
				if (useFalloff)
				{
					coord.x = x; coord.z = z;

					falloff = coord.GetInterpolatedFalloff(mapCenter, mapRadius / (useRotation ? 1.414213562373095f : 1), hardness, smooth:1);
					if (falloff < 0.00001f) continue;
				}

				//value
				float value = 0;
				if (useRotation)
				{
					stampPercentX = (stampPercentX-0.5f)*1.414213562373095f + 0.5f;  //reducing stamp square to make it circumscribed
					stampPercentZ = (stampPercentZ-0.5f)*1.414213562373095f + 0.5f;  

					(stampPercentX, stampPercentZ) = GetRelativeRotatedCoords(stampPercentX, stampPercentZ, rotDirection, new Vector2D(0.5f, 0.5f));
				}

				float fx = stampPercentX*stampRect.size.x + stampRect.offset.x;
				float fz = stampPercentZ*stampRect.size.z + stampRect.offset.z;
				value = stamp.GetFloored(fx,fz);

				//applying
				value *= intensity;
				int pos = (z-matrix.rect.offset.z)*matrix.rect.size.x + x - matrix.rect.offset.x;  //coord.GetPos
				if (blendAdditive)
					matrix.arr[pos] += value*falloff;
				else //max
					if (value*falloff > matrix.arr[pos]) 
						matrix.arr[pos] = value*falloff;
			}
		}

		private static (float,float) GetRelativeRotatedCoords (float sx, float sz, Vector2D rotationDirection, Vector2D rotationPivot)
		/// Rotating relative (0-1) coordinates across pivot (0-1 too)
		{
			float cx = sx - rotationPivot.x;  float cz = sz - rotationPivot.z;

			Vector2D vx = rotationDirection * cx;
			Vector2D vz = new Vector2D(rotationDirection.z, -rotationDirection.x) * cz;
			Vector2D v = vx+vz;

			v.x += rotationPivot.x;  v.z += rotationPivot.z;

			if (v.x < 0) v.x = 0; if (v.x > 0.999f) v.x = 0.999f; //to avoid reading last pixel which could be Simple Form's new tile
			if (v.z < 0) v.z = 0; if (v.z > 0.999f) v.z = 0.999f;

			return (v.x, v.z);
		}
	}


	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Forest", iconName="GeneratorIcons/Forest", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Forest")]
	public class Forest200 : Generator, IMultiInlet, IOutlet<TransitionsList>
	{
		[Val("Seedlings", "Inlet")]		public readonly Inlet<TransitionsList> seedlingsIn = new Inlet<TransitionsList>();
		[Val("Other Trees", "Inlet")]	public readonly Inlet<TransitionsList> otherTreesIn = new Inlet<TransitionsList>();
		[Val("Soil", "Inlet")]			public readonly Inlet<MatrixWorld> soilIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets () { yield return seedlingsIn; yield return otherTreesIn; yield return soilIn; }

		[Val("Years")]			public int years = 100;
		[Val("Density")]		public float density = 10000; //max trees per 1*1km
		[Val("Fecundity")]		public float fecundity = 0.5f;
		[Val("Seed Dist")]		public float seedDist = 15;
		[Val("Reproductive Age")]public float reproductiveAge = 10;
		[Val("Survival Rate")]	public float survivalRate = 0.95f;
		[Val("Life Age")]		public float lifeAge = 100;
		[Val("Size Is Age")]	public bool sizeIsLife = true;
		[Val("Seed")]			public int seed = 12345;


		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList seedlings = data.products.ReadInlet(seedlingsIn);
			if (seedlings == null) return; 
			if (!enabled) { data.products[this]=seedlings; return; }

			TransitionsList otherTrees = data.products.ReadInlet(seedlingsIn);
			MatrixWorld soil = data.products.ReadInlet(soilIn);

			Noise random = new Noise(data.random, seed);

			TransitionsList forest = Forest(seedlings, otherTrees, soil, (Vector3)data.area.full.worldPos, (Vector3)data.area.full.worldSize, random, stop);

			data.products[this] = forest;
		}


		public TransitionsList Forest (TransitionsList seedlings, TransitionsList otherTrees, 
			MatrixWorld soil, Vector3 worldPos, Vector3 worldSize, Noise random, StopToken stop=null)
		{
			float cellSize = 1000 / Mathf.Sqrt(density);
			
			CoordRect rect = CoordRect.WorldToGridRect(ref worldPos, ref worldSize, cellSize);

			PositionMatrix forest = new PositionMatrix(rect, worldPos, worldSize);
			forest.Scatter(0, random, maxHeight:0);
			forest = forest.Relaxed();

			PositionMatrix otherForest = new PositionMatrix(rect, worldPos, worldSize);
			if (otherTrees != null)
				otherForest.AddTransitionsList(otherTrees, 1); //using custom height as tree age

			//growing
			for (int y=0; y<years; y++)
			{
				//filling seedlings - each iteration (except the last one) to make them persistent
				forest.AddTransitionsList(seedlings, reproductiveAge+1); //with custom height

				//generating
				Coord min = forest.rect.Min; Coord max = forest.rect.Max; 
				for (int x=min.x; x<max.x; x++)
				{
					if (stop!=null && stop.stop) return null; //checking stop every x line

					for (int z=min.z; z<max.z; z++)
					{
						float tree = forest.GetHeight(x,z);

						if (tree < 0.5f) continue;

						//growing tree
						forest.SetHeight(x,z, ++tree);

						//killing the tree
						float curSurvivalRate = survivalRate;
						if (soil != null) 
						{ 
							Vector3 wpos = forest[x,z];
							if (!soil.ContainsWorldValue(wpos.x, wpos.z)) curSurvivalRate = 0;
							else curSurvivalRate *= soil.GetWorldValue(wpos.x, wpos.z); 
						}
						if (tree > lifeAge || random.Random(x,z,y,0) > curSurvivalRate) 
							forest.SetHeight(x,z, 0);

						//breeding the tree
						//TODO: use id random
						if (tree > reproductiveAge && random.Random(x,z,y,1) < fecundity)
						{
							float angleRad = random.Random(x,z,y,2) * 6.283f;
							float dist = random.Random(x,z,y,3) * seedDist/forest.cellSize + 1;

							int nx = (int)(x + Mathf.Sin(angleRad)*dist); 
							int nz = (int)(z + Mathf.Cos(angleRad)*dist);

							if (forest.rect.Contains(nx, nz) && forest.GetHeight(nx,nz)<0.5f && otherForest.GetHeight(nx,nz)<0.01f) 
								forest.SetHeight(nx,nz, 1);
						}
					}
				}
			}

			//return forest.ToTransitionsList(minHeight:0.5f);
			//using method copy with some custom changes

			TransitionsList trsList = new TransitionsList(); //capacity rect.size.x * rect.size.z

			Coord rmin = rect.Min; Coord rmax = rect.Max;
			for (int x=rmin.x; x<rmax.x; x++)
				for (int z=rmin.z; z<rmax.z; z++)
				{
					Vector3 pos = forest[x,z];
					if (pos.y < 1.5f) continue; //removing trees on improper soil that were appeared for 1 year
					Transition trs = new Transition(pos.x, pos.z);
					if (sizeIsLife) { trs.scale.x=pos.y; trs.scale.y=pos.y; trs.scale.z=pos.y; }
					trs.hash = x*2000 + z; //to make hash independent from grid size
					trsList.Add(trs);
				}

			return trsList;
		}

	}

	[System.Serializable]
	[GeneratorMenu (menu="Objects/Modifiers", name ="Slide", iconName="GeneratorIcons/Slide", disengageable = true, helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/object_generators/Slide")]
	public class SlideGenerator200 : Generator, IMultiInlet, IOutlet<TransitionsList>
	{
		[Val("Input", "Inlet")]		public readonly Inlet<TransitionsList> srcIn = new Inlet<TransitionsList>();
		[Val("Stratum", "Inlet")]		public readonly Inlet<MatrixWorld> stratumIn = new Inlet<MatrixWorld>();

		[Val("Blur")]		public int smooth = 2;
		[Val("Iterations")]	public int iterations = 100;
		[Val("Move Factor")]	public float moveFactor = 0.2f;
		[Val("Stop Slope")]	public float stopSlope = 10;


		public IEnumerable<IInlet<object>> Inlets () { yield return srcIn; yield return stratumIn; }


		public override void Generate (TileData data, StopToken stop)
		{
			TransitionsList src = data.products.ReadInlet(srcIn);
			MatrixWorld stratum = data.products.ReadInlet(stratumIn);
			if (src == null) return;
			if (!enabled || stratum == null) { data.products[this]=src; return; }

			TransitionsList dst = new TransitionsList(src);

			//preparing matrix mip for smoothing
			if (smooth != 0)
			{
				Matrix[] mips = MatrixOps.GenerateMips(stratum, smooth);
				Matrix mip = mips[mips.Length-1];

				stratum = new MatrixWorld(mip, stratum.worldPos, stratum.worldSize); 
			}

			//finding stop slope (in 0-1 height difference, same as slope gen)
			float stopDelta = Mathf.Tan(stopSlope*Mathf.Deg2Rad) * data.area.PixelSize.x / data.globals.height;

			int resIndepIterations = (int)(iterations/512f * data.area.active.rect.size.x);

			for (int t=0; t<dst.count; t++)
				Slide(ref dst.arr[t], stratum, resIndepIterations, moveFactor, stopDelta, data.globals.height);
			//TODO: test resolution independance

			data.products[this] = dst;
		}


		public static void Slide (ref Transition trn, MatrixWorld stratum, int iterations, float moveFactor, float stopDelta, float terrainHeight)
		{
			for (int i=0; i<iterations; i++)
			{
				//flooring coordiantes
				//TODO: use built-in matrix operations
				Coord pos = stratum.WorldToPixel(trn.pos.x, trn.pos.z);

				if (!stratum.rect.Contains(pos.x, pos.z, 1.0001f)) break;

				float heightMXMZ = stratum[pos.x, pos.z];
				float heightPXMZ = stratum[pos.x+1, pos.z];
				float heightMXPZ = stratum[pos.x, pos.z+1];
				float heightPXPZ = stratum[pos.x+1, pos.z+1];

				float xNormal1 = heightMXPZ-heightPXPZ; //Mathf.Atan(heightPXPZ-heightMXPZ) / halfPi;
				float xNormal2 = heightMXMZ-heightPXMZ; //Mathf.Atan(heightPXMZ-heightMXMZ) / halfPi;
				float zNormal1 = heightPXMZ-heightPXPZ; //Mathf.Atan(heightPXPZ-heightPXMZ) / halfPi;
				float zNormal2 = heightMXMZ-heightMXPZ; //Mathf.Atan(heightMXPZ-heightMXMZ) / halfPi;

				//finding incline tha same way as the slope generator
				float xDelta1 = xNormal1>0? xNormal1 : -xNormal1; float xDelta2 = xNormal2>0? xNormal2 : -xNormal2; float xDelta = xDelta1>xDelta2? xDelta1 : xDelta2;
				float zDelta1 = zNormal1>0? zNormal1 : -zNormal1; float zDelta2 = zNormal2>0? zNormal2 : -zNormal2; float zDelta = zDelta1>zDelta2? zDelta1 : zDelta2;
				float delta = xDelta>zDelta? xDelta : zDelta; //because slope generator uses additive blend

				if (delta < stopDelta) continue;

				float xNormal = (xNormal1+xNormal2)/2f;
				float zNormal = (zNormal1+zNormal2)/2f; //TODO: use smooth interpolation

				trn.pos.x += xNormal*(terrainHeight*moveFactor); 
				trn.pos.z += zNormal*(terrainHeight*moveFactor); 
			}
		}
	}


}
