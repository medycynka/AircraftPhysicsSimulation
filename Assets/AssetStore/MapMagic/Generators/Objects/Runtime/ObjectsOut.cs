using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Den.Tools;
using Den.Tools.GUI;
using Den.Tools.Matrices;
using MapMagic.Products;
using MapMagic.Terrains;

namespace MapMagic.Nodes.ObjectsGenerators
{
	[System.Serializable]
	public abstract class BaseObjectsOutput : Generator
	// doesn't have to be Generator, but we can't inherit from both BaseObjectsOutput and Generator
	{
		public string name = "(Empty)";
		public GameObject[] prefabs = new GameObject[1];

		public bool objHeight = true;
		public bool relativeHeight = true;

		public bool useRotation = true; //in base since tree could also be rotated. Not the imposter ones, but anyways
		public bool takeTerrainNormal = false;
		public bool rotateYonly = false;
		public bool regardPrefabRotation = false;

		public bool useScale = true;
		public bool scaleYonly = false;
		public bool regardPrefabScale = false;
		

		public enum BiomeBlend { Sharp, Random, Scale, Pure }
		public BiomeBlend biomeBlend = BiomeBlend.Random;

		public bool guiMultiprefab;
		public bool guiProperties;
		public bool guiHeight;
		public bool guiRotation;
		public bool guiScale;

		#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		#endif
		[RuntimeInitializeOnLoadMethod] 
		static void Subscribe ()
		{
				Graph.OnBeforeOutputFinalize += (type, tileData, applyData, stopToken) =>
				{
					tileData.finalize.Mark(true, ObjectsOutput.finalizeAction, tileData.subDatas);
					tileData.finalize.Mark(true, TreesOutput.finalizeAction, tileData.subDatas);
				};
		}

		public void MoveRotateScale (ref Transition trs, TileData data)
		/// Floors object, and erases (yep) trs roation/scale values according to layer settings
		{
			if (!objHeight) trs.pos.y = 0;

			//flooring
			float terrainHeight = 0;
			if (relativeHeight && data.heights != null) //if checbox enabled and heights exist (at least one height generator is in the graph)
				terrainHeight = data.heights.GetWorldInterpolatedValue(trs.pos.x, trs.pos.z, roundToShort:true);
			if (terrainHeight > 1) terrainHeight = 1;
			terrainHeight *= data.globals.height;  //all coords should be in world units
			trs.pos.y += terrainHeight; 

			if (!useScale) trs.scale = new Vector3(1,1,1);
			else if (scaleYonly) trs.scale = new Vector3(1, trs.scale.y, 1);

			if (!useRotation) trs.rotation = Quaternion.identity;
			else if (takeTerrainNormal) 
			{
				Vector3 terrainNormal = GetTerrainNormal(trs.pos.x, trs.pos.z, data.heights, data.globals.height, data.area.PixelSize.x);
				Vector3 terrainTangent = Vector3.Cross(trs.rotation*new Vector3(0,0,1), terrainNormal);
				trs.rotation = Quaternion.LookRotation(terrainTangent, terrainNormal);
			}
			else if (rotateYonly) trs.rotation = Quaternion.Euler(0,trs.Yaw,0);
		}


		public static bool SkipOnBiome (ref Transition trs, BiomeBlend biomeBlend, MatrixWorld biomeMask, Noise random)
		/// True if object should not be spawned because of biome mask
		/// ref since it can change scale
		{
			float biomeFactor = biomeMask!=null ?  biomeMask.GetWorldInterpolatedValue(trs.pos.x, trs.pos.z) : 1;
			if (biomeFactor < 0.00001f) return true;

			bool skip;
			switch (biomeBlend)
			{
				case BiomeBlend.Sharp: 
					skip = biomeFactor < 0.5f;
					break;
				case BiomeBlend.Random:
					float rnd = random.Random((int)trs.pos.x, (int)trs.pos.y); //TODO: use id?
					if (biomeFactor > 0.5f) rnd = 1-rnd;
					skip = biomeFactor < rnd;
					break;
				case BiomeBlend.Scale:
					trs.scale *= biomeFactor;
					skip = biomeFactor < 0.0001f;
					break;
				case BiomeBlend.Pure:
					skip = biomeFactor < 0.9999f;
					break;
				default: skip = false; break;
			}

			return skip;
		}


		public static Vector3 GetTerrainNormal (float fx, float fz, MatrixWorld heightmap, float heightFactor, float pixelSize)
		{
			Coord coord = heightmap.WorldToPixel(fx, fz);
			int pos = heightmap.rect.GetPos(coord);

			float curHeight = heightmap.arr[pos];
						
			float prevXHeight = curHeight;
			if (coord.x>=heightmap.rect.offset.x+1) prevXHeight = heightmap.arr[pos-1];

			float nextXHeight = curHeight;
			if (coord.x<heightmap.rect.offset.x+heightmap.rect.size.x-1) nextXHeight = heightmap.arr[pos+1];
									
			float prevZHeight = curHeight;
			if (coord.z>=heightmap.rect.offset.z+1) prevZHeight = heightmap.arr[pos-heightmap.rect.size.x];

			float nextZHeight = curHeight;
			if (coord.z<heightmap.rect.offset.z+heightmap.rect.size.z-1) nextZHeight = heightmap.arr[pos+heightmap.rect.size.z];

			return new Vector3((prevXHeight-nextXHeight)*heightFactor, pixelSize*2, (prevZHeight-nextZHeight)*heightFactor).normalized;
		}
	}


