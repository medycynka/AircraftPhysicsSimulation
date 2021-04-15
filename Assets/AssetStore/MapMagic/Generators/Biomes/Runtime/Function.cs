using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using MapMagic.Products;


namespace MapMagic.Nodes.Biomes
{
	[Serializable]
	[GeneratorMenu (menu="Biomes", name ="Function", iconName="GeneratorIcons/Function", priority = 1, colorType = typeof(IBiome))]
	public class Function200 : Generator, ICustomClear, IMultiInlet, IMultiOutlet, IPrepare, IBiome, ICustomComplexity, ISerializationCallbackReceiver
	{
		#region Inlet/Outlet
			public interface IFnInlet<out T> : IInlet<T> where T:class
			{
				//Guid GenGuid { get; set; } //output generator in internal graph
				string Name { get; set; }
				IFunctionInput<T> GetInternalPortal (Graph graph);
			}

			[Serializable]
			public class FnInlet<T> : IInlet<T>, IFnInlet<T> where T: class
			{
				//public Guid GenGuid { get; set; }
				public string Name { get; set; }

				public Generator Gen { get; private set; }
				public void SetGen (Generator gen) => Gen=gen;

				public IFunctionInput<T> GetInternalPortal (Graph graph)
				{
					foreach (IFunctionInput<T> fnInput in graph.GeneratorsOfType<IFunctionInput<T>>())
						if (fnInput.Name == Name) 
							return fnInput;
					return null;
				}
			}

			public IFnInlet<object>[] inlets = new IFnInlet<object>[0];

			public IEnumerable<IInlet<object>> Inlets() 
			{
				for (int i=0; i<inlets.Length; i++)
					yield return inlets[i];
			}


			public interface IFnOutlet<out T> : IOutlet<T> where T:class
			{
				//Guid GenGuid { get; set; } //output generator in internal graph
				string Name { get; set; }
				IFunctionOutput<T> GetInternalPortal (Graph graph);
			}

			public class FnOutlet<T> : IOutlet<T>, IFnOutlet<T> where T:class
			{
				public Guid GenGuid { get; set; }
				public string Name { get; set; }

				public Generator Gen { get; private set; }
				public void SetGen (Generator gen) => Gen=gen;

				public IFunctionOutput<T> GetInternalPortal (Graph graph)
				{
					foreach (IFunctionOutput<T> fnInput in graph.GeneratorsOfType<IFunctionOutput<T>>())
						if (fnInput.Name == Name) 
							return fnInput;
					return null;
				}
			}

			public IFnOutlet<object>[] outlets = new IFnOutlet<object>[0];

			public IEnumerable<IOutlet<object>> Outlets() 
			{
				for (int i=0; i<outlets.Length; i++)
					yield return outlets[i];
			}

		#endregion

		#region Graph/Data

			public Graph srcGraph;
			private Graph internalGraph;
			public Graph SubGraph => !overrideExposedVals ? srcGraph : internalGraph;
			public Graph AssignedGraph => srcGraph;

			public void EnableInternalGraph (bool enable)
			{
				if (enable && internalGraph==null) internalGraph = Graph.Create();
				else if (!enable && internalGraph!=null) { GameObject.DestroyImmediate(internalGraph); internalGraph = null; }
			}

			private TileData GetSubData (TileData parentData)
			{
				TileData usedData = null;
				if (parentData != null)
				{
					usedData = parentData.subDatas[this];

					if (usedData == null)
					{
						usedData = new TileData(parentData);
						parentData.subDatas[this] = usedData;
					}
				}

				return usedData;
			}

			private Graph GetSubGraph (TileData parentData)
			/// Creates and copies internal graph if needed. Resets used data if graph has changed
			{
				if (srcGraph == null)
					return null;
				
				if (!overrideExposedVals)
					return srcGraph;

				else 
				{
					if (internalGraph == null)
						//throw new Exception($"No internal graph for {this}. Make sure it was assigned with Generator.Create");
						internalGraph = Graph.Create();

					if (internalGraph.changeVersion != srcGraph.changeVersion  ||  internalGraph.generators.Length != srcGraph.generators.Length)
					//checking generators number just in case usedGraph was just created and has changeVersion 0;
					{
						internalGraph.name = srcGraph.name;
						Graph.DeepCopy(srcGraph, internalGraph);
						internalGraph.random = srcGraph.random; //using not clone, but ref
						internalGraph.changeVersion = srcGraph.changeVersion;
						
						parentData.subDatas[this]?.Clear(); //clearing data if graph changed
					}
				}

				return internalGraph;
			}

		#endregion

		#region Complexity

			public float Complexity => srcGraph!=null ? srcGraph.GetGenerateComplexity() : 0;
			public float Progress (TileData data)
			{
				TileData usedData = GetSubData(data);
				Graph usedGraph = GetSubGraph(data);

				if (usedGraph == null  ||  usedData == null) return 0;
				return usedGraph.GetGenerateProgress(usedData);
			}

		#endregion

