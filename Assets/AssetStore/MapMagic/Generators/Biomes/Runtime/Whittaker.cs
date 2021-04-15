using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Products;
using Den.Tools.GUI;

namespace MapMagic.Nodes.Biomes
{
	public class WhittakerLayer : IBiome, IInlet<MatrixWorld>, IOutlet<MatrixWorld>, IOutput
	{
		public string name;
		public float opacity;
		public float influence;
		public string diagramName;

		public Generator Gen { get { return gen; } private set { gen = value;} }
		public Generator gen; //property is not serialized
		public void SetGen (Generator gen) => this.gen=gen;

		public Graph graph;
		public Graph SubGraph => graph;
		public Graph AssignedGraph => graph;

		public WhittakerLayer () { opacity=1; }
		public WhittakerLayer (string name) { opacity=1; this.name=name; }
		public WhittakerLayer (string name, string diagramName) { opacity=1; this.name=name; this.diagramName=diagramName; }

		public TileData GetSubData (TileData parentData)
		{
			TileData usedData = parentData.subDatas[this];
			if (usedData == null)
			{
				usedData = new TileData(parentData);
				parentData.subDatas[this] = usedData;
			}
			return usedData;
		}

		public bool guiExpanded;
	}

	[Serializable]
	[GeneratorMenu (menu="Biomes", name ="Whittaker", iconName="GeneratorIcons/Whittaker", priority = 1, colorType = typeof(IBiome))]
	public class Whittaker200 : Generator, IPrepare, IMultiInlet, IMultiOutlet, IMultiBiome, ICustomClear, ICustomComplexity
	{
		public static MatrixAsset[] diagramAssets;

		[Val("Temperature", "Inlet")] public readonly Inlet<MatrixWorld> temperatureIn = new Inlet<MatrixWorld>();
		[Val("Moisture ", "Inlet")]	public readonly Inlet<MatrixWorld> moistureIn = new Inlet<MatrixWorld>(); 
		public IEnumerable<IInlet<object>> Inlets () { yield return temperatureIn; yield return moistureIn; }

		[Val("Sharpness")] public float sharpness = 0.6f;

		//[Val(name="Temperature Limit")] public float brightness = 0f;
		//[Val(name="Contrast")] public float contrast = 1f;

		public WhittakerLayer tropicalRainforest = new WhittakerLayer("Tropic Rainforest", "TropicalRainforest");
		public WhittakerLayer temperateRainforest = new WhittakerLayer("Mild Rainforest", "TemperateRainforest");

		public WhittakerLayer tropicalForest = new WhittakerLayer("Tropic Forest", "TropicalForest");
		public WhittakerLayer temperateForest = new WhittakerLayer("Mild Forest", "TemperateForest");
		public WhittakerLayer taiga = new WhittakerLayer("Taiga", "Taiga");

		public WhittakerLayer tropicalGrassland = new WhittakerLayer("Savanna", "Savanna");
		public WhittakerLayer temperateGrassland = new WhittakerLayer("Grassland", "Grassland");
		public WhittakerLayer tundra = new WhittakerLayer("Tundra", "Tundra");

		public WhittakerLayer hotDesert = new WhittakerLayer("Hot Desert", "Desert");
		public WhittakerLayer coldDesert = new WhittakerLayer("Cold Desert", "ColdDesert");

		public IEnumerable<WhittakerLayer> Layers () 
		{ 
			yield return tropicalRainforest; yield return temperateRainforest;
			yield return tropicalForest; yield return temperateForest; yield return taiga;
			yield return tropicalGrassland; yield return temperateGrassland; yield return tundra;
			yield return hotDesert; yield return coldDesert;
		}

		public IEnumerable<IOutlet<object>> Outlets () 
		{ 
			foreach (WhittakerLayer layer in Layers())
				yield return layer;
		}

		public IEnumerable<IBiome> Biomes() 
		{ 
			foreach (WhittakerLayer layer in Layers())
				yield return layer;
		}

		public float Complexity
		{get{
			float sum = 0;
			foreach (WhittakerLayer layer in Layers())
				if (layer.graph != null)
					sum += layer.graph.GetGenerateComplexity();
			return sum;
		}}