	[System.Serializable]
	[GeneratorMenu(menu = "Objects/Outputs", name = "Objects", section=2, colorType = typeof(TransitionsList), iconName="GeneratorIcons/ObjectsOut", helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Objects")]
	public class ObjectsOutput : BaseObjectsOutput, IInlet<TransitionsList>, IOutputGenerator, IOutput
	{
		public bool allowReposition = true;
		public bool instantiateClones = false;

		public OutputLevel outputLevel = OutputLevel.Main;
		public OutputLevel OutputLevel { get{ return outputLevel; } }

		/*public void Prepare (TileData tileData, Terrain terrain)
		{
			for (int i=0; i<prefabs.Length; i++)
				if (prefabs[i] == null) prefabs[i] = null; //yep. If null then = null. It could be a removed object 
		}*/

		public List<ObjectsPool.Prototype> GetPrototypes ()
		{
			List<ObjectsPool.Prototype> prototypes = new List<ObjectsPool.Prototype>();
			for (int p=0; p<prefabs.Length; p++)
				if (!prefabs[p].IsNull())  //if (prefabs[p] != null) 
					prototypes.Add (new ObjectsPool.Prototype() {
						prefab = prefabs[p],
						allowReposition = allowReposition,
						instantiateClones = instantiateClones,
						regardPrefabRotation = regardPrefabRotation,
						regardPrefabScale = regardPrefabScale } );
			return prototypes;
		}

		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			if (!enabled) { data.finalize.Remove(finalizeAction, this); return; }
			TransitionsList trns = data.products.ReadInlet(this);
				
			//adding to finalize
			data.finalize.Add(finalizeAction, this, trns, data.currentBiomeMask);
		}


		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			Noise random = new Noise(data.random, 12345);

			//List<ObjectsPool.Prototype> objPrototypesList = new List<ObjectsPool.Prototype>();
			//List<List<Transition>> objTransitionsList = new List<List<Transition>>();
			//List<(ObjectsPool.Prototype prot, List<Transition> trns)> allObjsList = new List<(ObjectsPool.Prototype, List<Transition>)>();
			Dictionary<ObjectsPool.Prototype, List<Transition>> objs = new Dictionary<ObjectsPool.Prototype, List<Transition>>();

			foreach ((ObjectsOutput output, TransitionsList trns, MatrixWorld biomeMask) 
				in data.finalize.ProductSets<ObjectsOutput,TransitionsList,MatrixWorld>(finalizeAction, data.subDatas))
			{
				if (stop!=null && stop.stop) return;

				if (trns == null) continue;
				if (biomeMask!=null  &&  biomeMask.IsEmpty()) continue; 

				List<ObjectsPool.Prototype> prototypes = output.GetPrototypes();
				if (prototypes.Count == 0) continue;

				foreach (ObjectsPool.Prototype prot in prototypes)
					if (!objs.ContainsKey(prot)) objs.Add(prot, new List<Transition>());

				//objects
				for (int t=0; t<trns.count; t++)
				{
					Transition trn = trns.arr[t]; //using copy since it's changing in MoveRotateScale

					if (!data.area.active.Contains(trn.pos)) continue; //skipping out-of-active area
					if (SkipOnBiome(ref trn, output.biomeBlend, biomeMask, data.random)) continue; //after area check since uses biome matrix

					output.MoveRotateScale(ref trn, data);

					trn.pos -= (Vector3)data.area.active.worldPos; //objects pool use local positions

					//float rnd = random.Random(trs.hash);
					//int listNum = transitionsCount + (int)(rnd*output.prefabs.Length);
					//objTransitionsList[listNum].Add(trsCpy);
					//objsList.[listNum].Add(trsCpy);

					float rnd = random.Random(trn.hash);
					ObjectsPool.Prototype prototype = prototypes[ (int)(rnd*prototypes.Count) ];
					objs[prototype].Add(trn);
				}
			}

			//pushing to apply
			if (stop!=null && stop.stop) return;
			ApplyObjectsData applyData = new ApplyObjectsData() { 
				prototypes=objs.Keys.ToArray(), 
				transitions=objs.Values.ToArray(), 
				terrainHeight = data.globals.height};
			Graph.OnBeforeOutputFinalize?.Invoke(typeof(ObjectsOutput), data, applyData, stop);
			data.apply.Add(applyData);
		}


		public class ApplyObjectsData : IApplyDataRoutine 
		{
			public ObjectsPool.Prototype[] prototypes;
			public List<Transition>[] transitions;
			public float terrainHeight; //to get relative object height (since all of the terrain data is 0-1). //TODO: maybe move it to HeightData in "Height in meters" task


			public void Apply(Terrain terrain)
			{
				ObjectsPool pool = terrain.transform.parent.GetComponent<TerrainTile>().objectsPool;
				pool.Reposition(prototypes, transitions);
			}

			public IEnumerator ApplyRoutine (Terrain terrain)
			{
				ObjectsPool pool = terrain.transform.parent.GetComponent<TerrainTile>().objectsPool;
		
				IEnumerator e = pool.RepositionRoutine(prototypes, transitions);
				while (e.MoveNext()) { yield return null; }
			}

			public int Resolution {get{ return 0; }}
		}


		public void Purge (TileData data, Terrain terrain)
		{
			TerrainData terrainData = terrain.terrainData;
			Vector3 terrainSize = terrainData.size;

			ObjectsPool pool = terrain.transform.parent.GetComponent<TerrainTile>().objectsPool;
			List<ObjectsPool.Prototype> prototypes = GetPrototypes();
			pool.ClearPrototypes(prototypes.ToArray());
		}
	}