		public bool overrideExposedVals = false;
		[NonSerialized] public Dictionary<Exposed.Entry, object> exposedOverride = new Dictionary<Exposed.Entry, object>();
		// reads src graph override values in gui (editor), but applies to used graph in here

		#region Generate

			public void Prepare (TileData data, Terrain terrain)
			{
				TileData usedData = GetSubData(data);
				Graph usedGraph = GetSubGraph(data);

				if (usedGraph == null) return;
				
				usedGraph.Prepare(usedData, terrain);
			}


			public override void Generate (TileData data, StopToken stop)
			{
				if (stop!=null && stop.stop) return;
				if (srcGraph == null) return;

				TileData usedData = GetSubData(data);
				Graph usedGraph = GetSubGraph(data);

				//synchronizing exposed values override
				if (overrideExposedVals)
					lock (exposedOverride) 
						srcGraph.exposed.ApplyOverride(usedGraph, exposedOverride); //src exposed, but apply to used graph !!!

				//transferring inlet products to internal data
				if (stop!=null && stop.stop) return;
				foreach (IFnInlet<object> inlet in inlets)
				{
					IFunctionInput<object> fnInput = inlet.GetInternalPortal(usedGraph); //usedGraph.GetGenerator<IFunctionInput<object>>(inlet.GenGuid);
					if (fnInput==null) continue;

//					usedGraph.Clear((Generator)fnInput, usedData);

					object product = data.products.ReadInlet(inlet);
					usedData.products[fnInput] = product;

					usedData.ready[(Generator)fnInput] = true;


				}

				//generating
				if (stop!=null && stop.stop) return;
				usedGraph.Generate(usedData, stop);

				//transferring generated product to this
				if (stop!=null && stop.stop) return;
				foreach (IFnOutlet<object> outlet in outlets)
				{
					IFunctionOutput<object> fnOutput = outlet.GetInternalPortal(usedGraph); //usedGraph.GetGenerator<IFunctionOutput<object>>(outlet.GenGuid);
					if (fnOutput==null) continue;

					object product = usedData.products.ReadInlet(fnOutput); //fnOutput doesn't store products, so read directly from its inlet
					data.products[outlet] = product;
				}
			}


			public void OnBeforeClear (Graph parentGraph, TileData parentData)
			//if any of the internal generators changed - resetting this one
			{
				if (!parentData.ready[this]) return; //skipping since it has already changed
			
				TileData usedData = GetSubData(parentData);
				Graph usedGraph = GetSubGraph(parentData);

				usedGraph.ClearChanged(usedData);
				//changing sub-graph relevant gens. Yep, it is cleared twice for biomes

				//checking outputs in case function is used as a biome (or has sub-functions)
				foreach (Generator relGen in usedGraph.RelevantGenerators(parentData.isDraft))
					if (!usedData.ready[relGen])
					{
						parentData.ready[this] = false;
						return;
					}

				//specially checking function outputs
				foreach (IFunctionOutput<object> fnOut in usedGraph.GeneratorsOfType<IFunctionOutput<object>>())
					if (!usedData.ready[(Generator)fnOut]  &&  parentData.ready[this])
					{
						parentData.ready[this] = false;
						return;
					}
			}


			public void OnAfterClear (Graph parentGraph, TileData parentData)
			//if this changed - resetting all of the internal relevant generators
			{
				if (parentData.ready[this]) return; //not changed
			
				TileData usedData = GetSubData(parentData);
				Graph usedGraph = GetSubGraph(parentData);

				//checking outputs in case function is used as a biome (or has sub-functions)
				foreach (Generator relGen in usedGraph.RelevantGenerators(parentData.isDraft))
					usedData.ready[relGen] = false;

				//specially removing fn inputs
				if (!parentData.ready[this])
					usedData.ready.RemoveOfType<IFunctionInput<object>>();

				//graph.ClearChanged(subData); //will be cleared afterwards anyways
			}

		#endregion


		#region Inlets/Outlets Sync

			public bool SyncInlets ()
			/// Makes inlets arr generators refGuids match ref guids trying to preserve arr references when possible.
			/// Returns true if outlets has been changed (to refresh hashes)
			{
				Graph graph = srcGraph; //no need to use usedGraph here

				//checking if there's a need generate new inlets
				bool inputGenRemoved = false;
				foreach (IFnInlet<object> inlet in inlets)
					if (inlet.GetInternalPortal(graph) == null) //if no internal portal for this inlet
						{ inputGenRemoved=true; break; }
				bool inputGenAdded = graph.GeneratorsCount<IFunctionInput<object>>() != inlets.Length;
				if (!inputGenRemoved && !inputGenAdded) return false;

				//storing all layers in a pool
				Dictionary<string,IFnInlet<object>> pool = new Dictionary<string, IFnInlet<object>>();
				foreach (IFnInlet<object> inlet in inlets)
					if (!pool.ContainsKey(inlet.Name)) //should show inlets/outlets even if there are duplicate names
						pool.Add(inlet.Name, inlet);

				//taking inlet from pool when possible (and remove from pool), create when not possible
				bool created = false;
				List<IFnInlet<object>> newInputsList = new List<IFnInlet<object>>();
				foreach (IFunctionInput<object> internalPortal in graph.GeneratorsOfType<IFunctionInput<object>>())
				{
					string interanlName = internalPortal.Name;

					IFnInlet<object> input;
					if (pool.ContainsKey(interanlName)) //TODO: check type too (slow operation)
					{ 
						input = pool[interanlName]; 
						pool.Remove(interanlName); //only removed gens will be left in pool
					}
					else //creating inlet of same generic type
					{
						Type genericType = internalPortal.GetType().BaseType.GetGenericArguments()[0];
						Type inletType = typeof(FnInlet<>).MakeGenericType(genericType);
						input = (IFnInlet<object>)Activator.CreateInstance(inletType);
						input.Name = internalPortal.Name;

						created = true;
					}

					newInputsList.Add(input);
				}

				//unlinking all outlets left in pool
				bool removed = false;
				foreach (var kvp in pool)
				{
					graph.UnlinkInlet(kvp.Value);
					removed = true;
				}

				//re-creating list on change
				if (created || removed)
				{
					inlets = newInputsList.ToArray();
					return true;
				}
				else return false;
			}