		public float Progress (TileData data)
		{
			float sum = 0;
			foreach (WhittakerLayer layer in Layers())
			{
				if (layer.graph == null) continue;

				TileData subData = data.subDatas[layer];
				if (subData == null) continue;
					
				sum += layer.graph.GetGenerateProgress(subData);
			}
			return sum;
		}


		public void Prepare (TileData data, Terrain terrain)
		{
			if (diagramAssets != null) return;

			diagramAssets = new MatrixAsset[10];
			int i=0;
			foreach (WhittakerLayer layer in Layers())
			{
				diagramAssets[i] =  Resources.Load<MatrixAsset>("MapMagic/Whittaker/" + layer.diagramName);
				i++;
			}
		}


		public override void Generate (TileData data, StopToken stop) 
		{
			//reading inputs
			if (stop!=null && stop.stop) return;
			MatrixWorld temperatureMatrix = data.products.ReadInlet(temperatureIn);
			MatrixWorld moistureMatrix = data.products.ReadInlet(moistureIn);
			if (temperatureMatrix == null || moistureMatrix == null || !enabled) return; 

			if (diagramAssets == null)
				throw new Exception("Could not find Whittaker diagrams. Possibly generator is not initialized");
			Coord diagramSize = diagramAssets[0].matrix.rect.size;

			//creating biome masks
			if (stop!=null && stop.stop) return;
			MatrixWorld[] masks = new MatrixWorld[10];
			for (int i=0; i<masks.Length; i++)
				masks[i] = new MatrixWorld(temperatureMatrix.rect, temperatureMatrix.worldPos, temperatureMatrix.worldSize);

			if (stop!=null && stop.stop) return;
			Coord min = temperatureMatrix.rect.Min; Coord max = temperatureMatrix.rect.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
				{
					int pos = temperatureMatrix.rect.GetPos(x,z);
					float temperature = temperatureMatrix.arr[pos] * diagramSize.x;
					float moisture = moistureMatrix.arr[pos] * diagramSize.z;

					for (int m=0; m<masks.Length; m++)
					{
						float val = diagramAssets[m].matrix.GetInterpolated(temperature, moisture);
						val -= sharpness/2;
						if (val < 0) val = 0;
						masks[m].arr[pos] = val;
					}
				}

			//and opacities
			if (stop!=null && stop.stop) return;
			float[] opacityies = new float[masks.Length];
			int t=0;
			foreach (WhittakerLayer layer in Layers())
				{ opacityies[t] = layer.opacity; t++; }

			if (stop!=null && stop.stop) return;
			Matrix.NormalizeLayers(masks, allowBelowOne:false);

			//saving products
			if (stop!=null && stop.stop) return;
			t=0;
			foreach (WhittakerLayer layer in Layers())
			{ 
				TileData layerData = data.subDatas[layer];
				if (layerData == null)
				{
					layerData = new TileData(data);
					data.subDatas[layer] = layerData;
				}
				layerData.SetBiomeMask(masks[t], data.currentBiomeMask); 

				data.products[layer] = masks[t]; 

				t++; 
			}
		}


		public void OnBeforeClear (Graph parentGraph, TileData parentData)
		//if any of the internal generators changed - resetting this one
		{
			if (!parentData.ready[this]) return; //skipping since it has already changed
			
			foreach (WhittakerLayer layer in Layers())
			{
				if (layer.graph == null) continue;
				TileData subData = layer.GetSubData(parentData);

				layer.graph.ClearChanged(subData);
				//changing sub-graph relevant gens. Yep, it is cleared twice for biomes

				foreach (Generator relGen in layer.graph.RelevantGenerators(parentData.isDraft))
					if (!subData.ready[relGen])
					{
						parentData.ready[this] = false;
						return;
					}
			}
		}


		public void OnAfterClear (Graph parentGraph, TileData parentData)
		//if this changed - resetting all of the internal relevant generators
		{
			if (parentData.ready[this]) return; //not changed
			
			foreach (WhittakerLayer layer in Layers())
			{
				if (layer.graph == null) continue;
				TileData subData = layer.GetSubData(parentData);

				foreach (Generator relGen in layer.graph.RelevantGenerators(parentData.isDraft))
					subData.ready[relGen] = false;

				//layer.graph.ClearChanged(subData); //will be cleared afterwards anyways
			}
		}
	}
}
