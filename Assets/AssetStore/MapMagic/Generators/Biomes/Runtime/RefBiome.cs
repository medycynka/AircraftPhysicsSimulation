using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using Den.Tools.Matrices;
using MapMagic.Products;
using Den.Tools.GUI;

namespace MapMagic.Nodes
{
	[Serializable]
	//[GeneratorMenu (menu="Biomes", name ="Ref Biome", priority = 1)]
	public class RefBiome : Generator, IMultiInlet, IBiome, ICustomComplexity, ICustomClear
	{
		//could be Inlet<mask> but do so since mask is not mandatory
		[Val("Mask", "Inlet")] public readonly Inlet<MatrixWorld> maskIn = new Inlet<MatrixWorld>();
		public IEnumerable<IInlet<object>> Inlets() { yield return maskIn; }

		public Graph graph;
		public Graph SubGraph => graph;
		public Graph AssignedGraph => graph;

		public float Complexity => graph!=null ? graph.GetGenerateComplexity() : 0;
		public float Progress (TileData data)
		{
			TileData subData = data.subDatas[this];
			if (graph == null  ||  subData == null) return 0;
			return graph.GetGenerateProgress(subData);
		}

		private TileData GetSubData (TileData parentData)
		{
			TileData usedData = parentData.subDatas[this];
			if (usedData == null)
			{
				usedData = new TileData(parentData);
				parentData.subDatas[this] = usedData;
			}
			return usedData;
		}

		public override void Generate (TileData data, StopToken stop) 
		{
			if (stop!=null && stop.stop) return;
			if (graph == null) return;

			MatrixWorld mask = data.products.ReadInlet(maskIn);
			GetSubData(data).SetBiomeMask(mask, data.currentBiomeMask);  
		}


		public void OnBeforeClear (Graph parentGraph, TileData parentData)
		//if any of the internal generators changed - resetting this one
		{
			if (!parentData.ready[this]) return; //skipping since it has already changed
			
			TileData subData = GetSubData(parentData);

			graph.ClearChanged(subData);
			//changing sub-graph relevant gens. Yep, it is cleared twice for biomes

			foreach (Generator relGen in graph.RelevantGenerators(parentData.isDraft))
				if (!subData.ready[relGen])
				{
					parentData.ready[this] = false;
					break;
				}
		}


		public void OnAfterClear (Graph parentGraph, TileData parentData)
		//if this changed - resetting all of the internal relevant generators
		{
			if (parentData.ready[this]) return; //not changed
			
			TileData subData = GetSubData(parentData);

			foreach (Generator relGen in graph.RelevantGenerators(parentData.isDraft))
				subData.ready[relGen] = false;

			//graph.ClearChanged(subData); //will be cleared afterwards anyways
		}
	}
}