	[System.Serializable]
	[GeneratorMenu(menu = "Objects/Outputs", name = "Trees", section=2, colorType = typeof(TransitionsList), iconName="GeneratorIcons/TreesOut", helpLink = "https://gitlab.com/denispahunov/mapmagic/wikis/output_generators/Objects")]
	public class TreesOutput : BaseObjectsOutput, IInlet<TransitionsList>, IOutputGenerator, IOutput
	{
		public Color color = Color.white;
		public float bendFactor;

		public OutputLevel outputLevel = OutputLevel.Main;
		public OutputLevel OutputLevel { get{ return outputLevel; } }


		public List<TreePrototype> GetPrototypes ()
		{
			List<TreePrototype> prototypes = new List<TreePrototype>();
			for (int p=0; p<prefabs.Length; p++)
				if (!prefabs[p].IsNull())  //if (prefabs[p] != null)
					prototypes.Add (new TreePrototype() { prefab = prefabs[p], bendFactor = bendFactor } );
			return prototypes;
		}


		public override void Generate (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			if (!enabled) 
				{ data.finalize.Remove(finalizeAction, this); return; }

			TransitionsList trns = data.products.ReadInlet(this);
				
			//adding to finalize
			data.finalize.Add(finalizeAction, this, trns, data.currentBiomeMask); 
		}


