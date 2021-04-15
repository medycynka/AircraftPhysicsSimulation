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

namespace MapMagic.Nodes.GUI
{
	public class SplineEditors
	{
		[Draw.Editor(typeof(SplinesGenerators.Stamp200))]
		public static void DrawStamp (SplinesGenerators.Stamp200 gen)
		{
			using (Cell.Padded(1,1,0,0)) 
			{
				using (Cell.LineStd) Draw.Field(ref gen.algorithm, "Algorithm");

				if (gen.algorithm == SplinesGenerators.Stamp200.Algorithm.Flatten || gen.algorithm == SplinesGenerators.Stamp200.Algorithm.Both)
				{
					using (Cell.LineStd) Draw.Field(ref gen.flatRange, "Flat Range");
					using (Cell.LineStd) Draw.Field(ref gen.blendRange, "Blend Range");
				}

				if (gen.algorithm == SplinesGenerators.Stamp200.Algorithm.Detail || gen.algorithm == SplinesGenerators.Stamp200.Algorithm.Both)
				{
					using (Cell.LineStd) Draw.Field(ref gen.detailRange, "Detail Range");
					using (Cell.LineStd) Draw.Field(ref gen.detail, "Detail Size");
				}
			}
		}
	}
}