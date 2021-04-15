using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEngine.Profiling;

using Den.Tools;
using Den.Tools.Matrices;
using Den.Tools.GUI;
using MapMagic.Core;
using MapMagic.Products;
using MapMagic.Nodes.GUI;
using MapMagic.Nodes.Biomes;

namespace MapMagic.Nodes.GUI
{
	public static class BiomesEditors
	{
		[Draw.Editor(typeof(RefBiome))]
		public static void DrawRefBiome (RefBiome gen)
		{
			using (Cell.Padded(1,1,0,0)) 
			{
				using (Cell.LineStd) 
				{
					Draw.ObjectField(ref gen.graph, "Graph");

					if (Cell.current.valChanged)
						GraphWindow.RefreshMapMagic();
				}

				using (Cell.LineStd)
					if (Draw.Button("Open") && gen.graph!=null)
						UI.current.DrawAfter( ()=> GraphWindow.current.OpenBiome(gen.graph) );
			}
		}

		[Draw.Editor(typeof(Function200), cat="Header")]
		public static void DrawFunctionHeader (Function200 fn)
		{
			if (fn.srcGraph == null) return;

			//checking same inlet/outlet names
			string sameInletName = fn.CheckSameInletsNames();
			string sameOutletName = fn.CheckSameOutletsNames();
			if (sameInletName != null)
				using (Cell.LinePx(54))
					Draw.Label($"Two or more inlets \nhave the same name \n{sameInletName}");
			if (sameOutletName != null)
				using (Cell.LinePx(54))
					Draw.Label($"Two or more outlets \nhave the same name \n{sameOutletName}");

			//syncing inlets/outlets
			bool inletsChanged; bool outletsChanged;

			lock (fn.inlets)   //SyncInlets may change inputs
				inletsChanged = fn.SyncInlets();

			lock (fn.outlets)
				outletsChanged = fn.SyncOutlets();

			if (inletsChanged || outletsChanged)
				GraphWindow.current.graph.ResetCachedLinks();

			using (Cell.LinePx(0))
			{
				using (Cell.Row)
				{
					for (int i=0; i<fn.inlets.Length; i++)
					{
						using (Cell.LineStd)
						{
							using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(fn.inlets[i], fn);
							Cell.EmptyRowPx(8);
							using (Cell.Row) Draw.Label(fn.inlets[i].Name);
						}
					}
				}

				Cell.EmptyRowPx(10);

				using (Cell.Row)
				{
					for (int i=0; i<fn.outlets.Length; i++)
					{
						using (Cell.LineStd)
						{
							using (Cell.Row) Draw.Label(fn.outlets[i].Name);
							Cell.EmptyRowPx(8);
							using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(fn.outlets[i]);
						}
					}
				}
			}
		}

		[Draw.Editor(typeof(Function200))]
		public static void DrawFunction (Function200 fn)
		{
			using (Cell.Padded(1,1,0,0)) 
			{
				Cell.EmptyLinePx(2);

				using (Cell.LineStd) 
				{
					using (Cell.Row)
					{
						Draw.ObjectField(ref fn.srcGraph); 

						if (Cell.current.valChanged)
							GraphWindow.RefreshMapMagic();	
					}

					Texture2D openIcon = UI.current.textures.GetTexture("DPUI/Icons/FolderOpen");
					using (Cell.RowPx(22))
						if (Draw.Button(icon:openIcon, iconScale:0.5f, visible:false) && fn.srcGraph!=null)
							UI.current.DrawAfter( ()=> GraphWindow.current.OpenBiome(fn.srcGraph) );
				}

				Cell.EmptyLinePx(5);

				if (fn.srcGraph != null)
				{
					if (fn.overrideExposedVals)
						lock (fn.exposedOverride)   //SyncOverride may change exposedOverride
							fn.srcGraph.exposed.ReadOverride(fn.srcGraph, fn.exposedOverride);

					if (fn.srcGraph.exposed.entries != null  && fn.srcGraph.exposed.entries.Length != 0)
					{
						for (int e=0; e<fn.srcGraph.exposed.entries.Length; e++)
						//	using (Cell.LineStd) ExposedDraw.DrawEntry(fn.srcGraph.exposed.entries[e]);
						{
							Exposed.Entry entry = fn.srcGraph.exposed.entries[e];

							using (Cell.LineStd)
							{
								if (fn.overrideExposedVals)
								{
									object val = fn.exposedOverride[entry];
									val = Draw.UniversalField(val, entry.type, entry.guiName);

									if (Cell.current.valChanged)
									{
										fn.exposedOverride[entry] = val; 

										IExposedGuid changed = fn.SubGraph.FindGenByGuid(entry.guid);
										if (changed != null)
											GraphWindow.RefreshMapMagic(changed.Gen);

										GraphWindow.RefreshMapMagic(fn);
									}
								}

								else
								{
									IExposedGuid gen = fn.srcGraph.FindGenByGuid(entry.guid);
									System.Reflection.FieldInfo field = gen.GetType().GetField(entry.fieldName);

										if (field==null)
											Draw.DualLabel(entry.guiName, "unknown");

										else
											Draw.ClassField(
												field: field, 
												type: entry.type, 
												obj: gen,
												name: entry.guiName);
								}

								if (Cell.current.valChanged)
								{
									IExposedGuid changed = fn.SubGraph.FindGenByGuid(entry.guid);
									if (changed != null)
										GraphWindow.RefreshMapMagic(changed.Gen);

									GraphWindow.RefreshMapMagic(fn);
								}
							}
						}
					}

					Cell.EmptyLinePx(5);

					using (Cell.LineStd) 
					{
						Draw.ToggleLeft(ref fn.overrideExposedVals, "Override Exposed");
						if (Cell.current.valChanged)
							fn.EnableInternalGraph(fn.overrideExposedVals);
					}

					Cell.EmptyLinePx(2);
				}
			}
		}


