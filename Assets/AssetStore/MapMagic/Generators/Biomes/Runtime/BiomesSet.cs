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
	public class BiomeLayer : IBiome, INormalizableLayer, IInlet<MatrixWorld>, IOutlet<MatrixWorld>, IOutput
	{
		public float Opacity { get; set; }

		public Generator Gen { get { return gen; } private set { gen = value;} }
		public Generator gen; //property is not serialized
		public void SetGen (Generator gen) => this.gen=gen;

		public Graph graph;
		public Graph SubGraph => graph;
		public Graph AssignedGraph => graph;

		public BiomeLayer () => Opacity=1;

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
	}


	[Serializable]
	[GeneratorMenu (menu="Biomes", name ="Biomes Set", iconName="GeneratorIcons/Biome", priority = 1, colorType = typeof(IBiome))]
	public class BiomesSet200 : Generator, IMultiInlet, IMultiBiome, ICustomClear, ICustomComplexity, ILayered<BiomeLayer>
	{
		public BiomeLayer[] layers = new BiomeLayer[] { new BiomeLayer() };
		public BiomeLayer[] Layers => layers; 
		public void SetLayers(object[] ls) => layers = Array.ConvertAll(ls, i=>(BiomeLayer)i);

		public IEnumerable<IInlet<object>> Inlets() 
		{ 
			foreach (BiomeLayer layer in layers)
				yield return layer;
			//TODO: return layers
		}

		public IEnumerable<IBiome> Biomes() 
		{ 
			foreach (BiomeLayer layer in layers)
				yield return layer;
		}

		public float Complexity
		{get{
			float sum = 0;
			foreach (BiomeLayer layer in layers)
				if (layer.graph != null)
					sum += layer.graph.GetGenerateComplexity();
			return sum;
		}}

		public float Progress (TileData data)
		{
			float sum = 0;
			foreach (BiomeLayer layer in layers)
			{
				if (layer.graph == null) continue;

				TileData subData = data.subDatas[layer];
				if (subData == null) continue;
					
				sum += layer.graph.GetGenerateProgress(subData);
			}
			return sum;
		}


		public override void Generate (TileData data, StopToken stop) 
		{
			if (layers.Length == 0) return;

			//reading + normalizing + writing
			if (stop!=null && stop.stop) return;
			NormalizeLayers(layers, data, stop);

			//setting as biomes masks
			foreach (BiomeLayer layer in layers)
			{
				if (stop!=null && stop.stop) return;

				MatrixWorld mask = (MatrixWorld)data.products[layer]; //data.products.ReadInlet(layer);

				TileData layerData = data.subDatas[layer];
				if (layerData == null)
				{
					layerData = new TileData(data);
					data.subDatas[layer] = layerData;
				}

				layerData.SetBiomeMask(mask, data.currentBiomeMask); 
			}
		}


		public void OnBeforeClear (Graph parentGraph, TileData parentData)
		//if any of the internal generators changed - resetting this one
		{
			if (!parentData.ready[this]) return; //skipping since it has already changed
			
			foreach (BiomeLayer layer in layers)
			{
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
			
			foreach (BiomeLayer layer in layers)
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