		public static FinalizeAction finalizeAction = Finalize; //class identified for FinalizeData
		public static void Finalize (TileData data, StopToken stop)
		{
			if (stop!=null && stop.stop) return;
			Noise random = new Noise(data.random, 12345);

			List<TreeInstance> instancesList = new List<TreeInstance>();
			List<TreePrototype> prototypesList = new List<TreePrototype>();

			int prototypesCount = 0; //the total number of prototypes added to give unique index for trees

			foreach ((TreesOutput output, TransitionsList trns, MatrixWorld biomeMask) 
				in data.finalize.ProductSets<TreesOutput,TransitionsList,MatrixWorld>(Finalize, data.subDatas))
			{
				if (stop!=null && stop.stop) return;

				if (trns == null) continue;
				if (biomeMask!=null  &&  biomeMask.IsEmpty()) continue; 

				//prototypes
				//TODO: use GetPrototypes (and skip RemoveNullPrototypes)
				TreePrototype[] prototypesArr = new TreePrototype[output.prefabs.Length];
				for (int p=0; p<output.prefabs.Length; p++)
					prototypesArr[p] = new TreePrototype() { prefab =  output.prefabs[p], bendFactor = output.bendFactor };
				prototypesList.AddRange(prototypesArr);
				
				//instances
				for (int t=0; t<trns.count; t++)
				{
					Transition trn = trns.arr[t]; //using copy since it's changing in MoveRotateScale

					if (!data.area.active.Contains(trn.pos)) continue; //skipping out-of-active area
					if (SkipOnBiome(ref trn, output.biomeBlend, biomeMask, data.random)) continue;
					
					output.MoveRotateScale(ref trn, data);

					float rnd = random.Random(trn.hash);
					int index = (int)(rnd*output.prefabs.Length);
						
					TreeInstance tree = new TreeInstance();

					tree.position = (trn.pos - (Vector3)data.area.active.worldPos) / data.area.active.worldSize.x;
					if (tree.position.x < 0 || tree.position.z < 0 ||
						tree.position.x > 1 || tree.position.z > 1)
							continue;

					tree.position.y = trn.pos.y / data.globals.height; //trees should be in 0-1 range

					tree.rotation = trn.Yaw;
					tree.widthScale = trn.scale.x; // + trs.scale.z)/2;
					tree.heightScale = trn.scale.y;
					tree.prototypeIndex = prototypesCount + index;
					tree.color = output.color;
					tree.lightmapColor = output.color;

					instancesList.Add(tree);
				}

				prototypesCount += output.prefabs.Length;
			}

			//RemoveNullPrototypes(prototypesList, instancesList); //could not be executed in thread

			//pushing to apply
			if (stop!=null && stop.stop) return;
			ApplyTreesData applyData = new ApplyTreesData() { treePrototypes=prototypesList.ToArray(), treeInstances=instancesList.ToArray() };
			Graph.OnBeforeOutputFinalize?.Invoke(typeof(TreesOutput), data, applyData, stop);
			data.apply.Add(applyData);
		}


		public class ApplyTreesData : IApplyData
		{
			public TreeInstance[] treeInstances;  //tree positions use 0-1 range (percent relatively to terrain)
			public TreePrototype[] treePrototypes;