		[Draw.Editor(typeof(BiomesSet200))]
		public static void TexturesGeneratorEditor (BiomesSet200 gen)
		{
			using (Cell.LinePx(20)) GeneratorDraw.DrawLayersAddRemove(gen, ref gen.layers, inversed:true);
			using (Cell.LinePx(0)) GeneratorDraw.DrawLayersThemselves(gen, gen.layers, inversed:true, layerEditor:DrawBiomeLayer);
		}
		
		private static void DrawBiomeLayer (Generator tgen, int num)
		{
			BiomesSet200 gen = (BiomesSet200)tgen;
			BiomeLayer layer = gen.layers[num];
			if (layer == null) return;

			Cell.EmptyLinePx(3);
			using (Cell.LinePx(18))
			{
				if (num!=0) 
					using (Cell.RowPx(0)) GeneratorDraw.DrawInlet(layer, gen);
				else 
					//disconnecting last layer inlet
					if (GraphWindow.current.graph.IsLinked(layer))
						GraphWindow.current.graph.UnlinkInlet(layer);
				
				Cell.EmptyRowPx(12);

				Texture2D biomeIcon = UI.current.textures.GetTexture("MapMagic/Icons/Biomes");
				using (Cell.RowPx(14))	Draw.Icon(biomeIcon);

				using (Cell.Row) Draw.ObjectField(ref layer.graph);

				Texture2D openIcon = UI.current.textures.GetTexture("DPUI/Icons/FolderOpen");
				using (Cell.RowPx(20))
					if (Draw.Button(icon:openIcon, iconScale:0.5f, visible:false) && layer.graph!=null)
						UI.current.DrawAfter( ()=> GraphWindow.current.OpenBiome(layer.graph) );

				Cell.EmptyRowPx(10);

				using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
			}
			Cell.EmptyLinePx(3);
		}


		[Draw.Editor(typeof(Whittaker200))]
		public static void DrawWhittaker (Whittaker200 gen)
		{
			//using (Cell.LineStd) 
			//	using (Cell.Padded(1,1,0,0)) 
			//		Draw.Field(ref gen.sharpness, "Sharpness");

			foreach (WhittakerLayer layer in gen.Layers())
			{
				Cell.EmptyLinePx(2);
				using (Cell.LinePx(0)) 
				{
					//outlet
					using (Cell.Full)
					{
						using (Cell.LineStd)
						{
							Cell.EmptyRow();
							using (Cell.RowPx(0)) GeneratorDraw.DrawOutlet(layer);
						}
						Cell.EmptyLine();
					}

					//layer itself
					using (Cell.Full)
						using (Cell.Padded(3,3,0,0)) 
						{
							if (layer.guiExpanded) Draw.Element(UI.current.styles.foldoutBackground);

							using (Cell.LineStd) 
							{
								Cell.EmptyRowPx(2);
								using (Cell.Row) Draw.FoldoutLeft(ref layer.guiExpanded, layer.name);
							}

							if (layer.guiExpanded)
							{
								using (Cell.LinePx(0))
								using (Cell.Padded(2,2,0,0)) 
								{
									using (Cell.LineStd) Draw.ObjectField(ref layer.graph, "Graph");

									using (Cell.LineStd) 
										if (Draw.Button("Open") && layer.graph!=null)
											UI.current.DrawAfter( ()=> GraphWindow.current.OpenBiome(layer.graph) );
								
									using (Cell.LineStd) Draw.Field(ref layer.opacity, "Influence");
									//using (Cell.LineStd) Draw.Field(ref layer.opacity, "Opacity");
								}
								Cell.EmptyLinePx(3);
							}
						}
				}
				Cell.EmptyLinePx(2);
			}
		}

	}
}