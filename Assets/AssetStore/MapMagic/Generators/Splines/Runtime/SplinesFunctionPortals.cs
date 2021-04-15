using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Den.Tools;
using MapMagic.Products;

namespace MapMagic.Nodes.SplinesGenerators
{
	[GeneratorMenu (menu = "Spline/Function", name = "Enter")] public partial class SplineFunctionInput : FunctionInput<Den.Tools.Splines.SplineSys> { }
	[GeneratorMenu (menu = "Spline/Function", name = "Exit")] public partial class SplineFunctionOutput : FunctionOutput<Den.Tools.Splines.SplineSys> { }
}