			public bool SyncOutlets ()
			/// Returns true if outlets has been changed (to refresh hashes)
			{
				Graph graph = srcGraph; //no need to use usedGraph here

				//checking if there's a need generate new inlets
				bool outputGenRemoved = false;
				foreach (IFnOutlet<object> outlet in outlets)
					if (outlet.GetInternalPortal(graph) == null) //if no internal portal for this outlet
						{ outputGenRemoved=true; break; }
				bool outputGenAdded = graph.GeneratorsCount<IFunctionOutput<object>>() != outlets.Length;
				if (!outputGenRemoved && !outputGenAdded) return false;   

				//storing all layers in a pool
				Dictionary<string,IFnOutlet<object>> pool = new Dictionary<string, IFnOutlet<object>>();
				foreach(IFnOutlet<object> outlet in outlets)
					if (!pool.ContainsKey(outlet.Name)) //should show inlets/outlets even if there are duplicate names
						pool.Add(outlet.Name, outlet);

				//taking outlet from pool when possible (and remove from pool), create when not possible
				bool created = false;
				List<IFnOutlet<object>> newOutletsList = new List<IFnOutlet<object>>();
				foreach (IFunctionOutput<object> internalPortal in graph.GeneratorsOfType<IFunctionOutput<object>>())
				{
					string interanlName = internalPortal.Name;

					IFnOutlet<object> outlet;
					if (pool.ContainsKey(interanlName)) //TODO: check type too (slow operation)
					{ 
						outlet = pool[interanlName]; 
						pool.Remove(interanlName); //only removed gens will be left in pool
					}
					else //creating inlet of same generic type
					{
						Type genericType = internalPortal.GetType().BaseType.GetGenericArguments()[0];
						Type outletType = typeof(FnOutlet<>).MakeGenericType(genericType);
					
						outlet = (IFnOutlet<object>)Activator.CreateInstance(outletType);
						outlet.SetGen(this);
						outlet.Name = internalPortal.Name;

						created = true;
					}

					newOutletsList.Add(outlet);
				}

				//unlinking all outlets left in pool
				bool removed = false;
				foreach (var kvp in pool)
				{
					graph.UnlinkOutlet(kvp.Value);
					removed = true;
				}

				//re-creating list on change
				if (created || removed)
				{
					outlets = newOutletsList.ToArray();
					return true;
				}
				else return false;
			}


			public string CheckSameOutletsNames ()
			/// It two outlets have the same name returns this name
			/// Returns null if no same name found
			{
				foreach(IFnOutlet<object> outlet1 in outlets)
					foreach(IFnOutlet<object> outlet2 in outlets) //faster than creating hashset and no garbage
						if (outlet1 != outlet2  &&  outlet1.Name == outlet2.Name) return outlet1.Name;
				return null;
			}

			public string CheckSameInletsNames ()
			/// It two outlets have the same name returns this name
			{
				foreach(IFnInlet<object> inlet1 in inlets)
					foreach(IFnInlet<object> inlet2 in inlets)
						if (inlet1 != inlet2  &&  inlet1.Name == inlet2.Name) return inlet1.Name;
				return null;
			}

		#endregion


		#region Serialization

			[SerializeField] private (Exposed.Entry entry, object obj)[] serializedOverride = new (Exposed.Entry, object)[0];

			public void OnBeforeSerialize ()
			{
				if (serializedOverride.Length != exposedOverride.Count)
					serializedOverride = new (Exposed.Entry, object)[exposedOverride.Count];

				int i=0;
				foreach (var kvp in exposedOverride)
				{
					serializedOverride[i].entry = kvp.Key;
					serializedOverride[i].obj = kvp.Value;
					i++;
				}
			}

			public void OnAfterDeserialize ()
			{
				exposedOverride.Clear();
				foreach ((Exposed.Entry entry, object obj) in serializedOverride)
					exposedOverride.Add(entry, obj);
			}


		#endregion
	}
}