			public void Read (Terrain terrain) 
			{ 
				TerrainData data = terrain.terrainData;
				treeInstances = data.treeInstances;
				treePrototypes = data.treePrototypes;
			}

			public void Apply (Terrain terrain)
			{
				if (treePrototypes.Contains( p=>p.prefab==null ))
					RemoveNullPrototypes(ref treePrototypes, ref treeInstances);

				if (treePrototypes.Length == 0  &&  terrain.terrainData.treeInstanceCount != 0)
				{
					terrain.terrainData.treeInstances = new TreeInstance[0]; //setting instances first
					terrain.terrainData.treePrototypes = new TreePrototype[0];
				}

				terrain.terrainData.treePrototypes = treePrototypes;
				terrain.terrainData.treeInstances = treeInstances;
			}

			public int Resolution {get{ return 0; }}
		}


		public static void RemoveNullPrototypes (List<TreePrototype> prototypes, List<TreeInstance> instances)
		{
			Dictionary<int,int> indexToOptimized = new Dictionary<int, int>();
			
			int originalPrototypesCount = prototypes.Count;
			int counter = 0;
			for (int p=0; p<originalPrototypesCount; p++)
				if (prototypes[p].prefab != null)
				{
					indexToOptimized.Add(p,counter);
					counter++;
				}

			for (int p=originalPrototypesCount-1; p>=0; p--)
				if (prototypes[p].prefab == null)
					prototypes.RemoveAt(p);

			for (int i=instances.Count-1; i>=0; i--)
			{
				if (!indexToOptimized.TryGetValue(instances[i].prototypeIndex, out int optimizedIndex))
					instances.RemoveAt(i);

				else if (instances[i].prototypeIndex != optimizedIndex)
				{
					TreeInstance instance = instances[i];
					instance.prototypeIndex = optimizedIndex;
					instances[i] = instance;
				}
			}
		}

		public static void RemoveNullPrototypes (ref TreePrototype[] prototypes, ref TreeInstance[] instances)
		{
			List<TreePrototype> prototypesList = new List<TreePrototype>(prototypes);
			List<TreeInstance> instancesList = new List<TreeInstance>(instances);
			RemoveNullPrototypes(prototypesList, instancesList);
			prototypes = prototypesList.ToArray();
			instances = instancesList.ToArray();
		}

		public void Purge (TileData data, Terrain terrain)
		{
			TerrainData terrainData = terrain.terrainData;
			TreePrototype[] prototypes = terrainData.treePrototypes;
			TreeInstance[] instances = terrainData.treeInstances;

			List<TreePrototype> newPrototypes = new List<TreePrototype>();
			Dictionary<int,int> prototypeNumsLut = new Dictionary<int, int>();  //old -> new prototype number. If not contains then should be removed
			for (int num=0; num<prototypes.Length; num++)
			{
				bool contains = false;  //if terrain tree prototype contains in this generator
				for (int p=0; p<prefabs.Length; p++)
				{
					if (prototypes[num].prefab == prefabs[p] && prototypes[num].bendFactor < bendFactor+0.0001f && prototypes[num].bendFactor > bendFactor-0.0001f) 
					{
						contains = true;
						break;
					}
				}
				
				if (!contains)
				{
					prototypeNumsLut.Add(num, newPrototypes.Count);
					newPrototypes.Add(prototypes[num]);
				}
			}

			List<TreeInstance> newInstances = new List<TreeInstance>();
			for (int i=0; i<instances.Length; i++)
			{
				if (prototypeNumsLut.TryGetValue(instances[i].prototypeIndex, out int newIndex))
				{
					TreeInstance instance = instances[i];
					instance.prototypeIndex = newIndex;
					newInstances.Add(instance);
				}
			}

			terrainData.treeInstances = newInstances.ToArray();
			terrainData.treePrototypes = newPrototypes.ToArray();
		}
	}
}